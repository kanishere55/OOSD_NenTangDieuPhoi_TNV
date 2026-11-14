using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;

namespace Civ09.Api.DichVu;

// Xem thông tin điều phối riêng (UC11, BR15): xác định nhu cầu từ ca, kiểm tra quyền trước,
// chỉ khi được phép mới đọc bảng ThongTinDieuPhoiRieng.
public class ChiTietService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly TimeProvider _thoiGian;

    public ChiTietService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _thoiGian = thoiGian;
    }

    public async Task<object> XemChiTietRieng(NguoiDangNhap nd, string maCa)
    {
        var ca = await CaLam.LayDieuKien(_kho, maCa);
        if (!await _chinhSach.DuocXemRieng(nd, ca, _thoiGian.GetUtcNow().UtcDateTime))
            throw LoiNghiepVu.KhongDuQuyen(
                "Bạn không có quyền xem hướng dẫn riêng của ca này. Chỉ thành viên của tổ chức sở hữu và người đã " +
                "được phân công (trước giờ kết thúc ca) mới được xem.");

        var rieng = await ThongTinDieuPhoiRieng.LayNoiDung(_kho, ca.MaNhuCau);
        return new { ca.MaCaLam, ca.TieuDe, ca.BatDauDuKien, ca.KetThucDuKien, HuongDanRieng = rieng?.HuongDanRieng ?? "" };
    }
}
