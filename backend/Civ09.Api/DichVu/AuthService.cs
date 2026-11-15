using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

// Đăng nhập: đối chiếu danh tính do Keycloak cấp với tài khoản trên nền tảng (UC01, BR02).
public class AuthService
{
    private readonly KhoNghiepVu _kho;
    private readonly TimeProvider _thoiGian;

    public AuthService(KhoNghiepVu kho, TimeProvider thoiGian)
    {
        _kho = kho; _thoiGian = thoiGian;
    }

    // Chỉ mở phiên khi danh tính gắn với một tài khoản còn hoạt động. Danh tính chưa có tài khoản là của
    // người vừa tự đăng ký ở Keycloak: lập cho họ tài khoản tình nguyện viên kèm một hồ sơ.
    // Tên, tên đăng nhập và email được cập nhật theo Keycloak ở mỗi lần đăng nhập.
    public async Task<bool> XuLyCallback(string? sub, string? tenHienThi = null, string? tenDangNhap = null, string? email = null)
    {
        if (string.IsNullOrEmpty(sub)) return false;
        var nd = await _kho.NguoiDung.FirstOrDefaultAsync(x => x.MaDinhDanhOIDC == sub);
        if (nd != null)
        {
            if (!nd.DangHoatDong) return false;
            nd.GanThongTinDangNhap(tenHienThi, tenDangNhap, email);
            try { await _kho.SaveChangesAsync(); }
            catch (DbUpdateException) { _kho.ChangeTracker.Clear(); }
            return true;
        }

        await using var gd = await _kho.Database.BeginTransactionAsync();
        try
        {
            await NguoiDung.TaoTinhNguyenVien(_kho, sub, tenHienThi, tenDangNhap, email, _thoiGian.GetUtcNow().UtcDateTime);
            await gd.CommitAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Hai yêu cầu đăng nhập đầu tiên đến cùng lúc: yêu cầu kia đã lập tài khoản.
            await gd.RollbackAsync();
            _kho.ChangeTracker.Clear();
            return await NguoiDung.TimTheoDanhTinh(_kho, sub) is { DangHoatDong: true };
        }
    }
}
