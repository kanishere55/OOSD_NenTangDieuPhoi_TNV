using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Xunit;

namespace Civ09.Tests;

// Kiểm thử đơn vị cho các quy tắc thuần, không cần cơ sở dữ liệu.
public class KiemThuDonVi
{
    private static readonly DateTime Ngay = new(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime Gio(int gio, int phut = 0) => Ngay.AddHours(gio).AddMinutes(phut);

    [Fact(DisplayName = "TC01 (đơn vị) - Ca 08:00-10:00 và 09:00-11:00 là trùng lịch")]
    public void TC01_HaiCaGiaoNhau_LaTrungLich()
        => Assert.True(DangKyCaLam.GiaoNhau(Gio(8), Gio(10), Gio(9), Gio(11)));

    [Fact(DisplayName = "TC02 (đơn vị) - Ca sau bắt đầu đúng lúc ca trước kết thúc thì không trùng")]
    public void TC02_CaNoiTiep_KhongTrungLich()
        => Assert.False(DangKyCaLam.GiaoNhau(Gio(8), Gio(10), Gio(10), Gio(12)));

    [Fact(DisplayName = "TC04 (đơn vị) - Ca cần 1 người đã có 1 người thì hết chỗ")]
    public void TC04_DuNguoi_HetCho()
    {
        Assert.False(DieuKienPhanCong.ConCho(1, 1));
        Assert.True(DieuKienPhanCong.ConCho(0, 1));
    }

    [Fact(DisplayName = "TC03 (đơn vị) - Thiếu một kỹ năng của ca thì không đủ điều kiện")]
    public void TC03_ThieuKyNang_KhongDuDieuKien()
    {
        Assert.False(DieuKienPhanCong.DuKyNang(new[] { "KN-TIN-HOC", "KN-SO-CUU" }, new[] { "KN-TIN-HOC" }));
        Assert.True(DieuKienPhanCong.DuKyNang(new[] { "KN-TIN-HOC" }, new[] { "KN-TIN-HOC", "KN-NAU-AN" }));
    }

    [Fact(DisplayName = "TC07 (đơn vị) - Sửa nội dung sau khi rà soát thì công bố với phiên bản cũ bị từ chối")]
    public void TC07_SuaSauRaSoat_CongBoPhienBanCu_BiTuChoi()
    {
        // Đại diện rà soát phiên bản 1, sau đó nội dung bị sửa thành phiên bản 2.
        var nc = NhuCauCongDong.Tao("TC-A", "Tiêu đề", "Tóm tắt", "Khu vực");
        nc.CapNhatCongKhai("Tiêu đề đã sửa", "Tóm tắt", "Khu vực", daCoCa: false);
        Assert.Equal(2, nc.PhienBanCongKhai);
        Assert.Throws<LoiNghiepVu>(() => nc.CongBo(1));
        Assert.Equal(NhuCauCongDong.NHAP, nc.TrangThai);
        Assert.False(nc.DaRaSoatCongKhai);

        nc.CongBo(2);
        Assert.Equal(NhuCauCongDong.CONG_BO, nc.TrangThai);
        Assert.True(nc.DaRaSoatCongKhai);
    }

    [Fact(DisplayName = "TC14 (đơn vị) - Thời gian khai ngoài khung giờ của ca bị từ chối")]
    public void TC14_ThoiGianNgoaiCa_BiTuChoi()
    {
        Assert.Throws<LoiNghiepVu>(() => DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(7, 30), Gio(9)));
        Assert.Throws<LoiNghiepVu>(() => DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(9), Gio(10, 30)));
        Assert.Throws<LoiNghiepVu>(() => DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(9), Gio(9)));
        DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(8, 15), Gio(9, 45));
    }

    [Fact(DisplayName = "TC15 (đơn vị) - Ca 08:00-10:00, khai 08:15-09:45 thì số giờ bằng 1,5")]
    public void TC15_TinhSoGio_BangMotRuoi()
        => Assert.Equal(1.5m, DongGop.TinhSoGio(Gio(8, 15), Gio(9, 45)));

    [Fact(DisplayName = "TC17 (đơn vị) - Trả lại bản khai mà không có lý do bị từ chối")]
    public void TC17_TraLaiKhongLyDo_BiTuChoi()
    {
        var dg = new DongGop();
        Assert.Throws<LoiNghiepVu>(() => dg.TraLai("  "));
        Assert.Equal(DongGop.CHO_DUYET, dg.TrangThai);
        dg.TraLai("Thiếu mô tả công việc");
        Assert.Equal(DongGop.CAN_BO_SUNG, dg.TrangThai);
    }

    [Fact(DisplayName = "BR12 (đơn vị) - Ca chuyển Đang mở, Đang diễn ra, Đã kết thúc; chỉ hủy được ca chưa bắt đầu")]
    public void BR12_ChuyenTrangThaiCa()
    {
        var ca = new CaLam { BatDauDuKien = Gio(8), KetThucDuKien = Gio(10) };
        Assert.Throws<LoiNghiepVu>(() => ca.Huy(Gio(8)));
        ca.BatDau();
        Assert.Equal(CaLam.DANG_DIEN_RA, ca.TrangThai);
        ca.KetThuc();
        Assert.Equal(CaLam.DA_KET_THUC, ca.TrangThai);
        var ca2 = new CaLam { BatDauDuKien = Gio(8), KetThucDuKien = Gio(10) };
        ca2.Huy(Gio(7));
        Assert.Equal(CaLam.DA_HUY, ca2.TrangThai);
    }

    [Fact(DisplayName = "BR06 (đơn vị) - Không tạo ca cho nhu cầu chưa công bố hoặc ca bắt đầu trong quá khứ")]
    public void BR06_TaoCa_KiemTraDieuKien()
    {
        var nc = NhuCauCongDong.Tao("TC-A", "Tiêu đề", "Tóm tắt", "Khu vực");
        Assert.Throws<LoiNghiepVu>(() => CaLam.TaoCa(nc, Gio(8), Gio(10), 1, new[] { new YeuCauNhap("KN-TIN-HOC") }, Gio(0)));
        nc.CongBo(1);
        Assert.Throws<LoiNghiepVu>(() => CaLam.TaoCa(nc, Gio(8), Gio(10), 1, new[] { new YeuCauNhap("KN-TIN-HOC") }, Gio(9)));
        Assert.Throws<LoiNghiepVu>(() => CaLam.TaoCa(nc, Gio(8), Gio(10), 1, Array.Empty<YeuCauNhap>(), Gio(0)));
        var ca = CaLam.TaoCa(nc, Gio(8), Gio(10), 1, new[] { new YeuCauNhap("KN-TIN-HOC") }, Gio(0));
        Assert.Equal(CaLam.MO, ca.TrangThai);
    }

    [Fact(DisplayName = "TC47 (đơn vị) - Điểm phù hợp: người ở gần, rảnh đúng buổi, có mặt đầy đủ được điểm cao hơn; thiếu kỹ năng thì 0 điểm")]
    public void TC47_ChamDiemPhuHop()
    {
        var tot = ChamDiemPhuHop.Cham(new ThongTinGhep(true, 0, 0, 1.2, true, 5, 0));
        var kem = ChamDiemPhuHop.Cham(new ThongTinGhep(true, 0, 0, 14, false, 1, 3));
        var thieu = ChamDiemPhuHop.Cham(new ThongTinGhep(false, 0, 0, 0.5, true, 5, 0));
        Assert.True(tot.Diem > kem.Diem);
        Assert.InRange(tot.Diem, 90, 100);
        Assert.Equal(0, thieu.Diem);
        Assert.Contains(kem.LuuY, x => x.Contains("Vắng không báo 3 lần"));
    }

    [Fact(DisplayName = "TC35 (đơn vị) - Vùng công khai: tâm luôn lệch khỏi vị trí thật từ 20% đến 60% bán kính")]
    public void TC35_LamMoViTri()
    {
        var ngauNhien = new Random(7);
        for (var i = 0; i < 500; i++)
        {
            var (vd, kd) = ViTri.LamMo(10.85, 106.77, 1000, ngauNhien);
            Assert.InRange(ViTri.KhoangCachKm(10.85, 106.77, vd, kd), 0.199, 0.601);
        }
    }

    [Fact(DisplayName = "TC38 (đơn vị) - Mã điểm danh dùng được trong khung 2 phút hiện tại và khung ngay trước, sau đó hết hạn")]
    public void TC38_MaDiemDanh()
    {
        var khoa = MaDiemDanh.TaoKhoa();
        var luc = Gio(8);
        var (ma, _) = MaDiemDanh.MaHienTai(khoa, luc);
        Assert.True(MaDiemDanh.HopLe(khoa, ma, luc));
        Assert.True(MaDiemDanh.HopLe(khoa, ma.ToLowerInvariant(), luc.AddSeconds(MaDiemDanh.GiayMoiKhung)));
        Assert.False(MaDiemDanh.HopLe(khoa, ma, luc.AddSeconds(3 * MaDiemDanh.GiayMoiKhung)));
        Assert.False(MaDiemDanh.HopLe(MaDiemDanh.TaoKhoa(), ma, luc));
    }

    [Fact(DisplayName = "TC38 (đơn vị) - Giờ theo điểm danh được cắt trong khung giờ của ca")]
    public void TC38_GioTheoDiemDanh()
    {
        var dd = new DiemDanh { KetQua = DiemDanh.CO_MAT, GioVao = Gio(7, 45), GioRa = Gio(10, 20) };
        Assert.Equal((Gio(8), Gio(10)), dd.KhoangThoiGianTrongCa(Gio(8), Gio(10)));
        Assert.Null(new DiemDanh { KetQua = DiemDanh.CO_MAT, GioVao = Gio(8) }.KhoangThoiGianTrongCa(Gio(8), Gio(10)));
        Assert.Null(new DiemDanh { KetQua = DiemDanh.VANG_MAT }.KhoangThoiGianTrongCa(Gio(8), Gio(10)));
    }

    [Fact(DisplayName = "TC32 (đơn vị) - Đủ kỹ năng có xét cấp độ và chứng chỉ đã xác minh")]
    public void TC32_DuKyNangCoCapDo()
    {
        var yeuCau = new[] { new YeuCauDk("KN-SO-CUU", 2, true) };
        var capDo = new Dictionary<string, int> { ["KN-SO-CUU"] = 2 };
        Assert.False(DieuKienPhanCong.DuKyNang(yeuCau, capDo, new HashSet<string>()));
        Assert.True(DieuKienPhanCong.DuKyNang(yeuCau, capDo, new HashSet<string> { "KN-SO-CUU" }));
        Assert.False(DieuKienPhanCong.DuKyNang(yeuCau, new Dictionary<string, int> { ["KN-SO-CUU"] = 1 }, new HashSet<string> { "KN-SO-CUU" }));
    }

    [Fact(DisplayName = "BR20 (đơn vị) - Thứ và buổi của giờ bắt đầu ca tính theo giờ Việt Nam")]
    public void BR20_ThuVaBuoi()
    {
        // 01:00 UTC thứ hai 07/01/2030 là 08:00 sáng thứ hai giờ Việt Nam.
        Assert.Equal((1, Buoi.SANG), Buoi.Cua(new DateTime(2030, 1, 7, 1, 0, 0, DateTimeKind.Utc)));
        // 12:30 UTC chủ nhật 06/01/2030 là 19:30 tối chủ nhật.
        Assert.Equal((7, Buoi.TOI), Buoi.Cua(new DateTime(2030, 1, 6, 12, 30, 0, DateTimeKind.Utc)));
    }
}
