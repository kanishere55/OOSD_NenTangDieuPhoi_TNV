-- Tạo bảng, ràng buộc, chỉ mục và view cho CSDL DieuPhoiTinhNguyenVien (SQL Server).
-- Chạy sau 00_tao_csdl.sql, trong ngữ cảnh của CSDL đích.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
CREATE TABLE dbo.NguoiDung (
  MaNguoiDung varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaDinhDanhOIDC varchar(255) NOT NULL UNIQUE,
  TenHienThi nvarchar(100) NOT NULL,
  DangHoatDong bit NOT NULL DEFAULT 1,
  PRIMARY KEY (MaNguoiDung),
  CHECK (LTRIM(RTRIM(TenHienThi)) <> '')
);
CREATE TABLE dbo.NguoiDungVaiTro (
  MaNguoiDung varchar(36) NOT NULL,
  VaiTro varchar(20) NOT NULL,
  PRIMARY KEY (MaNguoiDung, VaiTro),
  FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (VaiTro IN ('TNV', 'DAI_DIEN', 'DIEU_PHOI'))
);
CREATE TABLE dbo.ToChucCongDong (
  MaToChuc varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenToChuc nvarchar(150) NOT NULL,
  PRIMARY KEY (MaToChuc),
  CHECK (LTRIM(RTRIM(TenToChuc)) <> '')
);
CREATE TABLE dbo.ThanhVienToChuc (
  MaNguoiDung varchar(36) NOT NULL,
  MaToChuc varchar(36) NOT NULL,
  VaiTro varchar(20) NOT NULL,
  DangHoatDong bit NOT NULL DEFAULT 1,
  PRIMARY KEY (MaNguoiDung, MaToChuc, VaiTro),
  FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung),
  FOREIGN KEY (MaToChuc) REFERENCES dbo.ToChucCongDong(MaToChuc),
  FOREIGN KEY (MaNguoiDung, VaiTro) REFERENCES dbo.NguoiDungVaiTro(MaNguoiDung, VaiTro),
  CHECK (VaiTro IN ('DAI_DIEN', 'DIEU_PHOI'))
);
CREATE TABLE dbo.HoSoTinhNguyenVien (
  MaHoSo varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaNguoiDung varchar(36) NOT NULL UNIQUE,
  VaiTro varchar(20) NOT NULL DEFAULT 'TNV',
  NgayCapNhatKyNang datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (MaHoSo),
  FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung),
  FOREIGN KEY (MaNguoiDung, VaiTro) REFERENCES dbo.NguoiDungVaiTro(MaNguoiDung, VaiTro),
  CHECK (VaiTro IN ('TNV'))
);
CREATE TABLE dbo.KyNang (
  MaKyNang varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenKyNang nvarchar(100) NOT NULL,
  MoTaKyNang nvarchar(max) NULL,
  TenChuanHoa AS LOWER(LTRIM(RTRIM(TenKyNang))) PERSISTED,
  PRIMARY KEY (MaKyNang),
  CHECK (LTRIM(RTRIM(TenKyNang)) <> '')
);
CREATE UNIQUE INDEX UQ_KyNang_TenChuanHoa
  ON dbo.KyNang (TenChuanHoa);
CREATE TABLE dbo.KyNangSoHuu (
  MaHoSo varchar(36) NOT NULL,
  MaKyNang varchar(36) NOT NULL,
  NgayKhaiBao datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (MaHoSo, MaKyNang),
  FOREIGN KEY (MaHoSo) REFERENCES dbo.HoSoTinhNguyenVien(MaHoSo),
  FOREIGN KEY (MaKyNang) REFERENCES dbo.KyNang(MaKyNang)
);
CREATE TABLE dbo.NhuCauCongDong (
  MaNhuCau varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaToChuc varchar(36) NOT NULL,
  TieuDe nvarchar(150) NOT NULL,
  TomTatCongKhai nvarchar(max) NOT NULL,
  KhuVucChung nvarchar(150) NOT NULL,
  PhienBanCongKhai int NOT NULL DEFAULT 1,
  DaRaSoatCongKhai bit NOT NULL DEFAULT 0,
  TrangThai varchar(20) NOT NULL DEFAULT 'NHAP',
  PRIMARY KEY (MaNhuCau),
  FOREIGN KEY (MaToChuc) REFERENCES dbo.ToChucCongDong(MaToChuc),
  CHECK (TrangThai IN ('NHAP', 'CONG_BO', 'DONG', 'DA_HUY')),
  CHECK (LTRIM(RTRIM(TieuDe)) <> ''),
  CHECK (LTRIM(RTRIM(TomTatCongKhai)) <> ''),
  CHECK (LTRIM(RTRIM(KhuVucChung)) <> ''),
  CHECK (PhienBanCongKhai > 0),
  CHECK (TrangThai <> 'CONG_BO' OR DaRaSoatCongKhai = 1)
);
CREATE TABLE dbo.ThongTinDieuPhoiRieng (
  MaNhuCau varchar(36) NOT NULL,
  HuongDanRieng nvarchar(max) NOT NULL,
  PRIMARY KEY (MaNhuCau),
  FOREIGN KEY (MaNhuCau) REFERENCES dbo.NhuCauCongDong(MaNhuCau),
  CHECK (LTRIM(RTRIM(HuongDanRieng)) <> '')
);
CREATE TABLE dbo.CaLam (
  MaCaLam varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaNhuCau varchar(36) NOT NULL,
  BatDauDuKien datetime2(0) NOT NULL,
  KetThucDuKien datetime2(0) NOT NULL,
  SoNguoiCan int NOT NULL,
  TrangThai varchar(20) NOT NULL DEFAULT 'MO',
  PRIMARY KEY (MaCaLam),
  FOREIGN KEY (MaNhuCau) REFERENCES dbo.NhuCauCongDong(MaNhuCau),
  CHECK (TrangThai IN ('MO', 'DANG_DIEN_RA', 'DA_KET_THUC', 'DA_HUY')),
  CHECK (SoNguoiCan > 0),
  CHECK (BatDauDuKien < KetThucDuKien)
);
CREATE TABLE dbo.YeuCauKyNang (
  MaCaLam varchar(36) NOT NULL,
  MaKyNang varchar(36) NOT NULL,
  PRIMARY KEY (MaCaLam, MaKyNang),
  FOREIGN KEY (MaCaLam) REFERENCES dbo.CaLam(MaCaLam),
  FOREIGN KEY (MaKyNang) REFERENCES dbo.KyNang(MaKyNang)
);
CREATE TABLE dbo.DangKyCaLam (
  MaDangKy varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaHoSo varchar(36) NOT NULL,
  MaCaLam varchar(36) NOT NULL,
  NgayDangKy datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  TrangThai varchar(20) NOT NULL DEFAULT 'CHO_DUYET',
  MaNguoiPhanCong varchar(36) NULL,
  NgayPhanCong datetime2(0) NULL,
  PRIMARY KEY (MaDangKy),
  FOREIGN KEY (MaHoSo) REFERENCES dbo.HoSoTinhNguyenVien(MaHoSo),
  FOREIGN KEY (MaCaLam) REFERENCES dbo.CaLam(MaCaLam),
  FOREIGN KEY (MaNguoiPhanCong) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (TrangThai IN ('CHO_DUYET', 'DA_PHAN_CONG', 'TU_CHOI', 'DA_RUT', 'DA_HUY')),
  CHECK ((MaNguoiPhanCong IS NULL AND NgayPhanCong IS NULL) OR
         (MaNguoiPhanCong IS NOT NULL AND NgayPhanCong IS NOT NULL)),
  CHECK (TrangThai <> 'DA_PHAN_CONG' OR MaNguoiPhanCong IS NOT NULL)
);
CREATE UNIQUE INDEX UQ_DangKy_DangHoatDong
  ON dbo.DangKyCaLam (MaHoSo, MaCaLam)
  WHERE TrangThai IN ('CHO_DUYET', 'DA_PHAN_CONG');
