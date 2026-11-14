import { useEffect, useState } from 'react'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { api, dangKyTaiKhoan, dangNhap, dangXuat } from './api'
import { DangTai, NguoiDungContext, useNguoiDung } from './chung'
import { TEN_VAI_TRO } from './tienIch'
import TrangChu from './trang/TrangChu'
import DangNhap from './trang/DangNhap'
import TimCa from './trang/TimCa'
import ChiTietCa from './trang/ChiTietCa'
import HoSo from './trang/HoSo'
import DangKyCuaToi from './trang/DangKyCuaToi'
import DanhSachNhuCau from './trang/DanhSachNhuCau'
import FormNhuCau from './trang/FormNhuCau'
import QuanLyCa from './trang/QuanLyCa'
import XetDuyetDangKy from './trang/XetDuyetDangKy'
import KhaiDongGop from './trang/KhaiDongGop'
import DuyetDongGop from './trang/DuyetDongGop'
import HuongDanRieng from './trang/HuongDanRieng'
import LichSuDongGop from './trang/LichSuDongGop'
import DieuPhoiCa from './trang/DieuPhoiCa'

// Bọc các màn hình cần đăng nhập: người chưa đăng nhập được mời đăng nhập thay vì thấy thông báo lỗi.
function CanDangNhap({ children }) {
  const { nguoiDung, daTai } = useNguoiDung()
  const viTri = useLocation()
  if (!daTai) return <DangTai />
  if (nguoiDung) return children
  return (
    <section className="the hep">
      <h1>Bạn cần đăng nhập</h1>
      <p>Mục này chỉ dùng được sau khi đăng nhập.</p>
      <div className="hang-nut">
        <button onClick={() => dangNhap(viTri.pathname)}>Đăng nhập</button>
        <button className="phu" onClick={() => dangKyTaiKhoan()}>Tạo tài khoản tình nguyện viên</button>
      </div>
    </section>
  )
}

export default function App() {
  const [nguoiDung, setNguoiDung] = useState(null)
  const [daTai, setDaTai] = useState(false)
  const viTri = useLocation()

  useEffect(() => {
    api.get('/api/auth/toi')
      .then(setNguoiDung)
      .catch(() => setNguoiDung(null))
      .finally(() => setDaTai(true))
  }, [])
  // Sang trang khác thì đưa người dùng về đầu trang.
  useEffect(() => { window.scrollTo(0, 0) }, [viTri.pathname])

  const laTnv = nguoiDung?.laTinhNguyenVien
  const laDaiDien = nguoiDung?.thanhVien.some((t) => t.vaiTro === 'DAI_DIEN')
  const laDieuPhoi = nguoiDung?.thanhVien.some((t) => t.vaiTro === 'DIEU_PHOI')
  const loiDangNhap = {
    'tai-khoan': 'Tài khoản của bạn đã ngừng hoạt động trên nền tảng nên không đăng nhập được. Hãy liên hệ tổ chức của bạn.',
    'dang-nhap': 'Đăng nhập chưa thành công. Hãy bấm Đăng nhập để thử lại.'
  }[new URLSearchParams(viTri.search).get('loi')]

  return (
    <NguoiDungContext.Provider value={{ nguoiDung, daTai, laTnv, laDaiDien, laDieuPhoi }}>
      <header>
        <div className="dau-trang">
          <div className="trong-dau">
            <Link to="/" className="ten-he-thong">Điều phối tình nguyện viên</Link>
            <div className="tai-khoan">
              {nguoiDung ? (
                <>
                  <span className="ten">
                    <strong>{nguoiDung.tenHienThi}</strong>
                    <small>{nguoiDung.vaiTro.map((v) => TEN_VAI_TRO[v]).join(', ')}</small>
                  </span>
                  <button className="phu" onClick={dangXuat}>Đăng xuất</button>
                </>
              ) : daTai && (
                <>
                  <button className="phu" onClick={() => dangKyTaiKhoan()}>Tạo tài khoản</button>
                  <button className="sang" onClick={() => dangNhap(viTri.pathname)}>Đăng nhập</button>
                </>
              )}
            </div>
          </div>
        </div>
        <div className="thanh-muc">
          <nav aria-label="Các mục chính">
            <NavLink to="/" end>Trang chủ</NavLink>
            <NavLink to="/tim-ca">Tìm ca làm</NavLink>
            {laTnv && <NavLink to="/dang-ky-cua-toi">Đăng ký của tôi</NavLink>}
            {laTnv && <NavLink to="/dong-gop/cua-toi">Lịch sử đóng góp</NavLink>}
            {laTnv && <NavLink to="/ho-so">Hồ sơ và kỹ năng</NavLink>}
            {(laDaiDien || laDieuPhoi) && <NavLink to="/nhu-cau">Nhu cầu</NavLink>}
            {(laDaiDien || laDieuPhoi) && <NavLink to="/dieu-phoi">Điều phối ca</NavLink>}
            {laDieuPhoi && <NavLink to="/dong-gop/cho-duyet">Duyệt đóng góp</NavLink>}
          </nav>
        </div>
      </header>
      <main>
        {loiDangNhap && <div className="thong-bao loi" role="alert">{loiDangNhap}</div>}
        <Routes>
          <Route path="/" element={<TrangChu />} />
          <Route path="/tim-ca" element={<TimCa />} />
          <Route path="/dang-nhap" element={<DangNhap />} />
          <Route path="/ca/:ma" element={<ChiTietCa />} />
          <Route path="/ca/:ma/huong-dan-rieng" element={<CanDangNhap><HuongDanRieng /></CanDangNhap>} />
          <Route path="/ca/:ma/xet-duyet" element={<CanDangNhap><XetDuyetDangKy /></CanDangNhap>} />
          <Route path="/ho-so" element={<CanDangNhap><HoSo /></CanDangNhap>} />
          <Route path="/dang-ky-cua-toi" element={<CanDangNhap><DangKyCuaToi /></CanDangNhap>} />
          <Route path="/dang-ky/:ma/khai" element={<CanDangNhap><KhaiDongGop /></CanDangNhap>} />
          <Route path="/nhu-cau" element={<CanDangNhap><DanhSachNhuCau /></CanDangNhap>} />
          <Route path="/nhu-cau/moi" element={<CanDangNhap><FormNhuCau /></CanDangNhap>} />
          <Route path="/nhu-cau/:ma" element={<CanDangNhap><FormNhuCau /></CanDangNhap>} />
          <Route path="/nhu-cau/:ma/ca" element={<CanDangNhap><QuanLyCa /></CanDangNhap>} />
          <Route path="/dieu-phoi" element={<CanDangNhap><DieuPhoiCa /></CanDangNhap>} />
          <Route path="/dong-gop/cho-duyet" element={<CanDangNhap><DuyetDongGop /></CanDangNhap>} />
          <Route path="/dong-gop/cua-toi" element={<CanDangNhap><LichSuDongGop /></CanDangNhap>} />
          <Route path="*" element={<div className="trong"><p>Không tìm thấy trang này.</p><p><Link className="nut" to="/">Về trang chủ</Link></p></div>} />
        </Routes>
      </main>
      <footer className="chan-trang">
        <div>Nền tảng điều phối tình nguyện viên theo kỹ năng. Giờ hiển thị trên trang là giờ Việt Nam.</div>
      </footer>
    </NguoiDungContext.Provider>
  )
}
