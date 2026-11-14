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
    public DbSet<KyNang> KyNang => Set<KyNang>();
    public DbSet<KyNangSoHuu> KyNangSoHuu => Set<KyNangSoHuu>();
    public DbSet<NhuCauCongDong> NhuCauCongDong => Set<NhuCauCongDong>();
    public DbSet<ThongTinDieuPhoiRieng> ThongTinDieuPhoiRieng => Set<ThongTinDieuPhoiRieng>();
    public DbSet<CaLam> CaLam => Set<CaLam>();
    public DbSet<YeuCauKyNang> YeuCauKyNang => Set<YeuCauKyNang>();
    public DbSet<DangKyCaLam> DangKyCaLam => Set<DangKyCaLam>();
    public DbSet<DongGop> DongGop => Set<DongGop>();
    public DbSet<CaCongKhai> CaCongKhai => Set<CaCongKhai>();

    // Gọi sp_ChuyenMocThoiGianCa (BR12).
    public async Task<KetQuaChuyenMoc> ChuyenMocThoiGianCaAsync(DateTime bayGio)
        => (await Database.SqlQuery<KetQuaChuyenMoc>($"EXEC dbo.sp_ChuyenMocThoiGianCa @BayGio = {bayGio}")
            .ToListAsync()).Single();

    // Gọi sp_GoiYUngVien: người đăng ký của ca kèm gợi ý đủ kỹ năng, trùng lịch.
    public Task<List<UngVienCuaCa>> GoiYUngVienAsync(string maCaLam)
        => Database.SqlQuery<UngVienCuaCa>($"EXEC dbo.sp_GoiYUngVien @MaCaLam = {maCaLam}").ToListAsync();

    // Gọi sp_TongGioDongGop: tổng số giây đóng góp đã xác nhận của một hồ sơ.
    public async Task<TongDongGop> TongGioDongGopAsync(string maHoSo)
        => (await Database.SqlQuery<TongDongGop>($"EXEC dbo.sp_TongGioDongGop @MaHoSo = {maHoSo}")
            .ToListAsync()).Single();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Thời điểm lưu theo UTC; khi đọc ra đánh dấu lại là UTC để giao diện đổi đúng sang giờ Việt Nam.
        b.Properties<DateTime>().HaveConversion<ChuyenUtc>().HaveColumnType("datetime2(0)");
        b.Properties<DateTime?>().HaveConversion<ChuyenUtcRong>().HaveColumnType("datetime2(0)");
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.HasDefaultSchema("dbo");

        m.Entity<NguoiDung>(e =>
        {
            e.ToTable("NguoiDung").HasKey(x => x.MaNguoiDung);
            e.HasMany(x => x.VaiTro).WithOne().HasForeignKey(x => x.MaNguoiDung);
            e.HasMany(x => x.ThanhVien).WithOne().HasForeignKey(x => x.MaNguoiDung);
        });
        m.Entity<NguoiDungVaiTro>().ToTable("NguoiDungVaiTro").HasKey(x => new { x.MaNguoiDung, x.VaiTro });
        m.Entity<ToChucCongDong>().ToTable("ToChucCongDong").HasKey(x => x.MaToChuc);
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
        });
        m.Entity<KyNang>(e =>
        {
            e.ToTable("KyNang").HasKey(x => x.MaKyNang);
            e.Property(x => x.TenChuanHoa).HasComputedColumnSql("LOWER(LTRIM(RTRIM(TenKyNang)))", stored: true);
        });
        m.Entity<KyNangSoHuu>(e =>
        {
            e.ToTable("KyNangSoHuu", tb => tb.HasTrigger("trg_KyNangSoHuu_GiuKyNangDangCan"))
                .HasKey(x => new { x.MaHoSo, x.MaKyNang });
            e.HasOne(x => x.KyNang).WithMany().HasForeignKey(x => x.MaKyNang);
        });
        m.Entity<NhuCauCongDong>(e =>
        {
            e.ToTable("NhuCauCongDong", tb => tb.HasTrigger("trg_NhuCauCongDong_GiuToChuc")).HasKey(x => x.MaNhuCau);
            e.HasOne(x => x.ToChuc).WithMany().HasForeignKey(x => x.MaToChuc);
            e.HasOne(x => x.ThongTinRieng).WithOne().HasForeignKey<ThongTinDieuPhoiRieng>(x => x.MaNhuCau);
            e.HasMany(x => x.CaLam).WithOne(x => x.NhuCau).HasForeignKey(x => x.MaNhuCau);
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
            e.ToTable("YeuCauKyNang", tb => tb.HasTrigger("trg_YeuCauKyNang_KiemTraSua")).HasKey(x => new { x.MaCaLam, x.MaKyNang });
            e.HasOne(x => x.KyNang).WithMany().HasForeignKey(x => x.MaKyNang);
        });
        m.Entity<DangKyCaLam>(e =>
        {
            e.ToTable("DangKyCaLam", tb => tb.HasTrigger("trg_DangKyCaLam_KiemTra")).HasKey(x => x.MaDangKy);
            e.HasOne(x => x.HoSo).WithMany().HasForeignKey(x => x.MaHoSo);
            e.HasOne(x => x.DongGop).WithOne(x => x.DangKy).HasForeignKey<DongGop>(x => x.MaDangKy);
        });
        m.Entity<DongGop>().ToTable("DongGop", tb => tb.HasTrigger("trg_DongGop_KiemTra")).HasKey(x => x.MaDongGop);

        m.Entity<CaCongKhai>().ToView("CaCongKhai").HasNoKey();
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

// Một dòng kết quả của sp_GoiYUngVien.
public class UngVienCuaCa
{
    public string MaDangKy { get; set; } = "";
    public string TenHienThi { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public DateTime NgayDangKy { get; set; }
    public string KyNangDaKhai { get; set; } = "";
    public bool DuKyNang { get; set; }
    public bool TrungLich { get; set; }
}

// Kết quả của sp_TongGioDongGop.
public class TongDongGop
{
    public int SoBanDaXacNhan { get; set; }
    public long TongSoGiay { get; set; }
}
