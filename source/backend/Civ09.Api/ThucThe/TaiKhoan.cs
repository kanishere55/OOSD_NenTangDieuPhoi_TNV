using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.ThucThe;

// Vai trò được cấp cho người dùng (bảng NguoiDungVaiTro, ThanhVienToChuc).
public static class CacVaiTro
{
    public const string TinhNguyenVien = "TNV";
    public const string DaiDien = "DAI_DIEN";
    public const string DieuPhoi = "DIEU_PHOI";
    public const string QuanTri = "QUAN_TRI";
}

// Tài khoản người dùng, mỗi dòng ứng với một danh tính đăng nhập trên Keycloak.
public class NguoiDung
{
    public string MaNguoiDung { get; set; } = Guid.NewGuid().ToString();
    public string MaDinhDanhOIDC { get; set; } = "";
    public string TenHienThi { get; set; } = "";
    public string? TenDangNhap { get; set; }
    public string? Email { get; set; }
    public bool DangHoatDong { get; set; } = true;
    public DateTime NgayTao { get; set; }

    public List<NguoiDungVaiTro> VaiTro { get; set; } = new();
    public List<ThanhVienToChuc> ThanhVien { get; set; } = new();

    public bool CoVaiTro(string vaiTro) => VaiTro.Any(v => v.VaiTro == vaiTro);

    // Tìm tài khoản theo mã danh tính (subject) do Keycloak cấp, kèm vai trò và tư cách thành viên.
    public static Task<NguoiDung?> TimTheoDanhTinh(KhoNghiepVu kho, string subject)
        => kho.NguoiDung.AsNoTracking()
            .Include(x => x.VaiTro)
            .Include(x => x.ThanhVien).ThenInclude(t => t.ToChuc)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.MaDinhDanhOIDC == subject);

    // Tìm người dùng theo tên đăng nhập hoặc email, dùng khi đại diện thêm điều phối viên (BR28).
    public static Task<NguoiDung?> TimTheoTenDangNhap(KhoNghiepVu kho, string tuKhoa)
    {
        var t = tuKhoa.Trim();
        return kho.NguoiDung.Include(x => x.VaiTro)
            .FirstOrDefaultAsync(x => x.TenDangNhap == t || x.Email == t);
    }

    // BR02: người mới tự đăng ký ở Keycloak được lập tài khoản với vai trò tình nguyện viên và một hồ sơ.
    // Vai trò đại diện, điều phối viên không tạo ở đây mà do tổ chức cấp.
    public static async Task<NguoiDung> TaoTinhNguyenVien(KhoNghiepVu kho, string subject, string? tenHienThi,
        string? tenDangNhap, string? email, DateTime thoiDiem)
    {
        var nd = new NguoiDung { MaDinhDanhOIDC = subject, NgayTao = thoiDiem };
        nd.GanThongTinDangNhap(tenHienThi, tenDangNhap, email);
        nd.VaiTro.Add(new NguoiDungVaiTro { MaNguoiDung = nd.MaNguoiDung, VaiTro = CacVaiTro.TinhNguyenVien });
        kho.NguoiDung.Add(nd);
        await kho.SaveChangesAsync();
        // Hồ sơ tham chiếu vai trò tình nguyện viên vừa lưu ở trên nên được lưu sau.
        kho.HoSoTinhNguyenVien.Add(new HoSoTinhNguyenVien { MaNguoiDung = nd.MaNguoiDung, NgayCapNhatKyNang = thoiDiem });
        await kho.SaveChangesAsync();
        return nd;
    }

    // Tên, tên đăng nhập và email lấy theo Keycloak ở mỗi lần đăng nhập.
    public void GanThongTinDangNhap(string? tenHienThi, string? tenDangNhap, string? email)
    {
        var ten = (tenHienThi ?? "").Trim();
        if (ten.Length == 0) ten = string.IsNullOrWhiteSpace(TenHienThi) ? "Tình nguyện viên mới" : TenHienThi;
        TenHienThi = ten.Length > 100 ? ten[..100] : ten;
        if (!string.IsNullOrWhiteSpace(tenDangNhap)) TenDangNhap = tenDangNhap.Trim()[..Math.Min(100, tenDangNhap.Trim().Length)];
        if (!string.IsNullOrWhiteSpace(email)) Email = email.Trim()[..Math.Min(200, email.Trim().Length)];
    }

    // BR28: quản trị viên khóa hoặc mở khóa tài khoản, không tự khóa mình.
    public void Khoa(string maNguoiThucHien)
    {
        if (maNguoiThucHien == MaNguoiDung) throw LoiNghiepVu.XungDot("Bạn không thể tự khóa tài khoản của mình.");
        if (!DangHoatDong) throw LoiNghiepVu.XungDot("Tài khoản này đang bị khóa.");
        DangHoatDong = false;
    }

    public void MoKhoa()
    {
        if (DangHoatDong) throw LoiNghiepVu.XungDot("Tài khoản này đang hoạt động.");
        DangHoatDong = true;
    }

    // Cấp vai trò nếu người dùng chưa có.
    public void CapVaiTro(string vaiTro)
    {
        if (!CoVaiTro(vaiTro)) VaiTro.Add(new NguoiDungVaiTro { MaNguoiDung = MaNguoiDung, VaiTro = vaiTro });
    }
}

