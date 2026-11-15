#!/bin/bash
# Khởi tạo CSDL DieuPhoiTinhNguyenVien: tạo CSDL, bảng, view và dữ liệu mẫu nếu chưa có;
# luôn cập nhật thủ tục, trigger và tài khoản của ứng dụng.
# Tệp 05 (các ca, đăng ký, bản khai để xem thử) nạp sau khi đã có trigger, nên cũng đi qua các kiểm tra của CSDL.
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
[ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools/bin/sqlcmd
# sqlcmd đọc mật khẩu từ biến môi trường này, không để lộ trên dòng lệnh.
export SQLCMDPASSWORD="$SQL_SA_PASSWORD"
chay() { "$SQLCMD" -S sqlserver -U sa -C -I -b -f 65001 "$@"; }

echo "Kiểm tra CSDL..."
DA_CO=$(chay -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'DieuPhoiTinhNguyenVien') IS NULL THEN 0 ELSE 1 END")
MOI=0
if [ "$(echo "$DA_CO" | tr -d '[:space:]')" = "1" ]; then
  echo "CSDL đã có, không tạo lại bảng và dữ liệu mẫu."
else
  MOI=1
  echo "Tạo CSDL..."
  chay -i /database/00_tao_csdl.sql
  echo "Tạo bảng và view..."
  chay -d DieuPhoiTinhNguyenVien -i /database/01_tao_bang.sql
  echo "Nạp dữ liệu mẫu..."
  chay -d DieuPhoiTinhNguyenVien -i /database/02_du_lieu_mau.sql
fi
echo "Cập nhật thủ tục và trigger..."
chay -d DieuPhoiTinhNguyenVien -i /database/03_thu_tuc_va_trigger.sql
echo "Cập nhật tài khoản của ứng dụng..."
chay -d DieuPhoiTinhNguyenVien -v MatKhauUngDung="$SQL_APP_PASSWORD" -i /database/04_tai_khoan_ung_dung.sql
if [ "$MOI" = "1" ]; then
  echo "Nạp dữ liệu xem thử..."
  chay -d DieuPhoiTinhNguyenVien -i /database/05_du_lieu_xem_thu.sql
fi
echo "Xong."
