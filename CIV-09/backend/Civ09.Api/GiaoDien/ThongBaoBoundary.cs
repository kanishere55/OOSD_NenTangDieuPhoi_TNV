using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Thông báo trên web (UC19, màn hình M15).
[Authorize, Route("api/thong-bao")]
public class ThongBaoBoundary : BoundaryCoSo
{
    private readonly ThongBaoService _dv;
    public ThongBaoBoundary(ThongBaoService dv) => _dv = dv;

    [HttpGet]
    public async Task<IActionResult> XemThongBao([FromQuery] bool chiChuaDoc = false)
        => Ok(await _dv.XemThongBao(await NguoiDungAsync(), chiChuaDoc));

    [HttpGet("so-chua-doc")]
    public async Task<IActionResult> SoChuaDoc() => Ok(new { soChuaDoc = await _dv.SoChuaDoc(await NguoiDungAsync()) });

    [HttpPost("{ma}/da-doc")]
    public async Task<IActionResult> DanhDauDaDoc(string ma)
    {
        await _dv.DanhDauDaDoc(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("da-doc-het")]
    public async Task<IActionResult> DanhDauTatCa()
    {
        await _dv.DanhDauTatCa(await NguoiDungAsync());
        return NoContent();
    }
}
