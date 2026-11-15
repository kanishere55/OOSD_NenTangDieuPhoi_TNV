using Civ09.Api.DichVu;
using Civ09.Api.HoTro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Tìm ca, gợi ý ca phù hợp, quản lý ca, đăng ký và rút đăng ký (UC04 đến UC07, UC17, màn hình M02, M03, M05, M08, M14).
[Route("api")]
public class CaBoundary : BoundaryCoSo
{
    private readonly CaService _dv;
    public CaBoundary(CaService dv) => _dv = dv;

    // Tìm ca công khai, không cần đăng nhập. Có vị trí thì lọc theo bán kính và xếp theo khoảng cách được.
    [AllowAnonymous, HttpGet("ca")]
    public async Task<IActionResult> TimCa([FromQuery] string? maKyNang, [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay, [FromQuery] string? khuVuc, [FromQuery] int trang = 1, [FromQuery] int kichThuoc = 10,
        [FromQuery] decimal? viDo = null, [FromQuery] decimal? kinhDo = null, [FromQuery] double? banKinhKm = null,
        [FromQuery] string? sapXep = null)
        => Ok(await _dv.TimCa(new BoLocCa(maKyNang, tuNgay, denNgay, khuVuc, trang, kichThuoc, viDo, kinhDo, banKinhKm, sapXep)));

    [Authorize, HttpGet("ca/phu-hop")]
    public async Task<IActionResult> XemCaPhuHop([FromQuery] int soLuong = 10)
        => Ok(await _dv.XemCaPhuHop(await NguoiDungAsync(), Math.Clamp(soLuong, 1, 30)));

    [AllowAnonymous, HttpGet("ca/{ma}")]
    public async Task<IActionResult> XemCa(string ma) => Ok(await _dv.XemCaCongKhai(ma));

    [Authorize, HttpGet("nhu-cau/{maNhuCau}/ca")]
    public async Task<IActionResult> XemCaCuaNhuCau(string maNhuCau)
        => Ok(await _dv.XemCaCuaNhuCau(await NguoiDungAsync(), maNhuCau));

    [Authorize, HttpPost("nhu-cau/{maNhuCau}/ca")]
    public async Task<IActionResult> TaoCa(string maNhuCau, [FromBody] CaNhap dl)
        => Ok(new { maCaLam = await _dv.TaoCa(await NguoiDungAsync(), maNhuCau, dl) });

    [Authorize, HttpPut("ca/{ma}")]
    public async Task<IActionResult> CapNhatCa(string ma, [FromBody] CaNhap dl)
    {
        await _dv.CapNhatCa(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }

    [Authorize, HttpPost("ca/{ma}/huy")]
    public async Task<IActionResult> HuyCa(string ma)
    {
        await _dv.HuyCa(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [Authorize, HttpPost("ca/{ma}/dang-ky")]
    public async Task<IActionResult> DangKy(string ma)
        => Ok(new { maDangKy = await _dv.DangKy(await NguoiDungAsync(), ma) });

    [Authorize, HttpPost("dang-ky/{ma}/rut")]
    public async Task<IActionResult> RutDangKy(string ma)
    {
        await _dv.RutDangKy(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [Authorize, HttpGet("dang-ky/cua-toi")]
    public async Task<IActionResult> XemDangKyCuaToi() => Ok(await _dv.XemDangKyCuaToi(await NguoiDungAsync()));
}
