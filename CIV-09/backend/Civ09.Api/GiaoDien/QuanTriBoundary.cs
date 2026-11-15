using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Trang quản trị: duyệt tổ chức, khóa tài khoản, xem nhật ký truy cập (UC21, UC23, UC24, màn hình M20).
[Authorize, Route("api/quan-tri")]
public class QuanTriBoundary : BoundaryCoSo
{
    private readonly QuanTriService _dv;
    public QuanTriBoundary(QuanTriService dv) => _dv = dv;

    [HttpGet("tong-quan")]
    public async Task<IActionResult> TongQuan() => Ok(await _dv.TongQuan(await NguoiDungAsync()));

    [HttpGet("to-chuc")]
    public async Task<IActionResult> XemToChuc() => Ok(await _dv.XemToChuc(await NguoiDungAsync()));

    [HttpPost("to-chuc/{ma}/duyet")]
    public async Task<IActionResult> DuyetToChuc(string ma)
    {
        await _dv.DuyetToChuc(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("to-chuc/{ma}/tu-choi")]
    public async Task<IActionResult> TuChoiToChuc(string ma, [FromBody] LyDoNhap dl)
    {
        await _dv.TuChoiToChuc(await NguoiDungAsync(), ma, dl.LyDo);
        return NoContent();
    }

    [HttpPost("to-chuc/{ma}/ngung")]
    public async Task<IActionResult> NgungToChuc(string ma)
    {
        await _dv.NgungToChuc(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("to-chuc/{ma}/mo-lai")]
    public async Task<IActionResult> MoLaiToChuc(string ma)
    {
        await _dv.MoLaiToChuc(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpGet("nguoi-dung")]
    public async Task<IActionResult> XemNguoiDung([FromQuery] string? tuKhoa)
        => Ok(await _dv.XemNguoiDung(await NguoiDungAsync(), tuKhoa));

    [HttpPost("nguoi-dung/{ma}/khoa")]
    public async Task<IActionResult> KhoaTaiKhoan(string ma)
    {
        await _dv.KhoaTaiKhoan(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("nguoi-dung/{ma}/mo-khoa")]
    public async Task<IActionResult> MoKhoaTaiKhoan(string ma)
    {
        await _dv.MoKhoaTaiKhoan(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpGet("nhat-ky")]
    public async Task<IActionResult> XemNhatKy([FromQuery] string? loai, [FromQuery] int trang = 1)
        => Ok(await _dv.XemNhatKy(await NguoiDungAsync(), loai, trang));
}
