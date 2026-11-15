# CIV-09: Nền tảng điều phối tình nguyện viên theo kỹ năng

Đồ án cuối kỳ môn Phương pháp phát triển phần mềm hướng đối tượng, lớp OOSD_CNPM1.

- Sinh viên: Nguyễn Thành Can, MSSV 1250080021, lớp 12_ĐH_CNPM1
- Giảng viên hướng dẫn: ThS. Phạm Trọng Huynh

Nền tảng nối nhu cầu hỗ trợ của các tổ chức cộng đồng với tình nguyện viên có kỹ năng phù hợp, chia việc thành ca làm và xác nhận thời gian đóng góp. Hướng dẫn riêng của mỗi nhu cầu (địa chỉ, vị trí, lưu ý khi tiếp xúc) được lưu tách khỏi phần công khai và chỉ người đủ quyền mới xem được.

## Chức năng

| Nhóm | Nội dung |
| --- | --- |
| Nhu cầu và ca làm | Đại diện tạo, rà soát, công bố nhu cầu; điều phối viên chia nhu cầu thành ca, đặt kỹ năng và cấp độ tối thiểu |
| Kỹ năng | Danh mục kỹ năng theo nhóm, khai kèm cấp độ, gửi chứng chỉ để xác minh, đề xuất kỹ năng mới |
| Đăng ký và phân công | Tìm ca theo kỹ năng, thời gian, khoảng cách; gợi ý ca phù hợp; xếp người đăng ký theo điểm phù hợp |
| Bản đồ | OpenStreetMap; phần công khai chỉ hiện một vùng khoảng 1 km, người được phân công mới thấy vị trí chính xác |
| Điểm danh | Mã QR đổi mỗi 2 phút, điều phối viên ghi thay khi không quét được |
| Đóng góp | Khai thời gian và công việc kèm ảnh minh chứng, điều phối viên xác nhận, tạo giấy xác nhận PDF có mã kiểm tra |
| Quản trị | Duyệt tổ chức, quản lý danh mục kỹ năng, khóa tài khoản, nhật ký xem dữ liệu riêng |
| Thông báo | Thông báo trên web khi được phân công, bị từ chối, ca đổi giờ, bản khai được duyệt... |

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Máy chủ ứng dụng | ASP.NET Core Web API (.NET 10), EF Core |
| Cơ sở dữ liệu | SQL Server 2022 |
| Đăng nhập | Keycloak 26 (OIDC) |
| Giao diện | React 19, Vite, Leaflet |
| Ảnh, PDF, mã QR | SkiaSharp, QuestPDF, QRCoder |
| Đóng gói | Docker Compose |
| Kiểm thử | xUnit |

## Cấu trúc thư mục

```
CIV-09/
├── backend/
│   ├── Civ09.Api/        Máy chủ ứng dụng
│   │   ├── GiaoDien/     Lớp Boundary (nhận yêu cầu từ giao diện)
│   │   ├── DichVu/       Lớp Service (xử lý từng use case)
│   │   ├── ThucThe/      Lớp thực thể
│   │   ├── HoTro/        Kiểm tra quyền, điều kiện phân công, bản đồ, ảnh, mã điểm danh
│   │   └── DuLieu/       Ánh xạ EF Core
│   ├── Civ09.Tests/      Kiểm thử đơn vị và kiểm thử tích hợp
│   └── Civ09.slnx
├── frontend/             Giao diện React (23 màn hình, M00 đến M22)
├── database/             Tệp lệnh SQL: bảng, view, thủ tục, trigger, tài khoản, dữ liệu mẫu
├── keycloak/             Cấu hình realm và tài khoản mẫu
├── docs/
│   ├── so_do/            Sơ đồ trong báo cáo (drawio và png), đặt tên theo số sơ đồ
│   ├── giao_dien/        Bản vẽ thiết kế màn hình (drawio và png)
│   ├── man_hinh/         Ảnh chụp giao diện đang chạy
│   └── kiem_thu/         Kết quả chạy kiểm thử
├── docker-compose.yml
├── Dockerfile
└── .env.example          Mẫu tệp cấu hình, sao chép thành .env trước khi chạy
```

Tên lớp và phương thức trong mã nguồn đặt theo Chương V của báo cáo.

## Cài đặt và chạy

Cần có Docker Desktop và Git. Muốn chạy ở chế độ phát triển hoặc chạy kiểm thử thì cần thêm .NET SDK 10 và Node.js 22.

