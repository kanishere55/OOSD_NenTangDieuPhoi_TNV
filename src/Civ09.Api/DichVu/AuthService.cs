using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;

namespace Civ09.Api.DichVu;

// Đăng nhập: đối chiếu danh tính do Keycloak cấp với tài khoản trên nền tảng (UC01, BR02).
public class AuthService
{
    private readonly KhoNghiepVu _kho;

    public AuthService(KhoNghiepVu kho) => _kho = kho;

    // Chỉ mở phiên khi danh tính đã gắn với một tài khoản còn hoạt động.
    public async Task<bool> XuLyCallback(string? sub)
    {
        if (string.IsNullOrEmpty(sub)) return false;
        var nd = await NguoiDung.TimTheoDanhTinh(_kho, sub);
        return nd is { DangHoatDong: true };
    }
}
