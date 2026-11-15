-- Tài khoản CSDL riêng cho máy chủ ứng dụng, chỉ có những quyền ứng dụng cần.
-- Chạy trong ngữ cảnh CSDL đích bằng sqlcmd, truyền mật khẩu qua biến:
--   sqlcmd ... -v MatKhauUngDung="..." -i 04_tai_khoan_ung_dung.sql
IF SUSER_ID(N'civ09_app') IS NULL
  CREATE LOGIN civ09_app WITH PASSWORD = N'$(MatKhauUngDung)', CHECK_POLICY = ON;
ELSE
  ALTER LOGIN civ09_app WITH PASSWORD = N'$(MatKhauUngDung)';
GO
IF USER_ID(N'civ09_app') IS NULL
  CREATE USER civ09_app FOR LOGIN civ09_app;
GO

-- Tài khoản: người mới được lập tài khoản ở lần đăng nhập đầu tiên; tên, tên đăng nhập, email được cập nhật
-- theo Keycloak mỗi lần đăng nhập; quản trị viên khóa hoặc mở khóa tài khoản (BR02, BR28). Không xóa tài khoản.
GRANT SELECT, INSERT ON dbo.NguoiDung TO civ09_app;
GRANT UPDATE ON dbo.NguoiDung (TenHienThi, TenDangNhap, Email, DangHoatDong) TO civ09_app;
GRANT SELECT, INSERT ON dbo.NguoiDungVaiTro TO civ09_app;

-- Tổ chức được đăng ký rồi duyệt; tư cách thành viên được thêm và ngừng, không xóa (BR28).
GRANT SELECT, INSERT, UPDATE ON dbo.ToChucCongDong TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.ThanhVienToChuc TO civ09_app;

-- Hồ sơ, lịch rảnh và kỹ năng đã khai.
GRANT SELECT, INSERT, UPDATE ON dbo.HoSoTinhNguyenVien TO civ09_app;
GRANT SELECT, INSERT, DELETE ON dbo.LichRanh TO civ09_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.KyNangSoHuu TO civ09_app;

-- Danh mục kỹ năng do quản trị viên quản lý; kỹ năng không xóa mà chuyển sang ngừng dùng (BR17).
GRANT SELECT, INSERT, UPDATE ON dbo.NhomKyNang TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.KyNang TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.DeXuatKyNang TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.ChungChiKyNang TO civ09_app;

-- Ảnh: được xóa khi người dùng bỏ ảnh khỏi nhu cầu hoặc bản khai còn sửa được.
GRANT SELECT, INSERT, DELETE ON dbo.TepDinhKem TO civ09_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.AnhNhuCau TO civ09_app;
GRANT SELECT, INSERT, DELETE ON dbo.AnhMinhChung TO civ09_app;

-- Nhu cầu, ca, đăng ký, điểm danh, đóng góp chỉ đổi trạng thái để giữ lịch sử nên không có quyền xóa.
-- Hướng dẫn riêng được xóa khi đại diện để trống toàn bộ. Kỹ năng yêu cầu của ca được bỏ khi sửa ca (BR11).
GRANT SELECT, INSERT, UPDATE ON dbo.NhuCauCongDong TO civ09_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.ThongTinDieuPhoiRieng TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.CaLam TO civ09_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.YeuCauKyNang TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.DangKyCaLam TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.DiemDanh TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.DongGop TO civ09_app;
GRANT SELECT, INSERT ON dbo.GiayXacNhan TO civ09_app;
GRANT SELECT, INSERT ON dbo.GiayXacNhanDongGop TO civ09_app;

-- Thông báo chỉ được đánh dấu đã đọc. Nhật ký truy cập chỉ được thêm (BR25, BR26).
GRANT SELECT, INSERT ON dbo.ThongBao TO civ09_app;
GRANT UPDATE ON dbo.ThongBao (DaDoc, NgayDoc) TO civ09_app;
GRANT SELECT, INSERT ON dbo.NhatKyTruyCap TO civ09_app;

-- View và thủ tục lưu trữ.
GRANT SELECT ON dbo.CaCongKhai TO civ09_app;
GRANT SELECT ON dbo.DongGopDaXacNhan TO civ09_app;
GRANT SELECT ON dbo.MucTinCay TO civ09_app;
GRANT EXECUTE ON dbo.sp_ChuyenMocThoiGianCa TO civ09_app;
GRANT EXECUTE ON dbo.sp_GoiYUngVien TO civ09_app;
GRANT EXECUTE ON dbo.sp_TongGioDongGop TO civ09_app;
GRANT EXECUTE ON dbo.sp_TaoNhacTruocCa TO civ09_app;
GRANT EXECUTE ON dbo.sp_ThongKeKyNangThieu TO civ09_app;
GO
