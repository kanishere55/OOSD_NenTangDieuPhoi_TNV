using System.Security.Claims;
using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.HoTro;

// Người dùng đang đăng nhập cùng vai trò và tư cách thành viên còn hoạt động.
public class NguoiDangNhap
{
    public string MaNguoiDung { get; init; } = "";
    public string TenHienThi { get; init; } = "";
    public string? TenDangNhap { get; init; }
    public HashSet<string> VaiTro { get; init; } = new();
    public List<(string MaToChuc, string TenToChuc, string VaiTro)> ThanhVien { get; init; } = new();
    public string? MaHoSo { get; init; }

    public bool LaQuanTri => VaiTro.Contains(CacVaiTro.QuanTri);

    public bool LaThanhVien(string maToChuc, string vaiTro)
        => ThanhVien.Any(t => t.MaToChuc == maToChuc && t.VaiTro == vaiTro);

    public bool LaThanhVienBatKy(string maToChuc)
        => ThanhVien.Any(t => t.MaToChuc == maToChuc);

    public IEnumerable<string> ToChucTheoVaiTro(string vaiTro)
        => ThanhVien.Where(t => t.VaiTro == vaiTro).Select(t => t.MaToChuc).Distinct();
}

// Kiểm tra quyền theo tài khoản, vai trò và tổ chức (BR02). Quyền nghiệp vụ lấy từ CSDL,
// không lấy từ token của Keycloak. Mọi lần xem dữ liệu nhạy cảm đều được ghi nhật ký (BR26).
public class ChinhSachTruyCap
{
    public const string LoaiClaimDinhDanh = "sub";
    private readonly KhoNghiepVu _kho;
    private readonly TimeProvider _thoiGian;

    public ChinhSachTruyCap(KhoNghiepVu kho, TimeProvider thoiGian)
    {
        _kho = kho; _thoiGian = thoiGian;
    }

    // Đọc người dùng từ phiên đăng nhập. Tài khoản phải còn hoạt động (BR28: tài khoản bị khóa không dùng được).
    public async Task<NguoiDangNhap> LayNguoiDungAsync(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(LoaiClaimDinhDanh) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(sub))
            throw new LoiNghiepVu(StatusCodes.Status401Unauthorized, "Bạn cần đăng nhập.");

        var nd = await NguoiDung.TimTheoDanhTinh(_kho, sub);
        if (nd == null || !nd.DangHoatDong)
            throw LoiNghiepVu.KhongDuQuyen("Tài khoản chưa được cấp hoặc đã bị khóa.");

        var maHoSo = await _kho.HoSoTinhNguyenVien.AsNoTracking()
            .Where(h => h.MaNguoiDung == nd.MaNguoiDung).Select(h => h.MaHoSo).FirstOrDefaultAsync();

