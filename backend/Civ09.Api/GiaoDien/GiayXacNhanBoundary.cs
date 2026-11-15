using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Giấy xác nhận giờ tình nguyện và trang kiểm tra công khai (UC26, UC27, màn hình M13, M22).
[Route("api")]
public class GiayXacNhanBoundary : BoundaryCoSo
{
    private readonly GiayXacNhanService _dv;
    public GiayXacNhanBoundary(GiayXacNhanService dv) => _dv = dv;

    [Authorize, HttpPost("giay-xac-nhan")]
    public async Task<IActionResult> CapGiay([FromBody] GiayNhap dl)
        => Ok(new { maGiay = await _dv.CapGiay(await NguoiDungAsync(), dl) });

    [Authorize, HttpGet("giay-xac-nhan/cua-toi")]
    public async Task<IActionResult> XemCuaToi() => Ok(await _dv.XemCuaToi(await NguoiDungAsync()));

    [Authorize, HttpGet("giay-xac-nhan/{ma}/pdf")]
    public async Task<IActionResult> TaiPdf(string ma)
    {
        var (pdf, ten) = await _dv.TaoPdf(await NguoiDungAsync(), ma, DiaChiGoc);
        Response.Headers.CacheControl = "private, no-store";
        return File(pdf, "application/pdf", ten);
    }

    [AllowAnonymous, HttpGet("kiem-tra-giay/{maKiemTra}")]
    public async Task<IActionResult> KiemTra(string maKiemTra) => Ok(await _dv.KiemTra(maKiemTra));
}
