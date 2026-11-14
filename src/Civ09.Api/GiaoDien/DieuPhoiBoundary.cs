using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Xét duyệt đăng ký: phân công, từ chối (UC08, màn hình M09).
[Authorize, Route("api")]
public class DieuPhoiBoundary : BoundaryCoSo
{
    private readonly DieuPhoiService _dv;
    public DieuPhoiBoundary(DieuPhoiService dv) => _dv = dv;

    [HttpGet("ca/{maCaLam}/dang-ky")]
    public async Task<IActionResult> GoiYUngVien(string maCaLam)
        => Ok(await _dv.GoiYUngVien(await NguoiDungAsync(), maCaLam));

    [HttpPost("dang-ky/{ma}/phan-cong")]
    public async Task<IActionResult> PhanCong(string ma)
    {
        await _dv.PhanCong(await NguoiDungAsync(), ma);
        return NoContent();
    }

    [HttpPost("dang-ky/{ma}/tu-choi")]
    public async Task<IActionResult> TuChoi(string ma)
    {
        await _dv.TuChoi(await NguoiDungAsync(), ma);
        return NoContent();
    }
}
