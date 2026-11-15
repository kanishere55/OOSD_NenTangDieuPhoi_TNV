using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Civ09.Api.DichVu;
using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Civ09.Tests.HaTang;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Civ09.Tests.HaTang.KichBan;

namespace Civ09.Tests;

// Kiểm thử tích hợp: gọi API thật của máy chủ ứng dụng, đọc ghi SQL Server thật trong Docker.
// Mã TC tương ứng Bảng 7.2 của báo cáo.
[Collection(NhomKiemThuTichHop.Ten)]
public class KiemThuTichHop
{
    private readonly MayChuKiemThu _may;
    private readonly KichBan _kb;

    public KiemThuTichHop(MayChuKiemThu may)
    {
        _may = may;
        _kb = new KichBan(may);
    }

    private async Task<string> TrangThaiDangKyAsync(string maDangKy)
    {
        using var scope = _may.Services.CreateScope();
        var kho = scope.ServiceProvider.GetRequiredService<KhoNghiepVu>();
        return await kho.DangKyCaLam.Where(d => d.MaDangKy == maDangKy).Select(d => d.TrangThai).FirstAsync();
    }

    [Fact(DisplayName = "TC01 - Phân công thêm ca 09:00-11:00 cho người đã có ca 08:00-10:00 bị từ chối vì trùng lịch")]
    public async Task TC01_PhanCongTrungLich_BiTuChoi()
    {
        var d = NgayRieng();
        var caA = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var caB = await _kb.TaoNhuCauVaCaAsync(d.AddHours(2), d.AddHours(4));
        await _kb.DangKyVaPhanCongAsync("tnv1", caA);
        var dkB = await _kb.DangKy("tnv1", caB);

        var r = await _kb.GuiPhanCongAsync(dkB);

        await Dat(r, HttpStatusCode.Conflict);
        Assert.Contains("trùng", await r.Content.ReadAsStringAsync());
        Assert.Equal(DangKyCaLam.CHO_DUYET, await TrangThaiDangKyAsync(dkB));
    }

    [Fact(DisplayName = "TC02 - Ca sau bắt đầu đúng lúc ca trước kết thúc thì vẫn phân công được")]
    public async Task TC02_CaNoiTiep_PhanCongDuoc()
    {
        var d = NgayRieng();
        var caA = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var caC = await _kb.TaoNhuCauVaCaAsync(d.AddHours(3), d.AddHours(5));
        await _kb.DangKyVaPhanCongAsync("tnv1", caA);
        var dkC = await _kb.DangKy("tnv1", caC);

        await Dat(await _kb.GuiPhanCongAsync(dkC), HttpStatusCode.NoContent);
        Assert.Equal(DangKyCaLam.DA_PHAN_CONG, await TrangThaiDangKyAsync(dkC));
    }

