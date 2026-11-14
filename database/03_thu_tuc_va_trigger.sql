-- Trigger và thủ tục lưu trữ của CSDL DieuPhoiTinhNguyenVien.
-- Chạy sau 01_tao_bang.sql. Dùng CREATE OR ALTER nên chạy lại nhiều lần được.
--
-- Lớp xử lý của ứng dụng đã kiểm tra các quy tắc trước khi lưu để báo lỗi rõ cho người dùng.
-- Trigger kiểm tra lại ngay trong CSDL, để dữ liệu vẫn đúng khi có thao tác không đi qua
-- ứng dụng hoặc khi nhiều giao dịch chạy cùng lúc.
--
-- Mã lỗi đặt theo dạng 50xxy: xx là số của quy tắc nghiệp vụ (Bảng 1.4), y là thứ tự lỗi trong quy tắc.
--   50031  BR03  bỏ kỹ năng đang cần cho ca đã phân công
--   50061  BR06  tạo ca cho nhu cầu chưa công bố
--   50062  BR06  đổi tổ chức sở hữu của nhu cầu
--   50071  BR07  đăng ký ca không còn mở hoặc nhu cầu không còn công bố
--   50091  BR09  phân công người thiếu kỹ năng
--   50092  BR09  phân công vượt số người cần
--   50093  BR09  phân công trùng lịch
--   50111  BR11  số người cần thấp hơn số người đã phân công
--   50112  BR11  đổi thời gian của ca đã có người được phân công
--   50113  BR11  đổi kỹ năng của ca đã có người được phân công
--   50121  BR12  thiếu thời điểm khi chuyển mốc giờ
--   50131  BR13  bản khai không thuộc đăng ký đã phân công hoặc ngoài khung giờ của ca
--   50141  BR14  người duyệt tự xác nhận bản khai của mình
--   50142  BR14  sửa bản khai đã xác nhận
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ---------------------------------------------------------------- TRIGGER

-- BR06: chỉ tạo ca cho nhu cầu đã công bố.
-- BR11: ca đã có người được phân công thì không đổi thời gian; số người cần không thấp hơn số đã phân công.
CREATE OR ALTER TRIGGER dbo.trg_CaLam_KiemTra
ON dbo.CaLam
AFTER INSERT, UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF NOT EXISTS (SELECT 1 FROM deleted)
  BEGIN
    IF EXISTS (
      SELECT 1
      FROM inserted AS i
      JOIN dbo.NhuCauCongDong AS n ON n.MaNhuCau = i.MaNhuCau
      WHERE n.TrangThai <> 'CONG_BO')
      THROW 50061, N'Chỉ tạo ca cho nhu cầu đã công bố.', 1;
    RETURN;
  END;

  IF UPDATE(SoNguoiCan) AND EXISTS (
    SELECT 1
    FROM inserted AS i
    WHERE i.SoNguoiCan < (SELECT COUNT(*) FROM dbo.DangKyCaLam AS d
                          WHERE d.MaCaLam = i.MaCaLam AND d.TrangThai = 'DA_PHAN_CONG'))
    THROW 50111, N'Số người cần không được thấp hơn số người đã được phân công.', 1;

  IF (UPDATE(BatDauDuKien) OR UPDATE(KetThucDuKien)) AND EXISTS (
    SELECT 1
    FROM inserted AS i
    JOIN deleted AS x ON x.MaCaLam = i.MaCaLam
    WHERE (i.BatDauDuKien <> x.BatDauDuKien OR i.KetThucDuKien <> x.KetThucDuKien)
      AND EXISTS (SELECT 1 FROM dbo.DangKyCaLam AS d
                  WHERE d.MaCaLam = i.MaCaLam AND d.TrangThai = 'DA_PHAN_CONG'))
    THROW 50112, N'Ca đã có người được phân công nên không được đổi thời gian.', 1;
END;
GO