CREATE TABLE dbo.DongGop (
  MaDongGop varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaDangKy varchar(36) NOT NULL UNIQUE,
  BatDauThucTe datetime2(0) NOT NULL,
  KetThucThucTe datetime2(0) NOT NULL,
  MoTaCongViec nvarchar(max) NOT NULL,
  TrangThai varchar(20) NOT NULL DEFAULT 'CHO_DUYET',
  LyDoTraLai nvarchar(max) NULL,
  MaNguoiXacNhan varchar(36) NULL,
  NgayXacNhan datetime2(0) NULL,
  PRIMARY KEY (MaDongGop),
  FOREIGN KEY (MaDangKy) REFERENCES dbo.DangKyCaLam(MaDangKy),
  FOREIGN KEY (MaNguoiXacNhan) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (TrangThai IN ('CHO_DUYET', 'CAN_BO_SUNG', 'DA_XAC_NHAN')),
  CHECK (LTRIM(RTRIM(MoTaCongViec)) <> ''),
  CHECK (BatDauThucTe < KetThucThucTe),
  CHECK ((TrangThai = 'DA_XAC_NHAN' AND MaNguoiXacNhan IS NOT NULL
          AND NgayXacNhan IS NOT NULL) OR
         (TrangThai <> 'DA_XAC_NHAN' AND MaNguoiXacNhan IS NULL
          AND NgayXacNhan IS NULL)),
  CHECK (TrangThai <> 'CAN_BO_SUNG' OR
         COALESCE(LTRIM(RTRIM(LyDoTraLai)), '') <> '')
);
CREATE INDEX IX_CaLam_NhuCau ON dbo.CaLam (MaNhuCau);
CREATE INDEX IX_CaLam_TimKiem ON dbo.CaLam (TrangThai, BatDauDuKien);
CREATE INDEX IX_DangKy_CaTrangThai ON dbo.DangKyCaLam (MaCaLam, TrangThai);
CREATE INDEX IX_DangKy_HoSo ON dbo.DangKyCaLam (MaHoSo);
CREATE INDEX IX_NhuCau_ToChuc ON dbo.NhuCauCongDong (MaToChuc);
GO
CREATE VIEW dbo.CaCongKhai AS
SELECT c.MaCaLam, n.TieuDe, n.TomTatCongKhai, n.KhuVucChung,
       c.BatDauDuKien, c.KetThucDuKien, c.SoNguoiCan
FROM dbo.CaLam c JOIN dbo.NhuCauCongDong n ON n.MaNhuCau = c.MaNhuCau
WHERE n.TrangThai = 'CONG_BO' AND n.DaRaSoatCongKhai = 1
  AND c.TrangThai = 'MO' AND c.BatDauDuKien > SYSUTCDATETIME();
GO
CREATE VIEW dbo.DongGopDaXacNhan AS
SELECT d.MaDongGop, k.MaHoSo, k.MaCaLam, d.MaNguoiXacNhan,
       d.NgayXacNhan,
       DATEDIFF_BIG(SECOND, d.BatDauThucTe, d.KetThucThucTe) AS SoGiay,
       CAST(DATEDIFF_BIG(SECOND, d.BatDauThucTe,
          d.KetThucThucTe) / 3600.0 AS decimal(18,6)) AS SoGio
FROM dbo.DongGop d JOIN dbo.DangKyCaLam k ON k.MaDangKy = d.MaDangKy
WHERE d.TrangThai = 'DA_XAC_NHAN';
GO