public class NguoiDungVaiTro
{
    public string MaNguoiDung { get; set; } = "";
    public string VaiTro { get; set; } = "";
}

// Tổ chức cộng đồng. Tổ chức mới do người dùng đăng ký và chờ quản trị viên duyệt (BR28).
public class ToChucCongDong
{
    public const string CHO_DUYET = "CHO_DUYET";
    public const string HOAT_DONG = "HOAT_DONG";
    public const string TU_CHOI = "TU_CHOI";
    public const string NGUNG_HOAT_DONG = "NGUNG_HOAT_DONG";

    public string MaToChuc { get; set; } = Guid.NewGuid().ToString();
    public string TenToChuc { get; set; } = "";
    public string? MoTa { get; set; }
    public string? KhuVucHoatDong { get; set; }
    public string? SoDienThoai { get; set; }
    public string TrangThai { get; set; } = HOAT_DONG;
    public string? MaNguoiDangKy { get; set; }
    public DateTime? NgayDangKy { get; set; }
    public string? MaNguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }

    public NguoiDung? NguoiDangKy { get; set; }

    public static ToChucCongDong DangKy(string maNguoiDangKy, string ten, string? moTa, string? khuVuc, string? soDienThoai,
        DateTime thoiDiem)
    {
        ten = (ten ?? "").Trim();
        if (ten.Length == 0) throw LoiNghiepVu.KhongHopLe("Hãy nhập tên tổ chức.");
        if (ten.Length > 150) throw LoiNghiepVu.KhongHopLe("Tên tổ chức tối đa 150 ký tự.");
        if (string.IsNullOrWhiteSpace(khuVuc)) throw LoiNghiepVu.KhongHopLe("Hãy nhập khu vực hoạt động của tổ chức.");
        if (string.IsNullOrWhiteSpace(soDienThoai)) throw LoiNghiepVu.KhongHopLe("Hãy nhập số điện thoại liên hệ để quản trị viên xác minh.");
        var so = new string(soDienThoai.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (so.Length < 8 || so.Length > 15) throw LoiNghiepVu.KhongHopLe("Số điện thoại chưa đúng.");
        return new ToChucCongDong
        {
            TenToChuc = ten,
            MoTa = CatChuoi(moTa, 500),
            KhuVucHoatDong = CatChuoi(khuVuc, 150),
            SoDienThoai = so,
            TrangThai = CHO_DUYET,
            MaNguoiDangKy = maNguoiDangKy,
            NgayDangKy = thoiDiem
        };
    }

    public void Duyet(string maQuanTri, DateTime thoiDiem)
    {
        if (TrangThai != CHO_DUYET) throw LoiNghiepVu.XungDot("Đăng ký này đã được xử lý.");
        TrangThai = HOAT_DONG;
        MaNguoiDuyet = maQuanTri;
        NgayDuyet = thoiDiem;
    }

    public void TuChoi(string maQuanTri, string? lyDo, DateTime thoiDiem)
    {
        if (string.IsNullOrWhiteSpace(lyDo)) throw LoiNghiepVu.KhongHopLe("Khi từ chối phải ghi lý do.");
        if (TrangThai != CHO_DUYET) throw LoiNghiepVu.XungDot("Đăng ký này đã được xử lý.");
        TrangThai = TU_CHOI;
        LyDoTuChoi = CatChuoi(lyDo, 500);
        MaNguoiDuyet = maQuanTri;
        NgayDuyet = thoiDiem;
    }

    public void NgungHoatDong()
    {
        if (TrangThai != HOAT_DONG) throw LoiNghiepVu.XungDot("Chỉ ngừng được tổ chức đang hoạt động.");
        TrangThai = NGUNG_HOAT_DONG;
    }

    public void MoLai()
    {
        if (TrangThai != NGUNG_HOAT_DONG) throw LoiNghiepVu.XungDot("Chỉ mở lại được tổ chức đã ngừng hoạt động.");
        TrangThai = HOAT_DONG;
    }

    internal static string? CatChuoi(string? s, int toiDa)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > toiDa ? s[..toiDa] : s;
    }
}

