namespace Civ09.Api.HoTro;

// Tính khoảng cách và làm mờ vị trí (BR21).
public static class ViTri
{
    public const int BanKinhVungMacDinh = 1000;
    private const double BanKinhTraiDatKm = 6371.0;

    public static void KiemTra(decimal viDo, decimal kinhDo)
    {
        if (viDo < -90 || viDo > 90 || kinhDo < -180 || kinhDo > 180)
            throw LoiNghiepVu.KhongHopLe("Vị trí trên bản đồ chưa đúng.");
    }

    // Khoảng cách theo đường chim bay giữa hai điểm (công thức haversine), đơn vị km.
    public static double KhoangCachKm(double viDo1, double kinhDo1, double viDo2, double kinhDo2)
    {
        static double Rad(double d) => d * Math.PI / 180.0;
        var dVi = Rad(viDo2 - viDo1);
        var dKinh = Rad(kinhDo2 - kinhDo1);
        var a = Math.Sin(dVi / 2) * Math.Sin(dVi / 2)
                + Math.Cos(Rad(viDo1)) * Math.Cos(Rad(viDo2)) * Math.Sin(dKinh / 2) * Math.Sin(dKinh / 2);
        return 2 * BanKinhTraiDatKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static double? KhoangCachKm(decimal? viDo1, decimal? kinhDo1, decimal? viDo2, decimal? kinhDo2)
        => viDo1 == null || kinhDo1 == null || viDo2 == null || kinhDo2 == null
            ? null
            : KhoangCachKm((double)viDo1, (double)kinhDo1, (double)viDo2, (double)kinhDo2);

    // Dời tâm ngẫu nhiên khỏi vị trí thật một khoảng từ 20% đến 60% bán kính, nên vị trí thật vẫn nằm trong
    // vòng tròn công khai nhưng không trùng tâm của nó.
    public static (double ViDo, double KinhDo) LamMo(double viDo, double kinhDo, int banKinhMet, Random ngauNhien)
    {
        var khoang = banKinhMet * (0.2 + 0.4 * ngauNhien.NextDouble());
        var huong = ngauNhien.NextDouble() * 2 * Math.PI;
        var dVi = khoang * Math.Cos(huong) / 111_320.0;
        var dKinh = khoang * Math.Sin(huong) / (111_320.0 * Math.Cos(viDo * Math.PI / 180.0));
        return (viDo + dVi, kinhDo + dKinh);
    }
}
