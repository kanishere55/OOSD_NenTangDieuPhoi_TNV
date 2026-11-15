namespace Civ09.Api.HoTro;

// Thông tin để chấm mức phù hợp giữa một tình nguyện viên và một ca (BR20).
public record ThongTinGhep(
    bool DuKyNang,
    int SoKyNangCanChungChi,
    int SoKyNangDaXacMinh,
    double? KhoangCachKm,
    bool? KhopLichRanh,
    int SoLanCoMat,
    int SoLanVang);

public record KetQuaCham(int Diem, List<string> LyDo, List<string> LuuY);

// Chấm điểm phù hợp trên thang 100 để xếp hạng gợi ý. Đây chỉ là gợi ý: điều kiện bắt buộc
// (đủ kỹ năng, còn chỗ, không trùng lịch) vẫn do DieuKienPhanCong kiểm tra (BR20).
//   Đủ kỹ năng và cấp độ: 40 điểm, thiếu thì 0 điểm cho cả ca.
//   Chứng chỉ đã xác minh: 10 điểm, chia theo tỉ lệ kỹ năng cần chứng chỉ đã được xác minh.
//   Khoảng cách: 20 điểm khi ở rất gần, giảm dần về 0 ở 15 km; chưa có vị trí thì 10 điểm.
//   Lịch rảnh: 15 điểm khi buổi bắt đầu ca nằm trong lịch rảnh; chưa khai lịch thì 7 điểm.
//   Mức tin cậy: 15 điểm nhân tỉ lệ có mặt; chưa có lần điểm danh nào thì 10 điểm.
public static class ChamDiemPhuHop
{
    public const double KhoangCachXaNhatKm = 15;

    public static KetQuaCham Cham(ThongTinGhep t)
    {
        var lyDo = new List<string>();
        var luuY = new List<string>();
        if (!t.DuKyNang)
            return new KetQuaCham(0, lyDo, new List<string> { "Chưa đủ kỹ năng hoặc cấp độ mà ca yêu cầu" });

        double diem = 40;
        lyDo.Add("Đủ kỹ năng và cấp độ");

        if (t.SoKyNangCanChungChi == 0) diem += 10;
        else
        {
            diem += 10.0 * t.SoKyNangDaXacMinh / t.SoKyNangCanChungChi;
            if (t.SoKyNangDaXacMinh == t.SoKyNangCanChungChi) lyDo.Add("Chứng chỉ đã được xác minh");
            else luuY.Add("Chứng chỉ chưa được xác minh");
        }

        if (t.KhoangCachKm is double km)
        {
            diem += 20 * Math.Max(0, 1 - km / KhoangCachXaNhatKm);
            if (km <= 3) lyDo.Add($"Ở gần, khoảng {LamTron(km)} km");
            else if (km > 10) luuY.Add($"Ở xa, khoảng {LamTron(km)} km");
            else lyDo.Add($"Cách khoảng {LamTron(km)} km");
        }
        else diem += 10;

        switch (t.KhopLichRanh)
        {
            case true: diem += 15; lyDo.Add("Trùng buổi đã khai là rảnh"); break;
            case false: luuY.Add("Không trùng buổi đã khai là rảnh"); break;
            default: diem += 7; break;
        }

        var tong = t.SoLanCoMat + t.SoLanVang;
        if (tong == 0) diem += 10;
        else
        {
            diem += 15.0 * t.SoLanCoMat / tong;
            if (t.SoLanVang == 0) lyDo.Add($"Có mặt đủ {t.SoLanCoMat}/{tong} ca");
            else luuY.Add($"Vắng không báo {t.SoLanVang} lần, có mặt {t.SoLanCoMat}/{tong} ca");
        }
        return new KetQuaCham((int)Math.Round(diem), lyDo, luuY);
    }

    private static string LamTron(double km) => km < 1 ? "dưới 1" : Math.Round(km, km < 10 ? 1 : 0).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
}
