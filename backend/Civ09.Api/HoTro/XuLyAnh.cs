using Civ09.Api.ThucThe;
using SkiaSharp;

namespace Civ09.Api.HoTro;

// Kiểm tra và chuẩn hóa ảnh trước khi lưu vào CSDL (BR22): chỉ nhận JPEG, PNG không quá 5 MB;
// xoay ảnh theo hướng chụp, thu nhỏ cạnh dài nhất còn 1600 điểm ảnh rồi mã hóa lại thành JPEG.
// Ảnh mã hóa lại không mang theo thông tin đi kèm của ảnh gốc (vị trí GPS, thiết bị, ngày chụp).
public static class XuLyAnh
{
    public const int KichThuocToiDa = 5 * 1024 * 1024;
    public const int CanhDaiToiDa = 1600;

    public static async Task<TepDinhKem> ChuanHoaAsync(IFormFile tep, string maNguoiTai, DateTime thoiDiem)
    {
        if (tep == null || tep.Length == 0) throw LoiNghiepVu.KhongHopLe("Chưa chọn ảnh.");
        if (tep.Length > KichThuocToiDa) throw LoiNghiepVu.KhongHopLe("Ảnh lớn hơn 5 MB. Hãy chọn ảnh nhỏ hơn.");
        await using var luong = tep.OpenReadStream();
        using var bo = new MemoryStream();
        await luong.CopyToAsync(bo);
        return ChuanHoa(bo.ToArray(), tep.FileName, maNguoiTai, thoiDiem);
    }

    public static TepDinhKem ChuanHoa(byte[] duLieu, string? tenTep, string maNguoiTai, DateTime thoiDiem)
    {
        if (duLieu.Length == 0) throw LoiNghiepVu.KhongHopLe("Chưa chọn ảnh.");
        if (duLieu.Length > KichThuocToiDa) throw LoiNghiepVu.KhongHopLe("Ảnh lớn hơn 5 MB. Hãy chọn ảnh nhỏ hơn.");

        using var codec = SKCodec.Create(new MemoryStream(duLieu));
        if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
            throw LoiNghiepVu.KhongHopLe("Chỉ nhận ảnh JPG hoặc PNG.");
        var huong = codec.EncodedOrigin;
        using var goc = SKBitmap.Decode(codec)
                        ?? throw LoiNghiepVu.KhongHopLe("Không đọc được ảnh. Hãy chọn ảnh khác.");

        using var daXoay = XoayTheoHuongChup(goc, huong);
        var tiLe = Math.Min(1.0, (double)CanhDaiToiDa / Math.Max(daXoay.Width, daXoay.Height));
        var rong = Math.Max(1, (int)Math.Round(daXoay.Width * tiLe));
        var cao = Math.Max(1, (int)Math.Round(daXoay.Height * tiLe));

        // Vẽ lên nền trắng: ảnh PNG có vùng trong suốt sẽ không bị đen khi lưu dạng JPEG.
        using var ketQua = new SKBitmap(new SKImageInfo(rong, cao, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var nen = new SKCanvas(ketQua))
        {
            nen.Clear(SKColors.White);
            using var anhVe = SKImage.FromBitmap(daXoay);
            nen.DrawImage(anhVe, new SKRect(0, 0, rong, cao), new SKSamplingOptions(SKCubicResampler.Mitchell));
        }
        using var jpeg = SKImage.FromBitmap(ketQua).Encode(SKEncodedImageFormat.Jpeg, 82);
        var noiDung = jpeg.ToArray();

        var ten = Path.GetFileNameWithoutExtension(tenTep ?? "");
        if (string.IsNullOrWhiteSpace(ten)) ten = "anh";
        if (ten.Length > 190) ten = ten[..190];
        return new TepDinhKem
        {
            TenTep = ten + ".jpg",
            LoaiNoiDung = "image/jpeg",
            KichThuoc = noiDung.Length,
            ChieuRong = rong,
            ChieuCao = cao,
            NoiDung = noiDung,
            NgayTai = thoiDiem,
            MaNguoiTai = maNguoiTai
        };
    }

    // Ảnh chụp bằng điện thoại thường lưu hướng xoay riêng; xoay lại để ảnh hiện đúng chiều.
    private static SKBitmap XoayTheoHuongChup(SKBitmap anh, SKEncodedOrigin huong)
    {
        var doiCanh = huong is SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom
            or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom;
        var rong = doiCanh ? anh.Height : anh.Width;
        var cao = doiCanh ? anh.Width : anh.Height;
        var moi = new SKBitmap(new SKImageInfo(rong, cao, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var c = new SKCanvas(moi);
        switch (huong)
        {
            case SKEncodedOrigin.TopRight: c.Translate(rong, 0); c.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: c.Translate(rong, cao); c.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: c.Translate(0, cao); c.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop:
            case SKEncodedOrigin.LeftTop: c.Translate(rong, 0); c.RotateDegrees(90); break;
            case SKEncodedOrigin.LeftBottom:
            case SKEncodedOrigin.RightBottom: c.Translate(0, cao); c.RotateDegrees(270); break;
        }
        c.DrawBitmap(anh, 0, 0);
        return moi;
    }
}
