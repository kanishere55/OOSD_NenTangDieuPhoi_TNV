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

-- Tài khoản, vai trò, tổ chức và danh mục kỹ năng do quản trị nạp sẵn: ứng dụng chỉ đọc.
GRANT SELECT ON dbo.NguoiDung TO civ09_app;
GRANT SELECT ON dbo.NguoiDungVaiTro TO civ09_app;
GRANT SELECT ON dbo.ToChucCongDong TO civ09_app;
GRANT SELECT ON dbo.ThanhVienToChuc TO civ09_app;
GRANT SELECT ON dbo.KyNang TO civ09_app;

-- Hồ sơ chỉ cập nhật thời điểm khai kỹ năng; kỹ năng đã khai được thêm và bỏ.
GRANT SELECT, UPDATE ON dbo.HoSoTinhNguyenVien TO civ09_app;
GRANT SELECT, INSERT, DELETE ON dbo.KyNangSoHuu TO civ09_app;

-- Nhu cầu, ca, đăng ký, đóng góp chỉ đổi trạng thái để giữ lịch sử nên không có quyền xóa.
-- Hướng dẫn riêng được xóa khi đại diện để trống nội dung.
GRANT SELECT, INSERT, UPDATE ON dbo.NhuCauCongDong TO civ09_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.ThongTinDieuPhoiRieng TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.CaLam TO civ09_app;
GRANT SELECT, INSERT ON dbo.YeuCauKyNang TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.DangKyCaLam TO civ09_app;
GRANT SELECT, INSERT, UPDATE ON dbo.DongGop TO civ09_app;

-- View và thủ tục lưu trữ.
GRANT SELECT ON dbo.CaCongKhai TO civ09_app;
GRANT SELECT ON dbo.DongGopDaXacNhan TO civ09_app;
GRANT EXECUTE ON dbo.sp_ChuyenMocThoiGianCa TO civ09_app;
GRANT EXECUTE ON dbo.sp_GoiYUngVien TO civ09_app;
GRANT EXECUTE ON dbo.sp_TongGioDongGop TO civ09_app;
GO
