-- Tạo bảng, ràng buộc, chỉ mục và view cho CSDL DieuPhoiTinhNguyenVien (SQL Server).
-- Chạy sau 00_tao_csdl.sql, trong ngữ cảnh của CSDL đích.
-- Ảnh (ảnh nhu cầu, ảnh minh chứng, ảnh chứng chỉ) lưu thẳng trong bảng TepDinhKem, không lưu ra thư mục.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
-- ---------------------------------------------------------------- TÀI KHOẢN VÀ TỔ CHỨC
CREATE TABLE dbo.NguoiDung (
  MaNguoiDung varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaDinhDanhOIDC varchar(255) NOT NULL UNIQUE,
  TenHienThi nvarchar(100) NOT NULL,
  TenDangNhap nvarchar(100) NULL,
  Email nvarchar(200) NULL,
  DangHoatDong bit NOT NULL DEFAULT 1,
  NgayTao datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (MaNguoiDung),
  CHECK (LTRIM(RTRIM(TenHienThi)) <> '')
);
CREATE UNIQUE INDEX UQ_NguoiDung_TenDangNhap ON dbo.NguoiDung (TenDangNhap) WHERE TenDangNhap IS NOT NULL;
CREATE TABLE dbo.NguoiDungVaiTro (
  MaNguoiDung varchar(36) NOT NULL,
  VaiTro varchar(20) NOT NULL,
  PRIMARY KEY (MaNguoiDung, VaiTro),
  FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (VaiTro IN ('TNV', 'DAI_DIEN', 'DIEU_PHOI', 'QUAN_TRI'))
);
-- Mọi tệp ảnh của nền tảng. Ảnh đã được máy chủ xóa thông tin đi kèm và thu nhỏ trước khi lưu (BR22).
CREATE TABLE dbo.TepDinhKem (
  MaTep varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenTep nvarchar(200) NOT NULL,
  LoaiNoiDung varchar(50) NOT NULL,
  KichThuoc int NOT NULL,
  ChieuRong int NOT NULL,
  ChieuCao int NOT NULL,
  NoiDung varbinary(max) NOT NULL,
  NgayTai datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  MaNguoiTai varchar(36) NOT NULL,
  PRIMARY KEY (MaTep),
  FOREIGN KEY (MaNguoiTai) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (LoaiNoiDung IN ('image/jpeg')),
  CHECK (KichThuoc > 0 AND KichThuoc <= 5242880 AND KichThuoc = DATALENGTH(NoiDung)),
  CHECK (ChieuRong > 0 AND ChieuCao > 0)
);
CREATE TABLE dbo.ToChucCongDong (
  MaToChuc varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenToChuc nvarchar(150) NOT NULL,
  MoTa nvarchar(500) NULL,
  KhuVucHoatDong nvarchar(150) NULL,
  SoDienThoai varchar(20) NULL,
  TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG',
  MaNguoiDangKy varchar(36) NULL,
  NgayDangKy datetime2(0) NULL,
  MaNguoiDuyet varchar(36) NULL,
  NgayDuyet datetime2(0) NULL,
  LyDoTuChoi nvarchar(500) NULL,
  PRIMARY KEY (MaToChuc),
  FOREIGN KEY (MaNguoiDangKy) REFERENCES dbo.NguoiDung(MaNguoiDung),
  FOREIGN KEY (MaNguoiDuyet) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (LTRIM(RTRIM(TenToChuc)) <> ''),
  CHECK (TrangThai IN ('CHO_DUYET', 'HOAT_DONG', 'TU_CHOI', 'NGUNG_HOAT_DONG')),
  CHECK (TrangThai <> 'CHO_DUYET' OR (MaNguoiDangKy IS NOT NULL AND NgayDangKy IS NOT NULL)),
  CHECK (TrangThai <> 'TU_CHOI' OR COALESCE(LTRIM(RTRIM(LyDoTuChoi)), '') <> '')
);
-- Mỗi người chỉ có một đăng ký tổ chức đang chờ duyệt; tên tổ chức đang hoạt động hoặc đang chờ duyệt không trùng.
CREATE UNIQUE INDEX UQ_ToChuc_MotDangKyChoDuyet ON dbo.ToChucCongDong (MaNguoiDangKy) WHERE TrangThai = 'CHO_DUYET';
CREATE UNIQUE INDEX UQ_ToChuc_Ten ON dbo.ToChucCongDong (TenToChuc) WHERE TrangThai IN ('CHO_DUYET', 'HOAT_DONG');
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
-- ---------------------------------------------------------------- HỒ SƠ VÀ KỸ NĂNG
-- Vị trí của tình nguyện viên chỉ dùng để tính khoảng cách, không hiện cho người khác (BR21).
CREATE TABLE dbo.HoSoTinhNguyenVien (
  MaHoSo varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaNguoiDung varchar(36) NOT NULL UNIQUE,
  VaiTro varchar(20) NOT NULL DEFAULT 'TNV',
  NgayCapNhatKyNang datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  NoiO nvarchar(150) NULL,
  ViDo decimal(9,6) NULL,
  KinhDo decimal(9,6) NULL,
  KhoangCachToiDa int NULL,
  PRIMARY KEY (MaHoSo),
  FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung),
  FOREIGN KEY (MaNguoiDung, VaiTro) REFERENCES dbo.NguoiDungVaiTro(MaNguoiDung, VaiTro),
  CHECK (VaiTro IN ('TNV')),
  CHECK ((ViDo IS NULL AND KinhDo IS NULL) OR (ViDo BETWEEN -90 AND 90 AND KinhDo BETWEEN -180 AND 180)),
  CHECK (KhoangCachToiDa IS NULL OR KhoangCachToiDa BETWEEN 1 AND 100)
);
-- Lịch rảnh theo tuần. Thu: 1 là thứ hai ... 7 là chủ nhật. Buoi: SANG (trước 12 giờ), CHIEU (12 đến 18 giờ), TOI (từ 18 giờ).
CREATE TABLE dbo.LichRanh (
  MaHoSo varchar(36) NOT NULL,
  Thu tinyint NOT NULL,
  Buoi varchar(5) NOT NULL,
  PRIMARY KEY (MaHoSo, Thu, Buoi),
  FOREIGN KEY (MaHoSo) REFERENCES dbo.HoSoTinhNguyenVien(MaHoSo),
  CHECK (Thu BETWEEN 1 AND 7),
  CHECK (Buoi IN ('SANG', 'CHIEU', 'TOI'))
);
CREATE TABLE dbo.NhomKyNang (
  MaNhom varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenNhom nvarchar(100) NOT NULL UNIQUE,
  ThuTu int NOT NULL DEFAULT 0,
  PRIMARY KEY (MaNhom),
  CHECK (LTRIM(RTRIM(TenNhom)) <> '')
);
CREATE TABLE dbo.KyNang (
  MaKyNang varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenKyNang nvarchar(100) NOT NULL,
  MoTaKyNang nvarchar(max) NULL,
  MaNhom varchar(36) NULL,
  CanChungChi bit NOT NULL DEFAULT 0,
  DangSuDung bit NOT NULL DEFAULT 1,
  TenChuanHoa AS LOWER(LTRIM(RTRIM(TenKyNang))) PERSISTED,
  PRIMARY KEY (MaKyNang),
  FOREIGN KEY (MaNhom) REFERENCES dbo.NhomKyNang(MaNhom),
  CHECK (LTRIM(RTRIM(TenKyNang)) <> '')
);
CREATE UNIQUE INDEX UQ_KyNang_TenChuanHoa
  ON dbo.KyNang (TenChuanHoa);
