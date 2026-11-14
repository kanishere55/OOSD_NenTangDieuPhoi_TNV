using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.ThucThe;

// Đăng ký của tình nguyện viên vào một ca và kết quả phân công.
public class DangKyCaLam
{
    public const string CHO_DUYET = "CHO_DUYET";
    public const string DA_PHAN_CONG = "DA_PHAN_CONG";
    public const string TU_CHOI = "TU_CHOI";
    public const string DA_RUT = "DA_RUT";
    public const string DA_HUY = "DA_HUY";

    public string MaDangKy { get; set; } = Guid.NewGuid().ToString();
    public string MaHoSo { get; set; } = "";
    public string MaCaLam { get; set; } = "";
    public DateTime NgayDangKy { get; set; }
    public string TrangThai { get; set; } = CHO_DUYET;
    public string? MaNguoiPhanCong { get; set; }
    public DateTime? NgayPhanCong { get; set; }

    public HoSoTinhNguyenVien? HoSo { get; set; }
    public CaLam? CaLam { get; set; }
    public DongGop? DongGop { get; set; }

    public bool ConHieuLuc => TrangThai == CHO_DUYET || TrangThai == DA_PHAN_CONG;

    // BR07, BR08: đăng ký mới ở trạng thái Chờ duyệt và chưa giữ chỗ.
    public static DangKyCaLam TaoDangKy(string maHoSo, string maCaLam, DateTime bayGio) => new()
    {
        MaHoSo = maHoSo,
        MaCaLam = maCaLam,
        NgayDangKy = bayGio
    };

    // Đọc một đăng ký (có theo dõi thay đổi) kèm ca của nó.
    public static async Task<DangKyCaLam> LayThongTin(KhoNghiepVu kho, string maDangKy)
        => await kho.DangKyCaLam.Include(d => d.CaLam).FirstOrDefaultAsync(d => d.MaDangKy == maDangKy)
           ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy đăng ký.");

    // Đăng ký còn hiệu lực (Chờ duyệt hoặc Đã phân công) của một hồ sơ trên một ca, nếu có.
    public static Task<DangKyCaLam?> LayThongTin(KhoNghiepVu kho, string maHoSo, string maCaLam)
        => kho.DangKyCaLam.AsNoTracking().FirstOrDefaultAsync(d => d.MaHoSo == maHoSo && d.MaCaLam == maCaLam
            && (d.TrangThai == CHO_DUYET || d.TrangThai == DA_PHAN_CONG));

    // Các đăng ký đã phân công của một hồ sơ, kèm ca (ca chưa bị hủy).
    public static Task<List<DangKyCaLam>> LayDaPhanCong(KhoNghiepVu kho, string maHoSo)
        => kho.DangKyCaLam.AsNoTracking().Include(d => d.CaLam)
            .Where(d => d.MaHoSo == maHoSo && d.TrangThai == DA_PHAN_CONG && d.CaLam!.TrangThai != CaLam.DA_HUY)
            .ToListAsync();

    // BR09: hồ sơ đã được phân công một ca khác giao về thời gian với ca đang xét hay chưa.
    public static async Task<bool> KiemTraLich(KhoNghiepVu kho, string maHoSo, DieuKienCa ca)
        => (await LayDaPhanCong(kho, maHoSo)).Any(d => d.MaCaLam != ca.MaCaLam
            && GiaoNhau(d.CaLam!.BatDauDuKien, d.CaLam.KetThucDuKien, ca.BatDauDuKien, ca.KetThucDuKien));

    // Hai khoảng thời gian giao nhau. Ca sau bắt đầu đúng lúc ca trước kết thúc thì không giao.
    public static bool GiaoNhau(DateTime batDauA, DateTime ketThucA, DateTime batDauB, DateTime ketThucB)
        => batDauA < ketThucB && batDauB < ketThucA;

    public void PhanCong(string maNguoiPhanCong, DateTime thoiDiem)
    {
        if (TrangThai != CHO_DUYET)
            throw LoiNghiepVu.XungDot("Chỉ phân công đăng ký đang chờ duyệt.");
        TrangThai = DA_PHAN_CONG;
        MaNguoiPhanCong = maNguoiPhanCong;
        NgayPhanCong = thoiDiem;
    }

    public void TuChoi()
    {
        if (TrangThai != CHO_DUYET)
            throw LoiNghiepVu.XungDot("Chỉ từ chối đăng ký đang chờ duyệt.");
        TrangThai = TU_CHOI;
    }

    public void Rut()
    {
        if (!ConHieuLuc)
            throw LoiNghiepVu.XungDot("Đăng ký này không còn hiệu lực để rút.");
        TrangThai = DA_RUT;
    }

    public void Huy()
    {
        if (ConHieuLuc) TrangThai = DA_HUY;
    }
}
