using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Xem hướng dẫn điều phối riêng của ca (UC11, màn hình M12).
[Authorize, Route("api")]
public class ChiTietBoundary : BoundaryCoSo
{
    private readonly ChiTietService _dv;
    public ChiTietBoundary(ChiTietService dv) => _dv = dv;

    [HttpGet("ca/{maCaLam}/huong-dan-rieng")]
    public async Task<IActionResult> XemChiTietRieng(string maCaLam)
        => Ok(await _dv.XemChiTietRieng(await NguoiDungAsync(), maCaLam));
}
