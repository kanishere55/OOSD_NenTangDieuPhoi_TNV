using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Quản trị danh mục kỹ năng và duyệt đề xuất (UC14, màn hình M20).
[Authorize, Route("api/quan-tri")]
public class KyNangBoundary : BoundaryCoSo
{
    private readonly KyNangService _dv;
    public KyNangBoundary(KyNangService dv) => _dv = dv;

    [HttpGet("ky-nang")]
    public async Task<IActionResult> XemDanhMuc() => Ok(await _dv.XemDanhMuc(await NguoiDungAsync()));

    [HttpPost("ky-nang")]
    public async Task<IActionResult> TaoKyNang([FromBody] KyNangNhap dl)
        => Ok(new { maKyNang = await _dv.TaoKyNang(await NguoiDungAsync(), dl) });

    [HttpPut("ky-nang/{ma}")]
    public async Task<IActionResult> SuaKyNang(string ma, [FromBody] KyNangNhap dl)
    {
        await _dv.SuaKyNang(await NguoiDungAsync(), ma, dl);
        return NoContent();
    }

    [HttpPost("nhom-ky-nang")]
    public async Task<IActionResult> TaoNhom([FromBody] NhomNhap dl)
        => Ok(new { maNhom = await _dv.TaoNhom(await NguoiDungAsync(), dl) });

    [HttpGet("de-xuat-ky-nang")]
    public async Task<IActionResult> XemDeXuat() => Ok(await _dv.XemDeXuat(await NguoiDungAsync()));

    [HttpPost("de-xuat-ky-nang/{ma}/duyet")]
    public async Task<IActionResult> DuyetDeXuat(string ma, [FromBody] KyNangNhap dl)
        => Ok(new { maKyNang = await _dv.DuyetDeXuat(await NguoiDungAsync(), ma, dl) });

    [HttpPost("de-xuat-ky-nang/{ma}/dung-co-san")]
    public async Task<IActionResult> DungKyNangCoSan(string ma, [FromBody] DungKyNangNhap dl)
    {
        await _dv.DungKyNangCoSan(await NguoiDungAsync(), ma, dl.MaKyNang);
        return NoContent();
    }

    [HttpPost("de-xuat-ky-nang/{ma}/tu-choi")]
    public async Task<IActionResult> TuChoiDeXuat(string ma, [FromBody] LyDoNhap dl)
    {
        await _dv.TuChoiDeXuat(await NguoiDungAsync(), ma, dl.LyDo);
        return NoContent();
    }
}
