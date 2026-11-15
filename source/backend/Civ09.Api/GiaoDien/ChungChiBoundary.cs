using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Xem và xác minh chứng chỉ kỹ năng (UC15, màn hình M21).
[Authorize, Route("api/chung-chi")]
public class ChungChiBoundary : BoundaryCoSo
{
    private readonly ChungChiService _dv;
    public ChungChiBoundary(ChungChiService dv) => _dv = dv;

    [HttpGet("cho-xac-minh")]
    public async Task<IActionResult> XemChoXacMinh() => Ok(await _dv.XemChoXacMinh(await NguoiDungAsync()));

    [HttpGet("{ma}/anh")]
    public async Task<IActionResult> XemAnh(string ma) => Anh(await _dv.XemAnh(await NguoiDungAsync(), ma), congKhai: false);

    [HttpPost("{ma}/xac-minh")]
    public async Task<IActionResult> XacMinh(string ma, [FromBody] XacMinhNhap dl)
    {
        await _dv.XacMinh(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }
}
