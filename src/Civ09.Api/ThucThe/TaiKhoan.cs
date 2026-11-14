using Civ09.Api.DuLieu;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.ThucThe;

// Vai trò được cấp cho người dùng (bảng NguoiDungVaiTro, ThanhVienToChuc).
public static class CacVaiTro
{
    public const string TinhNguyenVien = "TNV";
    public const string DaiDien = "DAI_DIEN";
    public const string DieuPhoi = "DIEU_PHOI";
}

// Tài khoản người dùng, mỗi dòng ứng với một danh tính đăng nhập trên Keycloak.
public class NguoiDung
{
    public string MaNguoiDung { get; set; } = Guid.NewGuid().ToString();
    public string MaDinhDanhOIDC { get; set; } = "";
    public string TenHienThi { get; set; } = "";
    public bool DangHoatDong { get; set; } = true;

    public List<NguoiDungVaiTro> VaiTro { get; set; } = new();
    public List<ThanhVienToChuc> ThanhVien { get; set; } = new();

    // Tìm tài khoản theo mã danh tính (subject) do Keycloak cấp, kèm vai trò và tư cách thành viên.
    public static Task<NguoiDung?> TimTheoDanhTinh(KhoNghiepVu kho, string subject)
        => kho.NguoiDung.AsNoTracking()
            .Include(x => x.VaiTro)
            .Include(x => x.ThanhVien).ThenInclude(t => t.ToChuc)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.MaDinhDanhOIDC == subject);

    // BR02: người mới tự đăng ký ở Keycloak được lập tài khoản với vai trò tình nguyện viên và một hồ sơ.
    // Vai trò đại diện, điều phối viên không tạo ở đây mà do tổ chức cấp.
    public static async Task<NguoiDung> TaoTinhNguyenVien(KhoNghiepVu kho, string subject, string? tenHienThi, DateTime thoiDiem)
    {
        var ten = (tenHienThi ?? "").Trim();
        if (ten.Length == 0) ten = "Tình nguyện viên mới";
        if (ten.Length > 100) ten = ten[..100];
        var nd = new NguoiDung { MaDinhDanhOIDC = subject, TenHienThi = ten };
        nd.VaiTro.Add(new NguoiDungVaiTro { MaNguoiDung = nd.MaNguoiDung, VaiTro = CacVaiTro.TinhNguyenVien });
        kho.NguoiDung.Add(nd);
        await kho.SaveChangesAsync();
        // Hồ sơ tham chiếu vai trò tình nguyện viên vừa lưu ở trên nên được lưu sau.
        kho.HoSoTinhNguyenVien.Add(new HoSoTinhNguyenVien { MaNguoiDung = nd.MaNguoiDung, NgayCapNhatKyNang = thoiDiem });
        await kho.SaveChangesAsync();
        return nd;
    }
}

public class NguoiDungVaiTro
{
    public string MaNguoiDung { get; set; } = "";
    public string VaiTro { get; set; } = "";
}

public class ToChucCongDong
{
    public string MaToChuc { get; set; } = Guid.NewGuid().ToString();
    public string TenToChuc { get; set; } = "";
}

// Vai trò của người dùng trong một tổ chức.
public class ThanhVienToChuc
{
    public string MaNguoiDung { get; set; } = "";
    public string MaToChuc { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public bool DangHoatDong { get; set; } = true;

    public ToChucCongDong? ToChuc { get; set; }
}

// Hồ sơ của tình nguyện viên và các kỹ năng đã khai.
public class HoSoTinhNguyenVien
{
    public string MaHoSo { get; set; } = Guid.NewGuid().ToString();
    public string MaNguoiDung { get; set; } = "";
    public string VaiTro { get; set; } = CacVaiTro.TinhNguyenVien;
    public DateTime NgayCapNhatKyNang { get; set; }

    public NguoiDung? NguoiDung { get; set; }
    public List<KyNangSoHuu> KyNang { get; set; } = new();

    public IEnumerable<string> LayKyNang() => KyNang.Select(k => k.MaKyNang);

    // BR03: mỗi kỹ năng chỉ khai một lần.
    public void CapNhatKyNang(IEnumerable<string> ds, DateTime thoiDiem)
    {
        var moi = ds.Distinct().ToHashSet();
        KyNang.RemoveAll(k => !moi.Contains(k.MaKyNang));
        foreach (var ma in moi.Where(m => KyNang.All(k => k.MaKyNang != m)))
            KyNang.Add(new KyNangSoHuu { MaHoSo = MaHoSo, MaKyNang = ma, NgayKhaiBao = thoiDiem });
        NgayCapNhatKyNang = thoiDiem;
    }
}

public class KyNang
{
    public string MaKyNang { get; set; } = Guid.NewGuid().ToString();
    public string TenKyNang { get; set; } = "";
    public string? MoTaKyNang { get; set; }
    public string? TenChuanHoa { get; set; }
}

public class KyNangSoHuu
{
    public string MaHoSo { get; set; } = "";
    public string MaKyNang { get; set; } = "";
    public DateTime NgayKhaiBao { get; set; }

    public KyNang? KyNang { get; set; }
}