// Vai trò của người dùng trong một tổ chức.
public class ThanhVienToChuc
{
    public string MaNguoiDung { get; set; } = "";
    public string MaToChuc { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public bool DangHoatDong { get; set; } = true;

    public ToChucCongDong? ToChuc { get; set; }
    public NguoiDung? NguoiDung { get; set; }
}

// Hồ sơ của tình nguyện viên: kỹ năng đã khai, lịch rảnh và vị trí gần đúng nơi ở.
public class HoSoTinhNguyenVien
{
    public string MaHoSo { get; set; } = Guid.NewGuid().ToString();
    public string MaNguoiDung { get; set; } = "";
    public string VaiTro { get; set; } = CacVaiTro.TinhNguyenVien;
    public DateTime NgayCapNhatKyNang { get; set; }
    public string? NoiO { get; set; }
    public decimal? ViDo { get; set; }
    public decimal? KinhDo { get; set; }
    public int? KhoangCachToiDa { get; set; }

    public NguoiDung? NguoiDung { get; set; }
    public List<KyNangSoHuu> KyNang { get; set; } = new();
    public List<LichRanh> LichRanh { get; set; } = new();

    public IEnumerable<string> LayKyNang() => KyNang.Select(k => k.MaKyNang);

    // BR03, BR18: mỗi kỹ năng chỉ khai một lần, kèm cấp độ.
    public void CapNhatKyNang(IEnumerable<KyNangKhai> ds, DateTime thoiDiem)
    {
        var moi = ds.GroupBy(k => k.MaKyNang).ToDictionary(g => g.Key, g => g.Last().CapDo);
        foreach (var capDo in moi.Values)
            if (!MucCapDo.HopLe(capDo)) throw LoiNghiepVu.KhongHopLe("Cấp độ kỹ năng chưa đúng.");
        KyNang.RemoveAll(k => !moi.ContainsKey(k.MaKyNang));
        foreach (var k in KyNang) k.CapDo = moi[k.MaKyNang];
        foreach (var (ma, capDo) in moi.Where(m => KyNang.All(k => k.MaKyNang != m.Key)))
            KyNang.Add(new KyNangSoHuu { MaHoSo = MaHoSo, MaKyNang = ma, CapDo = capDo, NgayKhaiBao = thoiDiem });
        NgayCapNhatKyNang = thoiDiem;
    }

    public void CapNhatLichRanh(IEnumerable<(int Thu, string Buoi)> ds)
    {
        var moi = ds.Distinct().ToList();
        if (moi.Any(l => l.Thu < 1 || l.Thu > 7 || !Buoi.HopLe(l.Buoi)))
            throw LoiNghiepVu.KhongHopLe("Lịch rảnh chưa đúng.");
        LichRanh.RemoveAll(l => !moi.Contains((l.Thu, l.Buoi)));
        foreach (var (thu, buoi) in moi.Where(m => !LichRanh.Any(l => l.Thu == m.Thu && l.Buoi == m.Buoi)))
            LichRanh.Add(new LichRanh { MaHoSo = MaHoSo, Thu = (byte)thu, Buoi = buoi });
    }

