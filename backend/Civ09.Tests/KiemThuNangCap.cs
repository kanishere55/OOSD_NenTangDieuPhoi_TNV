using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Civ09.Api.DichVu;
using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Civ09.Tests.HaTang;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Civ09.Tests.HaTang.KichBan;

namespace Civ09.Tests;

// Kiểm thử tích hợp cho các chức năng mở rộng: kỹ năng có cấp độ và chứng chỉ, bản đồ, ảnh, điểm danh,
// thông báo, nhật ký truy cập, tổ chức, khóa tài khoản và giấy xác nhận (BR17 đến BR29).
[Collection(NhomKiemThuTichHop.Ten)]
public class KiemThuNangCap
{
    private readonly MayChuKiemThu _may;
    private readonly KichBan _kb;

    public KiemThuNangCap(MayChuKiemThu may)
    {
        _may = may;
        _kb = new KichBan(may);
    }

    private async Task<T> DungKhoAsync<T>(Func<KhoNghiepVu, Task<T>> lam)
    {
        using var scope = _may.Services.CreateScope();
        return await lam(scope.ServiceProvider.GetRequiredService<KhoNghiepVu>());
    }

    private async Task<string> TaoCaYeuCauAsync(DateTime batDau, DateTime ketThuc, int soNguoiCan, params object[] yeuCau)
    {
        var nc = await _kb.TaoNhuCauCongBoAsync("Nhu cầu " + Guid.NewGuid().ToString("N")[..6], "Hướng dẫn riêng");
        var r = await _kb.Khach("dieuphoi1").PostAsJsonAsync($"/api/nhu-cau/{nc}/ca",
            new { batDauDuKien = batDau, ketThucDuKien = ketThuc, soNguoiCan, kyNang = yeuCau });
        await Dat(r, HttpStatusCode.OK);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maCaLam").GetString()!;
    }

