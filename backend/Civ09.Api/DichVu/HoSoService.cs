using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record KyNangDto(string MaKyNang, string TenKyNang, string? MoTaKyNang, string? MaNhom, string? TenNhom, bool CanChungChi);
public record LichRanhNhap(int Thu, string Buoi);
public record ViTriNhap(string? NoiO, decimal? ViDo, decimal? KinhDo, int? KhoangCachToiDa);
public record DeXuatNhap(string TenKyNang, string? MoTa, string? LyDo);

// Hồ sơ của tình nguyện viên: kỹ năng và cấp độ, chứng chỉ, lịch rảnh, vị trí; đề xuất kỹ năng mới
// (UC02, UC13, UC15, UC16, BR03, BR17, BR18, BR19, BR21).
public class HoSoService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public HoSoService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    public async Task<object> XemHoSo(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var hs = await _kho.HoSoTinhNguyenVien.AsNoTracking()
            .Include(h => h.KyNang).ThenInclude(k => k.KyNang)
            .Include(h => h.LichRanh)
            .AsSplitQuery()
            .FirstAsync(h => h.MaHoSo == nd.MaHoSo);
        var chungChi = await _kho.ChungChiKyNang.AsNoTracking().Where(c => c.MaHoSo == hs.MaHoSo)
            .OrderByDescending(c => c.NgayGui)
            .Select(c => new { c.MaChungChi, c.MaKyNang, c.TrangThai, c.NgayGui, c.LyDoTuChoi, c.NgayXacMinh })
            .ToListAsync();
        var tinCay = await _kho.MucTinCay.AsNoTracking().FirstOrDefaultAsync(m => m.MaHoSo == hs.MaHoSo);
        return new
        {
            hs.MaHoSo,
            nd.TenHienThi,
            hs.NgayCapNhatKyNang,
            hs.NoiO, hs.ViDo, hs.KinhDo, hs.KhoangCachToiDa,
            KyNang = hs.KyNang.OrderBy(k => k.KyNang!.TenKyNang).Select(k =>
            {
                var cuaKn = chungChi.Where(c => c.MaKyNang == k.MaKyNang).ToList();
                var dangCho = cuaKn.FirstOrDefault(c => c.TrangThai == ChungChiKyNang.CHO_XAC_MINH);
                var daXacMinh = cuaKn.FirstOrDefault(c => c.TrangThai == ChungChiKyNang.DA_XAC_MINH);
                var biTuChoi = cuaKn.FirstOrDefault();
                return new
                {
                    k.MaKyNang, k.KyNang!.TenKyNang, k.KyNang.MoTaKyNang, k.CapDo, TenCapDo = MucCapDo.Ten(k.CapDo),
                    k.KyNang.CanChungChi,
                    DaXacMinh = daXacMinh != null,
                    ChungChiDangCho = dangCho == null ? null : new { dangCho.MaChungChi, dangCho.NgayGui },
                    ChungChiDaXacMinh = daXacMinh == null ? null : new { daXacMinh.MaChungChi, daXacMinh.NgayXacMinh },
                    ChungChiBiTuChoi = dangCho == null && biTuChoi?.TrangThai == ChungChiKyNang.TU_CHOI
                        ? new { biTuChoi.MaChungChi, biTuChoi.LyDoTuChoi } : null
                };
            }).ToList(),
            LichRanh = hs.LichRanh.OrderBy(l => l.Thu).ThenBy(l => l.Buoi).Select(l => new { Thu = (int)l.Thu, l.Buoi }).ToList(),
            MucTinCay = new { SoLanCoMat = tinCay?.SoLanCoMat ?? 0, SoLanVang = tinCay?.SoLanVang ?? 0 }
        };
    }

    // Danh mục kỹ năng đang dùng, kèm nhóm, để chọn trong hồ sơ và khi tạo ca.
    public async Task<List<KyNangDto>> XemDanhMucKyNang()
        => await _kho.KyNang.AsNoTracking().Where(k => k.DangSuDung)
            .OrderBy(k => k.Nhom == null ? int.MaxValue : k.Nhom.ThuTu).ThenBy(k => k.TenKyNang)
            .Select(k => new KyNangDto(k.MaKyNang, k.TenKyNang, k.MoTaKyNang, k.MaNhom, k.Nhom == null ? null : k.Nhom.TenNhom, k.CanChungChi))
            .ToListAsync();

    // Lưu danh sách kỹ năng và cấp độ. Không cho bỏ kỹ năng, hoặc hạ cấp độ dưới mức đang cần cho ca đã được
    // phân công và chưa kết thúc (BR03, BR18).
    public async Task CapNhatKyNang(NguoiDangNhap nd, List<KyNangKhai> ds)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        ds = ds.Where(m => !string.IsNullOrWhiteSpace(m.MaKyNang)).GroupBy(k => k.MaKyNang).Select(g => g.Last()).ToList();
        var ma = ds.Select(k => k.MaKyNang).ToList();

        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("HoSoTinhNguyenVien", "MaHoSo", nd.MaHoSo!);
            var hs = await _kho.HoSoTinhNguyenVien.Include(h => h.KyNang).FirstAsync(h => h.MaHoSo == nd.MaHoSo);
            var daCo = hs.LayKyNang().ToHashSet();
            // Kỹ năng mới phải đang được dùng trong danh mục; kỹ năng cũ đã ngừng dùng vẫn giữ được.
            var hopLe = await _kho.KyNang.Where(k => ma.Contains(k.MaKyNang) && (k.DangSuDung || daCo.Contains(k.MaKyNang)))
                .ToDictionaryAsync(k => k.MaKyNang, k => k.TenKyNang);
            if (hopLe.Count != ma.Count) throw LoiNghiepVu.KhongHopLe("Có kỹ năng không nằm trong danh mục.");

            var moi = ds.ToDictionary(k => k.MaKyNang, k => k.CapDo);
            var viPham = new List<string>();
            foreach (var dk in await DangKyCaLam.LayDaPhanCong(_kho, hs.MaHoSo))
            {
                var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
                if (ca.TrangThaiCa is not (CaLam.MO or CaLam.DANG_DIEN_RA)) continue;
                foreach (var y in ca.YeuCau)
                    if (!moi.TryGetValue(y.MaKyNang, out var c) || c < y.CapDoToiThieu) viPham.Add(y.MaKyNang);
            }
            if (viPham.Count > 0)
            {
                var ten = await _kho.KyNang.Where(k => viPham.Contains(k.MaKyNang)).Select(k => k.TenKyNang).ToListAsync();
                throw LoiNghiepVu.XungDot(
                    $"Không thể bỏ hoặc hạ cấp độ kỹ năng {string.Join(", ", ten.Distinct())} vì đang cần cho ca bạn đã được phân công và chưa kết thúc.");
            }
            hs.CapNhatKyNang(ds, BayGio);
        });
    }

    public async Task CapNhatLichRanh(NguoiDangNhap nd, List<LichRanhNhap> ds)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var hs = await _kho.HoSoTinhNguyenVien.Include(h => h.LichRanh).FirstAsync(h => h.MaHoSo == nd.MaHoSo);
            hs.CapNhatLichRanh(ds.Select(l => (l.Thu, l.Buoi ?? "")));
        });
    }

    public async Task CapNhatViTri(NguoiDangNhap nd, ViTriNhap dl)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var hs = await _kho.HoSoTinhNguyenVien.FirstAsync(h => h.MaHoSo == nd.MaHoSo);
            hs.CapNhatViTri(dl.NoiO, dl.ViDo, dl.KinhDo, dl.KhoangCachToiDa);
        });
    }

    // BR19: tải ảnh chứng chỉ cho kỹ năng cần chứng chỉ đã khai. Bản đang chờ xác minh trước đó hết hiệu lực;
    // bản đã xác minh vẫn giữ đến khi bản mới được xác minh.
    public async Task<string> TaiChungChi(NguoiDangNhap nd, string maKyNang, IFormFile tep)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var anh = await XuLyAnh.ChuanHoaAsync(tep, nd.MaNguoiDung, BayGio);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("HoSoTinhNguyenVien", "MaHoSo", nd.MaHoSo!);
            var kn = await _kho.KyNangSoHuu.Include(k => k.KyNang)
                         .FirstOrDefaultAsync(k => k.MaHoSo == nd.MaHoSo && k.MaKyNang == maKyNang)
                     ?? throw LoiNghiepVu.KhongHopLe("Hãy thêm kỹ năng này vào hồ sơ và lưu lại trước khi tải chứng chỉ.");
            if (!kn.KyNang!.CanChungChi)
                throw LoiNghiepVu.KhongHopLe("Kỹ năng này không cần chứng chỉ.");
            foreach (var cu in await _kho.ChungChiKyNang.Where(c => c.MaHoSo == nd.MaHoSo && c.MaKyNang == maKyNang
                                                                     && c.TrangThai == ChungChiKyNang.CHO_XAC_MINH).ToListAsync())
                cu.TrangThai = ChungChiKyNang.HET_HIEU_LUC;
            // Lưu đổi trạng thái bản cũ trước để chỉ mục "mỗi kỹ năng một bản chờ xác minh" không bị vi phạm.
            await _kho.SaveChangesAsync();
            _kho.TepDinhKem.Add(anh);
            var cc = new ChungChiKyNang { MaHoSo = nd.MaHoSo!, MaKyNang = maKyNang, MaTep = anh.MaTep, NgayGui = BayGio };
            _kho.ChungChiKyNang.Add(cc);
            return cc.MaChungChi;
        });
    }

    // BR17: đề xuất kỹ năng chưa có trong danh mục. Tình nguyện viên và điều phối viên được đề xuất.
    public async Task<string> DeXuatKyNang(NguoiDangNhap nd, DeXuatNhap dl)
    {
        if (nd.MaHoSo == null && !nd.ToChucTheoVaiTro(CacVaiTro.DieuPhoi).Any())
            throw LoiNghiepVu.KhongDuQuyen("Tình nguyện viên và điều phối viên mới đề xuất được kỹ năng.");
        var dx = ThucThe.DeXuatKyNang.Tao(nd.MaNguoiDung, dl.TenKyNang, dl.MoTa, dl.LyDo, BayGio);
        var chuan = ThucThe.KyNang.ChuanHoa(dx.TenKyNang);
        var daCo = await _kho.KyNang.AsNoTracking().FirstOrDefaultAsync(k => k.TenChuanHoa == chuan);
        if (daCo != null)
            throw LoiNghiepVu.XungDot(daCo.DangSuDung
                ? $"Kỹ năng \"{daCo.TenKyNang}\" đã có trong danh mục. Bạn chọn được ngay trong danh sách."
                : $"Kỹ năng \"{daCo.TenKyNang}\" đã có nhưng đang ngừng dùng. Hãy liên hệ quản trị viên.");
        if (await _kho.DeXuatKyNang.AnyAsync(d => d.TenChuanHoa == chuan && d.TrangThai == ThucThe.DeXuatKyNang.CHO_DUYET))
            throw LoiNghiepVu.XungDot("Kỹ năng này đã có người đề xuất và đang chờ quản trị viên duyệt.");
        var quanTri = await _thongBao.QuanTriVien();
        return await _giaoDich.ThucHienAsync(() =>
        {
            _kho.DeXuatKyNang.Add(dx);
            _thongBao.Gui(quanTri, BoThongBao.DE_XUAT_KY_NANG,
                $"{nd.TenHienThi} đề xuất thêm kỹ năng \"{dx.TenKyNang}\".", "/quan-tri/ky-nang", dx.MaDeXuat);
            return Task.FromResult(dx.MaDeXuat);
        });
    }

    public async Task<List<object>> XemDeXuatCuaToi(NguoiDangNhap nd)
        => (await _kho.DeXuatKyNang.AsNoTracking().Where(d => d.MaNguoiDeXuat == nd.MaNguoiDung)
                .OrderByDescending(d => d.NgayDeXuat)
                .Select(d => new { d.MaDeXuat, d.TenKyNang, d.TrangThai, d.NgayDeXuat, d.LyDoTuChoi, TenKyNangDuocDung = d.KyNang == null ? null : d.KyNang.TenKyNang })
                .ToListAsync()).Cast<object>().ToList();
}
