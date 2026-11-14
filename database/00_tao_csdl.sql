-- Tạo CSDL của nền tảng điều phối tình nguyện viên.
IF DB_ID(N'DieuPhoiTinhNguyenVien') IS NULL
  CREATE DATABASE DieuPhoiTinhNguyenVien COLLATE Vietnamese_CI_AS;
GO
