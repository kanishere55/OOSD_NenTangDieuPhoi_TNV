-- Dữ liệu mẫu để chạy thử. Toàn bộ là dữ liệu giả, không có thông tin người thụ hưởng thật.
-- Tài khoản, tổ chức và danh mục kỹ năng được cấp sẵn (nền tảng không có chức năng quản trị).
-- MaDinhDanhOIDC trùng với id người dùng trong keycloak/civ09-realm.json.
SET NOCOUNT ON;

-- Tổ chức
INSERT INTO dbo.ToChucCongDong (MaToChuc, TenToChuc) VALUES
 ('TC-A', N'Nhóm Thiện nguyện Phường An Bình'),
 ('TC-B', N'Câu lạc bộ Tình nguyện Xanh');

-- Danh mục kỹ năng
INSERT INTO dbo.KyNang (MaKyNang, TenKyNang, MoTaKyNang) VALUES
 ('KN-TIN-HOC', N'Tin học cơ bản', N'Dùng máy tính, điện thoại, gửi email, tra cứu thông tin'),
 ('KN-SO-CUU', N'Sơ cứu', N'Sơ cứu ban đầu các vết thương nhẹ'),
 ('KN-NAU-AN', N'Nấu ăn', N'Nấu ăn số lượng lớn cho bếp cộng đồng'),
 ('KN-DAY-KEM', N'Dạy kèm', N'Kèm học sinh tiểu học và trung học cơ sở'),
 ('KN-VAN-CHUYEN', N'Vận chuyển hàng', N'Bốc xếp, chở hàng cứu trợ bằng xe máy');

-- Người dùng
INSERT INTO dbo.NguoiDung (MaNguoiDung, MaDinhDanhOIDC, TenHienThi) VALUES
 ('ND-01', 'aaaaaaaa-0000-4000-8000-000000000001', N'Trần Thị Mai'),
 ('ND-02', 'aaaaaaaa-0000-4000-8000-000000000002', N'Lê Văn Hùng'),
 ('ND-03', 'aaaaaaaa-0000-4000-8000-000000000003', N'Phạm Ngọc Lan'),
 ('ND-04', 'aaaaaaaa-0000-4000-8000-000000000004', N'Nguyễn Minh Khoa'),
 ('ND-05', 'aaaaaaaa-0000-4000-8000-000000000005', N'Võ Thị Hạnh'),
 ('ND-06', 'aaaaaaaa-0000-4000-8000-000000000006', N'Đặng Quốc Bảo'),
 ('ND-07', 'aaaaaaaa-0000-4000-8000-000000000007', N'Hoàng Gia Huy');

INSERT INTO dbo.NguoiDungVaiTro (MaNguoiDung, VaiTro) VALUES
 ('ND-01', 'DAI_DIEN'),
 ('ND-02', 'DIEU_PHOI'),
 ('ND-03', 'DIEU_PHOI'),
 ('ND-04', 'TNV'),
 ('ND-05', 'TNV'),
 ('ND-06', 'TNV'),
 ('ND-07', 'DIEU_PHOI'),
 ('ND-07', 'TNV');

INSERT INTO dbo.ThanhVienToChuc (MaNguoiDung, MaToChuc, VaiTro) VALUES
 ('ND-01', 'TC-A', 'DAI_DIEN'),
 ('ND-02', 'TC-A', 'DIEU_PHOI'),
 ('ND-03', 'TC-B', 'DIEU_PHOI'),
 ('ND-07', 'TC-A', 'DIEU_PHOI');

-- Hồ sơ tình nguyện viên và kỹ năng đã khai
INSERT INTO dbo.HoSoTinhNguyenVien (MaHoSo, MaNguoiDung) VALUES
 ('HS-04', 'ND-04'),
 ('HS-05', 'ND-05'),
 ('HS-06', 'ND-06'),
 ('HS-07', 'ND-07');

INSERT INTO dbo.KyNangSoHuu (MaHoSo, MaKyNang) VALUES
 ('HS-04', 'KN-TIN-HOC'),
 ('HS-04', 'KN-SO-CUU'),
 ('HS-05', 'KN-NAU-AN'),
 ('HS-06', 'KN-TIN-HOC'),
 ('HS-06', 'KN-DAY-KEM'),
 ('HS-07', 'KN-TIN-HOC');

-- Một nhu cầu đã công bố kèm hướng dẫn riêng, và một nhu cầu còn nháp
INSERT INTO dbo.NhuCauCongDong (MaNhuCau, MaToChuc, TieuDe, TomTatCongKhai, KhuVucChung,
                                PhienBanCongKhai, DaRaSoatCongKhai, TrangThai) VALUES
 ('NC-01', 'TC-A', N'Hướng dẫn dùng điện thoại thông minh cho người lớn tuổi',
  N'Hướng dẫn các cô chú lớn tuổi gọi video, nhắn tin và tra cứu thông tin y tế trên điện thoại.',
  N'Phường An Bình', 1, 1, 'CONG_BO'),
 ('NC-02', 'TC-A', N'Bếp ăn cuối tuần',
  N'Phụ nấu và chia suất ăn tại bếp cộng đồng vào sáng Chủ nhật.',
  N'Phường An Bình', 1, 0, 'NHAP');

INSERT INTO dbo.ThongTinDieuPhoiRieng (MaNhuCau, HuongDanRieng) VALUES
 ('NC-01', N'Địa điểm: nhà văn hóa khu phố 3, cửa bên hông. Liên hệ chị Mai khi đến. Một số cô chú nghe kém, cần nói chậm và rõ.');

-- Hai ca sắp tới của nhu cầu NC-01 (giờ lưu theo UTC: 01:00 UTC = 08:00 giờ Việt Nam)
DECLARE @homNay datetime2(0) = CAST(CAST(SYSUTCDATETIME() AS date) AS datetime2(0));
INSERT INTO dbo.CaLam (MaCaLam, MaNhuCau, BatDauDuKien, KetThucDuKien, SoNguoiCan) VALUES
 ('CA-01', 'NC-01', DATEADD(hour, 1, DATEADD(day, 3, @homNay)), DATEADD(hour, 3, DATEADD(day, 3, @homNay)), 2),
 ('CA-02', 'NC-01', DATEADD(hour, 1, DATEADD(day, 5, @homNay)), DATEADD(hour, 4, DATEADD(day, 5, @homNay)), 1);

INSERT INTO dbo.YeuCauKyNang (MaCaLam, MaKyNang) VALUES
 ('CA-01', 'KN-TIN-HOC'),
 ('CA-02', 'KN-TIN-HOC');
GO
