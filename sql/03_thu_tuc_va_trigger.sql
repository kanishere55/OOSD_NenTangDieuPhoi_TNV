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
--   50132  BR13  khai đóng góp cho đăng ký đã bị ghi vắng mặt
--   50181  BR18  yêu cầu xác minh với kỹ năng không cần chứng chỉ
--   50182  BR18  hạ cấp độ kỹ năng dưới mức ca đã phân công đang cần
--   50191  BR19  người xác minh tự xác minh chứng chỉ của mình
--   50211  BR22  nhu cầu có quá 5 ảnh
--   50221  BR23  điểm danh cho đăng ký chưa phân công hoặc ngoài khung giờ cho phép
--   50231  BR24  bản khai có quá 5 ảnh minh chứng
--   50261  BR26  sửa hoặc xóa nhật ký truy cập
--   50281  BR28  tạo nhu cầu cho tổ chức không còn hoạt động
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

  -- BR18: có kỹ năng, cấp độ không thấp hơn yêu cầu, đã xác minh nếu ca yêu cầu.
  IF EXISTS (
    SELECT 1
    FROM @MoiPhanCong AS m
    JOIN dbo.YeuCauKyNang AS y ON y.MaCaLam = m.MaCaLam
    WHERE NOT EXISTS (SELECT 1 FROM dbo.KyNangSoHuu AS s
                      WHERE s.MaHoSo = m.MaHoSo AND s.MaKyNang = y.MaKyNang
                        AND s.CapDo >= y.CapDoToiThieu
                        AND (y.CanXacMinh = 0 OR EXISTS (
                              SELECT 1 FROM dbo.ChungChiKyNang AS cc
                              WHERE cc.MaHoSo = m.MaHoSo AND cc.MaKyNang = y.MaKyNang
                                AND cc.TrangThai = 'DA_XAC_MINH'))))
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
    JOIN dbo.DiemDanh AS dd ON dd.MaDangKy = i.MaDangKy
    WHERE dd.KetQua = 'VANG_MAT')
    THROW 50132, N'Đăng ký đã được ghi nhận vắng mặt nên không khai đóng góp được.', 1;

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

  -- BR18: không hạ cấp độ dưới mức mà ca đã phân công, chưa kết thúc đang cần.
  IF EXISTS (
    SELECT 1
    FROM inserted AS i
    JOIN dbo.DangKyCaLam AS d ON d.MaHoSo = i.MaHoSo AND d.TrangThai = 'DA_PHAN_CONG'
    JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam AND c.TrangThai IN ('MO', 'DANG_DIEN_RA')
    JOIN dbo.YeuCauKyNang AS y ON y.MaCaLam = c.MaCaLam AND y.MaKyNang = i.MaKyNang
    WHERE i.CapDo < y.CapDoToiThieu)
    THROW 50182, N'Không được hạ cấp độ kỹ năng dưới mức mà ca đã được phân công đang cần.', 1;
END;
GO

-- BR18: chỉ yêu cầu đã xác minh với kỹ năng cần chứng chỉ.
CREATE OR ALTER TRIGGER dbo.trg_YeuCauKyNang_KiemTraXacMinh
ON dbo.YeuCauKyNang
AFTER INSERT, UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1 FROM inserted AS i JOIN dbo.KyNang AS k ON k.MaKyNang = i.MaKyNang
    WHERE i.CanXacMinh = 1 AND k.CanChungChi = 0)
    THROW 50181, N'Chỉ yêu cầu đã xác minh với kỹ năng cần chứng chỉ.', 1;
END;
GO

-- BR19: người xác minh không phải chủ của chứng chỉ.
CREATE OR ALTER TRIGGER dbo.trg_ChungChiKyNang_KiemTra
ON dbo.ChungChiKyNang
AFTER INSERT, UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1 FROM inserted AS i JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = i.MaHoSo
    WHERE i.MaNguoiXacMinh = h.MaNguoiDung)
    THROW 50191, N'Không được tự xác minh chứng chỉ của mình.', 1;
END;
GO