    [Fact(DisplayName = "TC03 - Tình nguyện viên thiếu kỹ năng đăng ký ca bị từ chối, không tạo đăng ký")]
    public async Task TC03_ThieuKyNang_KhongTaoDangKy()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));

        var r = await _kb.GuiDangKyAsync("tnv2", ca);

        await Dat(r, HttpStatusCode.Conflict);
        var cuaToi = await _kb.Khach("tnv2").GetStringAsync("/api/dang-ky/cua-toi");
        Assert.DoesNotContain(ca, cuaToi);
    }

    [Fact(DisplayName = "TC04 - Ca đủ người vẫn nhận đăng ký chờ duyệt, nhưng phân công thêm bị từ chối vì hết chỗ")]
    public async Task TC04_CaDuNguoi_PhanCongBiTuChoi()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), soNguoiCan: 1);
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);

        var dk3 = await _kb.DangKy("tnv3", ca);
        Assert.Equal(DangKyCaLam.CHO_DUYET, await TrangThaiDangKyAsync(dk3));

        var r = await _kb.GuiPhanCongAsync(dk3);
        await Dat(r, HttpStatusCode.Conflict);
        Assert.Contains("đủ người", await r.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "TC05 - Ca còn một chỗ, hai yêu cầu phân công gửi cùng lúc thì chỉ một yêu cầu thành công")]
    public async Task TC05_PhanCongDongThoi_ChiMotThanhCong()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), soNguoiCan: 1);
        var dk1 = await _kb.DangKy("tnv1", ca);
        var dk3 = await _kb.DangKy("tnv3", ca);

        var ketQua = await Task.WhenAll(_kb.GuiPhanCongAsync(dk1), _kb.GuiPhanCongAsync(dk3));

        Assert.Equal(1, ketQua.Count(r => r.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, ketQua.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var json = await _kb.Khach("dieuphoi1").GetFromJsonAsync<JsonElement>($"/api/ca/{ca}/dang-ky");
        Assert.Equal(1, json.GetProperty("soDaPhanCong").GetInt32());
    }

    [Fact(DisplayName = "TC06 - Đăng ký lần hai vào cùng ca khi đăng ký trước còn hiệu lực bị từ chối")]
    public async Task TC06_DangKyHaiLan_LanHaiBiTuChoi()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        await _kb.DangKy("tnv1", ca);

        await Dat(await _kb.GuiDangKyAsync("tnv1", ca), HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "TC07 - Rà soát, sửa nội dung công khai rồi công bố với phiên bản cũ bị từ chối")]
    public async Task TC07_CongBoPhienBanCu_BiTuChoi()
    {
        var dd = _kb.Khach("daidien1");
        var r = await dd.PostAsJsonAsync("/api/nhu-cau", new
        {
            maToChuc = "TC-A", tieuDe = "Dạy kèm cuối tuần", tomTatCongKhai = "Kèm học sinh lớp 5", khuVucChung = "Phường An Bình"
        });
        var ma = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNhuCau").GetString();
        // Đại diện đã rà soát phiên bản 1, sau đó sửa tóm tắt nên nội dung thành phiên bản 2.
        await Dat(await dd.PutAsJsonAsync($"/api/nhu-cau/{ma}", new
        {
            tieuDe = "Dạy kèm cuối tuần", tomTatCongKhai = "Kèm học sinh lớp 5 tại nhà em Nam", khuVucChung = "Phường An Bình"
        }), HttpStatusCode.NoContent);

        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{ma}/cong-bo", new { phienBan = 1 }), HttpStatusCode.Conflict);
        var nc = await dd.GetFromJsonAsync<JsonElement>($"/api/nhu-cau/{ma}");
        Assert.Equal("NHAP", nc.GetProperty("trangThai").GetString());

        // Rà soát lại phiên bản mới rồi công bố thì được.
        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{ma}/cong-bo", new { phienBan = 2 }), HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "TC08 - Tìm ca công khai khi chưa đăng nhập: có dữ liệu, không chứa hướng dẫn riêng")]
    public async Task TC08_TimCaCongKhai_KhongLoHuongDanRieng()
    {
        var d = NgayRieng();
        var nc = await _kb.TaoNhuCauCongBoAsync("Hỗ trợ dọn nhà sau mưa", "BI_MAT_TC08 số nhà 12 hẻm 5", khuVuc: "Khu TC08");
        var ca = await _kb.TaoCa(nc, d.AddHours(1), d.AddHours(3));

        var r = await _kb.Khach().GetAsync("/api/ca?khuVuc=Khu%20TC08");
        await Dat(r, HttpStatusCode.OK);
        var noiDung = await r.Content.ReadAsStringAsync();
        Assert.Contains(ca, noiDung);
        Assert.DoesNotContain("BI_MAT_TC08", noiDung);
        Assert.DoesNotContain("huongDanRieng", noiDung, StringComparison.OrdinalIgnoreCase);

        var chiTiet = await _kb.Khach().GetStringAsync($"/api/ca/{ca}");
        Assert.DoesNotContain("BI_MAT_TC08", chiTiet);
    }

    [Fact(DisplayName = "TC09 - Điều phối viên tổ chức khác xem hướng dẫn riêng bị từ chối")]
    public async Task TC09_DieuPhoiToChucKhac_KhongXemDuoc()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), huongDanRieng: "BI_MAT_TC09");

        var r = await _kb.Khach("dieuphoi2").GetAsync($"/api/ca/{ca}/huong-dan-rieng");

        await Dat(r, HttpStatusCode.Forbidden);
        Assert.DoesNotContain("BI_MAT_TC09", await r.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "TC10 - Tình nguyện viên đã đăng ký nhưng chưa được phân công xem hướng dẫn riêng bị từ chối")]
    public async Task TC10_ChuaPhanCong_KhongXemDuoc()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), huongDanRieng: "BI_MAT_TC10");
        await _kb.DangKy("tnv3", ca);

        var r = await _kb.Khach("tnv3").GetAsync($"/api/ca/{ca}/huong-dan-rieng");

        await Dat(r, HttpStatusCode.Forbidden);
        Assert.DoesNotContain("BI_MAT_TC10", await r.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "TC11 - Tình nguyện viên đã được phân công xem hướng dẫn riêng trước giờ kết thúc ca")]
    public async Task TC11_DaPhanCong_TruocKetThuc_XemDuoc()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), huongDanRieng: "BI_MAT_TC11");
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);

        var r = await _kb.Khach("tnv1").GetAsync($"/api/ca/{ca}/huong-dan-rieng");

        await Dat(r, HttpStatusCode.OK);
        Assert.Contains("BI_MAT_TC11", await r.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "TC12 - Sau giờ kết thúc ca, hoặc khi ca bị hủy, tình nguyện viên không còn xem được hướng dẫn riêng")]
    public async Task TC12_SauKetThucHoacCaBiHuy_KhongXemDuoc()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), huongDanRieng: "BI_MAT_TC12");
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        _kb.DatGio(d.AddHours(3));
        await Dat(await _kb.Khach("tnv1").GetAsync($"/api/ca/{ca}/huong-dan-rieng"), HttpStatusCode.Forbidden);

        _kb.DatGio(DongHoGia.MocGoc.UtcDateTime);
        var caHuy = await _kb.TaoNhuCauVaCaAsync(d.AddHours(5), d.AddHours(7), huongDanRieng: "BI_MAT_TC12B");
        await _kb.DangKyVaPhanCongAsync("tnv1", caHuy);
        await Dat(await _kb.Khach("dieuphoi1").PostAsync($"/api/ca/{caHuy}/huy", null), HttpStatusCode.NoContent);
        var r = await _kb.Khach("tnv1").GetAsync($"/api/ca/{caHuy}/huong-dan-rieng");
        await Dat(r, HttpStatusCode.Forbidden);
        Assert.DoesNotContain("BI_MAT_TC12B", await r.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "TC13 - Đến giờ bắt đầu: ca Đang diễn ra, đăng ký chờ duyệt bị hủy, đăng ký đã phân công giữ nguyên")]
    public async Task TC13_DenGioBatDau_ChuyenTrangThai()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dkPhanCong = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        var dkCho = await _kb.DangKy("tnv3", ca);

        _kb.DatGio(d.AddHours(1).AddMinutes(1));
        using (var scope = _may.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<CaService>().ChuyenMocThoiGian();

        using var s2 = _may.Services.CreateScope();
        var kho = s2.ServiceProvider.GetRequiredService<KhoNghiepVu>();
        Assert.Equal(CaLam.DANG_DIEN_RA, await kho.CaLam.Where(c => c.MaCaLam == ca).Select(c => c.TrangThai).FirstAsync());
        Assert.Equal(DangKyCaLam.DA_HUY, await TrangThaiDangKyAsync(dkCho));
        Assert.Equal(DangKyCaLam.DA_PHAN_CONG, await TrangThaiDangKyAsync(dkPhanCong));
    }

    [Fact(DisplayName = "TC14 - Khai đóng góp trước khi ca kết thúc, hoặc thời gian ngoài khung ca, bị từ chối")]
    public async Task TC14_KhaiSaiThoiDiemHoacSaiGio_BiTuChoi()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv1", ca);

        _kb.DatGio(d.AddHours(2));
        await Dat(await _kb.GuiKhaiAsync("tnv1", dk, d.AddHours(1), d.AddHours(2)), HttpStatusCode.Conflict);

        _kb.DatGio(d.AddHours(4));
        await Dat(await _kb.GuiKhaiAsync("tnv1", dk, d.AddHours(0.5), d.AddHours(2)), HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC15 - Ca 08:00-10:00, khai 08:15-09:45 thì số giờ được tính là 1,5")]
    public async Task TC15_KhaiDongGop_TinhSoGio()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        _kb.DatGio(d.AddHours(4));
        await _kb.KhaiAsync("tnv1", dk, d.AddHours(1).AddMinutes(15), d.AddHours(2).AddMinutes(45));

        var json = await _kb.Khach("tnv1").GetFromJsonAsync<JsonElement>("/api/dong-gop/cua-toi");
        var ban = json.GetProperty("danhSach").EnumerateArray().First(x => x.GetProperty("maDangKy").GetString() == dk);
        Assert.Equal(1.5m, ban.GetProperty("soGio").GetDecimal());
    }

    [Fact(DisplayName = "TC16 - Điều phối viên duyệt bản khai của chính mình bị từ chối")]
    public async Task TC16_TuDuyetBanKhaiCuaMinh_BiTuChoi()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("dieuphoi_tnv", ca);
        _kb.DatGio(d.AddHours(4));
        var dg = await _kb.KhaiAsync("dieuphoi_tnv", dk, d.AddHours(1), d.AddHours(3));

        await Dat(await _kb.GuiDuyetAsync("dieuphoi_tnv", dg, xacNhan: true), HttpStatusCode.Forbidden);
        // Điều phối viên khác của tổ chức thì duyệt được.
        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dg, xacNhan: true), HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "TC17 - Trả lại bản khai mà không nhập lý do bị từ chối")]
    public async Task TC17_TraLaiKhongLyDo_BiTuChoi()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        _kb.DatGio(d.AddHours(4));
        var dg = await _kb.KhaiAsync("tnv1", dk, d.AddHours(1), d.AddHours(3));

        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dg, xacNhan: false, lyDo: ""), HttpStatusCode.BadRequest);
        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dg, xacNhan: false, lyDo: "Ghi rõ đã hướng dẫn những gì"),
            HttpStatusCode.NoContent);
        // Người khai sửa và gửi lại bản đang cần bổ sung.
        await Dat(await _kb.GuiKhaiAsync("tnv1", dk, d.AddHours(1), d.AddHours(3), "Hướng dẫn gọi video và đặt lịch khám"),
            HttpStatusCode.OK);
    }

    [Fact(DisplayName = "TC18 - Tổng giờ chỉ tính bản đã xác nhận, không tính bản chờ duyệt và bản cần bổ sung")]
    public async Task TC18_TongGio_ChiTinhBanDaXacNhan()
    {
        var d = NgayRieng();
        var ca1 = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(2));
        var ca2 = await _kb.TaoNhuCauVaCaAsync(d.AddHours(3), d.AddHours(5));
        var ca3 = await _kb.TaoNhuCauVaCaAsync(d.AddHours(6), d.AddHours(9));
        var dk1 = await _kb.DangKyVaPhanCongAsync("tnv3", ca1);
        var dk2 = await _kb.DangKyVaPhanCongAsync("tnv3", ca2);
        var dk3 = await _kb.DangKyVaPhanCongAsync("tnv3", ca3);
        var truoc = await _kb.TongGioAsync("tnv3");

        _kb.DatGio(d.AddHours(10));
        var dg1 = await _kb.KhaiAsync("tnv3", dk1, d.AddHours(1), d.AddHours(2));
        await _kb.KhaiAsync("tnv3", dk2, d.AddHours(3), d.AddHours(5));
        var dg3 = await _kb.KhaiAsync("tnv3", dk3, d.AddHours(6), d.AddHours(9));
        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dg1, true), HttpStatusCode.NoContent);
        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dg3, false, "Bổ sung mô tả"), HttpStatusCode.NoContent);

        Assert.Equal(truoc + 1.0m, await _kb.TongGioAsync("tnv3"));
    }

    [Fact(DisplayName = "TC19 - Bỏ kỹ năng đang cần cho ca đã phân công chưa kết thúc bị từ chối, kỹ năng cũ giữ nguyên")]
    public async Task TC19_BoKyNangDangCan_BiTuChoi()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), huongDanRieng: "x", kyNang: new[] { "KN-SO-CUU" });
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);

        var r = await _kb.Khach("tnv1").PutAsJsonAsync("/api/ho-so/ky-nang", new { kyNang = new[] { new { maKyNang = "KN-TIN-HOC", capDo = 2 } } });

        await Dat(r, HttpStatusCode.Conflict);
        Assert.Contains("Sơ cứu", await r.Content.ReadAsStringAsync());
        var hoSo = await _kb.Khach("tnv1").GetStringAsync("/api/ho-so");
        Assert.Contains("KN-SO-CUU", hoSo);
        Assert.Contains("KN-TIN-HOC", hoSo);
    }

    [Fact(DisplayName = "TC21 - Sửa thẳng trong CSDL để phân công vượt số chỗ thì trigger từ chối, dữ liệu giữ nguyên")]
    public async Task TC21_TriggerChanPhanCongVuotCho()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), soNguoiCan: 1);
        var dk1 = await _kb.DangKy("tnv1", ca);
        var dk3 = await _kb.DangKy("tnv3", ca);

        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "UPDATE dbo.DangKyCaLam SET TrangThai = 'DA_PHAN_CONG', MaNguoiPhanCong = 'ND-02', " +
            "NgayPhanCong = SYSUTCDATETIME() WHERE MaCaLam = @ca", ("@ca", ca)));

        Assert.Equal(50092, loi.Number);
        Assert.Equal(DangKyCaLam.CHO_DUYET, await TrangThaiDangKyAsync(dk1));
        Assert.Equal(DangKyCaLam.CHO_DUYET, await TrangThaiDangKyAsync(dk3));
    }

    [Fact(DisplayName = "TC22 - Sửa thẳng trong CSDL để tự xác nhận bản khai, hoặc sửa bản đã xác nhận, thì trigger từ chối")]
    public async Task TC22_TriggerChanTuDuyetVaSuaBanDaXacNhan()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKyVaPhanCongAsync("dieuphoi_tnv", ca);
        _kb.DatGio(d.AddHours(4));
        var dg = await _kb.KhaiAsync("dieuphoi_tnv", dk, d.AddHours(1), d.AddHours(3));

        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "UPDATE dbo.DongGop SET TrangThai = 'DA_XAC_NHAN', MaNguoiXacNhan = 'ND-07', " +
            "NgayXacNhan = SYSUTCDATETIME() WHERE MaDongGop = @dg", ("@dg", dg)));
        Assert.Equal(50141, loi.Number);

        await Dat(await _kb.GuiDuyetAsync("dieuphoi1", dg, xacNhan: true), HttpStatusCode.NoContent);
        loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "UPDATE dbo.DongGop SET KetThucThucTe = DATEADD(MINUTE, -30, KetThucThucTe) WHERE MaDongGop = @dg",
            ("@dg", dg)));
        Assert.Equal(50142, loi.Number);
    }

    [Fact(DisplayName = "TC23 - Tài khoản CSDL của ứng dụng không được xóa đăng ký, lịch sử được giữ")]
    public async Task TC23_TaiKhoanUngDungKhongDuocXoaLichSu()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3));
        var dk = await _kb.DangKy("tnv1", ca);

        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "DELETE FROM dbo.DangKyCaLam WHERE MaDangKy = @dk", ("@dk", dk)));

        Assert.Equal(229, loi.Number);
        Assert.Equal(DangKyCaLam.CHO_DUYET, await TrangThaiDangKyAsync(dk));
    }

    [Fact(DisplayName = "TC24 - Sửa thẳng trong CSDL số người cần, giờ hoặc kỹ năng của ca đã có người được phân công thì trigger từ chối")]
    public async Task TC24_TriggerChanSuaCaDaPhanCong()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), soNguoiCan: 2);
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        await _kb.DangKyVaPhanCongAsync("tnv3", ca);

        var loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "UPDATE dbo.CaLam SET SoNguoiCan = 1 WHERE MaCaLam = @ca", ("@ca", ca)));
        Assert.Equal(50111, loi.Number);

        loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "UPDATE dbo.CaLam SET BatDauDuKien = DATEADD(MINUTE, 30, BatDauDuKien) WHERE MaCaLam = @ca", ("@ca", ca)));
        Assert.Equal(50112, loi.Number);

        loi = await Assert.ThrowsAsync<SqlException>(() => _may.ChaySqlUngDungAsync(
            "INSERT INTO dbo.YeuCauKyNang (MaCaLam, MaKyNang) VALUES (@ca, 'KN-NAU-AN')", ("@ca", ca)));
        Assert.Equal(50113, loi.Number);

        // Tăng số người cần thì vẫn được.
        await _may.ChaySqlUngDungAsync("UPDATE dbo.CaLam SET SoNguoiCan = 3 WHERE MaCaLam = @ca", ("@ca", ca));
    }

    [Fact(DisplayName = "TC25 - Sửa ca: chưa có người được phân công thì sửa được; đã có thì không đổi được giờ, kỹ năng và không giảm dưới số đã phân công")]
    public async Task TC25_SuaCa_TheoDieuKien()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), soNguoiCan: 2);
        var dp = _kb.Khach("dieuphoi1");

        // Chưa ai được phân công: đổi giờ, số người và kỹ năng đều được.
        await Dat(await dp.PutAsJsonAsync($"/api/ca/{ca}", new
        {
            batDauDuKien = d.AddHours(2), ketThucDuKien = d.AddHours(4), soNguoiCan = 3, kyNang = YC("KN-TIN-HOC", "KN-SO-CUU")
        }), HttpStatusCode.NoContent);
        using (var scope = _may.Services.CreateScope())
        {
            var kho = scope.ServiceProvider.GetRequiredService<KhoNghiepVu>();
            var c = await kho.CaLam.Include(x => x.YeuCauKyNang).FirstAsync(x => x.MaCaLam == ca);
            Assert.Equal(d.AddHours(2), c.BatDauDuKien);
            Assert.Equal(3, c.SoNguoiCan);
            Assert.Equal(2, c.YeuCauKyNang.Count);
        }

        // tnv1 có đủ hai kỹ năng nên được phân công. Từ đây giờ và kỹ năng của ca bị khóa.
        await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        await Dat(await dp.PutAsJsonAsync($"/api/ca/{ca}", new
        {
            batDauDuKien = d.AddHours(1), ketThucDuKien = d.AddHours(4), soNguoiCan = 3, kyNang = YC("KN-TIN-HOC", "KN-SO-CUU")
        }), HttpStatusCode.Conflict);
        await Dat(await dp.PutAsJsonAsync($"/api/ca/{ca}", new
        {
            batDauDuKien = d.AddHours(2), ketThucDuKien = d.AddHours(4), soNguoiCan = 3, kyNang = YC("KN-TIN-HOC")
        }), HttpStatusCode.Conflict);
        await Dat(await dp.PutAsJsonAsync($"/api/ca/{ca}", new
        {
            batDauDuKien = d.AddHours(2), ketThucDuKien = d.AddHours(4), soNguoiCan = 0, kyNang = YC("KN-TIN-HOC", "KN-SO-CUU")
        }), HttpStatusCode.BadRequest);
        // Chỉ đổi số người cần (không thấp hơn số đã phân công) thì được.
        await Dat(await dp.PutAsJsonAsync($"/api/ca/{ca}", new
        {
            batDauDuKien = d.AddHours(2), ketThucDuKien = d.AddHours(4), soNguoiCan = 1, kyNang = YC("KN-TIN-HOC", "KN-SO-CUU")
        }), HttpStatusCode.NoContent);
        // Điều phối viên tổ chức khác không được sửa.
        await Dat(await _kb.Khach("dieuphoi2").PutAsJsonAsync($"/api/ca/{ca}", new
        {
            batDauDuKien = d.AddHours(2), ketThucDuKien = d.AddHours(4), soNguoiCan = 2, kyNang = YC("KN-TIN-HOC", "KN-SO-CUU")
        }), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "TC26 - Hủy phân công trước giờ bắt đầu thì chỗ được trả lại cho ca; sau giờ bắt đầu thì bị từ chối")]
    public async Task TC26_HuyPhanCong_TraLaiCho()
    {
        var d = NgayRieng();
        var ca = await _kb.TaoNhuCauVaCaAsync(d.AddHours(1), d.AddHours(3), soNguoiCan: 1);
        var dk1 = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        var dk3 = await _kb.DangKy("tnv3", ca);
        var dp = _kb.Khach("dieuphoi1");

        // Ca đã đủ người nên chưa phân công thêm được.
        await Dat(await _kb.GuiPhanCongAsync(dk3), HttpStatusCode.Conflict);
        // Đăng ký chờ duyệt không phải đối tượng của hủy phân công.
        await Dat(await dp.PostAsync($"/api/dang-ky/{dk3}/huy-phan-cong", null), HttpStatusCode.Conflict);

        await Dat(await dp.PostAsync($"/api/dang-ky/{dk1}/huy-phan-cong", null), HttpStatusCode.NoContent);
        Assert.Equal(DangKyCaLam.DA_HUY, await TrangThaiDangKyAsync(dk1));
        await Dat(await _kb.GuiPhanCongAsync(dk3), HttpStatusCode.NoContent);

        // Đến giờ bắt đầu thì không hủy phân công được nữa.
        _kb.DatGio(d.AddHours(1));
        await Dat(await dp.PostAsync($"/api/dang-ky/{dk3}/huy-phan-cong", null), HttpStatusCode.Conflict);
        Assert.Equal(DangKyCaLam.DA_PHAN_CONG, await TrangThaiDangKyAsync(dk3));
    }

    [Fact(DisplayName = "TC27 - Sửa nội dung công khai của nhu cầu đã công bố: chưa có ca thì về bản nháp, đã có ca thì bị từ chối")]
    public async Task TC27_SuaNhuCauDaCongBo()
    {
        var d = NgayRieng();
        var dd = _kb.Khach("daidien1");
        var nc = await _kb.TaoNhuCauCongBoAsync("Phát quà trung thu", "Điểm phát: sân nhà văn hóa");

        await Dat(await dd.PutAsJsonAsync($"/api/nhu-cau/{nc}", new
        {
            tieuDe = "Phát quà trung thu cho thiếu nhi", tomTatCongKhai = "Chia quà và tổ chức trò chơi", khuVucChung = "Phường An Bình"
        }), HttpStatusCode.NoContent);
        var sau = await dd.GetFromJsonAsync<JsonElement>($"/api/nhu-cau/{nc}");
        Assert.Equal("NHAP", sau.GetProperty("trangThai").GetString());
        Assert.Equal(2, sau.GetProperty("phienBanCongKhai").GetInt32());
        Assert.False(sau.GetProperty("daRaSoatCongKhai").GetBoolean());

        // Công bố lại rồi tạo ca: từ đây phần công khai không sửa được, hướng dẫn riêng vẫn sửa được.
        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{nc}/cong-bo", new { phienBan = 2 }), HttpStatusCode.NoContent);
        await _kb.TaoCa(nc, d.AddHours(1), d.AddHours(3));
        await Dat(await dd.PutAsJsonAsync($"/api/nhu-cau/{nc}", new
        {
            tieuDe = "Tiêu đề khác", tomTatCongKhai = "Chia quà và tổ chức trò chơi", khuVucChung = "Phường An Bình"
        }), HttpStatusCode.Conflict);
        await Dat(await dd.PutAsJsonAsync($"/api/nhu-cau/{nc}", new
        {
            tieuDe = "Phát quà trung thu cho thiếu nhi", tomTatCongKhai = "Chia quà và tổ chức trò chơi", khuVucChung = "Phường An Bình",
            huongDanRieng = "Điểm phát: sân nhà văn hóa, cổng sau"
        }), HttpStatusCode.NoContent);
        sau = await dd.GetFromJsonAsync<JsonElement>($"/api/nhu-cau/{nc}");
        Assert.Equal("CONG_BO", sau.GetProperty("trangThai").GetString());
        Assert.Contains("cổng sau", sau.GetProperty("huongDanRieng").GetString());
    }

    [Fact(DisplayName = "TC28 - Đóng nhu cầu: chưa có ca hoặc còn ca đang mở thì bị từ chối; mọi ca đã xong thì đóng được và không tạo thêm ca")]
    public async Task TC28_DongNhuCau()
    {
        var d = NgayRieng();
        var dd = _kb.Khach("daidien1");
        var nc = await _kb.TaoNhuCauCongBoAsync("Sửa bàn ghế lớp học tình thương", "Liên hệ thầy phụ trách khi đến");
        await Dat(await dd.PostAsync($"/api/nhu-cau/{nc}/dong", null), HttpStatusCode.Conflict);

        var ca = await _kb.TaoCa(nc, d.AddHours(1), d.AddHours(3));
        await Dat(await dd.PostAsync($"/api/nhu-cau/{nc}/dong", null), HttpStatusCode.Conflict);
        // Điều phối viên không phải người đóng nhu cầu.
        await Dat(await _kb.Khach("dieuphoi1").PostAsync($"/api/nhu-cau/{nc}/dong", null), HttpStatusCode.Forbidden);

        _kb.DatGio(d.AddHours(3).AddMinutes(1));
        using (var scope = _may.Services.CreateScope())
        {
            var dv = scope.ServiceProvider.GetRequiredService<CaService>();
            await dv.ChuyenMocThoiGian();
        }
        await Dat(await dd.PostAsync($"/api/nhu-cau/{nc}/dong", null), HttpStatusCode.NoContent);
        var sau = await dd.GetFromJsonAsync<JsonElement>($"/api/nhu-cau/{nc}");
        Assert.Equal("DONG", sau.GetProperty("trangThai").GetString());

        var r = await _kb.Khach("dieuphoi1").PostAsJsonAsync($"/api/nhu-cau/{nc}/ca", new
        {
            batDauDuKien = d.AddDays(1), ketThucDuKien = d.AddDays(1).AddHours(2), soNguoiCan = 1, kyNang = YC("KN-TIN-HOC")
        });
        await Dat(r, HttpStatusCode.Conflict);
        Assert.NotNull(ca);
    }

    [Fact(DisplayName = "TC29 - Hủy nhu cầu khi chưa có ca nào bắt đầu: ca và đăng ký còn hiệu lực bị hủy theo; đã có ca bắt đầu thì bị từ chối")]
    public async Task TC29_HuyNhuCau()
    {
        var d = NgayRieng();
        var dd = _kb.Khach("daidien1");
        var nc = await _kb.TaoNhuCauCongBoAsync("Trồng cây ven kênh", "Nhận cây giống tại trụ sở khu phố");
        var ca = await _kb.TaoCa(nc, d.AddHours(1), d.AddHours(3));
        var dkPhanCong = await _kb.DangKyVaPhanCongAsync("tnv1", ca);
        var dkCho = await _kb.DangKy("tnv3", ca);

        await Dat(await dd.PostAsync($"/api/nhu-cau/{nc}/huy", null), HttpStatusCode.NoContent);
        using (var scope = _may.Services.CreateScope())
        {
            var kho = scope.ServiceProvider.GetRequiredService<KhoNghiepVu>();
            Assert.Equal(NhuCauCongDong.DA_HUY, await kho.NhuCauCongDong.Where(n => n.MaNhuCau == nc).Select(n => n.TrangThai).FirstAsync());
            Assert.Equal(CaLam.DA_HUY, await kho.CaLam.Where(c => c.MaCaLam == ca).Select(c => c.TrangThai).FirstAsync());
        }
        Assert.Equal(DangKyCaLam.DA_HUY, await TrangThaiDangKyAsync(dkPhanCong));
        Assert.Equal(DangKyCaLam.DA_HUY, await TrangThaiDangKyAsync(dkCho));
        // Ca đã hủy không còn trong kết quả tìm ca công khai.
        await Dat(await _kb.Khach().GetAsync($"/api/ca/{ca}"), HttpStatusCode.NotFound);

        // Nhu cầu đã có ca bắt đầu thì không hủy được.
        var d2 = NgayRieng();
        _kb.DatGio(DongHoGia.MocGoc.UtcDateTime);
        var nc2 = await _kb.TaoNhuCauCongBoAsync("Trồng cây ven kênh đợt hai", "Nhận cây giống tại trụ sở khu phố");
        await _kb.TaoCa(nc2, d2.AddHours(1), d2.AddHours(3));
        _kb.DatGio(d2.AddHours(1).AddMinutes(5));
        await Dat(await dd.PostAsync($"/api/nhu-cau/{nc2}/huy", null), HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "TC30 - Người mới tự đăng ký: lần đăng nhập đầu tiên được lập tài khoản tình nguyện viên và một hồ sơ, không lập trùng; tên cập nhật theo Keycloak")]
    public async Task TC30_NguoiMoi_DuocLapTaiKhoanTinhNguyenVien()
    {
        var sub = Guid.NewGuid().ToString();
        using (var scope = _may.Services.CreateScope())
        {
            var dv = scope.ServiceProvider.GetRequiredService<AuthService>();
            Assert.True(await dv.XuLyCallback(sub, "Lý Thu Trang", "ly.thu.trang." + sub[..6], null));
        }
        using (var scope = _may.Services.CreateScope())
        {
            // Đăng nhập lần sau dùng lại tài khoản đã có; tên được cập nhật theo Keycloak.
            var dv = scope.ServiceProvider.GetRequiredService<AuthService>();
            Assert.True(await dv.XuLyCallback(sub, "Lý Thu Trang Anh", "ly.thu.trang." + sub[..6], null));
            var kho = scope.ServiceProvider.GetRequiredService<KhoNghiepVu>();
            var nd = await kho.NguoiDung.Include(x => x.VaiTro).SingleAsync(x => x.MaDinhDanhOIDC == sub);
            Assert.Equal("Lý Thu Trang Anh", nd.TenHienThi);
            Assert.Equal("ly.thu.trang." + sub[..6], nd.TenDangNhap);
            Assert.Equal(new[] { CacVaiTro.TinhNguyenVien }, nd.VaiTro.Select(v => v.VaiTro).ToArray());
            Assert.Equal(1, await kho.HoSoTinhNguyenVien.CountAsync(h => h.MaNguoiDung == nd.MaNguoiDung));
        }

        // Người mới dùng được chức năng của tình nguyện viên, không có quyền của đại diện hay điều phối viên.
        var khach = _kb.Khach();
        khach.DefaultRequestHeaders.Add(XacThucKiemThu.TieuDe, sub);
        var toi = await khach.GetFromJsonAsync<JsonElement>("/api/auth/toi");
        Assert.True(toi.GetProperty("laTinhNguyenVien").GetBoolean());
        await Dat(await khach.GetAsync("/api/dang-ky/cua-toi"), HttpStatusCode.OK);
        await Dat(await khach.GetAsync("/api/nhu-cau"), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "BR02 - Gọi chức năng cần đăng nhập khi chưa đăng nhập thì nhận mã 401")]
    public async Task BR02_ChuaDangNhap_Nhan401()
        => await Dat(await _kb.Khach().GetAsync("/api/dang-ky/cua-toi"), HttpStatusCode.Unauthorized);
}
