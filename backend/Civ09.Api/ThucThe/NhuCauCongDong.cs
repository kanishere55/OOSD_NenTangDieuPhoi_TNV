using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.ThucThe;

// Nhu cầu hỗ trợ của tổ chức cộng đồng, gồm phần nội dung công khai.
public class NhuCauCongDong
{
    public const string NHAP = "NHAP";
    public const string CONG_BO = "CONG_BO";
    public const string DONG = "DONG";
    public const string DA_HUY = "DA_HUY";

    public string MaNhuCau { get; set; } = Guid.NewGuid().ToString();
    public string MaToChuc { get; set; } = "";
    public string TieuDe { get; set; } = "";
    public string TomTatCongKhai { get; set; } = "";
    public string KhuVucChung { get; set; } = "";
    public decimal? ViDoVung { get; set; }
    public decimal? KinhDoVung { get; set; }
    public int? BanKinhVung { get; set; }
    public int PhienBanCongKhai { get; set; } = 1;
    public bool DaRaSoatCongKhai { get; set; }
    public string TrangThai { get; set; } = NHAP;

    public ToChucCongDong? ToChuc { get; set; }
    public ThongTinDieuPhoiRieng? ThongTinRieng { get; set; }
    public List<CaLam> CaLam { get; set; } = new();
    public List<AnhNhuCau> Anh { get; set; } = new();

    // BR05: nhu cầu mới là bản nháp.
    public static NhuCauCongDong Tao(string maToChuc, string tieuDe, string tomTat, string khuVuc)
    {
        var nc = new NhuCauCongDong { MaToChuc = maToChuc };
        nc.GanNoiDung(tieuDe, tomTat, khuVuc);
        return nc;
    }

    // Đọc nhu cầu (có theo dõi thay đổi). Không kèm hướng dẫn riêng.
    public static async Task<NhuCauCongDong> LayThongTin(KhoNghiepVu kho, string maNhuCau)
        => await kho.NhuCauCongDong.Include(n => n.ToChuc).FirstOrDefaultAsync(n => n.MaNhuCau == maNhuCau)
           ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy nhu cầu.");

    // BR05: sửa nội dung công khai thì tăng phiên bản và phải rà soát lại. Nhu cầu đã công bố chỉ sửa được
    // phần công khai khi chưa có ca, và khi đó trở về bản nháp.
    public void CapNhatCongKhai(string tieuDe, string tomTat, string khuVuc, bool daCoCa)
    {
        tieuDe = tieuDe.Trim(); tomTat = tomTat.Trim(); khuVuc = khuVuc.Trim();
        if (tieuDe == TieuDe && tomTat == TomTatCongKhai && khuVuc == KhuVucChung) return;
        DanhDauSuaCongKhai(daCoCa);
        GanNoiDung(tieuDe, tomTat, khuVuc);
    }

    // BR05, BR22: thêm hoặc bỏ ảnh, đổi vùng trên bản đồ cũng là sửa nội dung công khai.
    // Nhu cầu đã công bố chỉ sửa được khi chưa có ca và khi đó trở về bản nháp.
    public void DanhDauSuaCongKhai(bool daCoCa)
    {
        if (TrangThai == CONG_BO)
        {
            if (daCoCa)
                throw LoiNghiepVu.XungDot("Nhu cầu đã có ca làm nên không sửa được nội dung công khai, ảnh và địa điểm. Phần hướng dẫn riêng vẫn sửa được.");
            TrangThai = NHAP;
        }
        else if (TrangThai != NHAP)
            throw LoiNghiepVu.XungDot("Nhu cầu đã đóng hoặc đã hủy nên không sửa được.");
        PhienBanCongKhai++;
        DaRaSoatCongKhai = false;
    }

    // BR21: vùng công khai là vòng tròn bán kính 1 km, tâm dời ngẫu nhiên khỏi vị trí thật.
    // Không có vị trí thật thì cũng không có vùng.
    public void GanVung(decimal? viDo, decimal? kinhDo, Random ngauNhien)
    {
        if (viDo == null || kinhDo == null)
        {
            ViDoVung = null; KinhDoVung = null; BanKinhVung = null;
            return;
        }
        var (vd, kd) = ViTri.LamMo((double)viDo.Value, (double)kinhDo.Value, ViTri.BanKinhVungMacDinh, ngauNhien);
        ViDoVung = Math.Round((decimal)vd, 6);
        KinhDoVung = Math.Round((decimal)kd, 6);
        BanKinhVung = ViTri.BanKinhVungMacDinh;
    }

