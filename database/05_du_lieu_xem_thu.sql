-- Dữ liệu để xem thử các màn hình: thêm nhu cầu, ca sắp tới, ca đã qua, đăng ký và bản khai.
-- Chạy sau 03_thu_tuc_va_trigger.sql nên mọi dòng đều đi qua trigger kiểm tra của CSDL.
-- Bộ kiểm thử tự động không nạp tệp này. Toàn bộ là dữ liệu giả.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @homNay datetime2(0) = CAST(CAST(SYSUTCDATETIME() AS date) AS datetime2(0));

-- Thêm kỹ năng cho tình nguyện viên mẫu
INSERT INTO dbo.KyNangSoHuu (MaHoSo, MaKyNang) VALUES
 ('HS-05', 'KN-LAO-DONG'),
 ('HS-06', 'KN-LAO-DONG'),
 ('HS-04', 'KN-VAN-CHUYEN');

-- Thêm nhu cầu đã công bố của hai tổ chức
INSERT INTO dbo.NhuCauCongDong (MaNhuCau, MaToChuc, TieuDe, TomTatCongKhai, KhuVucChung,
                                PhienBanCongKhai, DaRaSoatCongKhai, TrangThai) VALUES
 ('NC-03', 'TC-A', N'Dạy kèm Toán và Tiếng Việt cho học sinh tiểu học',
  N'Kèm các em lớp 3 đến lớp 5 làm bài tập và ôn lại bài trên lớp vào buổi tối.',
  N'Phường An Bình', 1, 1, 'CONG_BO'),
 ('NC-04', 'TC-B', N'Chuyển nhu yếu phẩm đến các hộ khó khăn',
  N'Nhận gạo, mì và nước uống tại điểm tập kết rồi chở đến các hộ trong danh sách của câu lạc bộ.',
  N'Phường Tân Lập', 1, 1, 'CONG_BO'),
 ('NC-05', 'TC-A', N'Dọn vệ sinh sân chơi khu phố',
  N'Quét dọn, nhặt rác và lau các thiết bị ở sân chơi trẻ em trước dịp cuối tuần.',
  N'Phường An Bình', 1, 1, 'CONG_BO');

INSERT INTO dbo.ThongTinDieuPhoiRieng (MaNhuCau, HuongDanRieng) VALUES
 ('NC-03', N'Lớp học ở phòng sinh hoạt khu phố 5. Gặp chị Mai để nhận danh sách lớp. Không chụp ảnh các em khi chưa xin phép phụ huynh.'),
 ('NC-04', N'Tập trung tại kho của câu lạc bộ, số 14 đường số 3. Danh sách hộ nhận do chị Lan giữ, chỉ xem tại chỗ, không chụp lại.');

-- Ca sắp tới (giờ lưu theo UTC: 11:00 UTC = 18:00 giờ Việt Nam)
INSERT INTO dbo.CaLam (MaCaLam, MaNhuCau, BatDauDuKien, KetThucDuKien, SoNguoiCan) VALUES
 ('CA-03', 'NC-03', DATEADD(hour, 11, DATEADD(day, 2, @homNay)), DATEADD(hour, 13, DATEADD(day, 2, @homNay)), 2),
 ('CA-04', 'NC-03', DATEADD(hour, 11, DATEADD(day, 6, @homNay)), DATEADD(hour, 13, DATEADD(day, 6, @homNay)), 2),
 ('CA-05', 'NC-04', DATEADD(minute, 30, DATEADD(day, 4, @homNay)), DATEADD(minute, 210, DATEADD(day, 4, @homNay)), 3);
-- Ca đã qua: tạo ở trạng thái Đang mở để ghi đăng ký, sau đó chuyển sang Đã kết thúc
INSERT INTO dbo.CaLam (MaCaLam, MaNhuCau, BatDauDuKien, KetThucDuKien, SoNguoiCan) VALUES
 ('CA-06', 'NC-01', DATEADD(hour, 1, DATEADD(day, -2, @homNay)), DATEADD(hour, 3, DATEADD(day, -2, @homNay)), 2),
 ('CA-07', 'NC-05', DATEADD(day, -5, @homNay), DATEADD(hour, 2, DATEADD(day, -5, @homNay)), 2);

