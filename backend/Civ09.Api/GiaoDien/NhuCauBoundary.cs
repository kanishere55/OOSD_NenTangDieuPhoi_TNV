using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Quản lý nhu cầu của tổ chức, ảnh và địa điểm (UC03, màn hình M06, M07).
[Authorize, Route("api")]
public class NhuCauBoundary : BoundaryCoSo
{
    private readonly NhuCauService _dv;
    public NhuCauBoundary(NhuCauService dv) => _dv = dv;

    [HttpGet("nhu-cau")]
    public async Task<IActionResult> XemNhuCau() => Ok(await _dv.XemNhuCau(await NguoiDungAsync()));

    [HttpGet("nhu-cau/{ma}")]
    public async Task<IActionResult> XemChiTiet(string ma) => Ok(await _dv.XemChiTiet(await NguoiDungAsync(), ma));

    [HttpPost("nhu-cau")]
    public async Task<IActionResult> TaoNhuCau([FromBody] NhuCauNhap dl)
        => Ok(new { maNhuCau = await _dv.TaoNhuCau(await NguoiDungAsync(), dl) });

    [HttpPut("nhu-cau/{ma}")]
    public async Task<IActionResult> CapNhatNhuCau(string ma, [FromBody] NhuCauSua dl)
    {
        await _dv.CapNhatNhuCau(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }

    [HttpPost("nhu-cau/{ma}/cong-bo")]
    public async Task<IActionResult> CongBoNhuCau(string ma, [FromBody] PhienBanNhap dl)
    {
        await _dv.CongBoNhuCau(await NguoiDungAsync(), ma, dl.PhienBan);
        return NoContent();
    }

    [HttpPost("nhu-cau/{ma}/dong")]
    public async Task<IActionResult> DongNhuCau(string ma)
    {
        await _dv.DongNhuCau(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("nhu-cau/{ma}/huy")]
    public async Task<IActionResult> HuyNhuCau(string ma)
    {
        await _dv.HuyNhuCau(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("nhu-cau/{ma}/anh")]
    public async Task<IActionResult> ThemAnh(string ma, IFormFile tep)
        => Ok(new { maTep = await _dv.ThemAnh(await NguoiDungAsync(), ma, tep) });

    [HttpPost("nhu-cau/{ma}/anh/{maTep}/bo")]
    public async Task<IActionResult> BoAnh(string ma, string maTep)
    {
        await _dv.BoAnh(await NguoiDungAsync(), ma, maTep);
        return NoContent();
    }

    // Ảnh nhu cầu đã công bố ai cũng xem được.
    [AllowAnonymous, HttpGet("anh/nhu-cau/{maTep}")]
    public async Task<IActionResult> XemAnh(string maTep)
    {
        var nd = await NguoiDungNeuCoAsync();
        var tep = await _dv.XemAnh(nd, maTep);
        return Anh(tep, congKhai: nd == null);
    }
}
