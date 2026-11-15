using Civ09.Api.HoTro;

namespace Civ09.Api.ThucThe;

// Đề xuất thêm một kỹ năng vào danh mục chung (BR17).
public class DeXuatKyNang
{
    public const string CHO_DUYET = "CHO_DUYET";
    public const string DA_DUYET = "DA_DUYET";
    public const string TU_CHOI = "TU_CHOI";

    public string MaDeXuat { get; set; } = Guid.NewGuid().ToString();
    public string TenKyNang { get; set; } = "";
    public string? MoTa { get; set; }
    public string? LyDo { get; set; }
    public string MaNguoiDeXuat { get; set; } = "";
    public DateTime NgayDeXuat { get; set; }
    public string TrangThai { get; set; } = CHO_DUYET;
    public string? MaKyNang { get; set; }
    public string? MaNguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
    public string? TenChuanHoa { get; set; }

    public NguoiDung? NguoiDeXuat { get; set; }
    public KyNang? KyNang { get; set; }

    public static DeXuatKyNang Tao(string maNguoiDeXuat, string ten, string? moTa, string? lyDo, DateTime thoiDiem)
    {
        ten = (ten ?? "").Trim();
        if (ten.Length == 0) throw LoiNghiepVu.KhongHopLe("Hãy nhập tên kỹ năng muốn đề xuất.");
        if (ten.Length > 100) throw LoiNghiepVu.KhongHopLe("Tên kỹ năng tối đa 100 ký tự.");
        return new DeXuatKyNang
        {
            TenKyNang = ten,
            MoTa = ToChucCongDong.CatChuoi(moTa, 300),
            LyDo = ToChucCongDong.CatChuoi(lyDo, 500),
            MaNguoiDeXuat = maNguoiDeXuat,
            NgayDeXuat = thoiDiem
        };
    }

    // Duyệt bằng một kỹ năng mới vừa thêm vào danh mục, hoặc bằng một kỹ năng đã có sẵn.
    public void Duyet(string maKyNang, string maQuanTri, DateTime thoiDiem)
    {
        if (TrangThai != CHO_DUYET) throw LoiNghiepVu.XungDot("Đề xuất này đã được xử lý.");
        TrangThai = DA_DUYET;
        MaKyNang = maKyNang;
        MaNguoiDuyet = maQuanTri;
        NgayDuyet = thoiDiem;
    }

    public void TuChoi(string? lyDo, string maQuanTri, DateTime thoiDiem)
    {
        if (string.IsNullOrWhiteSpace(lyDo)) throw LoiNghiepVu.KhongHopLe("Khi từ chối phải ghi lý do.");
        if (TrangThai != CHO_DUYET) throw LoiNghiepVu.XungDot("Đề xuất này đã được xử lý.");
        TrangThai = TU_CHOI;
        LyDoTuChoi = ToChucCongDong.CatChuoi(lyDo, 500);
        MaNguoiDuyet = maQuanTri;
        NgayDuyet = thoiDiem;
    }
}

// Chứng chỉ của một kỹ năng cần chứng chỉ (BR19). Mỗi lần tải là một dòng mới để giữ lịch sử.
public class ChungChiKyNang
{
    public const string CHO_XAC_MINH = "CHO_XAC_MINH";
    public const string DA_XAC_MINH = "DA_XAC_MINH";
    public const string TU_CHOI = "TU_CHOI";
    public const string HET_HIEU_LUC = "HET_HIEU_LUC";

    public string MaChungChi { get; set; } = Guid.NewGuid().ToString();
    public string MaHoSo { get; set; } = "";
    public string MaKyNang { get; set; } = "";
    public string MaTep { get; set; } = "";
    public DateTime NgayGui { get; set; }
    public string TrangThai { get; set; } = CHO_XAC_MINH;
    public string? MaNguoiXacMinh { get; set; }
    public DateTime? NgayXacMinh { get; set; }
    public string? LyDoTuChoi { get; set; }

    public HoSoTinhNguyenVien? HoSo { get; set; }
    public KyNang? KyNang { get; set; }
    public TepDinhKem? Tep { get; set; }

    // Xác minh hợp lệ thì chứng chỉ đã xác minh trước đó (nếu có) của cùng kỹ năng hết hiệu lực.
    public void XacNhanHopLe(string maNguoiXacMinh, DateTime thoiDiem, ChungChiKyNang? banCu)
    {
        if (TrangThai != CHO_XAC_MINH) throw LoiNghiepVu.XungDot("Chứng chỉ này đã được xử lý.");
        if (banCu != null && banCu.MaChungChi != MaChungChi) banCu.TrangThai = HET_HIEU_LUC;
        TrangThai = DA_XAC_MINH;
        MaNguoiXacMinh = maNguoiXacMinh;
        NgayXacMinh = thoiDiem;
    }

    public void TuChoi(string maNguoiXacMinh, string? lyDo, DateTime thoiDiem)
    {
        if (string.IsNullOrWhiteSpace(lyDo)) throw LoiNghiepVu.KhongHopLe("Khi từ chối chứng chỉ phải ghi lý do.");
        if (TrangThai != CHO_XAC_MINH) throw LoiNghiepVu.XungDot("Chứng chỉ này đã được xử lý.");
        TrangThai = TU_CHOI;
        LyDoTuChoi = ToChucCongDong.CatChuoi(lyDo, 500);
        MaNguoiXacMinh = maNguoiXacMinh;
        NgayXacMinh = thoiDiem;
    }
}