INSERT INTO dbo.YeuCauKyNang (MaCaLam, MaKyNang) VALUES
 ('CA-03', 'KN-DAY-KEM'),
 ('CA-04', 'KN-DAY-KEM'),
 ('CA-05', 'KN-VAN-CHUYEN'),
 ('CA-06', 'KN-TIN-HOC'),
 ('CA-07', 'KN-LAO-DONG');

-- Đăng ký đang chờ duyệt
INSERT INTO dbo.DangKyCaLam (MaDangKy, MaHoSo, MaCaLam, NgayDangKy) VALUES
 ('DK-01', 'HS-06', 'CA-01', DATEADD(hour, -20, SYSUTCDATETIME())),
 ('DK-02', 'HS-07', 'CA-01', DATEADD(hour, -5, SYSUTCDATETIME())),
 ('DK-03', 'HS-04', 'CA-05', DATEADD(hour, -3, SYSUTCDATETIME()));
-- Đăng ký đã được phân công
INSERT INTO dbo.DangKyCaLam (MaDangKy, MaHoSo, MaCaLam, NgayDangKy, TrangThai, MaNguoiPhanCong, NgayPhanCong) VALUES
 ('DK-04', 'HS-04', 'CA-02', DATEADD(day, -1, SYSUTCDATETIME()), 'DA_PHAN_CONG', 'ND-02', DATEADD(hour, -12, SYSUTCDATETIME())),
 ('DK-05', 'HS-06', 'CA-03', DATEADD(day, -1, SYSUTCDATETIME()), 'DA_PHAN_CONG', 'ND-02', DATEADD(hour, -10, SYSUTCDATETIME())),
 ('DK-06', 'HS-04', 'CA-06', DATEADD(day, -6, SYSUTCDATETIME()), 'DA_PHAN_CONG', 'ND-02', DATEADD(day, -5, SYSUTCDATETIME())),
 ('DK-07', 'HS-06', 'CA-06', DATEADD(day, -6, SYSUTCDATETIME()), 'DA_PHAN_CONG', 'ND-02', DATEADD(day, -5, SYSUTCDATETIME())),
 ('DK-08', 'HS-05', 'CA-07', DATEADD(day, -8, SYSUTCDATETIME()), 'DA_PHAN_CONG', 'ND-02', DATEADD(day, -7, SYSUTCDATETIME()));

UPDATE dbo.CaLam SET TrangThai = 'DA_KET_THUC' WHERE MaCaLam IN ('CA-06', 'CA-07');

-- Bản khai: một bản đã xác nhận, một bản đang chờ duyệt. Đăng ký DK-07 chưa khai.
INSERT INTO dbo.DongGop (MaDongGop, MaDangKy, BatDauThucTe, KetThucThucTe, MoTaCongViec, TrangThai, MaNguoiXacNhan, NgayXacNhan) VALUES
 ('DG-01', 'DK-06', DATEADD(hour, 1, DATEADD(day, -2, @homNay)), DATEADD(hour, 3, DATEADD(day, -2, @homNay)),
  N'Hướng dẫn 5 cô chú cài ứng dụng gọi video và lưu số điện thoại của con cháu.', 'DA_XAC_NHAN', 'ND-02', DATEADD(day, -1, SYSUTCDATETIME()));
INSERT INTO dbo.DongGop (MaDongGop, MaDangKy, BatDauThucTe, KetThucThucTe, MoTaCongViec) VALUES
 ('DG-02', 'DK-08', DATEADD(minute, 15, DATEADD(day, -5, @homNay)), DATEADD(hour, 2, DATEADD(day, -5, @homNay)),
  N'Quét sân, gom rác vào bao và lau cầu trượt, xích đu.');
GO