        var vaiTro = nd.VaiTro.Select(v => v.VaiTro).ToHashSet();
        return new NguoiDangNhap
        {
            MaNguoiDung = nd.MaNguoiDung,
            TenHienThi = nd.TenHienThi,
            TenDangNhap = nd.TenDangNhap,
            VaiTro = vaiTro,
            // Chỉ giữ tư cách thành viên còn hoạt động, có vai trò tương ứng đã được cấp, ở tổ chức đang hoạt động.
            ThanhVien = nd.ThanhVien.Where(t => t.DangHoatDong && vaiTro.Contains(t.VaiTro)
                                                && t.ToChuc?.TrangThai == ToChucCongDong.HOAT_DONG)
                .Select(t => (t.MaToChuc, t.ToChuc?.TenToChuc ?? "", t.VaiTro)).ToList(),
            MaHoSo = vaiTro.Contains(CacVaiTro.TinhNguyenVien) ? maHoSo : null
        };
    }

    public void YeuCauTinhNguyenVien(NguoiDangNhap nd)
    {
        if (nd.MaHoSo == null)
            throw LoiNghiepVu.KhongDuQuyen("Chức năng này dành cho tình nguyện viên.");
    }

    public void YeuCauQuanTri(NguoiDangNhap nd)
    {
        if (!nd.LaQuanTri)
            throw LoiNghiepVu.KhongDuQuyen("Chức năng này dành cho quản trị viên.");
    }

    public void YeuCauThanhVien(NguoiDangNhap nd, string maToChuc, string vaiTro)
    {
        if (!nd.LaThanhVien(maToChuc, vaiTro))
            throw LoiNghiepVu.KhongDuQuyen(vaiTro == CacVaiTro.DaiDien
                ? "Chỉ đại diện đang hoạt động của tổ chức sở hữu mới được thực hiện."
                : "Chỉ điều phối viên đang hoạt động của tổ chức sở hữu mới được thực hiện.");
    }

    // BR15: đại diện và điều phối viên đang hoạt động của tổ chức sở hữu được xem hướng dẫn riêng;
    // tình nguyện viên chỉ được xem khi đã được phân công vào ca, ca chưa bị hủy và chưa qua giờ kết thúc.
    public async Task<bool> DuocXemRieng(NguoiDangNhap nd, DieuKienCa ca, DateTime bayGio)
    {
        if (DuocXemRiengCuaNhuCau(nd, ca.MaToChuc)) return true;
        if (nd.MaHoSo == null) return false;
        if (ca.TrangThaiCa == CaLam.DA_HUY || bayGio >= ca.KetThucDuKien) return false;
        return await _kho.DangKyCaLam.AnyAsync(d =>
            d.MaCaLam == ca.MaCaLam && d.MaHoSo == nd.MaHoSo && d.TrangThai == DangKyCaLam.DA_PHAN_CONG);
    }

    // BR15 ở mức nhu cầu: chỉ đại diện, điều phối viên đang hoạt động của tổ chức sở hữu.
    public bool DuocXemRiengCuaNhuCau(NguoiDangNhap nd, string maToChucSoHuu)
        => nd.LaThanhVien(maToChucSoHuu, CacVaiTro.DaiDien) || nd.LaThanhVien(maToChucSoHuu, CacVaiTro.DieuPhoi);

    // BR24: ảnh minh chứng chỉ người khai, đại diện và điều phối viên của tổ chức sở hữu ca được xem.
    public bool DuocXemAnhMinhChung(NguoiDangNhap nd, string maToChucSoHuu, string maHoSoNguoiKhai)
        => nd.MaHoSo == maHoSoNguoiKhai || DuocXemRiengCuaNhuCau(nd, maToChucSoHuu);

    // BR19: ảnh chứng chỉ chỉ chủ hồ sơ, quản trị viên và điều phối viên của tổ chức mà người đó đang có
    // đăng ký còn hiệu lực được xem và xác minh.
    public async Task<bool> DuocXemChungChi(NguoiDangNhap nd, string maHoSo)
    {
        if (nd.MaHoSo == maHoSo || nd.LaQuanTri) return true;
        var toChuc = nd.ToChucTheoVaiTro(CacVaiTro.DieuPhoi).ToList();
        if (toChuc.Count == 0) return false;
        return await _kho.DangKyCaLam.AnyAsync(d => d.MaHoSo == maHoSo
            && (d.TrangThai == DangKyCaLam.CHO_DUYET || d.TrangThai == DangKyCaLam.DA_PHAN_CONG)
            && toChuc.Contains(d.CaLam!.NhuCau!.MaToChuc));
    }

    // Người được xác minh chứng chỉ: như người được xem nhưng không phải chủ hồ sơ (BR19).
    public async Task<bool> DuocXacMinhChungChi(NguoiDangNhap nd, string maHoSo)
        => nd.MaHoSo != maHoSo && await DuocXemChungChi(nd, maHoSo);

    // BR26: ghi một lần xem dữ liệu nhạy cảm, kể cả lần bị từ chối. Lưu ngay, không phụ thuộc thao tác chính.
    public async Task GhiNhatKyAsync(NguoiDangNhap nd, string loaiDuLieu, string maDoiTuong, string? maToChuc, bool duocPhep)
    {
        _kho.NhatKyTruyCap.Add(new NhatKyTruyCap
        {
            MaNguoiXem = nd.MaNguoiDung, LoaiDuLieu = loaiDuLieu, MaDoiTuong = maDoiTuong, MaToChuc = maToChuc,
            ThoiDiem = _thoiGian.GetUtcNow().UtcDateTime, DuocPhep = duocPhep
        });
        await _kho.SaveChangesAsync();
    }
}