-- BR11: ca đã có người được phân công thì không thêm, bỏ hoặc đổi kỹ năng yêu cầu.
CREATE OR ALTER TRIGGER dbo.trg_YeuCauKyNang_KiemTraSua
ON dbo.YeuCauKyNang
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1
    FROM (SELECT MaCaLam FROM inserted UNION SELECT MaCaLam FROM deleted) AS c
    WHERE EXISTS (SELECT 1 FROM dbo.DangKyCaLam AS d
                  WHERE d.MaCaLam = c.MaCaLam AND d.TrangThai = 'DA_PHAN_CONG'))
    THROW 50113, N'Ca đã có người được phân công nên không được đổi kỹ năng yêu cầu.', 1;
END;
GO

-- BR07: đăng ký mới chỉ tạo cho ca đang mở thuộc nhu cầu đã công bố.
-- BR09, BR10: khi đăng ký chuyển sang Đã phân công thì người đó phải đủ kỹ năng của ca,
-- ca không vượt số người cần và người đó không có ca đã phân công nào giao về thời gian.
CREATE OR ALTER TRIGGER dbo.trg_DangKyCaLam_KiemTra
ON dbo.DangKyCaLam
AFTER INSERT, UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1
    FROM inserted AS i
    JOIN dbo.CaLam AS c ON c.MaCaLam = i.MaCaLam
    JOIN dbo.NhuCauCongDong AS n ON n.MaNhuCau = c.MaNhuCau
    WHERE NOT EXISTS (SELECT 1 FROM deleted AS x WHERE x.MaDangKy = i.MaDangKy)
      AND (c.TrangThai <> 'MO' OR n.TrangThai <> 'CONG_BO'))
    THROW 50071, N'Chỉ đăng ký được ca đang mở thuộc nhu cầu đã công bố.', 1;

  -- Các dòng vừa chuyển sang Đã phân công trong lệnh này.
  DECLARE @MoiPhanCong TABLE (MaDangKy varchar(36) PRIMARY KEY, MaHoSo varchar(36), MaCaLam varchar(36));
  INSERT INTO @MoiPhanCong (MaDangKy, MaHoSo, MaCaLam)
  SELECT i.MaDangKy, i.MaHoSo, i.MaCaLam
  FROM inserted AS i
  LEFT JOIN deleted AS x ON x.MaDangKy = i.MaDangKy
  WHERE i.TrangThai = 'DA_PHAN_CONG' AND (x.MaDangKy IS NULL OR x.TrangThai <> 'DA_PHAN_CONG');

  IF NOT EXISTS (SELECT 1 FROM @MoiPhanCong)
    RETURN;

  IF EXISTS (
    SELECT 1
    FROM @MoiPhanCong AS m
    JOIN dbo.YeuCauKyNang AS y ON y.MaCaLam = m.MaCaLam
    WHERE NOT EXISTS (SELECT 1 FROM dbo.KyNangSoHuu AS s
                      WHERE s.MaHoSo = m.MaHoSo AND s.MaKyNang = y.MaKyNang))
    THROW 50091, N'Tình nguyện viên chưa có đủ kỹ năng mà ca yêu cầu.', 1;

  IF EXISTS (
    SELECT 1
    FROM (SELECT DISTINCT MaCaLam FROM @MoiPhanCong) AS m
    JOIN dbo.CaLam AS c ON c.MaCaLam = m.MaCaLam
    WHERE (SELECT COUNT(*) FROM dbo.DangKyCaLam AS d
           WHERE d.MaCaLam = c.MaCaLam AND d.TrangThai = 'DA_PHAN_CONG') > c.SoNguoiCan)
    THROW 50092, N'Ca đã đủ người, không thể phân công thêm.', 1;

  IF EXISTS (
    SELECT 1
    FROM @MoiPhanCong AS m
    JOIN dbo.CaLam AS c1 ON c1.MaCaLam = m.MaCaLam
    JOIN dbo.DangKyCaLam AS d2
      ON d2.MaHoSo = m.MaHoSo AND d2.MaDangKy <> m.MaDangKy AND d2.TrangThai = 'DA_PHAN_CONG'
    JOIN dbo.CaLam AS c2 ON c2.MaCaLam = d2.MaCaLam AND c2.TrangThai <> 'DA_HUY'
    WHERE c1.BatDauDuKien < c2.KetThucDuKien
      AND c2.BatDauDuKien < c1.KetThucDuKien)
    THROW 50093, N'Tình nguyện viên đã được phân công một ca khác trùng thời gian.', 1;
