import { dangNhap } from '../api'
import { useNguoiDung } from '../chung'
import { Link } from 'react-router-dom'

// M01 Đăng nhập: chuyển sang trang đăng nhập của Keycloak.
export default function DangNhap() {
  const { nguoiDung } = useNguoiDung()
  if (nguoiDung) return <p>Bạn đã đăng nhập với tên <strong>{nguoiDung.tenHienThi}</strong>. <Link to="/">Về trang tìm ca</Link></p>
  return (
    <section className="the hep">
      <h1>Đăng nhập</h1>
      <p>Nền tảng dùng Keycloak để đăng nhập. Bấm nút dưới đây để chuyển sang trang đăng nhập, nhập tên đăng nhập và mật khẩu đã được cấp.</p>
      <button onClick={() => dangNhap('/')}>Đăng nhập</button>
    </section>
  )
}
