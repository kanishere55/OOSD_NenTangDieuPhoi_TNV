using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Khai, duyệt đóng góp và xem tổng giờ (UC09, UC10, UC12, màn hình M10, M11, M13).
[Authorize, Route("api")]
public class DongGopBoundary : BoundaryCoSo
{
    private readonly DongGopService _dv;
    public DongGopBoundary(DongGopService dv) => _dv = dv;

    [HttpPost("dang-ky/{maDangKy}/dong-gop")]
    public async Task<IActionResult> GuiDongGop(string maDangKy, [FromBody] DongGopNhap dl)
        => Ok(new { maDongGop = await _dv.GuiDongGop(await NguoiDungAsync(), maDangKy, dl) });

    [HttpGet("dong-gop/cho-duyet")]
    public async Task<IActionResult> XemDongGopChoDuyet() => Ok(await _dv.XemDongGopChoDuyet(await NguoiDungAsync()));

    [HttpPost("dong-gop/{ma}/duyet")]
    public async Task<IActionResult> DuyetDongGop(string ma, [FromBody] QuyetDinhDuyet qd)
    {
        await _dv.DuyetDongGop(await NguoiDungAsync(), ma, qd);
        return NoContent();
    }

    [HttpGet("dong-gop/cua-toi")]
    public async Task<IActionResult> XemDongGopCuaToi() => Ok(await _dv.XemDongGopCuaToi(await NguoiDungAsync()));
}
