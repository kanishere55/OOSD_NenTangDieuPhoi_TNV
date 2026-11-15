using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Đăng ký tổ chức, thành viên, nhật ký truy cập và kỹ năng đang thiếu của tổ chức (UC20, UC22, UC24, UC25,
// màn hình M18, M19).
[Authorize, Route("api/to-chuc")]
public class ToChucBoundary : BoundaryCoSo
{
    private readonly ToChucService _dv;
    public ToChucBoundary(ToChucService dv) => _dv = dv;

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKyToChuc([FromBody] DangKyToChucNhap dl)
        => Ok(new { maToChuc = await _dv.DangKyToChuc(await NguoiDungAsync(), dl) });

    [HttpGet("dang-ky-cua-toi")]
    public async Task<IActionResult> XemDangKyCuaToi() => Ok(await _dv.XemDangKyCuaToi(await NguoiDungAsync()));

    [HttpGet("{ma}/thanh-vien")]
    public async Task<IActionResult> XemThanhVien(string ma) => Ok(await _dv.XemThanhVien(await NguoiDungAsync(), ma));

    [HttpPost("{ma}/thanh-vien")]
    public async Task<IActionResult> ThemThanhVien(string ma, [FromBody] ThanhVienNhap dl)
    {
        await _dv.ThemThanhVien(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }

    [HttpPost("{ma}/thanh-vien/{maNguoiDung}/ngung")]
    public async Task<IActionResult> NgungThanhVien(string ma, string maNguoiDung, [FromQuery] string vaiTro)
    {
        await _dv.NgungThanhVien(await NguoiDungAsync(), ma, maNguoiDung, vaiTro);
        return NoContent();
    }

    [HttpGet("{ma}/nhat-ky")]
    public async Task<IActionResult> XemNhatKy(string ma, [FromQuery] int trang = 1)
        => Ok(await _dv.XemNhatKy(await NguoiDungAsync(), ma, trang));

    [HttpGet("{ma}/ky-nang-thieu")]
    public async Task<IActionResult> ThongKeKyNangThieu(string ma) => Ok(await _dv.ThongKeKyNangThieu(await NguoiDungAsync(), ma));
}