    private async Task<string> TaoNguoiMoiAsync(string ten)
    {
        var sub = Guid.NewGuid().ToString();
        using var scope = _may.Services.CreateScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<AuthService>()
            .XuLyCallback(sub, ten, "nguoi." + sub[..8], null));
        return sub;
    }

    private HttpClient KhachTheoDanhTinh(string sub)
    {
        var c = _kb.Khach();
        c.DefaultRequestHeaders.Add(XacThucKiemThu.TieuDe, sub);
        return c;
    }

    [Fact(DisplayName = "TC31 - Đề xuất kỹ năng: trùng tên có sẵn bị từ chối; quản trị viên duyệt thì kỹ năng vào danh mục và người đề xuất được báo")]
    public async Task TC31_DeXuatKyNang()
    {
        var ten = "Cắt tóc " + Guid.NewGuid().ToString("N")[..4];
        await Dat(await _kb.Khach("tnv2").PostAsJsonAsync("/api/ky-nang/de-xuat", new { tenKyNang = "  sơ CỨU " }), HttpStatusCode.Conflict);
        var r = await _kb.Khach("tnv2").PostAsJsonAsync("/api/ky-nang/de-xuat", new { tenKyNang = ten, lyDo = "Cắt tóc miễn phí cho người già" });
        await Dat(r, HttpStatusCode.OK);
        var ma = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDeXuat").GetString();
        // Đề xuất trùng với đề xuất đang chờ cũng bị từ chối.
        await Dat(await _kb.Khach("tnv3").PostAsJsonAsync("/api/ky-nang/de-xuat", new { tenKyNang = ten.ToUpper() }), HttpStatusCode.Conflict);

        await Dat(await _kb.Khach("dieuphoi1").PostAsJsonAsync($"/api/quan-tri/de-xuat-ky-nang/{ma}/duyet",
            new { tenKyNang = ten, maNhom = "NHOM-YTE", canChungChi = false }), HttpStatusCode.Forbidden);
        await Dat(await _kb.Khach("quantri1").PostAsJsonAsync($"/api/quan-tri/de-xuat-ky-nang/{ma}/duyet",
            new { tenKyNang = ten, maNhom = "NHOM-YTE", canChungChi = false }), HttpStatusCode.OK);

        Assert.Contains(ten, await _kb.Khach().GetStringAsync("/api/ky-nang"));
        var tb = await _kb.Khach("tnv2").GetFromJsonAsync<JsonElement>("/api/thong-bao");
        Assert.Contains(tb.GetProperty("danhSach").EnumerateArray(),
            t => t.GetProperty("loai").GetString() == BoThongBao.KET_QUA_DE_XUAT && t.GetProperty("noiDung").GetString()!.Contains(ten));
    }

    [Fact(DisplayName = "TC32 - Cấp độ: ca cần mức thành thạo thì người mức cơ bản không đăng ký được; nâng cấp độ rồi được; đã phân công thì không hạ cấp độ được")]
    public async Task TC32_CapDoKyNang()
    {
        var d = NgayRieng();
        var ca = await TaoCaYeuCauAsync(d.AddHours(1), d.AddHours(3), 1, new { maKyNang = "KN-DAY-KEM", capDoToiThieu = 3 });
        var r = await _kb.GuiDangKyAsync("tnv3", ca);
        await Dat(r, HttpStatusCode.Conflict);
        Assert.Contains("thành thạo", await r.Content.ReadAsStringAsync());

        var tnv3 = _kb.Khach("tnv3");
        await Dat(await tnv3.PutAsJsonAsync("/api/ho-so/ky-nang", new
        {
            kyNang = new[] { new { maKyNang = "KN-TIN-HOC", capDo = 1 }, new { maKyNang = "KN-DAY-KEM", capDo = 3 } }
        }), HttpStatusCode.NoContent);
        await _kb.DangKyVaPhanCongAsync("tnv3", ca);

        r = await tnv3.PutAsJsonAsync("/api/ho-so/ky-nang", new
        {
            kyNang = new[] { new { maKyNang = "KN-TIN-HOC", capDo = 1 }, new { maKyNang = "KN-DAY-KEM", capDo = 2 } }
        });
        await Dat(r, HttpStatusCode.Conflict);
        // Sửa thẳng trong CSDL cũng bị trigger chặn.
        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "UPDATE dbo.KyNangSoHuu SET CapDo = 1 WHERE MaHoSo = 'HS-06' AND MaKyNang = 'KN-DAY-KEM'"));
        Assert.Equal(50182, loi.Number);
    }

    [Fact(DisplayName = "TC33 - Ca yêu cầu chứng chỉ đã xác minh: chưa xác minh thì không đăng ký được; quản trị viên xác minh thì được; không tự xác minh chứng chỉ của mình")]
    public async Task TC33_ChungChiDaXacMinh()
    {
        var d = NgayRieng();
        // Chỉ yêu cầu xác minh với kỹ năng cần chứng chỉ.
        var nc = await _kb.TaoNhuCauCongBoAsync("Nhu cầu " + Guid.NewGuid().ToString("N")[..6], "x");
        await Dat(await _kb.Khach("dieuphoi1").PostAsJsonAsync($"/api/nhu-cau/{nc}/ca", new
        {
            batDauDuKien = d.AddHours(1), ketThucDuKien = d.AddHours(2), soNguoiCan = 1,
            kyNang = new[] { new { maKyNang = "KN-TIN-HOC", capDoToiThieu = 1, canXacMinh = true } }
        }), HttpStatusCode.BadRequest);

        var ca = await TaoCaYeuCauAsync(d.AddHours(1), d.AddHours(3), 1, new { maKyNang = "KN-LAI-XE", capDoToiThieu = 1, canXacMinh = true });
        var tnv1 = _kb.Khach("tnv1");
        var hs = await tnv1.GetFromJsonAsync<JsonElement>("/api/ho-so");
        var kyNang = hs.GetProperty("kyNang").EnumerateArray()
            .Select(k => new { maKyNang = k.GetProperty("maKyNang").GetString(), capDo = k.GetProperty("capDo").GetInt32() })
            .Where(k => k.maKyNang != "KN-LAI-XE").Append(new { maKyNang = (string?)"KN-LAI-XE", capDo = 2 }).ToArray();
        await Dat(await tnv1.PutAsJsonAsync("/api/ho-so/ky-nang", new { kyNang }), HttpStatusCode.NoContent);

        var r = await _kb.GuiDangKyAsync("tnv1", ca);
        await Dat(r, HttpStatusCode.Conflict);
        Assert.Contains("chứng chỉ", await r.Content.ReadAsStringAsync());

        // Kỹ năng không cần chứng chỉ thì không tải chứng chỉ.
        await Dat(await _kb.TaiTepAsync("tnv1", "/api/ho-so/chung-chi/KN-TIN-HOC", AnhMau()), HttpStatusCode.BadRequest);
        r = await _kb.TaiTepAsync("tnv1", "/api/ho-so/chung-chi/KN-LAI-XE", AnhMau());
        await Dat(r, HttpStatusCode.OK);
        var maCc = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maChungChi").GetString();
        await Dat(await _kb.Khach("quantri1").PostAsJsonAsync($"/api/chung-chi/{maCc}/xac-minh", new { hopLe = false }), HttpStatusCode.BadRequest);
        await Dat(await _kb.Khach("quantri1").PostAsJsonAsync($"/api/chung-chi/{maCc}/xac-minh", new { hopLe = true }), HttpStatusCode.NoContent);
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);

        // Điều phối viên đồng thời là tình nguyện viên không tự xác minh chứng chỉ của mình.
        var huy = _kb.Khach("dieuphoi_tnv");
        await Dat(await huy.PutAsJsonAsync("/api/ho-so/ky-nang", new
        {
            kyNang = new[] { new { maKyNang = "KN-TIN-HOC", capDo = 2 }, new { maKyNang = "KN-SO-CUU", capDo = 1 } }
        }), HttpStatusCode.NoContent);
        r = await _kb.TaiTepAsync("dieuphoi_tnv", "/api/ho-so/chung-chi/KN-SO-CUU", AnhMau());
        var maCcHuy = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maChungChi").GetString();
        await Dat(await huy.PostAsJsonAsync($"/api/chung-chi/{maCcHuy}/xac-minh", new { hopLe = true }), HttpStatusCode.Forbidden);
        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlQuanTriAsync(
            "UPDATE dbo.ChungChiKyNang SET TrangThai = 'DA_XAC_MINH', MaNguoiXacMinh = 'ND-07', NgayXacMinh = SYSUTCDATETIME() WHERE MaChungChi = @ma",
            ("@ma", maCcHuy!)));
        Assert.Equal(50191, loi.Number);
    }

    [Fact(DisplayName = "TC34 - Ảnh chứng chỉ: điều phối viên không liên quan bị từ chối, chủ hồ sơ xem được; mọi lần xem đều ghi nhật ký")]
    public async Task TC34_XemChungChi()
    {
        var r = await _kb.TaiTepAsync("tnv1", "/api/ho-so/chung-chi/KN-SO-CUU", AnhMau());
        await Dat(r, HttpStatusCode.OK);
        var maCc = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maChungChi").GetString()!;

        await Dat(await _kb.Khach("dieuphoi2").GetAsync($"/api/chung-chi/{maCc}/anh"), HttpStatusCode.Forbidden);
        var anh = await _kb.Khach("tnv1").GetAsync($"/api/chung-chi/{maCc}/anh");
        await Dat(anh, HttpStatusCode.OK);
        Assert.Equal("image/jpeg", anh.Content.Headers.ContentType!.MediaType);

        var nhatKy = await DungKhoAsync(k => k.NhatKyTruyCap.Where(n => n.MaDoiTuong == maCc).ToListAsync());
        Assert.Contains(nhatKy, n => n.MaNguoiXem == "ND-03" && !n.DuocPhep);
        Assert.Contains(nhatKy, n => n.MaNguoiXem == "ND-04" && n.DuocPhep);
    }

    [Fact(DisplayName = "TC35 - Vị trí: phần công khai chỉ có vùng bán kính 1 km lệch khỏi vị trí thật, không có địa chỉ hay tọa độ thật")]
    public async Task TC35_ViTriCongKhaiChiLaVung()
    {
        var d = NgayRieng();
        var dd = _kb.Khach("daidien1");
        var r = await dd.PostAsJsonAsync("/api/nhu-cau", new
        {
            maToChuc = "TC-A", tieuDe = "Thăm cụ ông neo đơn", tomTatCongKhai = "Trò chuyện và dọn nhà", khuVucChung = "Phường An Bình",
            huongDanRieng = "Gọi chị Mai", diaChiCuThe = "Hẻm 12 đường Hoa Sữa", viDo = 10.851234m, kinhDo = 106.771234m
        });
        var nc = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNhuCau").GetString();
        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{nc}/cong-bo", new { phienBan = 1 }), HttpStatusCode.NoContent);
        var ca = await _kb.TaoCa(nc!, d.AddHours(1), d.AddHours(3));

        var vanBan = await _kb.Khach().GetStringAsync($"/api/ca/{ca}");
        Assert.DoesNotContain("10.851234", vanBan);
        Assert.DoesNotContain("106.771234", vanBan);
        Assert.DoesNotContain("Hoa Sữa", vanBan);
        var json = JsonDocument.Parse(vanBan).RootElement;
        Assert.Equal(1000, json.GetProperty("banKinhVung").GetInt32());
        var km = ViTri.KhoangCachKm(10.851234, 106.771234, json.GetProperty("viDoVung").GetDouble(), json.GetProperty("kinhDoVung").GetDouble());
        Assert.InRange(km, 0.15, 0.65);

        // Người được phân công xem được địa chỉ và vị trí thật.
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        var rieng = await _kb.Khach("tnv1").GetFromJsonAsync<JsonElement>($"/api/ca/{ca}/huong-dan-rieng");
        Assert.Equal("Hẻm 12 đường Hoa Sữa", rieng.GetProperty("diaChiCuThe").GetString());
        Assert.Equal(10.851234m, rieng.GetProperty("viDo").GetDecimal());
    }

    [Fact(DisplayName = "TC36 - Tìm ca theo khoảng cách: chỉ trả ca trong bán kính chọn, xếp theo gần nhất")]
    public async Task TC36_TimCaTheoKhoangCach()
    {
        var d = NgayRieng();
        var dd = _kb.Khach("daidien1");
        async Task<string> TaoAsync(string tieuDe, decimal viDo, decimal kinhDo)
        {
            var r = await dd.PostAsJsonAsync("/api/nhu-cau", new
            {
                maToChuc = "TC-A", tieuDe, tomTatCongKhai = "Tóm tắt", khuVucChung = "Khu vực", viDo, kinhDo
            });
            var nc = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNhuCau").GetString()!;
            await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{nc}/cong-bo", new { phienBan = 1 }), HttpStatusCode.NoContent);
            return await _kb.TaoCa(nc, d.AddHours(1), d.AddHours(2));
        }
        var gan = await TaoAsync("Ca ở gần", 20.000000m, 105.000000m);
        var xa = await TaoAsync("Ca ở xa", 20.300000m, 105.300000m);

        var kq = await _kb.Khach().GetStringAsync("/api/ca?viDo=20.001&kinhDo=105.001&banKinhKm=5&sapXep=gan-nhat&kichThuoc=50");
        Assert.Contains(gan, kq);
        Assert.DoesNotContain(xa, kq);
        var json = JsonDocument.Parse(kq).RootElement.GetProperty("danhSach")[0];
        Assert.Equal(gan, json.GetProperty("maCaLam").GetString());
        Assert.True(json.GetProperty("khoangCachKm").GetDouble() < 5);
    }

    [Fact(DisplayName = "TC37 - Ảnh nhu cầu: ảnh lưu vào CSDL đã xóa thông tin EXIF; tệp không phải ảnh bị từ chối; thêm ảnh phải rà soát lại; tối đa 5 ảnh")]
    public async Task TC37_AnhNhuCau()
    {
        var dd = _kb.Khach("daidien1");
        var r = await dd.PostAsJsonAsync("/api/nhu-cau", new { maToChuc = "TC-A", tieuDe = "Có ảnh", tomTatCongKhai = "Tóm tắt", khuVucChung = "Khu vực" });
        var nc = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNhuCau").GetString()!;

        var coExif = ChenExif(AnhMau(), "GPSLatitude=10.850320;GPSLongitude=106.772510");
        Assert.Contains("GPSLatitude", Encoding.ASCII.GetString(coExif));
        r = await _kb.TaiTepAsync("daidien1", $"/api/nhu-cau/{nc}/anh", coExif);
        await Dat(r, HttpStatusCode.OK);
        var maTep = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maTep").GetString()!;
        var luu = await DungKhoAsync(k => k.TepDinhKem.AsNoTracking().FirstAsync(t => t.MaTep == maTep));
        var chuoi = Encoding.ASCII.GetString(luu.NoiDung);
        Assert.DoesNotContain("Exif", chuoi);
        Assert.DoesNotContain("GPSLatitude", chuoi);

        await Dat(await _kb.TaiTepAsync("daidien1", $"/api/nhu-cau/{nc}/anh", Encoding.UTF8.GetBytes("không phải ảnh"), "a.jpg"),
            HttpStatusCode.BadRequest);

        // Thêm ảnh làm tăng phiên bản: công bố với phiên bản đã rà soát trước đó bị từ chối.
        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{nc}/cong-bo", new { phienBan = 1 }), HttpStatusCode.Conflict);
        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{nc}/cong-bo", new { phienBan = 2 }), HttpStatusCode.NoContent);
        // Ảnh của nhu cầu đã công bố ai cũng xem được.
        await Dat(await _kb.Khach().GetAsync($"/api/anh/nhu-cau/{maTep}"), HttpStatusCode.OK);

        for (var i = 0; i < 4; i++)
            await Dat(await _kb.TaiTepAsync("daidien1", $"/api/nhu-cau/{nc}/anh", AnhMau()), HttpStatusCode.OK);
        await Dat(await _kb.TaiTepAsync("daidien1", $"/api/nhu-cau/{nc}/anh", AnhMau()), HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC38 - Điểm danh bằng mã QR: chưa đến giờ thì chưa có mã, mã sai bị từ chối, quét vào rồi ra; bản khai lấy giờ theo điểm danh")]
    public async Task TC38_DiemDanhQr()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        var dp = _kb.Khach("dieuphoi1");
        var tnv1 = _kb.Khach("tnv1");

        _kb.DatGio(d.AddHours(1).AddMinutes(-40));
        await Dat(await dp.GetAsync($"/api/ca/{ca}/diem-danh/ma-qr"), HttpStatusCode.Conflict);

        _kb.DatGio(d.AddHours(1).AddMinutes(-10));
        var qr = await dp.GetFromJsonAsync<JsonElement>($"/api/ca/{ca}/diem-danh/ma-qr");
        Assert.StartsWith("data:image/png;base64,", qr.GetProperty("anhQr").GetString());
        await Dat(await tnv1.PostAsJsonAsync($"/api/ca/{ca}/diem-danh", new { ma = "SAIMA12345" }), HttpStatusCode.BadRequest);
        // Người chưa được phân công không điểm danh được.
        await Dat(await _kb.Khach("tnv3").PostAsJsonAsync($"/api/ca/{ca}/diem-danh", new { ma = qr.GetProperty("ma").GetString() }), HttpStatusCode.Conflict);
        var vao = await tnv1.PostAsJsonAsync($"/api/ca/{ca}/diem-danh", new { ma = qr.GetProperty("ma").GetString() });
        await Dat(vao, HttpStatusCode.OK);
        Assert.Equal("VAO", (await vao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("loai").GetString());
        await Dat(await tnv1.PostAsJsonAsync($"/api/ca/{ca}/diem-danh", new { ma = qr.GetProperty("ma").GetString() }), HttpStatusCode.Conflict);

        _kb.DatGio(d.AddHours(3).AddMinutes(-10));
        // Mã cũ đã hết hạn vì mã đổi mỗi 2 phút.
        await Dat(await tnv1.PostAsJsonAsync($"/api/ca/{ca}/diem-danh", new { ma = qr.GetProperty("ma").GetString() }), HttpStatusCode.BadRequest);
        qr = await dp.GetFromJsonAsync<JsonElement>($"/api/ca/{ca}/diem-danh/ma-qr");
        var ra = await tnv1.PostAsJsonAsync($"/api/ca/{ca}/diem-danh", new { ma = qr.GetProperty("ma").GetString() });
        await Dat(ra, HttpStatusCode.OK);
        Assert.Equal("RA", (await ra.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("loai").GetString());

        _kb.DatGio(d.AddHours(3).AddMinutes(5));
        // Khai giờ khác với điểm danh: hệ thống lấy giờ vào (cắt về giờ bắt đầu ca) và giờ ra.
        var maDg = await _kb.KhaiAsync("tnv1", dk, d.AddHours(1), d.AddHours(3));
        var ban = await DungKhoAsync(k => k.DongGop.AsNoTracking().FirstAsync(g => g.MaDongGop == maDg));
        Assert.True(ban.GioTheoDiemDanh);
        Assert.Equal(d.AddHours(1), ban.BatDauThucTe);
        Assert.Equal(d.AddHours(3).AddMinutes(-10), ban.KetThucThucTe);
    }

    [Fact(DisplayName = "TC39 - Vắng mặt: chỉ ghi nhận sau giờ bắt đầu; người bị ghi vắng không khai được và mức tin cậy giảm; sửa lại có mặt thì khai được")]
    public async Task TC39_VangMat()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv3", ca);
        var dp = _kb.Khach("dieuphoi1");
        var truoc = (await _kb.Khach("tnv3").GetFromJsonAsync<JsonElement>("/api/ho-so")).GetProperty("mucTinCay").GetProperty("soLanVang").GetInt32();

        _kb.DatGio(d.AddMinutes(30));
        await Dat(await dp.PostAsync($"/api/dang-ky/{dk}/vang-mat", null), HttpStatusCode.Conflict);
        _kb.DatGio(d.AddHours(2));
        await Dat(await dp.PostAsync($"/api/dang-ky/{dk}/vang-mat", null), HttpStatusCode.NoContent);

        _kb.DatGio(d.AddHours(4));
        var r = await _kb.GuiKhaiAsync("tnv3", dk, d.AddHours(1), d.AddHours(3));
        await Dat(r, HttpStatusCode.Conflict);
        Assert.Contains("vắng mặt", await r.Content.ReadAsStringAsync());
        var hs = await _kb.Khach("tnv3").GetFromJsonAsync<JsonElement>("/api/ho-so");
        Assert.Equal(truoc + 1, hs.GetProperty("mucTinCay").GetProperty("soLanVang").GetInt32());
        var tb = await _kb.Khach("tnv3").GetFromJsonAsync<JsonElement>("/api/thong-bao");
        Assert.Contains(tb.GetProperty("danhSach").EnumerateArray(), t => t.GetProperty("loai").GetString() == BoThongBao.VANG_MAT);

        // Ghi nhầm: điều phối viên sửa lại có mặt kèm giờ, người tham gia khai được.
        await Dat(await dp.PostAsJsonAsync($"/api/dang-ky/{dk}/co-mat", new { gioVao = d.AddHours(1), gioRa = d.AddHours(2) }), HttpStatusCode.NoContent);
        await _kb.KhaiAsync("tnv3", dk, d.AddHours(1), d.AddHours(3));
    }

    [Fact(DisplayName = "TC40 - Ảnh minh chứng: người khai và điều phối viên của tổ chức xem được, người khác bị từ chối; đã xác nhận thì không thêm ảnh")]
    public async Task TC40_AnhMinhChung()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        _kb.DatGio(d.AddHours(4));
        var maDg = await _kb.KhaiAsync("tnv1", dk, d.AddHours(1), d.AddHours(3));

        var r = await _kb.TaiTepAsync("tnv1", $"/api/dong-gop/{maDg}/anh", AnhMau());
        await Dat(r, HttpStatusCode.OK);
        var maTep = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maTep").GetString()!;
        await Dat(await _kb.TaiTepAsync("tnv3", $"/api/dong-gop/{maDg}/anh", AnhMau()), HttpStatusCode.Forbidden);

        await Dat(await _kb.Khach("tnv1").GetAsync($"/api/anh/minh-chung/{maTep}"), HttpStatusCode.OK);
        await Dat(await _kb.Khach("dieuphoi1").GetAsync($"/api/anh/minh-chung/{maTep}"), HttpStatusCode.OK);
        await Dat(await _kb.Khach("dieuphoi2").GetAsync($"/api/anh/minh-chung/{maTep}"), HttpStatusCode.Forbidden);
        await Dat(await _kb.Khach("tnv3").GetAsync($"/api/anh/minh-chung/{maTep}"), HttpStatusCode.Forbidden);
        await Dat(await _kb.Khach().GetAsync($"/api/anh/minh-chung/{maTep}"), HttpStatusCode.Unauthorized);
        Assert.Equal(2, await DungKhoAsync(k => k.NhatKyTruyCap.CountAsync(n => n.MaDoiTuong == maDg && !n.DuocPhep)));

        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", maDg, true), HttpStatusCode.NoContent);
        await Dat(await _kb.TaiTepAsync("tnv1", $"/api/dong-gop/{maDg}/anh", AnhMau()), HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "TC41 - Thông báo: được phân công thì nhận thông báo; không đánh dấu được thông báo của người khác; nhắc trước ca chỉ tạo một lần")]
    public async Task TC41_ThongBao()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv1", ca);

        var tb = await _kb.Khach("tnv1").GetFromJsonAsync<JsonElement>("/api/thong-bao");
        var phanCong = tb.GetProperty("danhSach").EnumerateArray()
            .First(t => t.GetProperty("loai").GetString() == BoThongBao.PHAN_CONG);
        var maTb = phanCong.GetProperty("maThongBao").GetString();
        await Dat(await _kb.Khach("tnv3").PostAsync($"/api/thong-bao/{maTb}/da-doc", null), HttpStatusCode.Forbidden);
        await Dat(await _kb.Khach("tnv1").PostAsync($"/api/thong-bao/{maTb}/da-doc", null), HttpStatusCode.NoContent);

        _kb.DatGio(d.AddHours(1).AddHours(-20));
        using (var scope = _may.Services.CreateScope())
        {
            var dv = scope.ServiceProvider.GetRequiredService<CaService>();
            await dv.TaoNhacTruocCa();
            await dv.TaoNhacTruocCa();
        }
        Assert.Equal(1, await DungKhoAsync(k => k.ThongBao.CountAsync(t => t.Loai == "NHAC_CA" && t.MaThamChieu == dk)));
    }

    [Fact(DisplayName = "TC42 - Nhật ký truy cập chỉ được thêm: tài khoản ứng dụng không sửa, không xóa được; sửa bằng tài khoản quản trị CSDL cũng bị trigger chặn")]
    public async Task TC42_NhatKyChiThem()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        await Dat(await _kb.Khach("dieuphoi2").GetAsync($"/api/ca/{ca}/huong-dan-rieng"), HttpStatusCode.Forbidden);
        await Dat(await _kb.Khach("dieuphoi1").GetAsync($"/api/ca/{ca}/huong-dan-rieng"), HttpStatusCode.OK);
        var nk = await DungKhoAsync(k => k.NhatKyTruyCap.Where(n => n.LoaiDuLieu == NhatKyTruyCap.HUONG_DAN_RIENG && n.MaToChuc == "TC-A")
            .OrderByDescending(n => n.MaNhatKy).Take(2).ToListAsync());
        Assert.Contains(nk, n => n.MaNguoiXem == "ND-03" && !n.DuocPhep);
        Assert.Contains(nk, n => n.MaNguoiXem == "ND-02" && n.DuocPhep);

        await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync("UPDATE dbo.NhatKyTruyCap SET DuocPhep = 1"));
        await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync("DELETE FROM dbo.NhatKyTruyCap"));
        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlQuanTriAsync("DELETE FROM dbo.NhatKyTruyCap"));
        Assert.Equal(50261, loi.Number);

        // Đại diện xem được nhật ký của tổ chức mình, điều phối viên thì không.
        await Dat(await _kb.Khach("daidien1").GetAsync("/api/to-chuc/TC-A/nhat-ky"), HttpStatusCode.OK);
        await Dat(await _kb.Khach("dieuphoi1").GetAsync("/api/to-chuc/TC-A/nhat-ky"), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "TC43 - Đăng ký tổ chức: chưa duyệt thì chưa tạo được nhu cầu; quản trị viên duyệt thì người đăng ký thành đại diện và tạo được nhu cầu")]
    public async Task TC43_DangKyToChuc()
    {
        var sub = await TaoNguoiMoiAsync("Trương Văn Lộc");
        var nguoi = KhachTheoDanhTinh(sub);
        var ten = "Nhóm Bạn Trẻ " + Guid.NewGuid().ToString("N")[..5];
        var r = await nguoi.PostAsJsonAsync("/api/to-chuc/dang-ky", new { tenToChuc = ten, khuVucHoatDong = "Phường 7", soDienThoai = "0901234567" });
        await Dat(r, HttpStatusCode.OK);
        var maTc = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maToChuc").GetString();
        await Dat(await nguoi.PostAsJsonAsync("/api/to-chuc/dang-ky", new { tenToChuc = ten + " 2", khuVucHoatDong = "x", soDienThoai = "0901234567" }),
            HttpStatusCode.Conflict);
        await Dat(await nguoi.PostAsJsonAsync("/api/nhu-cau", new { maToChuc = maTc, tieuDe = "a", tomTatCongKhai = "b", khuVucChung = "c" }),
            HttpStatusCode.Forbidden);

        await Dat(await _kb.Khach("tnv1").PostAsync($"/api/quan-tri/to-chuc/{maTc}/duyet", null), HttpStatusCode.Forbidden);
        await Dat(await _kb.Khach("quantri1").PostAsync($"/api/quan-tri/to-chuc/{maTc}/duyet", null), HttpStatusCode.NoContent);

        var toi = await nguoi.GetFromJsonAsync<JsonElement>("/api/auth/toi");
        Assert.Contains(toi.GetProperty("thanhVien").EnumerateArray(),
            t => t.GetProperty("maToChuc").GetString() == maTc && t.GetProperty("vaiTro").GetString() == CacVaiTro.DaiDien);
        await Dat(await nguoi.PostAsJsonAsync("/api/nhu-cau", new { maToChuc = maTc, tieuDe = "a", tomTatCongKhai = "b", khuVucChung = "c" }),
            HttpStatusCode.OK);

        // Tổ chức bị ngừng hoạt động thì đại diện mất quyền ở tổ chức; trigger chặn tạo nhu cầu.
        await Dat(await _kb.Khach("quantri1").PostAsync($"/api/quan-tri/to-chuc/{maTc}/ngung", null), HttpStatusCode.NoContent);
        await Dat(await nguoi.PostAsJsonAsync("/api/nhu-cau", new { maToChuc = maTc, tieuDe = "a", tomTatCongKhai = "b", khuVucChung = "c" }),
            HttpStatusCode.Forbidden);
        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "INSERT INTO dbo.NhuCauCongDong (MaToChuc, TieuDe, TomTatCongKhai, KhuVucChung) VALUES (@tc, N'a', N'b', N'c')", ("@tc", maTc!)));
        Assert.Equal(50281, loi.Number);
    }

    [Fact(DisplayName = "TC44 - Đại diện thêm điều phối viên bằng tên đăng nhập; ngừng thì người đó mất quyền; điều phối viên không thêm được thành viên")]
    public async Task TC44_ThanhVienToChuc()
    {
        var sub = await TaoNguoiMoiAsync("Mai Văn Phúc");
        var tenDangNhap = "nguoi." + sub[..8];
        var nguoi = KhachTheoDanhTinh(sub);
        var dd = _kb.Khach("daidien1");

        await Dat(await dd.PostAsJsonAsync("/api/to-chuc/TC-A/thanh-vien", new { tenDangNhap = "khong.co.ai", vaiTro = "DIEU_PHOI" }), HttpStatusCode.NotFound);
        await Dat(await _kb.Khach("dieuphoi1").PostAsJsonAsync("/api/to-chuc/TC-A/thanh-vien", new { tenDangNhap, vaiTro = "DIEU_PHOI" }), HttpStatusCode.Forbidden);
        await Dat(await dd.PostAsJsonAsync("/api/to-chuc/TC-A/thanh-vien", new { tenDangNhap, vaiTro = "DAI_DIEN" }), HttpStatusCode.Forbidden);
        await Dat(await nguoi.GetAsync("/api/nhu-cau"), HttpStatusCode.Forbidden);

        await Dat(await dd.PostAsJsonAsync("/api/to-chuc/TC-A/thanh-vien", new { tenDangNhap, vaiTro = "DIEU_PHOI" }), HttpStatusCode.NoContent);
        await Dat(await nguoi.GetAsync("/api/nhu-cau"), HttpStatusCode.OK);

        var maNguoi = await DungKhoAsync(k => k.NguoiDung.Where(x => x.MaDinhDanhOIDC == sub).Select(x => x.MaNguoiDung).FirstAsync());
        await Dat(await dd.PostAsync($"/api/to-chuc/TC-A/thanh-vien/{maNguoi}/ngung?vaiTro=DIEU_PHOI", null), HttpStatusCode.NoContent);
        await Dat(await nguoi.GetAsync("/api/nhu-cau"), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "TC45 - Khóa tài khoản: tài khoản bị khóa không dùng được nền tảng; quản trị viên không tự khóa mình; mở khóa thì dùng lại được")]
    public async Task TC45_KhoaTaiKhoan()
    {
        var sub = await TaoNguoiMoiAsync("Lâm Thị Hoa");
        var nguoi = KhachTheoDanhTinh(sub);
        var maNguoi = await DungKhoAsync(k => k.NguoiDung.Where(x => x.MaDinhDanhOIDC == sub).Select(x => x.MaNguoiDung).FirstAsync());
        var qt = _kb.Khach("quantri1");

        await Dat(await nguoi.GetAsync("/api/auth/toi"), HttpStatusCode.OK);
        await Dat(await qt.PostAsync($"/api/quan-tri/nguoi-dung/{maNguoi}/khoa", null), HttpStatusCode.NoContent);
        await Dat(await nguoi.GetAsync("/api/auth/toi"), HttpStatusCode.Forbidden);
        using (var scope = _may.Services.CreateScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<AuthService>().XuLyCallback(sub, "Lâm Thị Hoa"));
        await Dat(await qt.PostAsync("/api/quan-tri/nguoi-dung/ND-08/khoa", null), HttpStatusCode.Conflict);
        await Dat(await _kb.Khach("daidien1").PostAsync($"/api/quan-tri/nguoi-dung/{maNguoi}/mo-khoa", null), HttpStatusCode.Forbidden);
        await Dat(await qt.PostAsync($"/api/quan-tri/nguoi-dung/{maNguoi}/mo-khoa", null), HttpStatusCode.NoContent);
        await Dat(await nguoi.GetAsync("/api/auth/toi"), HttpStatusCode.OK);
    }

    [Fact(DisplayName = "TC46 - Giấy xác nhận: chỉ tính bản đã xác nhận; ai có mã đều kiểm tra được; tệp PDF chỉ chủ giấy tải được")]
    public async Task TC46_GiayXacNhan()
    {
        var sub = await TaoNguoiMoiAsync("Phan Thị Ngọc");
        var nguoi = KhachTheoDanhTinh(sub);
        await Dat(await nguoi.PutAsJsonAsync("/api/ho-so/ky-nang", new { kyNang = new[] { new { maKyNang = "KN-TIN-HOC", capDo = 1 } } }),
            HttpStatusCode.NoContent);
        await Dat(await nguoi.PostAsJsonAsync("/api/giay-xac-nhan", new { }), HttpStatusCode.Conflict);

        var d = NgayRieng();
        var caA = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var caB = await _kb.TaoNhuCauVaCaAsync(d.AddHours(4), d.AddHours(5));
        async Task<string> ThamGiaAsync(string ca)
        {
            var r = await nguoi.PostAsync($"/api/ca/{ca}/dang-ky", null);
            var dk = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDangKy").GetString()!;
            await Dat(await _kb.GuiPhanCongAsync(dk), HttpStatusCode.NoContent);
            return dk;
        }
        var dkA = await ThamGiaAsync(caA);
        var dkB = await ThamGiaAsync(caB);
        _kb.DatGio(d.AddHours(6));
        async Task<string> KhaiAsync(string dk, DateTime a, DateTime b)
        {
            var r = await nguoi.PostAsJsonAsync($"/api/dang-ky/{dk}/dong-gop", new { batDauThucTe = a, ketThucThucTe = b, moTaCongViec = "Hướng dẫn dùng điện thoại" });
            await Dat(r, HttpStatusCode.OK);
            return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDongGop").GetString()!;
        }
        var dgA = await KhaiAsync(dkA, d.AddHours(1), d.AddHours(3));
        await KhaiAsync(dkB, d.AddHours(4), d.AddHours(5));
        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dgA, true), HttpStatusCode.NoContent);

        var gr = await nguoi.PostAsJsonAsync("/api/giay-xac-nhan", new { });
        await Dat(gr, HttpStatusCode.OK);
        var maGiay = (await gr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maGiay").GetString();
        var ds = await nguoi.GetFromJsonAsync<JsonElement>("/api/giay-xac-nhan/cua-toi");
        var maKiemTra = ds[0].GetProperty("maKiemTra").GetString();

        var kt = await _kb.Khach().GetFromJsonAsync<JsonElement>($"/api/kiem-tra-giay/{maKiemTra}");
        Assert.Equal(1, kt.GetProperty("soCa").GetInt32());
        Assert.Equal(2m, kt.GetProperty("tongGio").GetDecimal());
        Assert.Equal("Phan Thị Ngọc", kt.GetProperty("tenNguoiNhan").GetString());
        await Dat(await _kb.Khach().GetAsync("/api/kiem-tra-giay/KHONGCOMA1"), HttpStatusCode.NotFound);

        var pdf = await nguoi.GetAsync($"/api/giay-xac-nhan/{maGiay}/pdf");
        await Dat(pdf, HttpStatusCode.OK);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        await Dat(await _kb.Khach("tnv1").GetAsync($"/api/giay-xac-nhan/{maGiay}/pdf"), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "TC47 - Xét duyệt có điểm phù hợp: người đủ điều kiện có điểm, danh sách chờ duyệt xếp theo điểm giảm dần")]
    public async Task TC47_XepHangUngVien()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        await _kb.DangKy("tnv3", ca);
        await _kb.DangKy("tnv1", ca);
        var json = await _kb.Khach("dieuphoi1").GetFromJsonAsync<JsonElement>($"/api/ca/{ca}/dang-ky");
        var diem = json.GetProperty("dangKy").EnumerateArray().Select(x => x.GetProperty("diem").GetInt32()).ToList();
        Assert.Equal(2, diem.Count);
        Assert.All(diem, x => Assert.InRange(x, 40, 100));
        Assert.Equal(diem.OrderByDescending(x => x).ToList(), diem);
    }

    [Fact(DisplayName = "TC48 - Kỹ năng đang thiếu: thống kê đúng số chỗ thiếu theo kỹ năng; tổ chức khác không xem được")]
    public async Task TC48_KyNangDangThieu()
    {
        var d = NgayRieng();
        await TaoCaYeuCauAsync(d.AddHours(1), d.AddHours(3), 3, new { maKyNang = "KN-CHAM-SOC" });
        var tk = await _kb.Khach("daidien1").GetFromJsonAsync<JsonElement>("/api/to-chuc/TC-A/ky-nang-thieu");
        var dong = tk.EnumerateArray().First(x => x.GetProperty("maKyNang").GetString() == "KN-CHAM-SOC");
        Assert.True(dong.GetProperty("soChoThieu").GetInt32() >= 3);
        await Dat(await _kb.Khach("dieuphoi2").GetAsync("/api/to-chuc/TC-A/ky-nang-thieu"), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "TC49 - Ca phù hợp với tình nguyện viên: chỉ gợi ý ca đủ điều kiện kỹ năng, chưa đăng ký, kèm lý do")]
    public async Task TC49_CaPhuHop()
    {
        var d = NgayRieng();
        var hop = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var khongHop = await TaoCaYeuCauAsync(d.AddHours(1), d.AddHours(3), 1, new { maKyNang = "KN-NAU-AN" });
        var ds = await _kb.Khach("tnv3").GetStringAsync("/api/ca/phu-hop?soLuong=30");
        Assert.DoesNotContain(khongHop, ds);
        var json = JsonDocument.Parse(ds).RootElement;
        var dong = json.EnumerateArray().FirstOrDefault(x => x.GetProperty("ca").GetProperty("maCaLam").GetString() == hop);
        if (dong.ValueKind == JsonValueKind.Object)
            Assert.Contains("Đủ kỹ năng và cấp độ", dong.GetProperty("lyDo").EnumerateArray().Select(x => x.GetString()));
        await Dat(await _kb.Khach("dieuphoi1").GetAsync("/api/ca/phu-hop"), HttpStatusCode.Forbidden);
    }
}
