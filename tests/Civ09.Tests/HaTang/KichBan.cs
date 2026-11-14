using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Civ09.Tests.HaTang;

// Các bước dựng dữ liệu dùng chung cho kiểm thử tích hợp, đi qua API như người dùng thật.
public class KichBan
{
    private static int _soNgay;
    private readonly MayChuKiemThu _may;

    public KichBan(MayChuKiemThu may)
    {
        _may = may;
        _may.DongHo.BayGio = DongHoGia.MocGoc;
    }

    public HttpClient Khach(string? ten = null) => _may.MayKhach(ten);

    // Mỗi kiểm thử dùng một ngày riêng (theo giờ UTC) để ca của các kiểm thử không trùng lịch nhau.
    public static DateTime NgayRieng()
        => DongHoGia.MocGoc.UtcDateTime.AddDays(10 + 3 * Interlocked.Increment(ref _soNgay));

    public void DatGio(DateTime utc) => _may.DongHo.BayGio = new DateTimeOffset(utc, TimeSpan.Zero);

    public async Task<string> TaoNhuCauCongBoAsync(string tieuDe, string huongDanRieng, string khuVuc = "Phường An Bình")
    {
        var dd = Khach("daidien1");
        var r = await dd.PostAsJsonAsync("/api/nhu-cau", new
        {
            maToChuc = "TC-A", tieuDe, tomTatCongKhai = "Tóm tắt công khai của " + tieuDe, khuVucChung = khuVuc, huongDanRieng
        });
        await Dat(r, HttpStatusCode.OK);
        var ma = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNhuCau").GetString()!;
        await Dat(await dd.PostAsJsonAsync($"/api/nhu-cau/{ma}/cong-bo", new { phienBan = 1 }), HttpStatusCode.NoContent);
        return ma;
    }

    public async Task<string> TaoCa(string maNhuCau, DateTime batDau, DateTime ketThuc, int soNguoiCan = 2,
        params string[] kyNang)
    {
        if (kyNang.Length == 0) kyNang = new[] { "KN-TIN-HOC" };
        var r = await Khach("dieuphoi1").PostAsJsonAsync($"/api/nhu-cau/{maNhuCau}/ca", new
        {
            batDauDuKien = batDau, ketThucDuKien = ketThuc, soNguoiCan, kyNang
        });
        await Dat(r, HttpStatusCode.OK);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maCaLam").GetString()!;
    }

    public async Task<string> TaoNhuCauVaCaAsync(DateTime batDau, DateTime ketThuc, int soNguoiCan = 2,
        string huongDanRieng = "Hướng dẫn riêng", params string[] kyNang)
    {
        var nc = await TaoNhuCauCongBoAsync("Nhu cầu kiểm thử " + Guid.NewGuid().ToString("N")[..6], huongDanRieng);
        return await TaoCa(nc, batDau, ketThuc, soNguoiCan, kyNang);
    }

    public Task<HttpResponseMessage> GuiDangKyAsync(string nguoi, string maCa)
        => Khach(nguoi).PostAsync($"/api/ca/{maCa}/dang-ky", null);

    public async Task<string> DangKy(string nguoi, string maCa)
    {
        var r = await GuiDangKyAsync(nguoi, maCa);
        await Dat(r, HttpStatusCode.OK);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDangKy").GetString()!;
    }

    public Task<HttpResponseMessage> GuiPhanCongAsync(string maDangKy, string nguoi = "dieuphoi1")
        => Khach(nguoi).PostAsync($"/api/dang-ky/{maDangKy}/phan-cong", null);

    public async Task<string> DangKyVaPhanCongAsync(string nguoi, string maCa)
    {
        var dk = await DangKy(nguoi, maCa);
        await Dat(await GuiPhanCongAsync(dk), HttpStatusCode.NoContent);
        return dk;
    }

    public Task<HttpResponseMessage> GuiKhaiAsync(string nguoi, string maDangKy, DateTime batDau, DateTime ketThuc,
        string moTa = "Hướng dẫn các cô chú dùng điện thoại")
        => Khach(nguoi).PostAsJsonAsync($"/api/dang-ky/{maDangKy}/dong-gop",
            new { batDauThucTe = batDau, ketThucThucTe = ketThuc, moTaCongViec = moTa });

    public async Task<string> KhaiAsync(string nguoi, string maDangKy, DateTime batDau, DateTime ketThuc)
    {
        var r = await GuiKhaiAsync(nguoi, maDangKy, batDau, ketThuc);
        await Dat(r, HttpStatusCode.OK);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDongGop").GetString()!;
    }

    public Task<HttpResponseMessage> GuiDuyetAsync(string nguoi, string maDongGop, bool xacNhan, string? lyDo = null)
        => Khach(nguoi).PostAsJsonAsync($"/api/dong-gop/{maDongGop}/duyet", new { xacNhan, lyDoTraLai = lyDo });

    public async Task<decimal> TongGioAsync(string nguoi)
    {
        var json = await Khach(nguoi).GetFromJsonAsync<JsonElement>("/api/dong-gop/cua-toi");
        return json.GetProperty("tongGioDaXacNhan").GetDecimal();
    }

    // Kiểm tra mã trạng thái; nếu sai thì in kèm nội dung phản hồi để dễ tìm lỗi.
    public static async Task Dat(HttpResponseMessage r, HttpStatusCode mongDoi)
    {
        if (r.StatusCode != mongDoi)
            Assert.Fail($"Mong đợi {(int)mongDoi} nhưng nhận {(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
    }
}