    // BR05: đại diện rà soát nội dung công khai rồi công bố, gửi kèm số phiên bản đã rà soát.
    // Chỉ công bố khi đó vẫn là phiên bản hiện tại, tức nội dung không bị sửa sau lần rà soát.
    public void CongBo(int phienBanDaRaSoat)
    {
        if (TrangThai != NHAP)
            throw LoiNghiepVu.XungDot("Nhu cầu không còn là bản nháp.");
        if (phienBanDaRaSoat != PhienBanCongKhai)
            throw LoiNghiepVu.XungDot("Nội dung công khai đã thay đổi sau khi rà soát. Hãy đọc lại nội dung mới và rà soát trước khi công bố.");
        DaRaSoatCongKhai = true;
        TrangThai = CONG_BO;
    }

    // BR16: chỉ đóng nhu cầu đã công bố khi có ít nhất một ca và mọi ca đã kết thúc hoặc đã hủy.
    public void Dong()
    {
        if (TrangThai != CONG_BO)
            throw LoiNghiepVu.XungDot("Chỉ đóng được nhu cầu đã công bố.");
        if (CaLam.Count == 0)
            throw LoiNghiepVu.XungDot("Nhu cầu chưa có ca nào nên chưa đóng được. Nếu không cần nữa, hãy hủy nhu cầu.");
        if (CaLam.Any(c => c.TrangThai is ThucThe.CaLam.MO or ThucThe.CaLam.DANG_DIEN_RA))
            throw LoiNghiepVu.XungDot("Chỉ đóng nhu cầu khi tất cả các ca đã kết thúc hoặc đã hủy.");
        TrangThai = DONG;
    }

    // BR16: chỉ hủy nhu cầu nháp hoặc đã công bố khi chưa có ca nào bắt đầu. Các ca còn mở và đăng ký
    // còn hiệu lực của chúng bị hủy theo, lịch sử vẫn giữ.
    public void Huy(DateTime bayGio)
    {
        if (TrangThai != NHAP && TrangThai != CONG_BO)
            throw LoiNghiepVu.XungDot("Nhu cầu đã đóng hoặc đã hủy.");
        if (CaLam.Any(c => c.TrangThai is ThucThe.CaLam.DANG_DIEN_RA or ThucThe.CaLam.DA_KET_THUC
                           || (c.TrangThai == ThucThe.CaLam.MO && bayGio >= c.BatDauDuKien)))
            throw LoiNghiepVu.XungDot("Đã có ca bắt đầu nên không hủy được nhu cầu. Khi các ca kết thúc, hãy đóng nhu cầu.");
        foreach (var ca in CaLam.Where(c => c.TrangThai == ThucThe.CaLam.MO))
        {
            ca.Huy(bayGio);
            foreach (var dk in ca.DangKy) dk.Huy();
        }
        TrangThai = DA_HUY;
    }

    private void GanNoiDung(string tieuDe, string tomTat, string khuVuc)
    {
        if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(tomTat) || string.IsNullOrWhiteSpace(khuVuc))
            throw LoiNghiepVu.KhongHopLe("Tiêu đề, tóm tắt công khai và khu vực chung không được để trống.");
        if (tieuDe.Trim().Length > 150 || khuVuc.Trim().Length > 150)
            throw LoiNghiepVu.KhongHopLe("Tiêu đề và khu vực chung tối đa 150 ký tự.");
        TieuDe = tieuDe.Trim();
        TomTatCongKhai = tomTat.Trim();
        KhuVucChung = khuVuc.Trim();
    }
}

// Hướng dẫn riêng, địa chỉ cụ thể và vị trí chính xác của nhu cầu.
// Chỉ đọc sau khi ChinhSachTruyCap cho phép (BR15, BR21).
public class ThongTinDieuPhoiRieng
{
    public string MaNhuCau { get; set; } = "";
    public string? HuongDanRieng { get; set; }
    public string? DiaChiCuThe { get; set; }
    public decimal? ViDo { get; set; }
    public decimal? KinhDo { get; set; }

    public bool Trong => HuongDanRieng == null && DiaChiCuThe == null && ViDo == null;

    public static Task<ThongTinDieuPhoiRieng?> LayNoiDung(KhoNghiepVu kho, string maNhuCau)
        => kho.ThongTinDieuPhoiRieng.FirstOrDefaultAsync(r => r.MaNhuCau == maNhuCau);
}