END;
GO

-- BR13: bản khai thuộc đăng ký đã phân công của ca chưa bị hủy, thời gian nằm trong khung giờ của ca.
-- BR14: người duyệt không xác nhận bản của chính mình; bản đã xác nhận không được sửa.
CREATE OR ALTER TRIGGER dbo.trg_DongGop_KiemTra
ON dbo.DongGop
AFTER INSERT, UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (SELECT 1 FROM deleted WHERE TrangThai = 'DA_XAC_NHAN')
    THROW 50142, N'Bản khai đã được xác nhận nên không được sửa.', 1;

  IF EXISTS (
    SELECT 1
    FROM inserted AS i
    JOIN dbo.DangKyCaLam AS d ON d.MaDangKy = i.MaDangKy
    JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
    WHERE d.TrangThai <> 'DA_PHAN_CONG'
       OR c.TrangThai = 'DA_HUY'
       OR i.BatDauThucTe < c.BatDauDuKien
       OR i.KetThucThucTe > c.KetThucDuKien)
    THROW 50131, N'Bản khai phải thuộc đăng ký đã phân công và nằm trong khung giờ của ca.', 1;

  IF EXISTS (
    SELECT 1
    FROM inserted AS i
    JOIN dbo.DangKyCaLam AS d ON d.MaDangKy = i.MaDangKy
    JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = d.MaHoSo
    WHERE i.MaNguoiXacNhan = h.MaNguoiDung)
    THROW 50141, N'Người duyệt không được xác nhận bản khai của chính mình.', 1;
END;
GO

-- BR06: tổ chức sở hữu của nhu cầu không thay đổi sau khi tạo.
CREATE OR ALTER TRIGGER dbo.trg_NhuCauCongDong_GiuToChuc
ON dbo.NhuCauCongDong
AFTER UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF UPDATE(MaToChuc) AND EXISTS (
    SELECT 1 FROM inserted AS i JOIN deleted AS x ON x.MaNhuCau = i.MaNhuCau
    WHERE i.MaToChuc <> x.MaToChuc)
    THROW 50062, N'Không được đổi tổ chức sở hữu của nhu cầu.', 1;
END;
GO

-- BR03: không bỏ kỹ năng đang cần cho ca đã được phân công và chưa kết thúc.
-- Xét cả xóa dòng lẫn sửa dòng sang kỹ năng khác; nếu kỹ năng vẫn còn trong hồ sơ thì cho qua.
CREATE OR ALTER TRIGGER dbo.trg_KyNangSoHuu_GiuKyNangDangCan
ON dbo.KyNangSoHuu
AFTER UPDATE, DELETE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1
    FROM deleted AS x
    JOIN dbo.DangKyCaLam AS d ON d.MaHoSo = x.MaHoSo AND d.TrangThai = 'DA_PHAN_CONG'
    JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam AND c.TrangThai IN ('MO', 'DANG_DIEN_RA')
    JOIN dbo.YeuCauKyNang AS y ON y.MaCaLam = c.MaCaLam AND y.MaKyNang = x.MaKyNang
    WHERE NOT EXISTS (SELECT 1 FROM dbo.KyNangSoHuu AS s
                      WHERE s.MaHoSo = x.MaHoSo AND s.MaKyNang = x.MaKyNang))
    THROW 50031, N'Không được bỏ kỹ năng đang cần cho ca đã được phân công và chưa kết thúc.', 1;
END;
GO

-- ---------------------------------------------------------------- THỦ TỤC LƯU TRỮ

