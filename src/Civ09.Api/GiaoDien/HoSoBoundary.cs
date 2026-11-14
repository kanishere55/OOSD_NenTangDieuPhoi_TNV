using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

public record KyNangCapNhat(List<string> KyNang);

// Hồ sơ và kỹ năng của tình nguyện viên (UC02, màn hình M04).
[Route("api")]
public class HoSoBoundary : BoundaryCoSo
{
    private readonly HoSoService _dv;
    public HoSoBoundary(HoSoService dv) => _dv = dv;

    [Authorize, HttpGet("ho-so")]
    public async Task<IActionResult> XemHoSo() => Ok(await _dv.XemHoSo(await NguoiDungAsync()));

    [HttpGet("ky-nang")]
    public async Task<IActionResult> XemDanhMucKyNang() => Ok(await _dv.XemDanhMucKyNang());

    [Authorize, HttpPut("ho-so/ky-nang")]
    public async Task<IActionResult> CapNhatKyNang([FromBody] KyNangCapNhat dl)
    {
        await _dv.CapNhatKyNang(await NguoiDungAsync(), dl.KyNang ?? new());
        return NoContent();
    }
}