```powershell
git clone https://github.com/kanishere55/CIV-09.git
cd CIV-09
copy .env.example .env
```

Mở `.env` để đổi mật khẩu nếu muốn. Mật khẩu SQL Server cần ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký hiệu.

**Cách 1: chạy toàn bộ bằng Docker**

```powershell
docker compose --profile app up -d --build
```

Lần đầu mất vài phút để tải image và build. Sau đó mở http://127.0.0.1:8088.

**Cách 2: chạy khi phát triển**

```powershell
docker compose up -d                          # SQL Server, khởi tạo CSDL, Keycloak
dotnet run --project backend/Civ09.Api        # máy chủ ứng dụng: http://127.0.0.1:5000
cd frontend
npm install
npm run dev                                   # giao diện: http://127.0.0.1:5173
```

Lưu ý:

- Dùng địa chỉ `127.0.0.1`, không dùng `localhost`, để khớp cấu hình đăng nhập của Keycloak.
- Lần chạy đầu, dịch vụ `db-init` tạo CSDL `DieuPhoiTinhNguyenVien` từ các tệp trong `database/`. Muốn tạo lại từ đầu: `docker compose --profile app down -v` rồi dựng lại.
- Bản đồ lấy dữ liệu từ OpenStreetMap nên máy cần có Internet.
- Trang quản trị Keycloak: http://127.0.0.1:8080, tài khoản `admin`, mật khẩu là `KEYCLOAK_ADMIN_PASSWORD` trong `.env`.

## Tài khoản mẫu

Mật khẩu của các tài khoản dưới đây là giá trị `DEMO_PASSWORD` trong `.env`. Đây là dữ liệu giả để chạy thử. Ai cũng có thể tự tạo tài khoản tình nguyện viên bằng nút **Tạo tài khoản**.

| Tên đăng nhập | Vai trò |
| --- | --- |
| `quantri1` | Quản trị viên nền tảng |
| `daidien1` | Đại diện cộng đồng của tổ chức A |
| `dieuphoi1` | Điều phối viên của tổ chức A |
| `dieuphoi2` | Điều phối viên của tổ chức B |
| `tnv1`, `tnv2`, `tnv3` | Tình nguyện viên |
| `dieuphoi_tnv` | Vừa là điều phối viên tổ chức A, vừa là tình nguyện viên |

Thử điểm danh: đăng nhập `dieuphoi1`, vào **Điều phối ca**, bấm **Điểm danh** ở ca đang diễn ra để hiện mã QR. Ở một cửa sổ ẩn danh, đăng nhập `tnv1`, bấm **Điểm danh vào** ở trang chủ rồi nhập mã chữ hiện dưới mã QR.

## Cơ sở dữ liệu

| Tệp trong `database/` | Nội dung |
| --- | --- |
| `00_tao_csdl.sql` | Tạo CSDL |
| `01_tao_bang.sql` | 25 bảng, ràng buộc, chỉ mục và 3 view |
| `02_du_lieu_mau.sql` | Tổ chức, danh mục kỹ năng, tài khoản mẫu |
| `03_thu_tuc_va_trigger.sql` | 5 thủ tục lưu trữ và 13 trigger |
| `04_tai_khoan_ung_dung.sql` | Tài khoản `civ09_app` của máy chủ ứng dụng, cấp quyền theo từng bảng |
| `05_du_lieu_xem_thu.sql` | Nhu cầu, ca, đăng ký, bản khai có sẵn để mỗi vai trò đăng nhập là có việc xem thử |

Toàn bộ dữ liệu nằm trong CSDL, kể cả ảnh, thông báo và nhật ký truy cập. Máy chủ ứng dụng không ghi tệp nào vào thư mục dự án. Các quy tắc nghiệp vụ được kiểm tra ở máy chủ ứng dụng và kiểm tra lại bằng trigger trong CSDL.

## Kiểm thử

Cần SQL Server đang chạy (`docker compose up -d`). Bộ kiểm thử tự tạo CSDL riêng tên `DieuPhoiTinhNguyenVien_KiemThu` nên không ảnh hưởng dữ liệu đang dùng.

```powershell
dotnet test backend/Civ09.slnx
```

Có 65 ca kiểm thử (49 tích hợp, 16 đơn vị). Tên ca bắt đầu bằng mã TC trong Bảng 7.2 của báo cáo; ba ca đơn vị viết thêm bắt đầu bằng mã quy tắc (BR). Kết quả lần chạy gần nhất nằm ở `docs/kiem_thu`.