-- BR12: chuyển trạng thái ca theo giờ. Tác vụ nền của máy chủ ứng dụng gọi định kỳ và truyền
-- thời điểm hiện tại (giờ UTC), để thủ tục dùng cùng đồng hồ với phần còn lại của ứng dụng.
-- Đến giờ bắt đầu: ca sang Đang diễn ra. Ca không còn mở thì đăng ký còn chờ duyệt bị hủy.
-- Đến giờ kết thúc: ca sang Đã kết thúc.
CREATE OR ALTER PROCEDURE dbo.sp_ChuyenMocThoiGianCa
  @BayGio datetime2(0)
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  IF @BayGio IS NULL
    THROW 50121, N'Thiếu thời điểm hiện tại.', 1;

  DECLARE @SoCaBatDau int, @SoDangKyBiHuy int, @SoCaKetThuc int;

  BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.CaLam
    SET TrangThai = 'DANG_DIEN_RA'
    WHERE TrangThai = 'MO' AND BatDauDuKien <= @BayGio;
    SET @SoCaBatDau = @@ROWCOUNT;

    UPDATE d
    SET TrangThai = 'DA_HUY'
    FROM dbo.DangKyCaLam AS d
    JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
    WHERE d.TrangThai = 'CHO_DUYET' AND c.TrangThai <> 'MO';
    SET @SoDangKyBiHuy = @@ROWCOUNT;

    UPDATE dbo.CaLam
    SET TrangThai = 'DA_KET_THUC'
    WHERE TrangThai = 'DANG_DIEN_RA' AND KetThucDuKien <= @BayGio;
    SET @SoCaKetThuc = @@ROWCOUNT;

    COMMIT TRANSACTION;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
  END CATCH;

  SELECT @SoCaBatDau AS SoCaBatDau, @SoCaKetThuc AS SoCaKetThuc, @SoDangKyBiHuy AS SoDangKyBiHuy;
END;
GO

-- BR04, BR09: danh sách người đăng ký một ca kèm gợi ý cho điều phối viên:
-- có đủ kỹ năng của ca không, có trùng lịch với ca đã được phân công không.
-- Thủ tục chỉ gắn nhãn, điều phối viên vẫn là người quyết định.
CREATE OR ALTER PROCEDURE dbo.sp_GoiYUngVien
  @MaCaLam varchar(36)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    d.MaDangKy,
    n.TenHienThi,
    d.TrangThai,
    d.NgayDangKy,
    ISNULL((SELECT STRING_AGG(k.TenKyNang, N'; ') WITHIN GROUP (ORDER BY k.TenKyNang)
            FROM dbo.KyNangSoHuu AS s
            JOIN dbo.KyNang AS k ON k.MaKyNang = s.MaKyNang
            WHERE s.MaHoSo = d.MaHoSo), N'') AS KyNangDaKhai,
    CAST(CASE WHEN EXISTS (
           SELECT 1 FROM dbo.YeuCauKyNang AS y
           WHERE y.MaCaLam = d.MaCaLam
             AND NOT EXISTS (SELECT 1 FROM dbo.KyNangSoHuu AS s
                             WHERE s.MaHoSo = d.MaHoSo AND s.MaKyNang = y.MaKyNang))
         THEN 0 ELSE 1 END AS bit) AS DuKyNang,
    CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.DangKyCaLam AS d2
           JOIN dbo.CaLam AS c2 ON c2.MaCaLam = d2.MaCaLam
           WHERE d2.MaHoSo = d.MaHoSo AND d2.MaDangKy <> d.MaDangKy
             AND d2.TrangThai = 'DA_PHAN_CONG' AND c2.TrangThai <> 'DA_HUY'
             AND c2.BatDauDuKien < c.KetThucDuKien AND c.BatDauDuKien < c2.KetThucDuKien)
         THEN 1 ELSE 0 END AS bit) AS TrungLich
  FROM dbo.DangKyCaLam AS d
  JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
  JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = d.MaHoSo
  JOIN dbo.NguoiDung AS n ON n.MaNguoiDung = h.MaNguoiDung
  WHERE d.MaCaLam = @MaCaLam
  ORDER BY d.NgayDangKy, d.MaDangKy;
END;
GO

-- BR14: tổng thời gian đóng góp đã được xác nhận của một hồ sơ, lấy từ view DongGopDaXacNhan.
-- Trả về tổng số giây; ứng dụng đổi sang giờ và chỉ làm tròn khi hiển thị.
CREATE OR ALTER PROCEDURE dbo.sp_TongGioDongGop
  @MaHoSo varchar(36)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    COUNT(*) AS SoBanDaXacNhan,
    ISNULL(SUM(SoGiay), 0) AS TongSoGiay
  FROM dbo.DongGopDaXacNhan
  WHERE MaHoSo = @MaHoSo;
END;
GO
