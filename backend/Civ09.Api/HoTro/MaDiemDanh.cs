using System.Security.Cryptography;
using System.Text;

namespace Civ09.Api.HoTro;

// Mã điểm danh của một ca, đổi mỗi 2 phút (BR23). Mã được tính từ khóa riêng của ca (lưu trong CSDL)
// và khung thời gian hiện tại; máy chủ chấp nhận mã của khung hiện tại và khung ngay trước đó,
// nên ảnh chụp mã gửi cho người ở xa sẽ nhanh chóng hết dùng được.
public static class MaDiemDanh
{
    public const int GiayMoiKhung = 120;

    public static string TaoKhoa() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static long KhungCua(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds() / GiayMoiKhung;

    public static string Tinh(string khoa, long khung)
    {
        using var h = new HMACSHA256(Convert.FromHexString(khoa));
        var bam = h.ComputeHash(Encoding.ASCII.GetBytes(khung.ToString()));
        return Convert.ToHexString(bam, 0, 5);
    }

    public static (string Ma, DateTime HetHan) MaHienTai(string khoa, DateTime bayGio)
    {
        var khung = KhungCua(bayGio);
        return (Tinh(khoa, khung), DateTimeOffset.FromUnixTimeSeconds((khung + 1) * GiayMoiKhung).UtcDateTime);
    }

    public static bool HopLe(string? khoa, string? ma, DateTime bayGio)
    {
        if (string.IsNullOrEmpty(khoa) || string.IsNullOrWhiteSpace(ma)) return false;
        var khung = KhungCua(bayGio);
        var m = ma.Trim().ToUpperInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(m), Encoding.ASCII.GetBytes(Tinh(khoa, khung)))
               || CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(m), Encoding.ASCII.GetBytes(Tinh(khoa, khung - 1)));
    }
}
