using System.Globalization;
using System.Security.Cryptography;
using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Civ09.Api.DichVu;

public record GiayNhap(DateOnly? TuNgay, DateOnly? DenNgay);

// Giấy xác nhận giờ tình nguyện (UC26, UC27, BR29). Số liệu được chốt và lưu trong CSDL lúc cấp;
// tệp PDF được dựng lại từ dữ liệu đó mỗi lần tải, không lưu ra thư mục.
public class GiayXacNhanService
{
    private const string BoKyTu = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public GiayXacNhanService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    // Cấp giấy cho các bản khai đã xác nhận có ngày làm (giờ Việt Nam) nằm trong khoảng chọn.
    public async Task<string> CapGiay(NguoiDangNhap nd, GiayNhap dl)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        if (dl.TuNgay != null && dl.DenNgay != null && dl.TuNgay > dl.DenNgay)
            throw LoiNghiepVu.KhongHopLe("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        var tu = dl.TuNgay?.ToDateTime(TimeOnly.MinValue).AddHours(-7);
        var den = dl.DenNgay?.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7);
        var q = _kho.DongGop.AsNoTracking()
            .Where(g => g.TrangThai == DongGop.DA_XAC_NHAN && g.DangKy!.MaHoSo == nd.MaHoSo);
        if (tu != null) q = q.Where(g => g.BatDauThucTe >= DateTime.SpecifyKind(tu.Value, DateTimeKind.Utc));
        if (den != null) q = q.Where(g => g.BatDauThucTe < DateTime.SpecifyKind(den.Value, DateTimeKind.Utc));
        var ban = (await q.Select(g => new { g.MaDongGop, g.BatDauThucTe, g.KetThucThucTe }).ToListAsync())
            .Select(g => (g.MaDongGop, (long)(g.KetThucThucTe - g.BatDauThucTe).TotalSeconds)).ToList();
        var giay = GiayXacNhan.Cap(nd.MaHoSo!, nd.TenHienThi, dl.TuNgay, dl.DenNgay, ban, TaoMaKiemTra(),
            _thoiGian.GetUtcNow().UtcDateTime);
        return await _giaoDich.ThucHienAsync(() =>
        {
            _kho.GiayXacNhan.Add(giay);
            return Task.FromResult(giay.MaGiay);
        });
    }

    public async Task<List<object>> XemCuaToi(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var ds = await _kho.GiayXacNhan.AsNoTracking().Where(g => g.MaHoSo == nd.MaHoSo)
            .OrderByDescending(g => g.NgayCap)
            .Select(g => new { g.MaGiay, g.MaKiemTra, g.TuNgay, g.DenNgay, g.SoCa, g.TongSoGiay, g.NgayCap }).ToListAsync();
        return ds.Select(g => (object)new { g.MaGiay, g.MaKiemTra, g.TuNgay, g.DenNgay, g.SoCa, TongGio = g.TongSoGiay / 3600m, g.NgayCap }).ToList();
    }

    // Ai có mã đều kiểm tra được giấy; chỉ trả họ tên, tổng giờ, số ca, khoảng thời gian và tên tổ chức (BR29).
    public async Task<object> KiemTra(string maKiemTra)
    {
        var giay = await DocGiay(g => g.MaKiemTra == (maKiemTra ?? "").Trim().ToUpperInvariant());
        return new
        {
            giay.Giay.MaKiemTra, giay.Giay.TenNguoiNhan, giay.Giay.TuNgay, giay.Giay.DenNgay, giay.Giay.SoCa,
            TongGio = giay.Giay.TongSoGiay / 3600m, giay.Giay.NgayCap,
            ToChuc = giay.Dong.Select(d => d.TenToChuc).Distinct().OrderBy(t => t).ToList()
        };
    }

    public async Task<(byte[] Pdf, string TenTep)> TaoPdf(NguoiDangNhap nd, string maGiay, string diaChiGoc)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var giay = await DocGiay(g => g.MaGiay == maGiay);
        if (giay.Giay.MaHoSo != nd.MaHoSo) throw LoiNghiepVu.KhongDuQuyen("Đây không phải giấy xác nhận của bạn.");
        var duongKiemTra = $"{diaChiGoc.TrimEnd('/')}/kiem-tra/{giay.Giay.MaKiemTra}";
        return (DungPdf(giay.Giay, giay.Dong, duongKiemTra), $"giay-xac-nhan-{giay.Giay.MaKiemTra}.pdf");
    }

    private record DongGiay(DateTime BatDau, string TieuDe, string TenToChuc, decimal SoGio);

    private async Task<(GiayXacNhan Giay, List<DongGiay> Dong)> DocGiay(System.Linq.Expressions.Expression<Func<GiayXacNhan, bool>> dk)
    {
        var giay = await _kho.GiayXacNhan.AsNoTracking().FirstOrDefaultAsync(dk)
                   ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy giấy xác nhận với mã này.");
        var dong = (await _kho.GiayXacNhanDongGop.AsNoTracking().Where(x => x.MaGiay == giay.MaGiay)
                .Select(x => new
                {
                    x.DongGop!.BatDauThucTe, x.DongGop.KetThucThucTe, x.DongGop.DangKy!.CaLam!.NhuCau!.TieuDe,
                    x.DongGop.DangKy.CaLam.NhuCau.ToChuc!.TenToChuc
                }).ToListAsync())
            .OrderBy(x => x.BatDauThucTe)
            .Select(x => new DongGiay(x.BatDauThucTe, x.TieuDe, x.TenToChuc, DongGop.TinhSoGio(x.BatDauThucTe, x.KetThucThucTe)))
            .ToList();
        return (giay, dong);
    }

    private static string TaoMaKiemTra()
        => new(Enumerable.Range(0, 10).Select(_ => BoKyTu[RandomNumberGenerator.GetInt32(BoKyTu.Length)]).ToArray());

    private static string Ngay(DateTime utc) => utc.AddHours(7).ToString("dd/MM/yyyy", Vn);
    private static string So(decimal gio) => gio.ToString("0.##", Vn);

    private static byte[] DungPdf(GiayXacNhan g, List<DongGiay> dong, string duongKiemTra)
    {
        using var tao = new QRCodeGenerator();
        using var duLieuQr = tao.CreateQrCode(duongKiemTra, QRCodeGenerator.ECCLevel.M);
        var qr = new PngByteQRCode(duLieuQr).GetGraphic(8);
        var khoang = g.TuNgay == null && g.DenNgay == null ? "toàn bộ thời gian tham gia"
            : $"từ {(g.TuNgay?.ToString("dd/MM/yyyy") ?? "khi bắt đầu tham gia")} đến {(g.DenNgay?.ToString("dd/MM/yyyy") ?? "nay")}";

        return Document.Create(tl =>
        {
            tl.Page(trang =>
            {
                trang.Size(PageSizes.A4);
                trang.Margin(2, Unit.Centimetre);
                trang.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial", "DejaVu Sans", "Liberation Sans"));
                trang.Header().Column(c =>
                {
                    c.Item().AlignCenter().Text("NỀN TẢNG ĐIỀU PHỐI TÌNH NGUYỆN VIÊN").FontSize(11);
                    c.Item().PaddingTop(14).AlignCenter().Text("GIẤY XÁC NHẬN THAM GIA HOẠT ĐỘNG TÌNH NGUYỆN").Bold().FontSize(16);
                });
                trang.Content().PaddingTop(18).Column(c =>
                {
                    c.Spacing(6);
                    c.Item().Text(t => { t.Span("Xác nhận ông/bà: "); t.Span(g.TenNguoiNhan).Bold(); });
                    c.Item().Text($"đã tham gia các hoạt động tình nguyện dưới đây, {khoang}.");
                    c.Item().Text(t =>
                    {
                        t.Span("Tổng thời gian đã được tổ chức xác nhận: ");
                        t.Span($"{So(g.TongSoGiay / 3600m)} giờ").Bold();
                        t.Span($" trong {g.SoCa} ca làm.");
                    });
                    c.Item().PaddingTop(8).Table(b =>
                    {
                        b.ColumnsDefinition(cot =>
                        {
                            cot.ConstantColumn(36);
                            cot.ConstantColumn(72);
                            cot.RelativeColumn(3);
                            cot.RelativeColumn(2);
                            cot.ConstantColumn(48);
                        });
                        static IContainer O(IContainer x) => x.Border(0.5f).BorderColor(Colors.Grey.Darken1).Padding(4);
                        b.Header(h =>
                        {
                            foreach (var tieuDe in new[] { "STT", "Ngày", "Hoạt động", "Tổ chức", "Số giờ" })
                                h.Cell().Element(O).Background(Colors.Grey.Lighten3).Text(tieuDe).Bold().FontSize(11);
                        });
                        var i = 1;
                        foreach (var d in dong)
                        {
                            b.Cell().Element(O).Text((i++).ToString()).FontSize(11);
                            b.Cell().Element(O).Text(Ngay(d.BatDau)).FontSize(11);
                            b.Cell().Element(O).Text(d.TieuDe).FontSize(11);
                            b.Cell().Element(O).Text(d.TenToChuc).FontSize(11);
                            b.Cell().Element(O).AlignRight().Text(So(d.SoGio)).FontSize(11);
                        }
                    });
                    c.Item().PaddingTop(16).Row(r =>
                    {
                        r.ConstantItem(110).Image(qr);
                        r.RelativeItem().PaddingLeft(12).Column(k =>
                        {
                            k.Spacing(4);
                            k.Item().Text(t => { t.Span("Mã kiểm tra: "); t.Span(g.MaKiemTra).Bold(); });
                            k.Item().Text($"Ngày cấp: {Ngay(g.NgayCap)}");
                            k.Item().Text("Quét mã QR hoặc mở địa chỉ dưới đây để kiểm tra giấy này:").FontSize(10);
                            k.Item().Text(duongKiemTra).FontSize(10).Underline();
                        });
                    });
                });
                trang.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Số giờ được tính từ các bản khai đã được điều phối viên của tổ chức xác nhận. Trang ").FontSize(9);
                    t.CurrentPageNumber().FontSize(9);
                    t.Span("/").FontSize(9);
                    t.TotalPages().FontSize(9);
                });
            });
        }).GeneratePdf();
    }
}