-- BR22: mỗi nhu cầu có tối đa 5 ảnh.
CREATE OR ALTER TRIGGER dbo.trg_AnhNhuCau_GioiHan
ON dbo.AnhNhuCau
AFTER INSERT
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1 FROM (SELECT DISTINCT MaNhuCau FROM inserted) AS i
    WHERE (SELECT COUNT(*) FROM dbo.AnhNhuCau AS a WHERE a.MaNhuCau = i.MaNhuCau) > 5)
    THROW 50211, N'Mỗi nhu cầu có tối đa 5 ảnh.', 1;
END;
GO

-- BR24: mỗi bản khai có tối đa 5 ảnh minh chứng.
CREATE OR ALTER TRIGGER dbo.trg_AnhMinhChung_GioiHan
ON dbo.AnhMinhChung
AFTER INSERT
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1 FROM (SELECT DISTINCT MaDongGop FROM inserted) AS i
    WHERE (SELECT COUNT(*) FROM dbo.AnhMinhChung AS a WHERE a.MaDongGop = i.MaDongGop) > 5)
    THROW 50231, N'Mỗi bản khai có tối đa 5 ảnh minh chứng.', 1;
END;
GO

-- BR23: chỉ điểm danh đăng ký đã phân công; giờ vào từ 30 phút trước giờ bắt đầu đến giờ kết thúc,
-- giờ ra chậm nhất 2 giờ sau giờ kết thúc.
CREATE OR ALTER TRIGGER dbo.trg_DiemDanh_KiemTra
ON dbo.DiemDanh
AFTER INSERT, UPDATE
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1
    FROM inserted AS i
    JOIN dbo.DangKyCaLam AS d ON d.MaDangKy = i.MaDangKy
    JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
    WHERE d.TrangThai <> 'DA_PHAN_CONG'
       OR c.TrangThai = 'DA_HUY'
       OR i.GioVao < DATEADD(minute, -30, c.BatDauDuKien)
       OR i.GioVao > c.KetThucDuKien
       OR i.GioRa > DATEADD(hour, 2, c.KetThucDuKien))
    THROW 50221, N'Chỉ điểm danh người đã được phân công và trong khung giờ cho phép của ca.', 1;
END;
GO

-- BR26: nhật ký truy cập chỉ được thêm, không sửa, không xóa.
CREATE OR ALTER TRIGGER dbo.trg_NhatKyTruyCap_ChiThem
ON dbo.NhatKyTruyCap
INSTEAD OF UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  THROW 50261, N'Nhật ký truy cập không được sửa hoặc xóa.', 1;
END;
GO

-- BR28: chỉ tổ chức đang hoạt động mới tạo nhu cầu.
CREATE OR ALTER TRIGGER dbo.trg_NhuCauCongDong_ToChucHoatDong
ON dbo.NhuCauCongDong
AFTER INSERT
AS
BEGIN
  IF @@ROWCOUNT = 0 RETURN;
  SET NOCOUNT ON;

  IF EXISTS (
    SELECT 1 FROM inserted AS i JOIN dbo.ToChucCongDong AS t ON t.MaToChuc = i.MaToChuc
    WHERE t.TrangThai <> 'HOAT_DONG')
    THROW 50281, N'Tổ chức chưa được duyệt hoặc đã ngừng hoạt động nên chưa tạo được nhu cầu.', 1;
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

    -- BR25: báo cho người có đăng ký chờ duyệt bị hủy vì ca đã bắt đầu.
    INSERT INTO dbo.ThongBao (MaNguoiNhan, Loai, NoiDung, DuongDan, MaThamChieu, NgayTao)
    SELECT h.MaNguoiDung, 'DANG_KY_BI_HUY',
           LEFT(N'Ca "' + n.TieuDe + N'" đã bắt đầu nên đăng ký đang chờ duyệt của bạn không còn hiệu lực.', 500),
           N'/dang-ky-cua-toi', d.MaDangKy, @BayGio
    FROM dbo.DangKyCaLam AS d
    JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
    JOIN dbo.NhuCauCongDong AS n ON n.MaNhuCau = c.MaNhuCau
    JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = d.MaHoSo
    WHERE d.TrangThai = 'CHO_DUYET' AND c.TrangThai = 'DANG_DIEN_RA';

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

