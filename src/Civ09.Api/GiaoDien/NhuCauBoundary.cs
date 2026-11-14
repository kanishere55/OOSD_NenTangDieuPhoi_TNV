using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Quản lý nhu cầu của tổ chức (UC03, màn hình M06, M07).
[Authorize, Route("api/nhu-cau")]
public class NhuCauBoundary : BoundaryCoSo
{
    private readonly NhuCauService _dv;
    public NhuCauBoundary(NhuCauService dv) => _dv = dv;

    [HttpGet]
    public async Task<IActionResult> XemNhuCau() => Ok(await _dv.XemNhuCau(await NguoiDungAsync()));

    [HttpGet("{ma}")]
    public async Task<IActionResult> XemChiTiet(string ma) => Ok(await _dv.XemChiTiet(await NguoiDungAsync(), ma));

    [HttpPost]
    public async Task<IActionResult> TaoNhuCau([FromBody] NhuCauNhap dl)
        => Ok(new { maNhuCau = await _dv.TaoNhuCau(await NguoiDungAsync(), dl) });

    [HttpPut("{ma}")]
    public async Task<IActionResult> CapNhatNhuCau(string ma, [FromBody] NhuCauSua dl)
    {
        await _dv.CapNhatNhuCau(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }

    [HttpPost("{ma}/cong-bo")]
    public async Task<IActionResult> CongBoNhuCau(string ma, [FromBody] PhienBanNhap dl)
    {
        await _dv.CongBoNhuCau(await NguoiDungAsync(), ma, dl.PhienBan);
        return NoContent();
    }

    [HttpPost("{ma}/dong")]
    public async Task<IActionResult> DongNhuCau(string ma)
    {
        await _dv.DongNhuCau(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("{ma}/huy")]
    public async Task<IActionResult> HuyNhuCau(string ma)
    {
        await _dv.HuyNhuCau(await NguoiDungAsync(), ma);
        return NoContent();
    }
}