    // BR21: vị trí chỉ dùng để tính khoảng cách.
    public void CapNhatViTri(string? noiO, decimal? viDo, decimal? kinhDo, int? khoangCachToiDa)
    {
        if ((viDo == null) != (kinhDo == null)) throw LoiNghiepVu.KhongHopLe("Vị trí chưa đầy đủ.");
        if (viDo != null) ViTri.KiemTra(viDo.Value, kinhDo!.Value);
        if (khoangCachToiDa is < 1 or > 100) throw LoiNghiepVu.KhongHopLe("Khoảng cách tối đa từ 1 đến 100 km.");
        NoiO = ToChucCongDong.CatChuoi(noiO, 150);
        ViDo = viDo == null ? null : Math.Round(viDo.Value, 6);
        KinhDo = kinhDo == null ? null : Math.Round(kinhDo.Value, 6);
        KhoangCachToiDa = khoangCachToiDa;
    }
}

public record KyNangKhai(string MaKyNang, int CapDo);

// Lịch rảnh theo tuần: Thu 1 là thứ hai ... 7 là chủ nhật.
public class LichRanh
{
    public string MaHoSo { get; set; } = "";
    public byte Thu { get; set; }
    public string Buoi { get; set; } = "";
}

public static class Buoi
{
    public const string SANG = "SANG";
    public const string CHIEU = "CHIEU";
    public const string TOI = "TOI";
    public static bool HopLe(string b) => b is SANG or CHIEU or TOI;

    // Thứ và buổi của một thời điểm UTC theo giờ Việt Nam.
    public static (int Thu, string Buoi) Cua(DateTime utc)
    {
        var vn = utc.AddHours(7);
        var thu = ((int)vn.DayOfWeek + 6) % 7 + 1;
        var buoi = vn.Hour < 12 ? SANG : vn.Hour < 18 ? CHIEU : TOI;
        return (thu, buoi);
    }
}

public static class MucCapDo
{
    public const int CoBan = 1, Kha = 2, ThanhThao = 3;
    public static bool HopLe(int capDo) => capDo is >= 1 and <= 3;
    public static string Ten(int capDo) => capDo switch { 2 => "Khá", 3 => "Thành thạo", _ => "Cơ bản" };
}

public class NhomKyNang
{
    public string MaNhom { get; set; } = Guid.NewGuid().ToString();
    public string TenNhom { get; set; } = "";
    public int ThuTu { get; set; }
}

public class KyNang
{
    public string MaKyNang { get; set; } = Guid.NewGuid().ToString();
    public string TenKyNang { get; set; } = "";
    public string? MoTaKyNang { get; set; }
    public string? MaNhom { get; set; }
    public bool CanChungChi { get; set; }
    public bool DangSuDung { get; set; } = true;
    public string? TenChuanHoa { get; set; }

    public NhomKyNang? Nhom { get; set; }

    public static string ChuanHoa(string ten) => (ten ?? "").Trim().ToLowerInvariant();

    // BR17: tên không trống, không quá 100 ký tự.
    public void GanThongTin(string ten, string? moTa, string? maNhom, bool canChungChi)
    {
        ten = (ten ?? "").Trim();
        if (ten.Length == 0) throw LoiNghiepVu.KhongHopLe("Hãy nhập tên kỹ năng.");
        if (ten.Length > 100) throw LoiNghiepVu.KhongHopLe("Tên kỹ năng tối đa 100 ký tự.");
        TenKyNang = ten;
        MoTaKyNang = ToChucCongDong.CatChuoi(moTa, 300);
        MaNhom = string.IsNullOrWhiteSpace(maNhom) ? null : maNhom;
        CanChungChi = canChungChi;
    }
}

public class KyNangSoHuu
{
    public string MaHoSo { get; set; } = "";
    public string MaKyNang { get; set; } = "";
    public int CapDo { get; set; } = MucCapDo.CoBan;
    public DateTime NgayKhaiBao { get; set; }

    public KyNang? KyNang { get; set; }
}