-- BR04, BR09, BR18, BR20, BR27: danh sách người đăng ký một ca kèm các thông tin để điều phối viên chọn người:
-- đủ kỹ năng (có xét cấp độ và xác minh), còn thiếu xác minh, trùng lịch, buổi bắt đầu ca có nằm trong
-- lịch rảnh không, vị trí để tính khoảng cách, số lần có mặt và vắng mặt đã ghi nhận.
-- Thủ tục chỉ cung cấp thông tin, điều phối viên vẫn là người quyết định.
CREATE OR ALTER PROCEDURE dbo.sp_GoiYUngVien
  @MaCaLam varchar(36)
AS
BEGIN
  SET NOCOUNT ON;

  -- Thứ (1 là thứ hai) và buổi của giờ bắt đầu ca theo giờ Việt Nam (UTC+7). Ngày 01/01/1900 là thứ hai.
  DECLARE @BatDauVn datetime2(0) = (SELECT DATEADD(hour, 7, BatDauDuKien) FROM dbo.CaLam WHERE MaCaLam = @MaCaLam);
  DECLARE @Thu tinyint = DATEDIFF(day, '19000101', @BatDauVn) % 7 + 1;
  DECLARE @Buoi varchar(5) = CASE WHEN DATEPART(hour, @BatDauVn) < 12 THEN 'SANG'
                                  WHEN DATEPART(hour, @BatDauVn) < 18 THEN 'CHIEU' ELSE 'TOI' END;

  SELECT
    d.MaDangKy,
    d.MaHoSo,
    n.TenHienThi,
    d.TrangThai,
    d.NgayDangKy,
    -- Ví dụ: "Sơ cứu (khá, đã xác minh); Tin học cơ bản (cơ bản)".
    ISNULL((SELECT STRING_AGG(x.MoTa, N'; ') WITHIN GROUP (ORDER BY x.TenKyNang)
            FROM (SELECT k.TenKyNang,
                         k.TenKyNang + N' (' +
                         CASE s.CapDo WHEN 1 THEN N'cơ bản' WHEN 2 THEN N'khá' ELSE N'thành thạo' END +
                         CASE WHEN EXISTS (SELECT 1 FROM dbo.ChungChiKyNang AS cc
                                           WHERE cc.MaHoSo = s.MaHoSo AND cc.MaKyNang = s.MaKyNang
                                             AND cc.TrangThai = 'DA_XAC_MINH') THEN N', đã xác minh' ELSE N'' END + N')' AS MoTa
                  FROM dbo.KyNangSoHuu AS s
                  JOIN dbo.KyNang AS k ON k.MaKyNang = s.MaKyNang
                  WHERE s.MaHoSo = d.MaHoSo) AS x), N'') AS KyNangDaKhai,
    CAST(CASE WHEN EXISTS (
           SELECT 1 FROM dbo.YeuCauKyNang AS y
           WHERE y.MaCaLam = d.MaCaLam
             AND NOT EXISTS (SELECT 1 FROM dbo.KyNangSoHuu AS s
                             WHERE s.MaHoSo = d.MaHoSo AND s.MaKyNang = y.MaKyNang
                               AND s.CapDo >= y.CapDoToiThieu
                               AND (y.CanXacMinh = 0 OR EXISTS (
                                     SELECT 1 FROM dbo.ChungChiKyNang AS cc
                                     WHERE cc.MaHoSo = d.MaHoSo AND cc.MaKyNang = y.MaKyNang
                                       AND cc.TrangThai = 'DA_XAC_MINH'))))
         THEN 0 ELSE 1 END AS bit) AS DuKyNang,
    -- Có kỹ năng và đủ cấp độ nhưng chứng chỉ chưa được xác minh.
    CAST(CASE WHEN EXISTS (
           SELECT 1 FROM dbo.YeuCauKyNang AS y
           JOIN dbo.KyNangSoHuu AS s ON s.MaHoSo = d.MaHoSo AND s.MaKyNang = y.MaKyNang AND s.CapDo >= y.CapDoToiThieu
           WHERE y.MaCaLam = d.MaCaLam AND y.CanXacMinh = 1
             AND NOT EXISTS (SELECT 1 FROM dbo.ChungChiKyNang AS cc
                             WHERE cc.MaHoSo = d.MaHoSo AND cc.MaKyNang = y.MaKyNang AND cc.TrangThai = 'DA_XAC_MINH'))
         THEN 1 ELSE 0 END AS bit) AS ThieuXacMinh,
    -- Số kỹ năng cần chứng chỉ của ca mà người này đã được xác minh.
    (SELECT COUNT(*) FROM dbo.YeuCauKyNang AS y
     JOIN dbo.KyNang AS k ON k.MaKyNang = y.MaKyNang AND k.CanChungChi = 1
     WHERE y.MaCaLam = d.MaCaLam
       AND EXISTS (SELECT 1 FROM dbo.ChungChiKyNang AS cc
                   WHERE cc.MaHoSo = d.MaHoSo AND cc.MaKyNang = y.MaKyNang AND cc.TrangThai = 'DA_XAC_MINH')) AS SoKyNangDaXacMinh,
    (SELECT COUNT(*) FROM dbo.YeuCauKyNang AS y
     JOIN dbo.KyNang AS k ON k.MaKyNang = y.MaKyNang AND k.CanChungChi = 1
     WHERE y.MaCaLam = d.MaCaLam) AS SoKyNangCanChungChi,
    CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.DangKyCaLam AS d2
           JOIN dbo.CaLam AS c2 ON c2.MaCaLam = d2.MaCaLam
           WHERE d2.MaHoSo = d.MaHoSo AND d2.MaDangKy <> d.MaDangKy
             AND d2.TrangThai = 'DA_PHAN_CONG' AND c2.TrangThai <> 'DA_HUY'
             AND c2.BatDauDuKien < c.KetThucDuKien AND c.BatDauDuKien < c2.KetThucDuKien)
         THEN 1 ELSE 0 END AS bit) AS TrungLich,
    -- NULL khi người này chưa khai lịch rảnh.
    CAST(CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.LichRanh AS l WHERE l.MaHoSo = d.MaHoSo) THEN NULL
              WHEN EXISTS (SELECT 1 FROM dbo.LichRanh AS l WHERE l.MaHoSo = d.MaHoSo AND l.Thu = @Thu AND l.Buoi = @Buoi) THEN 1
              ELSE 0 END AS bit) AS KhopLichRanh,
    h.ViDo,
    h.KinhDo,
    ISNULL(m.SoLanCoMat, 0) AS SoLanCoMat,
    ISNULL(m.SoLanVang, 0) AS SoLanVang
  FROM dbo.DangKyCaLam AS d
  JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
  JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = d.MaHoSo
  JOIN dbo.NguoiDung AS n ON n.MaNguoiDung = h.MaNguoiDung
  LEFT JOIN dbo.MucTinCay AS m ON m.MaHoSo = d.MaHoSo
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

