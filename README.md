# CIV-09: Nền tảng điều phối tình nguyện viên theo kỹ năng

Đồ án cuối kỳ môn Phương pháp phát triển phần mềm hướng đối tượng (lớp OOSD_CNPM1).
Sinh viên thực hiện: Nguyễn Thành Can, MSSV 1250080021, lớp 12_ĐH_CNPM1.

Hệ thống kết nối **nhu cầu cộng đồng**, **kỹ năng** của tình nguyện viên, **ca làm** và **xác nhận đóng góp**.
Hướng dẫn riêng của mỗi nhu cầu (địa chỉ cụ thể, lưu ý khi tiếp xúc...) được tách khỏi phần công khai và chỉ người đủ quyền mới xem được, để không công khai dữ liệu nhạy cảm của người thụ hưởng.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Máy chủ ứng dụng | ASP.NET Core Web API (C#, .NET 10), EF Core |
| Cơ sở dữ liệu | SQL Server 2022 (chạy trong Docker) |
| Đăng nhập | Keycloak 26 theo OIDC, máy chủ ứng dụng giữ phiên bằng cookie |
| Giao diện | React 19 + Vite |
| Đóng gói | Docker, Docker Compose |
| Kiểm thử | xUnit, kiểm thử tích hợp qua `WebApplicationFactory` với SQL Server thật |

## Cấu trúc thư mục

```
CIV-09/
├── database/            Tệp lệnh tạo CSDL: bảng, view, thủ tục, trigger, tài khoản ứng dụng, dữ liệu mẫu
├── keycloak/            Cấu hình realm civ09 và tài khoản mẫu, nạp tự động khi Keycloak khởi động
├── src/Civ09.Api/       Máy chủ ứng dụng
│   ├── GiaoDien/        Lớp Boundary (AuthBoundary, CaBoundary, ...)
│   ├── DichVu/          Lớp Service điều khiển từng use case
│   ├── ThucThe/         Lớp thực thể (NhuCauCongDong, CaLam, DangKyCaLam, DongGop, ...)
│   ├── HoTro/           ChinhSachTruyCap, DieuKienPhanCong, BoLocCongKhai, CongOIDC, GiaoDich
│   └── DuLieu/          KhoNghiepVu (ánh xạ EF Core vào lược đồ có sẵn)
├── tests/Civ09.Tests/   Kiểm thử đơn vị và kiểm thử tích hợp theo Bảng 7.2 của báo cáo
├── web/                 Giao diện React (13 màn hình M01 đến M13)
├── docs/                Sơ đồ (draw.io và PNG), kết quả kiểm thử, ảnh màn hình
├── docker-compose.yml
├── Dockerfile
└── .env.example         Mẫu tệp cấu hình bí mật (tệp .env thật không đưa lên Git)
```

Tên lớp và phương thức trong mã nguồn đặt theo Chương V của báo cáo (C# viết hoa chữ đầu tên phương thức, ví dụ `dangKy(...)` trong báo cáo là `DangKy(...)` trong mã).

## Cài đặt

Máy cần có:

- Docker Desktop
- .NET SDK 10
- Node.js 22 trở lên (chỉ cần khi chạy giao diện ở chế độ phát triển)
- Git

Lấy mã nguồn và tạo tệp cấu hình:

```powershell
git clone https://github.com/kanishere55/CIV-09.git
cd CIV-09
copy .env.example .env
```

Mở `.env` và đổi các mật khẩu nếu muốn. Mật khẩu SQL Server phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký hiệu.

## Chạy chương trình

### Cách 1: chạy toàn bộ bằng Docker (một lệnh)

```powershell
docker compose --profile app up -d --build
```

Lần đầu Docker phải tải image và build nên mất vài phút. Sau đó mở **http://127.0.0.1:8088**.

### Cách 2: chạy khi phát triển

```powershell
docker compose up -d                       # SQL Server, khởi tạo CSDL, Keycloak
dotnet run --project src/Civ09.Api         # máy chủ ứng dụng ở http://127.0.0.1:5000
cd web
npm install
npm run dev                                # giao diện ở http://127.0.0.1:5173
```

Nếu đã chạy `npm run build` trong thư mục `web`, giao diện được build vào `src/Civ09.Api/wwwroot` và mở được trực tiếp ở http://127.0.0.1:5000.

Lần chạy đầu, dịch vụ `db-init` tạo CSDL `DieuPhoiTinhNguyenVien` từ các tệp trong `database/` rồi tự dừng. Keycloak nạp realm `civ09` từ `keycloak/civ09-realm.json`.

## Cơ sở dữ liệu

| Tệp | Nội dung |
| --- | --- |
| `00_tao_csdl.sql` | Tạo CSDL `DieuPhoiTinhNguyenVien` |
| `01_tao_bang.sql` | 13 bảng, ràng buộc khóa, CHECK, chỉ mục duy nhất có điều kiện, 2 view `CaCongKhai` và `DongGopDaXacNhan` |
| `02_du_lieu_mau.sql` | Tổ chức, danh mục kỹ năng, 7 tài khoản mẫu, một nhu cầu đã công bố có 2 ca |
| `03_thu_tuc_va_trigger.sql` | 6 trigger và 3 thủ tục lưu trữ |
| `04_tai_khoan_ung_dung.sql` | Tài khoản `civ09_app` mà máy chủ ứng dụng dùng để kết nối |

Thủ tục lưu trữ:

- `sp_ChuyenMocThoiGianCa`: đến giờ bắt đầu thì ca sang Đang diễn ra và hủy các đăng ký còn chờ duyệt, đến giờ kết thúc thì ca sang Đã kết thúc (BR12). Tác vụ nền của máy chủ ứng dụng gọi mỗi 30 giây.
- `sp_GoiYUngVien`: danh sách người đăng ký một ca kèm kỹ năng đã khai, có đủ kỹ năng của ca không và có trùng lịch không (BR04, BR09).
- `sp_TongGioDongGop`: tổng thời gian đóng góp đã được xác nhận của một tình nguyện viên, lấy từ view `DongGopDaXacNhan` (BR14).

Trigger:

- `trg_CaLam_KiemTra`: chỉ tạo ca cho nhu cầu đã công bố (BR06); ca đã có người được phân công thì không đổi giờ, số người cần không thấp hơn số đã phân công (BR11).
- `trg_YeuCauKyNang_KiemTraSua`: ca đã có người được phân công thì không đổi kỹ năng yêu cầu (BR11).
- `trg_DangKyCaLam_KiemTra`: chỉ đăng ký ca đang mở của nhu cầu đã công bố (BR07); khi phân công thì người đó phải đủ kỹ năng, ca còn chỗ và không trùng lịch (BR09, BR10).
- `trg_DongGop_KiemTra`: bản khai phải thuộc đăng ký đã phân công và nằm trong giờ của ca, không tự xác nhận bản của mình, không sửa bản đã xác nhận (BR13, BR14).
- `trg_NhuCauCongDong_GiuToChuc`: không đổi tổ chức sở hữu của nhu cầu (BR06).
- `trg_KyNangSoHuu_GiuKyNangDangCan`: không bỏ kỹ năng đang cần cho ca đã phân công và chưa kết thúc (BR03).

Lỗi do trigger và thủ tục báo ra có mã dạng `50xxy`, trong đó `xx` là số quy tắc nghiệp vụ, ví dụ `50092` là lỗi thứ hai của BR09 (ca đã đủ người). Danh sách mã lỗi ghi ở đầu tệp `03_thu_tuc_va_trigger.sql`.

Máy chủ ứng dụng kiểm tra các quy tắc này trước khi lưu để báo lỗi rõ ràng cho người dùng; trigger kiểm tra lại trong CSDL nên dữ liệu vẫn đúng khi có thao tác sửa thẳng vào CSDL hoặc nhiều yêu cầu đến cùng lúc.

Tài khoản `civ09_app` được cấp quyền theo từng bảng: chỉ đọc các bảng tài khoản, tổ chức và danh mục kỹ năng; đọc, thêm, sửa các bảng nghiệp vụ; chạy 3 thủ tục. Quyền xóa chỉ cấp cho `KyNangSoHuu` và `ThongTinDieuPhoiRieng`; các bảng nhu cầu, ca, đăng ký, đóng góp chỉ đổi trạng thái để giữ lịch sử. Tài khoản `sa` chỉ dùng để khởi tạo CSDL.

Nên mở bằng địa chỉ `127.0.0.1` như trên (không dùng `localhost`) để khớp với cấu hình đăng nhập của Keycloak.

## Tài khoản mẫu

Mật khẩu của tất cả tài khoản là giá trị `DEMO_PASSWORD` trong `.env` (mặc định `Demo@123`). Đây là dữ liệu giả để chạy thử.

| Tên đăng nhập | Vai trò | Ghi chú |
| --- | --- | --- |
| `daidien1` | Đại diện cộng đồng, tổ chức A | Tạo, rà soát, công bố nhu cầu |
| `dieuphoi1` | Điều phối viên, tổ chức A | Tạo ca, phân công, duyệt đóng góp |
| `dieuphoi2` | Điều phối viên, tổ chức B | Dùng để thử người ngoài tổ chức bị từ chối |
| `tnv1` | Tình nguyện viên | Kỹ năng: Tin học cơ bản, Sơ cứu |
| `tnv2` | Tình nguyện viên | Kỹ năng: Nấu ăn |
| `tnv3` | Tình nguyện viên | Kỹ năng: Tin học cơ bản, Dạy kèm |
| `dieuphoi_tnv` | Điều phối viên tổ chức A, đồng thời là tình nguyện viên | Dùng để thử chặn tự duyệt bản khai |

Trang quản trị Keycloak: http://127.0.0.1:8080, tài khoản `admin`, mật khẩu là `KEYCLOAK_ADMIN_PASSWORD` trong `.env`.

## Chạy kiểm thử

Cần SQL Server đang chạy (`docker compose up -d`). Bộ kiểm thử tự tạo một CSDL riêng tên `DieuPhoiTinhNguyenVien_KiemThu` từ đúng các tệp lệnh trong `database/`, nên không ảnh hưởng dữ liệu đang dùng. Máy chủ ứng dụng trong kiểm thử cũng kết nối bằng tài khoản `civ09_app` như khi chạy thật.

```powershell
dotnet test Civ09.slnx --logger "console;verbosity=normal"
```

Tên mỗi ca kiểm thử bắt đầu bằng mã KT trong Bảng 7.2 của báo cáo. KT21 đến KT24 sửa thẳng vào CSDL (không qua ứng dụng) để kiểm tra trigger và quyền của tài khoản `civ09_app`. Kiểm thử tích hợp gọi API thật, đọc ghi SQL Server thật và giả lập giờ bằng `TimeProvider` để thử các quy tắc theo thời gian (bắt đầu ca, kết thúc ca, hạn xem hướng dẫn riêng).

## Bảo vệ dữ liệu của người thụ hưởng

- Hướng dẫn riêng nằm ở bảng `ThongTinDieuPhoiRieng`, tách khỏi `NhuCauCongDong`. Chức năng tìm ca chỉ đọc view `CaCongKhai`, view này không nối với bảng hướng dẫn riêng.
- `BoLocCongKhai` là điểm duy nhất trả dữ liệu công khai, chỉ gồm tiêu đề, tóm tắt đã rà soát, khu vực chung, thời gian, số người cần và kỹ năng.
- Mọi yêu cầu xem hướng dẫn riêng đi qua `ChinhSachTruyCap.DuocXemRieng(...)` tại máy chủ (quy tắc BR15) trước khi dữ liệu được đọc.
- Nhu cầu chỉ được công bố khi đại diện đã rà soát đúng phiên bản nội dung công khai hiện tại (BR05).

## Sơ đồ và tài liệu

- `docs/so_do/drawio`: tệp nguồn draw.io của các sơ đồ trong báo cáo, đặt tên theo số sơ đồ.
- `docs/so_do/png`: ảnh xuất từ các tệp trên.
- `docs/kiem_thu`: kết quả chạy kiểm thử.
- `docs/man_hinh`: ảnh chụp các màn hình M01 đến M13.

Mật khẩu, khóa bí mật và tệp `.env` không được đưa lên kho mã nguồn.
