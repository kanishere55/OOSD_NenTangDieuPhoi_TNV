using Civ09.Api.DichVu;
using Civ09.Api.ThucThe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

public record KyNangCapNhat(List<KyNangKhai> KyNang);
public record LichRanhCapNhat(List<LichRanhNhap> LichRanh);

// Hồ sơ, kỹ năng, cấp độ, chứng chỉ, lịch rảnh, vị trí; đề xuất kỹ năng (UC02, UC13, UC15, UC16, màn hình M04).
[Route("api")]
public class HoSoBoundary : BoundaryCoSo
{
    private readonly HoSoService _dv;
    public HoSoBoundary(HoSoService dv) => _dv = dv;

    [Authorize, HttpGet("ho-so")]
    public async Task<IActionResult> XemHoSo() => Ok(await _dv.XemHoSo(await NguoiDungAsync()));

    [HttpGet("ky-nang")]
    public async Task<IActionResult> XemDanhMucKyNang() => Ok(await _dv.XemDanhMucKyNang());

    [Authorize, HttpPut("ho-so/ky-nang")]
    public async Task<IActionResult> CapNhatKyNang([FromBody] KyNangCapNhat dl)
    {
        await _dv.CapNhatKyNang(await NguoiDungAsync(), dl.KyNang ?? new());
        return NoContent();
    }

    [Authorize, HttpPut("ho-so/lich-ranh")]
    public async Task<IActionResult> CapNhatLichRanh([FromBody] LichRanhCapNhat dl)
    {
        await _dv.CapNhatLichRanh(await NguoiDungAsync(), dl.LichRanh ?? new());
        return NoContent();
    }

    [Authorize, HttpPut("ho-so/vi-tri")]
    public async Task<IActionResult> CapNhatViTri([FromBody] ViTriNhap dl)
    {
        await _dv.CapNhatViTri(await NguoiDungAsync(), dl);
        return NoContent();
    }

    [Authorize, HttpPost("ho-so/chung-chi/{maKyNang}")]
    public async Task<IActionResult> TaiChungChi(string maKyNang, IFormFile tep)
        => Ok(new { maChungChi = await _dv.TaiChungChi(await NguoiDungAsync(), maKyNang, tep) });

    [Authorize, HttpPost("ky-nang/de-xuat")]
    public async Task<IActionResult> DeXuatKyNang([FromBody] DeXuatNhap dl)
        => Ok(new { maDeXuat = await _dv.DeXuatKyNang(await NguoiDungAsync(), dl) });

    [Authorize, HttpGet("ky-nang/de-xuat/cua-toi")]
    public async Task<IActionResult> XemDeXuatCuaToi() => Ok(await _dv.XemDeXuatCuaToi(await NguoiDungAsync()));
}