-- BR25: nhắc tình nguyện viên trước ca 24 giờ. Mỗi đăng ký chỉ nhắc một lần (chỉ mục UQ_ThongBao_NhacCa).
-- Tác vụ nền gọi định kỳ cùng lúc với sp_ChuyenMocThoiGianCa.
CREATE OR ALTER PROCEDURE dbo.sp_TaoNhacTruocCa
  @BayGio datetime2(0)
AS
BEGIN
  SET NOCOUNT ON;

  INSERT INTO dbo.ThongBao (MaNguoiNhan, Loai, NoiDung, DuongDan, MaThamChieu, NgayTao)
  SELECT h.MaNguoiDung, 'NHAC_CA',
         LEFT(N'Nhắc lịch: ca "' + n.TieuDe + N'" bắt đầu lúc ' +
              FORMAT(DATEADD(hour, 7, c.BatDauDuKien), 'HH:mm dd/MM/yyyy') + N'. Xem hướng dẫn riêng trước khi đi.', 500),
         N'/ca/' + c.MaCaLam + N'/huong-dan-rieng', d.MaDangKy, @BayGio
  FROM dbo.DangKyCaLam AS d
  JOIN dbo.CaLam AS c ON c.MaCaLam = d.MaCaLam
  JOIN dbo.NhuCauCongDong AS n ON n.MaNhuCau = c.MaNhuCau
  JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = d.MaHoSo
  WHERE d.TrangThai = 'DA_PHAN_CONG' AND c.TrangThai = 'MO'
    AND c.BatDauDuKien > @BayGio AND c.BatDauDuKien <= DATEADD(hour, 24, @BayGio)
    AND NOT EXISTS (SELECT 1 FROM dbo.ThongBao AS t
                    WHERE t.MaNguoiNhan = h.MaNguoiDung AND t.Loai = 'NHAC_CA' AND t.MaThamChieu = d.MaDangKy);

  SELECT @@ROWCOUNT AS SoNhacMoi;
