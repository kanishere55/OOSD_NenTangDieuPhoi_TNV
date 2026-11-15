using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Điểm danh tại ca (UC18, màn hình M16, M17).
[Authorize, Route("api")]
public class DiemDanhBoundary : BoundaryCoSo
{
    private readonly DiemDanhService _dv;
    public DiemDanhBoundary(DiemDanhService dv) => _dv = dv;

    [HttpGet("ca/{ma}/diem-danh")]
    public async Task<IActionResult> XemDiemDanh(string ma) => Ok(await _dv.XemDiemDanhCuaCa(await NguoiDungAsync(), ma));

    [HttpGet("ca/{ma}/diem-danh/ma-qr")]
    public async Task<IActionResult> LayMaQr(string ma) => Ok(await _dv.LayMaQr(await NguoiDungAsync(), ma, DiaChiGoc));

    [HttpPost("ca/{ma}/diem-danh")]
    public async Task<IActionResult> QuetMa(string ma, [FromBody] QuetMaNhap dl)
        => Ok(await _dv.QuetMa(await NguoiDungAsync(), ma, dl.Ma));

    [HttpPost("dang-ky/{ma}/co-mat")]
    public async Task<IActionResult> GhiNhanCoMat(string ma, [FromBody] CoMatNhap dl)
    {
        await _dv.GhiNhanCoMat(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }

    [HttpPost("dang-ky/{ma}/vang-mat")]
    public async Task<IActionResult> GhiNhanVang(string ma)
    {
        await _dv.GhiNhanVang(await NguoiDungAsync(), ma);
        return NoContent();
    }
}