-- Đề xuất thêm kỹ năng vào danh mục (BR17).
CREATE TABLE dbo.DeXuatKyNang (
  MaDeXuat varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  TenKyNang nvarchar(100) NOT NULL,
  MoTa nvarchar(300) NULL,
  LyDo nvarchar(500) NULL,
  MaNguoiDeXuat varchar(36) NOT NULL,
  NgayDeXuat datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  TrangThai varchar(20) NOT NULL DEFAULT 'CHO_DUYET',
  MaKyNang varchar(36) NULL,
  MaNguoiDuyet varchar(36) NULL,
  NgayDuyet datetime2(0) NULL,
  LyDoTuChoi nvarchar(500) NULL,
  TenChuanHoa AS LOWER(LTRIM(RTRIM(TenKyNang))) PERSISTED,
  PRIMARY KEY (MaDeXuat),
  FOREIGN KEY (MaNguoiDeXuat) REFERENCES dbo.NguoiDung(MaNguoiDung),
  FOREIGN KEY (MaKyNang) REFERENCES dbo.KyNang(MaKyNang),
  FOREIGN KEY (MaNguoiDuyet) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (LTRIM(RTRIM(TenKyNang)) <> ''),
  CHECK (TrangThai IN ('CHO_DUYET', 'DA_DUYET', 'TU_CHOI')),
  CHECK ((TrangThai = 'CHO_DUYET' AND MaNguoiDuyet IS NULL AND NgayDuyet IS NULL) OR
         (TrangThai <> 'CHO_DUYET' AND MaNguoiDuyet IS NOT NULL AND NgayDuyet IS NOT NULL)),
  CHECK (TrangThai <> 'DA_DUYET' OR MaKyNang IS NOT NULL),
  CHECK (TrangThai <> 'TU_CHOI' OR COALESCE(LTRIM(RTRIM(LyDoTuChoi)), '') <> '')
);
CREATE UNIQUE INDEX UQ_DeXuatKyNang_ChoDuyet ON dbo.DeXuatKyNang (TenChuanHoa) WHERE TrangThai = 'CHO_DUYET';
-- CapDo: 1 Cơ bản, 2 Khá, 3 Thành thạo (BR18).
CREATE TABLE dbo.KyNangSoHuu (
  MaHoSo varchar(36) NOT NULL,
  MaKyNang varchar(36) NOT NULL,
  CapDo tinyint NOT NULL DEFAULT 1,
  NgayKhaiBao datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (MaHoSo, MaKyNang),
  FOREIGN KEY (MaHoSo) REFERENCES dbo.HoSoTinhNguyenVien(MaHoSo),
  FOREIGN KEY (MaKyNang) REFERENCES dbo.KyNang(MaKyNang),
  CHECK (CapDo BETWEEN 1 AND 3)
);
-- Chứng chỉ của kỹ năng cần chứng chỉ (BR19). Mỗi lần tải là một dòng để giữ lịch sử.
CREATE TABLE dbo.ChungChiKyNang (
  MaChungChi varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaHoSo varchar(36) NOT NULL,
  MaKyNang varchar(36) NOT NULL,
  MaTep varchar(36) NOT NULL UNIQUE,
  NgayGui datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  TrangThai varchar(20) NOT NULL DEFAULT 'CHO_XAC_MINH',
  MaNguoiXacMinh varchar(36) NULL,
  NgayXacMinh datetime2(0) NULL,
  LyDoTuChoi nvarchar(500) NULL,
  PRIMARY KEY (MaChungChi),
  FOREIGN KEY (MaHoSo) REFERENCES dbo.HoSoTinhNguyenVien(MaHoSo),
  FOREIGN KEY (MaKyNang) REFERENCES dbo.KyNang(MaKyNang),
  FOREIGN KEY (MaTep) REFERENCES dbo.TepDinhKem(MaTep),
  FOREIGN KEY (MaNguoiXacMinh) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (TrangThai IN ('CHO_XAC_MINH', 'DA_XAC_MINH', 'TU_CHOI', 'HET_HIEU_LUC')),
  CHECK (TrangThai NOT IN ('DA_XAC_MINH', 'TU_CHOI') OR (MaNguoiXacMinh IS NOT NULL AND NgayXacMinh IS NOT NULL)),
  CHECK (TrangThai <> 'TU_CHOI' OR COALESCE(LTRIM(RTRIM(LyDoTuChoi)), '') <> '')
);
CREATE UNIQUE INDEX UQ_ChungChi_ChoXacMinh ON dbo.ChungChiKyNang (MaHoSo, MaKyNang) WHERE TrangThai = 'CHO_XAC_MINH';
CREATE UNIQUE INDEX UQ_ChungChi_DaXacMinh ON dbo.ChungChiKyNang (MaHoSo, MaKyNang) WHERE TrangThai = 'DA_XAC_MINH';
-- ---------------------------------------------------------------- NHU CẦU
-- ViDoVung, KinhDoVung, BanKinhVung: vùng tròn công khai, tâm đã dời khỏi vị trí thật (BR21).
CREATE TABLE dbo.NhuCauCongDong (
  MaNhuCau varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaToChuc varchar(36) NOT NULL,
  TieuDe nvarchar(150) NOT NULL,
  TomTatCongKhai nvarchar(max) NOT NULL,
  KhuVucChung nvarchar(150) NOT NULL,
  ViDoVung decimal(9,6) NULL,
  KinhDoVung decimal(9,6) NULL,
  BanKinhVung int NULL,
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
  CHECK (TrangThai <> 'CONG_BO' OR DaRaSoatCongKhai = 1),
  CHECK ((ViDoVung IS NULL AND KinhDoVung IS NULL AND BanKinhVung IS NULL) OR
         (ViDoVung BETWEEN -90 AND 90 AND KinhDoVung BETWEEN -180 AND 180 AND BanKinhVung BETWEEN 200 AND 5000))
);
-- Ảnh minh họa của nhu cầu, thuộc nội dung công khai (BR22).
CREATE TABLE dbo.AnhNhuCau (
  MaNhuCau varchar(36) NOT NULL,
  MaTep varchar(36) NOT NULL UNIQUE,
  ThuTu tinyint NOT NULL DEFAULT 1,
  PRIMARY KEY (MaNhuCau, MaTep),
  FOREIGN KEY (MaNhuCau) REFERENCES dbo.NhuCauCongDong(MaNhuCau),
  FOREIGN KEY (MaTep) REFERENCES dbo.TepDinhKem(MaTep)
);
-- Hướng dẫn riêng, địa chỉ cụ thể và vị trí chính xác (BR15, BR21).
CREATE TABLE dbo.ThongTinDieuPhoiRieng (
  MaNhuCau varchar(36) NOT NULL,
  HuongDanRieng nvarchar(max) NULL,
  DiaChiCuThe nvarchar(300) NULL,
  ViDo decimal(9,6) NULL,
  KinhDo decimal(9,6) NULL,
  PRIMARY KEY (MaNhuCau),
  FOREIGN KEY (MaNhuCau) REFERENCES dbo.NhuCauCongDong(MaNhuCau),
  CHECK (HuongDanRieng IS NULL OR LTRIM(RTRIM(HuongDanRieng)) <> ''),
  CHECK (DiaChiCuThe IS NULL OR LTRIM(RTRIM(DiaChiCuThe)) <> ''),
  CHECK ((ViDo IS NULL AND KinhDo IS NULL) OR (ViDo BETWEEN -90 AND 90 AND KinhDo BETWEEN -180 AND 180)),
  CHECK (HuongDanRieng IS NOT NULL OR DiaChiCuThe IS NOT NULL OR ViDo IS NOT NULL)
);
-- ---------------------------------------------------------------- CA LÀM, ĐĂNG KÝ, ĐIỂM DANH
CREATE TABLE dbo.CaLam (
  MaCaLam varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaNhuCau varchar(36) NOT NULL,
  BatDauDuKien datetime2(0) NOT NULL,
  KetThucDuKien datetime2(0) NOT NULL,
  SoNguoiCan int NOT NULL,
  TrangThai varchar(20) NOT NULL DEFAULT 'MO',
  KhoaDiemDanh varchar(64) NULL,
  PRIMARY KEY (MaCaLam),
  FOREIGN KEY (MaNhuCau) REFERENCES dbo.NhuCauCongDong(MaNhuCau),
  CHECK (TrangThai IN ('MO', 'DANG_DIEN_RA', 'DA_KET_THUC', 'DA_HUY')),
  CHECK (SoNguoiCan > 0),
  CHECK (BatDauDuKien < KetThucDuKien)
);
CREATE TABLE dbo.YeuCauKyNang (
  MaCaLam varchar(36) NOT NULL,
  MaKyNang varchar(36) NOT NULL,
  CapDoToiThieu tinyint NOT NULL DEFAULT 1,
  CanXacMinh bit NOT NULL DEFAULT 0,
  PRIMARY KEY (MaCaLam, MaKyNang),
  FOREIGN KEY (MaCaLam) REFERENCES dbo.CaLam(MaCaLam),
  FOREIGN KEY (MaKyNang) REFERENCES dbo.KyNang(MaKyNang),
  CHECK (CapDoToiThieu BETWEEN 1 AND 3)
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
-- Điểm danh của một đăng ký đã phân công (BR23, BR27).
CREATE TABLE dbo.DiemDanh (
  MaDangKy varchar(36) NOT NULL,
  KetQua varchar(20) NOT NULL,
  GioVao datetime2(0) NULL,
  GioRa datetime2(0) NULL,
  CachGhi varchar(20) NOT NULL,
  MaNguoiGhiNhan varchar(36) NULL,
  NgayCapNhat datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (MaDangKy),
  FOREIGN KEY (MaDangKy) REFERENCES dbo.DangKyCaLam(MaDangKy),
  FOREIGN KEY (MaNguoiGhiNhan) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (KetQua IN ('CO_MAT', 'VANG_MAT')),
  CHECK (CachGhi IN ('QR', 'DIEU_PHOI')),
  CHECK ((KetQua = 'VANG_MAT' AND GioVao IS NULL AND GioRa IS NULL) OR (KetQua = 'CO_MAT' AND GioVao IS NOT NULL)),
  CHECK (GioRa IS NULL OR GioRa > GioVao),
  CHECK (CachGhi = 'QR' OR MaNguoiGhiNhan IS NOT NULL)
);
-- ---------------------------------------------------------------- ĐÓNG GÓP
CREATE TABLE dbo.DongGop (
  MaDongGop varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaDangKy varchar(36) NOT NULL UNIQUE,
  BatDauThucTe datetime2(0) NOT NULL,
  KetThucThucTe datetime2(0) NOT NULL,
  GioTheoDiemDanh bit NOT NULL DEFAULT 0,
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
-- Ảnh minh chứng của bản khai, chỉ người liên quan xem được (BR24).
CREATE TABLE dbo.AnhMinhChung (
  MaDongGop varchar(36) NOT NULL,
  MaTep varchar(36) NOT NULL UNIQUE,
  PRIMARY KEY (MaDongGop, MaTep),
  FOREIGN KEY (MaDongGop) REFERENCES dbo.DongGop(MaDongGop),
  FOREIGN KEY (MaTep) REFERENCES dbo.TepDinhKem(MaTep)
);
-- Giấy xác nhận giờ tình nguyện (BR29).
CREATE TABLE dbo.GiayXacNhan (
  MaGiay varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaHoSo varchar(36) NOT NULL,
  MaKiemTra varchar(12) NOT NULL UNIQUE,
  TenNguoiNhan nvarchar(100) NOT NULL,
  TuNgay date NULL,
  DenNgay date NULL,
  SoCa int NOT NULL,
  TongSoGiay bigint NOT NULL,
  NgayCap datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (MaGiay),
  FOREIGN KEY (MaHoSo) REFERENCES dbo.HoSoTinhNguyenVien(MaHoSo),
  CHECK (SoCa > 0 AND TongSoGiay > 0),
  CHECK (TuNgay IS NULL OR DenNgay IS NULL OR TuNgay <= DenNgay)
);
CREATE TABLE dbo.GiayXacNhanDongGop (
  MaGiay varchar(36) NOT NULL,
  MaDongGop varchar(36) NOT NULL,
  PRIMARY KEY (MaGiay, MaDongGop),
  FOREIGN KEY (MaGiay) REFERENCES dbo.GiayXacNhan(MaGiay),
  FOREIGN KEY (MaDongGop) REFERENCES dbo.DongGop(MaDongGop)
);
-- ---------------------------------------------------------------- THÔNG BÁO VÀ NHẬT KÝ
CREATE TABLE dbo.ThongBao (
  MaThongBao varchar(36) NOT NULL DEFAULT CONVERT(varchar(36), NEWID()),
  MaNguoiNhan varchar(36) NOT NULL,
  Loai varchar(30) NOT NULL,
  NoiDung nvarchar(500) NOT NULL,
  DuongDan nvarchar(300) NULL,
  MaThamChieu varchar(36) NULL,
  NgayTao datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  DaDoc bit NOT NULL DEFAULT 0,
  NgayDoc datetime2(0) NULL,
  PRIMARY KEY (MaThongBao),
  FOREIGN KEY (MaNguoiNhan) REFERENCES dbo.NguoiDung(MaNguoiDung),
  CHECK (LTRIM(RTRIM(NoiDung)) <> ''),
  CHECK ((DaDoc = 0 AND NgayDoc IS NULL) OR (DaDoc = 1 AND NgayDoc IS NOT NULL))
);
CREATE INDEX IX_ThongBao_NguoiNhan ON dbo.ThongBao (MaNguoiNhan, DaDoc, NgayTao);
-- Mỗi đăng ký chỉ được nhắc trước ca một lần (BR25).
CREATE UNIQUE INDEX UQ_ThongBao_NhacCa ON dbo.ThongBao (MaNguoiNhan, Loai, MaThamChieu) WHERE Loai = 'NHAC_CA';
-- Nhật ký xem dữ liệu nhạy cảm, chỉ thêm (BR26).
CREATE TABLE dbo.NhatKyTruyCap (
  MaNhatKy bigint IDENTITY(1,1) NOT NULL,
  MaNguoiXem varchar(36) NOT NULL,
  LoaiDuLieu varchar(30) NOT NULL,
  MaDoiTuong varchar(36) NOT NULL,
  MaToChuc varchar(36) NULL,
  ThoiDiem datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  DuocPhep bit NOT NULL,
  PRIMARY KEY (MaNhatKy),
  FOREIGN KEY (MaNguoiXem) REFERENCES dbo.NguoiDung(MaNguoiDung),
  FOREIGN KEY (MaToChuc) REFERENCES dbo.ToChucCongDong(MaToChuc),
  CHECK (LoaiDuLieu IN ('HUONG_DAN_RIENG', 'ANH_MINH_CHUNG', 'CHUNG_CHI'))
);
CREATE INDEX IX_NhatKy_ToChuc ON dbo.NhatKyTruyCap (MaToChuc, ThoiDiem);
CREATE INDEX IX_CaLam_NhuCau ON dbo.CaLam (MaNhuCau);
CREATE INDEX IX_CaLam_TimKiem ON dbo.CaLam (TrangThai, BatDauDuKien);
CREATE INDEX IX_DangKy_CaTrangThai ON dbo.DangKyCaLam (MaCaLam, TrangThai);
CREATE INDEX IX_DangKy_HoSo ON dbo.DangKyCaLam (MaHoSo);
CREATE INDEX IX_NhuCau_ToChuc ON dbo.NhuCauCongDong (MaToChuc);
GO
-- ---------------------------------------------------------------- VIEW
-- Chỉ các trường được công khai (BR01, BR21): vùng gần đúng, không có vị trí thật hay địa chỉ cụ thể.
CREATE VIEW dbo.CaCongKhai AS
SELECT c.MaCaLam, n.MaNhuCau, t.TenToChuc, n.TieuDe, n.TomTatCongKhai, n.KhuVucChung,
       n.ViDoVung, n.KinhDoVung, n.BanKinhVung,
       c.BatDauDuKien, c.KetThucDuKien, c.SoNguoiCan,
       (SELECT COUNT(*) FROM dbo.DangKyCaLam d
        WHERE d.MaCaLam = c.MaCaLam AND d.TrangThai = 'DA_PHAN_CONG') AS SoDaPhanCong
FROM dbo.CaLam c JOIN dbo.NhuCauCongDong n ON n.MaNhuCau = c.MaNhuCau
JOIN dbo.ToChucCongDong t ON t.MaToChuc = n.MaToChuc
WHERE n.TrangThai = 'CONG_BO' AND n.DaRaSoatCongKhai = 1 AND t.TrangThai = 'HOAT_DONG'
  AND c.TrangThai = 'MO' AND c.BatDauDuKien > SYSUTCDATETIME();
GO
CREATE VIEW dbo.DongGopDaXacNhan AS
SELECT d.MaDongGop, k.MaHoSo, k.MaCaLam, d.MaNguoiXacNhan,
       d.NgayXacNhan, d.BatDauThucTe,
       DATEDIFF_BIG(SECOND, d.BatDauThucTe, d.KetThucThucTe) AS SoGiay,
       CAST(DATEDIFF_BIG(SECOND, d.BatDauThucTe,
          d.KetThucThucTe) / 3600.0 AS decimal(18,6)) AS SoGio
FROM dbo.DongGop d JOIN dbo.DangKyCaLam k ON k.MaDangKy = d.MaDangKy
WHERE d.TrangThai = 'DA_XAC_NHAN';
GO
-- Số lần có mặt và vắng mặt đã ghi nhận của mỗi hồ sơ (BR27).
CREATE VIEW dbo.MucTinCay AS
SELECT h.MaHoSo,
       SUM(CASE WHEN dd.KetQua = 'CO_MAT' THEN 1 ELSE 0 END) AS SoLanCoMat,
       SUM(CASE WHEN dd.KetQua = 'VANG_MAT' THEN 1 ELSE 0 END) AS SoLanVang
FROM dbo.HoSoTinhNguyenVien h
LEFT JOIN dbo.DangKyCaLam d ON d.MaHoSo = h.MaHoSo
LEFT JOIN dbo.DiemDanh dd ON dd.MaDangKy = d.MaDangKy
GROUP BY h.MaHoSo;
GO
