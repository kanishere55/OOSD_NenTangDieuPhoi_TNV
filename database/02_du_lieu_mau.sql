-- Dữ liệu mẫu để chạy thử. Toàn bộ là dữ liệu giả, không có thông tin người thụ hưởng thật.
-- Hai tổ chức đang hoạt động, đại diện, điều phối viên, quản trị viên và danh mục kỹ năng được nạp sẵn.
-- Tình nguyện viên mới có thể tự đăng ký; ba tài khoản tnv1, tnv2, tnv3 ở đây có sẵn để chạy thử.
-- MaDinhDanhOIDC trùng với id người dùng trong keycloak/civ09-realm.json.
SET NOCOUNT ON;

-- Người dùng
INSERT INTO dbo.NguoiDung (MaNguoiDung, MaDinhDanhOIDC, TenHienThi, TenDangNhap, Email) VALUES
 ('ND-01', 'aaaaaaaa-0000-4000-8000-000000000001', N'Trần Thị Mai', N'daidien1', N'daidien1@civ09.local'),
 ('ND-02', 'aaaaaaaa-0000-4000-8000-000000000002', N'Lê Văn Hùng', N'dieuphoi1', N'dieuphoi1@civ09.local'),
 ('ND-03', 'aaaaaaaa-0000-4000-8000-000000000003', N'Phạm Ngọc Lan', N'dieuphoi2', N'dieuphoi2@civ09.local'),
 ('ND-04', 'aaaaaaaa-0000-4000-8000-000000000004', N'Nguyễn Minh Khoa', N'tnv1', N'tnv1@civ09.local'),
 ('ND-05', 'aaaaaaaa-0000-4000-8000-000000000005', N'Võ Thị Hạnh', N'tnv2', N'tnv2@civ09.local'),
 ('ND-06', 'aaaaaaaa-0000-4000-8000-000000000006', N'Đặng Quốc Bảo', N'tnv3', N'tnv3@civ09.local'),
 ('ND-07', 'aaaaaaaa-0000-4000-8000-000000000007', N'Hoàng Gia Huy', N'dieuphoi_tnv', N'dieuphoi_tnv@civ09.local'),
 ('ND-08', 'aaaaaaaa-0000-4000-8000-000000000008', N'Đỗ Minh Quân', N'quantri1', N'quantri1@civ09.local');

INSERT INTO dbo.NguoiDungVaiTro (MaNguoiDung, VaiTro) VALUES
 ('ND-01', 'DAI_DIEN'),
 ('ND-02', 'DIEU_PHOI'),
 ('ND-03', 'DIEU_PHOI'),
 ('ND-04', 'TNV'),
 ('ND-05', 'TNV'),
 ('ND-06', 'TNV'),
 ('ND-07', 'DIEU_PHOI'),
 ('ND-07', 'TNV'),
 ('ND-08', 'QUAN_TRI');

-- Tổ chức
INSERT INTO dbo.ToChucCongDong (MaToChuc, TenToChuc, MoTa, KhuVucHoatDong, TrangThai) VALUES
 ('TC-A', N'Nhóm Thiện nguyện Phường An Bình', N'Nhóm tình nguyện của khu phố, hỗ trợ người lớn tuổi và trẻ em trong phường.', N'Phường An Bình', 'HOAT_DONG'),
 ('TC-B', N'Câu lạc bộ Tình nguyện Xanh', N'Câu lạc bộ của sinh viên, chuyển hàng cứu trợ và làm sạch môi trường.', N'Phường Tân Lập', 'HOAT_DONG');

INSERT INTO dbo.ThanhVienToChuc (MaNguoiDung, MaToChuc, VaiTro) VALUES
 ('ND-01', 'TC-A', 'DAI_DIEN'),
 ('ND-02', 'TC-A', 'DIEU_PHOI'),
 ('ND-03', 'TC-B', 'DIEU_PHOI'),
 ('ND-07', 'TC-A', 'DIEU_PHOI');

-- Nhóm kỹ năng và danh mục kỹ năng
INSERT INTO dbo.NhomKyNang (MaNhom, TenNhom, ThuTu) VALUES
 ('NHOM-YTE', N'Y tế và chăm sóc', 1),
 ('NHOM-GIAO-DUC', N'Giáo dục và công nghệ', 2),
 ('NHOM-HAU-CAN', N'Hậu cần và vận chuyển', 3),
 ('NHOM-BEP', N'Nấu ăn', 4);

