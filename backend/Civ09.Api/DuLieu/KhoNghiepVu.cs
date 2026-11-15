using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Civ09.Api.DuLieu;

// Đọc và lưu dữ liệu trong SQL Server. Ánh xạ vào lược đồ có sẵn trong thư mục database (không tự sinh bảng)
// và gọi các thủ tục lưu trữ của CSDL.
public class KhoNghiepVu : DbContext
{
    public KhoNghiepVu(DbContextOptions<KhoNghiepVu> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDung => Set<NguoiDung>();
    public DbSet<NguoiDungVaiTro> NguoiDungVaiTro => Set<NguoiDungVaiTro>();
    public DbSet<ToChucCongDong> ToChucCongDong => Set<ToChucCongDong>();
    public DbSet<ThanhVienToChuc> ThanhVienToChuc => Set<ThanhVienToChuc>();
    public DbSet<HoSoTinhNguyenVien> HoSoTinhNguyenVien => Set<HoSoTinhNguyenVien>();
    public DbSet<LichRanh> LichRanh => Set<LichRanh>();
    public DbSet<NhomKyNang> NhomKyNang => Set<NhomKyNang>();
    public DbSet<KyNang> KyNang => Set<KyNang>();
    public DbSet<DeXuatKyNang> DeXuatKyNang => Set<DeXuatKyNang>();
    public DbSet<KyNangSoHuu> KyNangSoHuu => Set<KyNangSoHuu>();
    public DbSet<ChungChiKyNang> ChungChiKyNang => Set<ChungChiKyNang>();
    public DbSet<TepDinhKem> TepDinhKem => Set<TepDinhKem>();
    public DbSet<NhuCauCongDong> NhuCauCongDong => Set<NhuCauCongDong>();
    public DbSet<AnhNhuCau> AnhNhuCau => Set<AnhNhuCau>();
    public DbSet<ThongTinDieuPhoiRieng> ThongTinDieuPhoiRieng => Set<ThongTinDieuPhoiRieng>();
    public DbSet<CaLam> CaLam => Set<CaLam>();
    public DbSet<YeuCauKyNang> YeuCauKyNang => Set<YeuCauKyNang>();
    public DbSet<DangKyCaLam> DangKyCaLam => Set<DangKyCaLam>();
    public DbSet<DiemDanh> DiemDanh => Set<DiemDanh>();
    public DbSet<DongGop> DongGop => Set<DongGop>();
    public DbSet<AnhMinhChung> AnhMinhChung => Set<AnhMinhChung>();
    public DbSet<GiayXacNhan> GiayXacNhan => Set<GiayXacNhan>();
    public DbSet<GiayXacNhanDongGop> GiayXacNhanDongGop => Set<GiayXacNhanDongGop>();
    public DbSet<ThongBao> ThongBao => Set<ThongBao>();
    public DbSet<NhatKyTruyCap> NhatKyTruyCap => Set<NhatKyTruyCap>();
    public DbSet<CaCongKhai> CaCongKhai => Set<CaCongKhai>();
    public DbSet<MucTinCay> MucTinCay => Set<MucTinCay>();

    // Gọi sp_ChuyenMocThoiGianCa (BR12).
    public async Task<KetQuaChuyenMoc> ChuyenMocThoiGianCaAsync(DateTime bayGio)
        => (await Database.SqlQuery<KetQuaChuyenMoc>($"EXEC dbo.sp_ChuyenMocThoiGianCa @BayGio = {bayGio}")
            .ToListAsync()).Single();

    // Gọi sp_TaoNhacTruocCa (BR25): số thông báo nhắc vừa tạo.
    public async Task<int> TaoNhacTruocCaAsync(DateTime bayGio)
        => (await Database.SqlQuery<KetQuaNhacCa>($"EXEC dbo.sp_TaoNhacTruocCa @BayGio = {bayGio}").ToListAsync()).Single().SoNhacMoi;

    // Gọi sp_GoiYUngVien: người đăng ký của ca kèm thông tin để xếp hạng.
    public Task<List<UngVienCuaCa>> GoiYUngVienAsync(string maCaLam)
        => Database.SqlQuery<UngVienCuaCa>($"EXEC dbo.sp_GoiYUngVien @MaCaLam = {maCaLam}").ToListAsync();

    // Gọi sp_TongGioDongGop: tổng số giây đóng góp đã xác nhận của một hồ sơ.
    public async Task<TongDongGop> TongGioDongGopAsync(string maHoSo)
        => (await Database.SqlQuery<TongDongGop>($"EXEC dbo.sp_TongGioDongGop @MaHoSo = {maHoSo}")
            .ToListAsync()).Single();

    // Gọi sp_ThongKeKyNangThieu: kỹ năng đang thiếu ở các ca sắp tới của một tổ chức.
    public Task<List<KyNangThieu>> ThongKeKyNangThieuAsync(string maToChuc, DateTime bayGio)
        => Database.SqlQuery<KyNangThieu>($"EXEC dbo.sp_ThongKeKyNangThieu @MaToChuc = {maToChuc}, @BayGio = {bayGio}")
            .ToListAsync();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Thời điểm lưu theo UTC; khi đọc ra đánh dấu lại là UTC để giao diện đổi đúng sang giờ Việt Nam.
        b.Properties<DateTime>().HaveConversion<ChuyenUtc>().HaveColumnType("datetime2(0)");
        b.Properties<DateTime?>().HaveConversion<ChuyenUtcRong>().HaveColumnType("datetime2(0)");
        // Số thập phân trong CSDL chỉ có tọa độ, lưu 6 chữ số sau dấu phẩy.
        b.Properties<decimal>().HavePrecision(9, 6);
        b.Properties<decimal?>().HavePrecision(9, 6);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.HasDefaultSchema("dbo");

        m.Entity<NguoiDung>(e =>
        {
            e.ToTable("NguoiDung").HasKey(x => x.MaNguoiDung);
            e.HasMany(x => x.VaiTro).WithOne().HasForeignKey(x => x.MaNguoiDung);
            e.HasMany(x => x.ThanhVien).WithOne(x => x.NguoiDung).HasForeignKey(x => x.MaNguoiDung);
        });
        m.Entity<NguoiDungVaiTro>().ToTable("NguoiDungVaiTro").HasKey(x => new { x.MaNguoiDung, x.VaiTro });
        m.Entity<ToChucCongDong>(e =>
        {
            e.ToTable("ToChucCongDong").HasKey(x => x.MaToChuc);
            e.HasOne(x => x.NguoiDangKy).WithMany().HasForeignKey(x => x.MaNguoiDangKy);
        });
        m.Entity<ThanhVienToChuc>(e =>
        {
            e.ToTable("ThanhVienToChuc").HasKey(x => new { x.MaNguoiDung, x.MaToChuc, x.VaiTro });
            e.HasOne(x => x.ToChuc).WithMany().HasForeignKey(x => x.MaToChuc);
        });
        m.Entity<HoSoTinhNguyenVien>(e =>
        {
            e.ToTable("HoSoTinhNguyenVien").HasKey(x => x.MaHoSo);
            e.HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaNguoiDung);
            e.HasMany(x => x.KyNang).WithOne().HasForeignKey(x => x.MaHoSo);
            e.HasMany(x => x.LichRanh).WithOne().HasForeignKey(x => x.MaHoSo);
        });
        m.Entity<LichRanh>().ToTable("LichRanh").HasKey(x => new { x.MaHoSo, x.Thu, x.Buoi });
        m.Entity<NhomKyNang>().ToTable("NhomKyNang").HasKey(x => x.MaNhom);
        m.Entity<KyNang>(e =>
        {
            e.ToTable("KyNang").HasKey(x => x.MaKyNang);
            e.Property(x => x.TenChuanHoa).HasComputedColumnSql("LOWER(LTRIM(RTRIM(TenKyNang)))", stored: true);
            e.HasOne(x => x.Nhom).WithMany().HasForeignKey(x => x.MaNhom);
        });
        m.Entity<DeXuatKyNang>(e =>
        {
            e.ToTable("DeXuatKyNang").HasKey(x => x.MaDeXuat);
            e.Property(x => x.TenChuanHoa).HasComputedColumnSql("LOWER(LTRIM(RTRIM(TenKyNang)))", stored: true);
            e.HasOne(x => x.NguoiDeXuat).WithMany().HasForeignKey(x => x.MaNguoiDeXuat);
            e.HasOne(x => x.KyNang).WithMany().HasForeignKey(x => x.MaKyNang);
        });
        m.Entity<KyNangSoHuu>(e =>
        {
            e.ToTable("KyNangSoHuu", tb => tb.HasTrigger("trg_KyNangSoHuu_GiuKyNangDangCan"))
                .HasKey(x => new { x.MaHoSo, x.MaKyNang });
            e.Property(x => x.CapDo).HasConversion<byte>();
            e.HasOne(x => x.KyNang).WithMany().HasForeignKey(x => x.MaKyNang);
        });
        m.Entity<ChungChiKyNang>(e =>
        {
            e.ToTable("ChungChiKyNang", tb => tb.HasTrigger("trg_ChungChiKyNang_KiemTra")).HasKey(x => x.MaChungChi);
            e.HasOne(x => x.HoSo).WithMany().HasForeignKey(x => x.MaHoSo);
            e.HasOne(x => x.KyNang).WithMany().HasForeignKey(x => x.MaKyNang);
            e.HasOne(x => x.Tep).WithMany().HasForeignKey(x => x.MaTep);
        });
        m.Entity<TepDinhKem>().ToTable("TepDinhKem").HasKey(x => x.MaTep);
        m.Entity<NhuCauCongDong>(e =>
        {
            e.ToTable("NhuCauCongDong", tb =>
            {
                tb.HasTrigger("trg_NhuCauCongDong_GiuToChuc");
                tb.HasTrigger("trg_NhuCauCongDong_ToChucHoatDong");
            }).HasKey(x => x.MaNhuCau);
            e.HasOne(x => x.ToChuc).WithMany().HasForeignKey(x => x.MaToChuc);
            e.HasOne(x => x.ThongTinRieng).WithOne().HasForeignKey<ThongTinDieuPhoiRieng>(x => x.MaNhuCau);
            e.HasMany(x => x.CaLam).WithOne(x => x.NhuCau).HasForeignKey(x => x.MaNhuCau);
            e.HasMany(x => x.Anh).WithOne().HasForeignKey(x => x.MaNhuCau);
        });
        m.Entity<AnhNhuCau>(e =>
        {
            e.ToTable("AnhNhuCau", tb => tb.HasTrigger("trg_AnhNhuCau_GioiHan")).HasKey(x => new { x.MaNhuCau, x.MaTep });
            e.HasOne(x => x.Tep).WithMany().HasForeignKey(x => x.MaTep);
        });
        m.Entity<ThongTinDieuPhoiRieng>().ToTable("ThongTinDieuPhoiRieng").HasKey(x => x.MaNhuCau);
        m.Entity<CaLam>(e =>
        {
            e.ToTable("CaLam", tb => tb.HasTrigger("trg_CaLam_KiemTra")).HasKey(x => x.MaCaLam);
            e.HasMany(x => x.YeuCauKyNang).WithOne().HasForeignKey(x => x.MaCaLam);
            e.HasMany(x => x.DangKy).WithOne(x => x.CaLam).HasForeignKey(x => x.MaCaLam);
        });
        m.Entity<YeuCauKyNang>(e =>
        {
            e.ToTable("YeuCauKyNang", tb =>
            {
                tb.HasTrigger("trg_YeuCauKyNang_KiemTraSua");
                tb.HasTrigger("trg_YeuCauKyNang_KiemTraXacMinh");
            }).HasKey(x => new { x.MaCaLam, x.MaKyNang });
            e.Property(x => x.CapDoToiThieu).HasConversion<byte>();
            e.HasOne(x => x.KyNang).WithMany().HasForeignKey(x => x.MaKyNang);
        });
        m.Entity<DangKyCaLam>(e =>
        {
            e.ToTable("DangKyCaLam", tb => tb.HasTrigger("trg_DangKyCaLam_KiemTra")).HasKey(x => x.MaDangKy);
            e.HasOne(x => x.HoSo).WithMany().HasForeignKey(x => x.MaHoSo);
            e.HasOne(x => x.DongGop).WithOne(x => x.DangKy).HasForeignKey<DongGop>(x => x.MaDangKy);
            e.HasOne(x => x.DiemDanh).WithOne().HasForeignKey<DiemDanh>(x => x.MaDangKy);
        });
        m.Entity<DiemDanh>().ToTable("DiemDanh", tb => tb.HasTrigger("trg_DiemDanh_KiemTra")).HasKey(x => x.MaDangKy);
        m.Entity<DongGop>(e =>
        {
            e.ToTable("DongGop", tb => tb.HasTrigger("trg_DongGop_KiemTra")).HasKey(x => x.MaDongGop);
            e.HasMany(x => x.Anh).WithOne().HasForeignKey(x => x.MaDongGop);
        });
        m.Entity<AnhMinhChung>(e =>
        {
            e.ToTable("AnhMinhChung", tb => tb.HasTrigger("trg_AnhMinhChung_GioiHan")).HasKey(x => new { x.MaDongGop, x.MaTep });
            e.HasOne(x => x.Tep).WithMany().HasForeignKey(x => x.MaTep);
        });
        m.Entity<GiayXacNhan>(e =>
        {
            e.ToTable("GiayXacNhan").HasKey(x => x.MaGiay);
            e.HasMany(x => x.DongGop).WithOne().HasForeignKey(x => x.MaGiay);
        });
        m.Entity<GiayXacNhanDongGop>(e =>
        {
            e.ToTable("GiayXacNhanDongGop").HasKey(x => new { x.MaGiay, x.MaDongGop });
            e.HasOne(x => x.DongGop).WithMany().HasForeignKey(x => x.MaDongGop);
        });
        m.Entity<ThongBao>().ToTable("ThongBao").HasKey(x => x.MaThongBao);
        m.Entity<NhatKyTruyCap>(e =>
        {
            e.ToTable("NhatKyTruyCap", tb => tb.HasTrigger("trg_NhatKyTruyCap_ChiThem")).HasKey(x => x.MaNhatKy);
            e.Property(x => x.MaNhatKy).ValueGeneratedOnAdd();
            e.HasOne(x => x.NguoiXem).WithMany().HasForeignKey(x => x.MaNguoiXem);
        });

