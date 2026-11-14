using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Xunit;

namespace Civ09.Tests;

// Kiểm thử đơn vị cho các quy tắc thuần, không cần cơ sở dữ liệu.
public class KiemThuDonVi
{
    private static readonly DateTime Ngay = new(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime Gio(int gio, int phut = 0) => Ngay.AddHours(gio).AddMinutes(phut);

    [Fact(DisplayName = "KT01 (đơn vị) - Ca 08:00-10:00 và 09:00-11:00 là trùng lịch")]
    public void KT01_HaiCaGiaoNhau_LaTrungLich()
        => Assert.True(DangKyCaLam.GiaoNhau(Gio(8), Gio(10), Gio(9), Gio(11)));

    [Fact(DisplayName = "KT02 (đơn vị) - Ca sau bắt đầu đúng lúc ca trước kết thúc thì không trùng")]
    public void KT02_CaNoiTiep_KhongTrungLich()
        => Assert.False(DangKyCaLam.GiaoNhau(Gio(8), Gio(10), Gio(10), Gio(12)));

    [Fact(DisplayName = "KT04 (đơn vị) - Ca cần 1 người đã có 1 người thì hết chỗ")]
    public void KT04_DuNguoi_HetCho()
    {
        Assert.False(DieuKienPhanCong.ConCho(1, 1));
        Assert.True(DieuKienPhanCong.ConCho(0, 1));
    }

    [Fact(DisplayName = "KT03 (đơn vị) - Thiếu một kỹ năng của ca thì không đủ điều kiện")]
    public void KT03_ThieuKyNang_KhongDuDieuKien()
    {
        Assert.False(DieuKienPhanCong.DuKyNang(new[] { "KN-TIN-HOC", "KN-SO-CUU" }, new[] { "KN-TIN-HOC" }));
        Assert.True(DieuKienPhanCong.DuKyNang(new[] { "KN-TIN-HOC" }, new[] { "KN-TIN-HOC", "KN-NAU-AN" }));
    }

    [Fact(DisplayName = "KT07 (đơn vị) - Sửa nội dung sau khi rà soát thì công bố với phiên bản cũ bị từ chối")]
    public void KT07_SuaSauRaSoat_CongBoPhienBanCu_BiTuChoi()
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

    [Fact(DisplayName = "KT14 (đơn vị) - Thời gian khai ngoài khung giờ của ca bị từ chối")]
    public void KT14_ThoiGianNgoaiCa_BiTuChoi()
    {
        Assert.Throws<LoiNghiepVu>(() => DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(7, 30), Gio(9)));
        Assert.Throws<LoiNghiepVu>(() => DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(9), Gio(10, 30)));
        Assert.Throws<LoiNghiepVu>(() => DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(9), Gio(9)));
        DongGop.KiemTraThoiGian(Gio(8), Gio(10), Gio(8, 15), Gio(9, 45));
    }

    [Fact(DisplayName = "KT15 (đơn vị) - Ca 08:00-10:00, khai 08:15-09:45 thì số giờ bằng 1,5")]
    public void KT15_TinhSoGio_BangMotRuoi()
        => Assert.Equal(1.5m, DongGop.TinhSoGio(Gio(8, 15), Gio(9, 45)));

    [Fact(DisplayName = "KT17 (đơn vị) - Trả lại bản khai mà không có lý do bị từ chối")]
    public void KT17_TraLaiKhongLyDo_BiTuChoi()
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
        Assert.Throws<LoiNghiepVu>(() => CaLam.TaoCa(nc, Gio(8), Gio(10), 1, new[] { "KN-TIN-HOC" }, Gio(0)));
        nc.CongBo(1);
        Assert.Throws<LoiNghiepVu>(() => CaLam.TaoCa(nc, Gio(8), Gio(10), 1, new[] { "KN-TIN-HOC" }, Gio(9)));
        Assert.Throws<LoiNghiepVu>(() => CaLam.TaoCa(nc, Gio(8), Gio(10), 1, Array.Empty<string>(), Gio(0)));
        var ca = CaLam.TaoCa(nc, Gio(8), Gio(10), 1, new[] { "KN-TIN-HOC" }, Gio(0));
        Assert.Equal(CaLam.MO, ca.TrangThai);
    }
}