INSERT INTO dbo.KyNang (MaKyNang, TenKyNang, MoTaKyNang, MaNhom, CanChungChi) VALUES
 ('KN-TIN-HOC', N'Tin học cơ bản', N'Dùng máy tính, điện thoại, gửi email, tra cứu thông tin', 'NHOM-GIAO-DUC', 0),
 ('KN-SO-CUU', N'Sơ cứu', N'Sơ cứu ban đầu các vết thương nhẹ', 'NHOM-YTE', 1),
 ('KN-NAU-AN', N'Nấu ăn', N'Nấu ăn số lượng lớn cho bếp cộng đồng', 'NHOM-BEP', 0),
 ('KN-DAY-KEM', N'Dạy kèm', N'Kèm học sinh tiểu học và trung học cơ sở', 'NHOM-GIAO-DUC', 0),
 ('KN-VAN-CHUYEN', N'Vận chuyển hàng', N'Bốc xếp, chở hàng cứu trợ bằng xe máy', 'NHOM-HAU-CAN', 0),
 ('KN-LAO-DONG', N'Lao động phổ thông', N'Dọn dẹp, sắp xếp, làm các việc tay chân nhẹ', 'NHOM-HAU-CAN', 0),
 ('KN-TIENG-ANH', N'Tiếng Anh giao tiếp', N'Nghe nói tiếng Anh ở mức trò chuyện thông thường', 'NHOM-GIAO-DUC', 0),
 ('KN-LAI-XE', N'Lái xe ô tô', N'Lái xe chở hàng hoặc đưa đón, cần giấy phép lái xe hạng B trở lên', 'NHOM-HAU-CAN', 1),
 ('KN-CHAM-SOC', N'Chăm sóc người cao tuổi', N'Trò chuyện, hỗ trợ đi lại, nhắc uống thuốc theo hướng dẫn của gia đình', 'NHOM-YTE', 0);

-- Hồ sơ tình nguyện viên, vị trí gần đúng nơi ở và kỹ năng đã khai (CapDo: 1 cơ bản, 2 khá, 3 thành thạo)
INSERT INTO dbo.HoSoTinhNguyenVien (MaHoSo, MaNguoiDung, NoiO, ViDo, KinhDo, KhoangCachToiDa) VALUES
 ('HS-04', 'ND-04', N'Khu phố 2, Phường An Bình', 10.848000, 106.770000, 10),
 ('HS-05', 'ND-05', N'Phường Tân Lập', 10.870000, 106.800000, 5),
 ('HS-06', 'ND-06', NULL, NULL, NULL, NULL),
 ('HS-07', 'ND-07', NULL, NULL, NULL, NULL);

INSERT INTO dbo.KyNangSoHuu (MaHoSo, MaKyNang, CapDo) VALUES
 ('HS-04', 'KN-TIN-HOC', 2),
 ('HS-04', 'KN-SO-CUU', 1),
 ('HS-05', 'KN-NAU-AN', 3),
 ('HS-06', 'KN-TIN-HOC', 1),
 ('HS-06', 'KN-DAY-KEM', 2),
 ('HS-07', 'KN-TIN-HOC', 2);

-- Một nhu cầu đã công bố kèm hướng dẫn riêng, và một nhu cầu còn nháp.
-- Vùng công khai có tâm lệch khỏi vị trí thật khoảng 600 m, bán kính 1 km.
INSERT INTO dbo.NhuCauCongDong (MaNhuCau, MaToChuc, TieuDe, TomTatCongKhai, KhuVucChung,
                                ViDoVung, KinhDoVung, BanKinhVung,
                                PhienBanCongKhai, DaRaSoatCongKhai, TrangThai) VALUES
 ('NC-01', 'TC-A', N'Hướng dẫn dùng điện thoại thông minh cho người lớn tuổi',
  N'Hướng dẫn các cô chú lớn tuổi gọi video, nhắn tin và tra cứu thông tin y tế trên điện thoại.',
  N'Phường An Bình', 10.853900, 106.768200, 1000, 1, 1, 'CONG_BO'),
 ('NC-02', 'TC-A', N'Bếp ăn cuối tuần',
  N'Phụ nấu và chia suất ăn tại bếp cộng đồng vào sáng Chủ nhật.',
  N'Phường An Bình', NULL, NULL, NULL, 1, 0, 'NHAP');

INSERT INTO dbo.ThongTinDieuPhoiRieng (MaNhuCau, HuongDanRieng, DiaChiCuThe, ViDo, KinhDo) VALUES
 ('NC-01', N'Vào bằng cửa bên hông. Liên hệ chị Mai khi đến. Một số cô chú nghe kém, cần nói chậm và rõ.',
  N'Nhà văn hóa khu phố 3, Phường An Bình', 10.850320, 106.772510);

-- Hai ca sắp tới của nhu cầu NC-01 (giờ lưu theo UTC: 01:00 UTC = 08:00 giờ Việt Nam)
DECLARE @homNay datetime2(0) = CAST(CAST(SYSUTCDATETIME() AS date) AS datetime2(0));
INSERT INTO dbo.CaLam (MaCaLam, MaNhuCau, BatDauDuKien, KetThucDuKien, SoNguoiCan) VALUES
 ('CA-01', 'NC-01', DATEADD(hour, 1, DATEADD(day, 3, @homNay)), DATEADD(hour, 3, DATEADD(day, 3, @homNay)), 2),
 ('CA-02', 'NC-01', DATEADD(hour, 1, DATEADD(day, 5, @homNay)), DATEADD(hour, 4, DATEADD(day, 5, @homNay)), 1);

INSERT INTO dbo.YeuCauKyNang (MaCaLam, MaKyNang) VALUES
 ('CA-01', 'KN-TIN-HOC'),
 ('CA-02', 'KN-TIN-HOC');
GO