        m.Entity<CaCongKhai>().ToView("CaCongKhai").HasNoKey();
        m.Entity<MucTinCay>().ToView("MucTinCay").HasNoKey();
    }

    private class ChuyenUtc : ValueConverter<DateTime, DateTime>
    {
        public ChuyenUtc() : base(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
    }

    private class ChuyenUtcRong : ValueConverter<DateTime?, DateTime?>
    {
        public ChuyenUtcRong() : base(
            v => v.HasValue && v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v) { }
    }
}

// Kết quả của sp_ChuyenMocThoiGianCa.
public class KetQuaChuyenMoc
{
    public int SoCaBatDau { get; set; }
    public int SoCaKetThuc { get; set; }
    public int SoDangKyBiHuy { get; set; }
}

// Kết quả của sp_TaoNhacTruocCa.
public class KetQuaNhacCa
{
    public int SoNhacMoi { get; set; }
}

// Một dòng kết quả của sp_GoiYUngVien.
public class UngVienCuaCa
{
    public string MaDangKy { get; set; } = "";
    public string MaHoSo { get; set; } = "";
    public string TenHienThi { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public DateTime NgayDangKy { get; set; }
    public string KyNangDaKhai { get; set; } = "";
    public bool DuKyNang { get; set; }
    public bool ThieuXacMinh { get; set; }
    public int SoKyNangDaXacMinh { get; set; }
    public int SoKyNangCanChungChi { get; set; }
    public bool TrungLich { get; set; }
    public bool? KhopLichRanh { get; set; }
    public decimal? ViDo { get; set; }
    public decimal? KinhDo { get; set; }
    public int SoLanCoMat { get; set; }
    public int SoLanVang { get; set; }
}

// Kết quả của sp_TongGioDongGop.
public class TongDongGop
{
    public int SoBanDaXacNhan { get; set; }
    public long TongSoGiay { get; set; }
}

// Một dòng kết quả của sp_ThongKeKyNangThieu.
public class KyNangThieu
{
    public string MaKyNang { get; set; } = "";
    public string TenKyNang { get; set; } = "";
    public int SoCa { get; set; }
    public int SoChoThieu { get; set; }
    public int SoDangKyChoDuyet { get; set; }
    public int SoTinhNguyenVienCoKyNang { get; set; }
}

// Dòng của view MucTinCay (BR27).
public class MucTinCay
{
    public string MaHoSo { get; set; } = "";
    public int SoLanCoMat { get; set; }
    public int SoLanVang { get; set; }
}
