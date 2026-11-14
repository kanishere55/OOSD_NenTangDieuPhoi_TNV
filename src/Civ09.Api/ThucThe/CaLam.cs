using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.ThucThe;

// Thông tin của ca dùng để kiểm tra điều kiện khi đăng ký, phân công, khai đóng góp, xem hướng dẫn riêng.
public record DieuKienCa(
    string MaCaLam, string MaNhuCau, string MaToChuc, string TieuDe,
    DateTime BatDauDuKien, DateTime KetThucDuKien, string TrangThaiCa, string TrangThaiNhuCau,
    int SoNguoiCan, List<string> KyNangYeuCau, int SoDaPhanCong);

// Ca làm của một nhu cầu.
public class CaLam
{
    public const string MO = "MO";
    public const string DANG_DIEN_RA = "DANG_DIEN_RA";
    public const string DA_KET_THUC = "DA_KET_THUC";
    public const string DA_HUY = "DA_HUY";

    public string MaCaLam { get; set; } = Guid.NewGuid().ToString();
    public string MaNhuCau { get; set; } = "";
    public DateTime BatDauDuKien { get; set; }
    public DateTime KetThucDuKien { get; set; }
    public int SoNguoiCan { get; set; }
    public string TrangThai { get; set; } = MO;

    public NhuCauCongDong? NhuCau { get; set; }
    public List<YeuCauKyNang> YeuCauKyNang { get; set; } = new();
    public List<DangKyCaLam> DangKy { get; set; } = new();

    // BR04, BR06
    public static CaLam TaoCa(NhuCauCongDong nhuCau, DateTime batDau, DateTime ketThuc, int soNguoiCan,
        IReadOnlyCollection<string> kyNang, DateTime bayGio)
    {
        if (nhuCau.TrangThai != NhuCauCongDong.CONG_BO)
            throw LoiNghiepVu.XungDot("Chỉ tạo ca cho nhu cầu đã công bố.");
        if (batDau <= bayGio)
            throw LoiNghiepVu.KhongHopLe("Ca phải bắt đầu trong tương lai.");
        if (ketThuc <= batDau)
            throw LoiNghiepVu.KhongHopLe("Giờ kết thúc phải sau giờ bắt đầu.");
        if (soNguoiCan < 1)
            throw LoiNghiepVu.KhongHopLe("Ca cần ít nhất một người.");
        if (kyNang.Count == 0)
            throw LoiNghiepVu.KhongHopLe("Ca phải có ít nhất một kỹ năng bắt buộc.");

        var ca = new CaLam
        {
            MaNhuCau = nhuCau.MaNhuCau,
            BatDauDuKien = batDau,
            KetThucDuKien = ketThuc,
            SoNguoiCan = soNguoiCan
        };
        foreach (var ma in kyNang.Distinct())
            ca.YeuCauKyNang.Add(new YeuCauKyNang { MaCaLam = ca.MaCaLam, MaKyNang = ma });
        return ca;
    }

    // Tìm ca công khai qua view CaCongKhai (view không nối với bảng hướng dẫn riêng).
    public static IQueryable<CaCongKhai> TimCaCongKhai(KhoNghiepVu kho, BoLocCa loc)
    {
        var q = kho.CaCongKhai.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(loc.MaKyNang))
            q = q.Where(c => kho.YeuCauKyNang.Any(y => y.MaCaLam == c.MaCaLam && y.MaKyNang == loc.MaKyNang));
        if (loc.TuNgay.HasValue) q = q.Where(c => c.BatDauDuKien >= loc.TuNgay.Value);
        if (loc.DenNgay.HasValue) q = q.Where(c => c.BatDauDuKien < loc.DenNgay.Value);
        if (!string.IsNullOrWhiteSpace(loc.KhuVuc)) q = q.Where(c => c.KhuVucChung.Contains(loc.KhuVuc.Trim()));
        return q;
    }

    // Thời gian, trạng thái của ca và của nhu cầu, tổ chức sở hữu, số người cần, kỹ năng yêu cầu
    // và số người đã phân công.
    public static async Task<DieuKienCa> LayDieuKien(KhoNghiepVu kho, string maCa)
        => await kho.CaLam.AsNoTracking().Where(c => c.MaCaLam == maCa)
               .Select(c => new DieuKienCa(
                   c.MaCaLam, c.MaNhuCau, c.NhuCau!.MaToChuc, c.NhuCau.TieuDe,
                   c.BatDauDuKien, c.KetThucDuKien, c.TrangThai, c.NhuCau.TrangThai, c.SoNguoiCan,
                   c.YeuCauKyNang.Select(y => y.MaKyNang).ToList(),
                   c.DangKy.Count(d => d.TrangThai == DangKyCaLam.DA_PHAN_CONG)))
               .FirstOrDefaultAsync()
           ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ca.");

    public IEnumerable<string> LayKyNangYeuCau() => YeuCauKyNang.Select(y => y.MaKyNang);

    // BR12. Khi chạy thật, việc chuyển trạng thái theo giờ do thủ tục sp_ChuyenMocThoiGianCa làm theo lô;
    // hai phương thức dưới đây mô tả cùng quy tắc cho một ca và được dùng trong kiểm thử đơn vị.
    public void BatDau()
    {
        if (TrangThai == MO) TrangThai = DANG_DIEN_RA;
    }

    public void KetThuc()
    {
        if (TrangThai == MO || TrangThai == DANG_DIEN_RA) TrangThai = DA_KET_THUC;
    }

    // BR12: chỉ hủy ca trước giờ bắt đầu.
    public void Huy(DateTime bayGio)
    {
        if (TrangThai != MO || bayGio >= BatDauDuKien)
            throw LoiNghiepVu.XungDot("Chỉ được hủy ca đang mở và chưa đến giờ bắt đầu.");
        TrangThai = DA_HUY;
    }
}

public class YeuCauKyNang
{
    public string MaCaLam { get; set; } = "";
    public string MaKyNang { get; set; } = "";

    public KyNang? KyNang { get; set; }
}
