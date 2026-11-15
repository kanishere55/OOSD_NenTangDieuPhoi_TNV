using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record KyNangNhap(string TenKyNang, string? MoTa, string? MaNhom, bool CanChungChi, bool DangSuDung = true);
public record NhomNhap(string TenNhom, int ThuTu);
public record LyDoNhap(string? LyDo);
public record DungKyNangNhap(string MaKyNang);

// Quản trị danh mục kỹ năng và duyệt đề xuất kỹ năng (UC14, BR17). Chỉ quản trị viên.
public class KyNangService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public KyNangService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    public async Task<object> XemDeXuat(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauQuanTri(nd);
        var ds = await _kho.DeXuatKyNang.AsNoTracking()
            .OrderBy(d => d.TrangThai == DeXuatKyNang.CHO_DUYET ? 0 : 1).ThenByDescending(d => d.NgayDeXuat)
            .Take(100)
            .Select(d => new
            {
                d.MaDeXuat, d.TenKyNang, d.MoTa, d.LyDo, d.TrangThai, d.NgayDeXuat, d.LyDoTuChoi,
                TenNguoiDeXuat = d.NguoiDeXuat!.TenHienThi, TenKyNangDuocDung = d.KyNang == null ? null : d.KyNang.TenKyNang
            }).ToListAsync();
        return ds;
    }

    // Duyệt đề xuất bằng một kỹ năng mới. Quản trị viên có thể sửa lại tên, mô tả và chọn nhóm trước khi thêm.
    public async Task<string> DuyetDeXuat(NguoiDangNhap nd, string maDeXuat, KyNangNhap dl)
    {
        _chinhSach.YeuCauQuanTri(nd);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            var dx = await LayDeXuat(maDeXuat);
            var kn = new KyNang();
            kn.GanThongTin(dl.TenKyNang, dl.MoTa, dl.MaNhom, dl.CanChungChi);
            await KiemTraTenChuaCo(kn.TenKyNang, null);
            await KiemTraNhom(kn.MaNhom);
            _kho.KyNang.Add(kn);
            await _kho.SaveChangesAsync();
            dx.Duyet(kn.MaKyNang, nd.MaNguoiDung, BayGio);
            _thongBao.Gui(dx.MaNguoiDeXuat, BoThongBao.KET_QUA_DE_XUAT,
                $"Kỹ năng \"{kn.TenKyNang}\" bạn đề xuất đã được thêm vào danh mục.", "/ho-so", dx.MaDeXuat);
            return kn.MaKyNang;
        });
    }

    // Đề xuất trùng nghĩa với một kỹ năng đã có: duyệt bằng kỹ năng đó, không thêm kỹ năng mới.
    public async Task DungKyNangCoSan(NguoiDangNhap nd, string maDeXuat, string maKyNang)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var dx = await LayDeXuat(maDeXuat);
            var kn = await _kho.KyNang.FirstOrDefaultAsync(k => k.MaKyNang == maKyNang && k.DangSuDung)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy kỹ năng đang dùng.");
            dx.Duyet(kn.MaKyNang, nd.MaNguoiDung, BayGio);
            _thongBao.Gui(dx.MaNguoiDeXuat, BoThongBao.KET_QUA_DE_XUAT,
                $"Đề xuất \"{dx.TenKyNang}\" trùng với kỹ năng \"{kn.TenKyNang}\" đã có. Bạn hãy chọn kỹ năng này.", "/ho-so", dx.MaDeXuat);
        });
    }

    public async Task TuChoiDeXuat(NguoiDangNhap nd, string maDeXuat, string? lyDo)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var dx = await LayDeXuat(maDeXuat);
            dx.TuChoi(lyDo, nd.MaNguoiDung, BayGio);
            _thongBao.Gui(dx.MaNguoiDeXuat, BoThongBao.KET_QUA_DE_XUAT,
                $"Đề xuất kỹ năng \"{dx.TenKyNang}\" chưa được duyệt. Lý do: {dx.LyDoTuChoi}", "/ho-so", dx.MaDeXuat);
        });
    }

    // Danh mục đầy đủ cho quản trị viên, kể cả kỹ năng đã ngừng dùng, kèm số người đã khai.
    public async Task<object> XemDanhMuc(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauQuanTri(nd);
        var kyNang = await _kho.KyNang.AsNoTracking()
            .OrderBy(k => k.Nhom == null ? int.MaxValue : k.Nhom.ThuTu).ThenBy(k => k.TenKyNang)
            .Select(k => new
            {
                k.MaKyNang, k.TenKyNang, k.MoTaKyNang, k.MaNhom, TenNhom = k.Nhom == null ? null : k.Nhom.TenNhom,
                k.CanChungChi, k.DangSuDung,
                SoNguoiDaKhai = _kho.KyNangSoHuu.Count(s => s.MaKyNang == k.MaKyNang)
            }).ToListAsync();
        var nhom = await _kho.NhomKyNang.AsNoTracking().OrderBy(n => n.ThuTu).ThenBy(n => n.TenNhom).ToListAsync();
        return new { KyNang = kyNang, Nhom = nhom };
    }

    public async Task<string> TaoKyNang(NguoiDangNhap nd, KyNangNhap dl)
    {
        _chinhSach.YeuCauQuanTri(nd);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            var kn = new KyNang();
            kn.GanThongTin(dl.TenKyNang, dl.MoTa, dl.MaNhom, dl.CanChungChi);
            await KiemTraTenChuaCo(kn.TenKyNang, null);
            await KiemTraNhom(kn.MaNhom);
            _kho.KyNang.Add(kn);
            return kn.MaKyNang;
        });
    }

    // Sửa kỹ năng. Kỹ năng không bị xóa mà chuyển sang ngừng dùng để giữ dữ liệu cũ (BR17).
    public async Task SuaKyNang(NguoiDangNhap nd, string maKyNang, KyNangNhap dl)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var kn = await _kho.KyNang.FirstOrDefaultAsync(k => k.MaKyNang == maKyNang)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy kỹ năng.");
            var boCanChungChi = kn.CanChungChi && !dl.CanChungChi;
            kn.GanThongTin(dl.TenKyNang, dl.MoTa, dl.MaNhom, dl.CanChungChi);
            if (boCanChungChi && await _kho.YeuCauKyNang.AnyAsync(y => y.MaKyNang == maKyNang && y.CanXacMinh))
                throw LoiNghiepVu.XungDot("Đang có ca yêu cầu chứng chỉ đã xác minh cho kỹ năng này nên chưa bỏ được mục cần chứng chỉ.");
            kn.DangSuDung = dl.DangSuDung;
            await KiemTraTenChuaCo(kn.TenKyNang, maKyNang);
            await KiemTraNhom(kn.MaNhom);
        });
    }

    public async Task<string> TaoNhom(NguoiDangNhap nd, NhomNhap dl)
    {
        _chinhSach.YeuCauQuanTri(nd);
        var ten = (dl.TenNhom ?? "").Trim();
        if (ten.Length == 0 || ten.Length > 100) throw LoiNghiepVu.KhongHopLe("Tên nhóm từ 1 đến 100 ký tự.");
        if (await _kho.NhomKyNang.AnyAsync(n => n.TenNhom == ten)) throw LoiNghiepVu.XungDot("Đã có nhóm kỹ năng cùng tên.");
        return await _giaoDich.ThucHienAsync(() =>
        {
            var n = new NhomKyNang { TenNhom = ten, ThuTu = dl.ThuTu };
            _kho.NhomKyNang.Add(n);
            return Task.FromResult(n.MaNhom);
        });
    }

    private async Task<DeXuatKyNang> LayDeXuat(string ma)
    {
        await _giaoDich.KhoaDongAsync("DeXuatKyNang", "MaDeXuat", ma);
        return await _kho.DeXuatKyNang.FirstOrDefaultAsync(d => d.MaDeXuat == ma)
               ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy đề xuất.");
    }

    private async Task KiemTraTenChuaCo(string ten, string? truMa)
    {
        var chuan = KyNang.ChuanHoa(ten);
        var trung = await _kho.KyNang.AsNoTracking().FirstOrDefaultAsync(k => k.TenChuanHoa == chuan && k.MaKyNang != truMa);
        if (trung != null) throw LoiNghiepVu.XungDot($"Đã có kỹ năng \"{trung.TenKyNang}\" trong danh mục.");
    }

    private async Task KiemTraNhom(string? maNhom)
    {
        if (maNhom != null && !await _kho.NhomKyNang.AnyAsync(n => n.MaNhom == maNhom))
            throw LoiNghiepVu.KhongHopLe("Nhóm kỹ năng không tồn tại.");
    }
}
