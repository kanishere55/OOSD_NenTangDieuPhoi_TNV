import { Link } from 'react-router-dom'
import { dangKyTaiKhoan, dangNhap } from '../api'
import { useNguoiDung } from '../chung'

// M01 Đăng nhập: chuyển sang trang đăng nhập hoặc trang tạo tài khoản của Keycloak.
export default function DangNhap() {
  const { nguoiDung } = useNguoiDung()
  if (nguoiDung) {
    return (
      <section className="the hep">
        <h1>Bạn đã đăng nhập</h1>
        <p>Bạn đang dùng tài khoản <strong>{nguoiDung.tenHienThi}</strong>.</p>
        <Link className="nut" to="/">Về trang chủ</Link>
      </section>
    )
  }
  return (
    <section className="the hep">
      <h1>Đăng nhập</h1>
      <p>Bấm nút dưới đây, trang đăng nhập sẽ mở ra để bạn gõ tên đăng nhập và mật khẩu.</p>
      <div className="hang-nut">
        <button className="lon" onClick={() => dangNhap('/')}>Đăng nhập</button>
      </div>
      <h2>Chưa có tài khoản?</h2>
      <p>Ai cũng có thể tạo tài khoản tình nguyện viên. Tài khoản của đại diện và điều phối viên do nền tảng cấp cho tổ chức.</p>
      <button className="phu" onClick={() => dangKyTaiKhoan()}>Tạo tài khoản tình nguyện viên</button>
    </section>
  )
}
