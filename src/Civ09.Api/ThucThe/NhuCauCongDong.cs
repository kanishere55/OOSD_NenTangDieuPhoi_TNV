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
    public int PhienBanCongKhai { get; set; } = 1;
    public bool DaRaSoatCongKhai { get; set; }
    public string TrangThai { get; set; } = NHAP;

    public ToChucCongDong? ToChuc { get; set; }
    public ThongTinDieuPhoiRieng? ThongTinRieng { get; set; }
    public List<CaLam> CaLam { get; set; } = new();

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

    // BR05: sửa nội dung công khai thì tăng phiên bản và phải rà soát lại.
    public void CapNhatCongKhai(string tieuDe, string tomTat, string khuVuc)
    {
        tieuDe = tieuDe.Trim(); tomTat = tomTat.Trim(); khuVuc = khuVuc.Trim();
        if (tieuDe == TieuDe && tomTat == TomTatCongKhai && khuVuc == KhuVucChung) return;
        if (TrangThai != NHAP)
            throw LoiNghiepVu.XungDot("Chỉ sửa được nội dung công khai khi nhu cầu còn là bản nháp.");
        GanNoiDung(tieuDe, tomTat, khuVuc);
        PhienBanCongKhai++;
        DaRaSoatCongKhai = false;
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

// Hướng dẫn riêng của nhu cầu. Chỉ đọc sau khi ChinhSachTruyCap cho phép (BR15).
public class ThongTinDieuPhoiRieng
{
    public string MaNhuCau { get; set; } = "";
    public string HuongDanRieng { get; set; } = "";

    public static Task<ThongTinDieuPhoiRieng?> LayNoiDung(KhoNghiepVu kho, string maNhuCau)
        => kho.ThongTinDieuPhoiRieng.FirstOrDefaultAsync(r => r.MaNhuCau == maNhuCau);
}