END;
GO

-- Thống kê kỹ năng đang thiếu của một tổ chức: với các ca đang mở, chưa bắt đầu và còn thiếu người,
-- mỗi kỹ năng cần cho bao nhiêu ca, thiếu bao nhiêu chỗ, có bao nhiêu đăng ký chờ duyệt
-- và trên nền tảng có bao nhiêu tình nguyện viên đang hoạt động có kỹ năng đó.
CREATE OR ALTER PROCEDURE dbo.sp_ThongKeKyNangThieu
  @MaToChuc varchar(36),
  @BayGio datetime2(0)
AS
BEGIN
  SET NOCOUNT ON;

  WITH DemDangKy AS (
    SELECT c.MaCaLam, c.SoNguoiCan,
           SUM(CASE WHEN d.TrangThai = 'DA_PHAN_CONG' THEN 1 ELSE 0 END) AS SoDaPhanCong,
           SUM(CASE WHEN d.TrangThai = 'CHO_DUYET' THEN 1 ELSE 0 END) AS SoChoDuyet
    FROM dbo.CaLam AS c
    JOIN dbo.NhuCauCongDong AS n ON n.MaNhuCau = c.MaNhuCau
    LEFT JOIN dbo.DangKyCaLam AS d ON d.MaCaLam = c.MaCaLam
    WHERE n.MaToChuc = @MaToChuc AND n.TrangThai = 'CONG_BO'
      AND c.TrangThai = 'MO' AND c.BatDauDuKien > @BayGio
    GROUP BY c.MaCaLam, c.SoNguoiCan
  ),
  CaThieu AS (
    SELECT MaCaLam, SoNguoiCan - SoDaPhanCong AS SoThieu, SoChoDuyet FROM DemDangKy
  )
  SELECT k.MaKyNang, k.TenKyNang,
         COUNT(*) AS SoCa,
         SUM(ct.SoThieu) AS SoChoThieu,
         SUM(ct.SoChoDuyet) AS SoDangKyChoDuyet,
         (SELECT COUNT(*) FROM dbo.KyNangSoHuu AS s
          JOIN dbo.HoSoTinhNguyenVien AS h ON h.MaHoSo = s.MaHoSo
          JOIN dbo.NguoiDung AS u ON u.MaNguoiDung = h.MaNguoiDung AND u.DangHoatDong = 1
          WHERE s.MaKyNang = k.MaKyNang) AS SoTinhNguyenVienCoKyNang
  FROM CaThieu AS ct
  JOIN dbo.YeuCauKyNang AS y ON y.MaCaLam = ct.MaCaLam
  JOIN dbo.KyNang AS k ON k.MaKyNang = y.MaKyNang
  WHERE ct.SoThieu > 0
  GROUP BY k.MaKyNang, k.TenKyNang
  ORDER BY SUM(ct.SoThieu) DESC, k.TenKyNang;
END;
GO
