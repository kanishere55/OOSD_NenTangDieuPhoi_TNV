import { useEffect, useState } from 'react'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { api, dangNhap, dangXuat } from './api'
import { NguoiDungContext } from './chung'
import { TEN_VAI_TRO } from './tienIch'
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

  const laTnv = nguoiDung?.laTinhNguyenVien
  const laDaiDien = nguoiDung?.vaiTro.includes('DAI_DIEN')
  const laDieuPhoi = nguoiDung?.vaiTro.includes('DIEU_PHOI')
  const loiDangNhap = {
    'tai-khoan': 'Đăng nhập không thành công: tài khoản chưa được cấp trên nền tảng hoặc đã ngừng hoạt động.',
    'dang-nhap': 'Đăng nhập không thành công. Hãy bấm Đăng nhập để thử lại.'
  }[new URLSearchParams(viTri.search).get('loi')]

  return (
    <NguoiDungContext.Provider value={{ nguoiDung, daTai }}>
      <header className="dau-trang">
        <Link to="/" className="ten-he-thong">Điều phối tình nguyện viên</Link>
        <nav>
          <NavLink to="/" end>Tìm ca làm</NavLink>
          {laTnv && <NavLink to="/dang-ky-cua-toi">Đăng ký của tôi</NavLink>}
          {laTnv && <NavLink to="/dong-gop/cua-toi">Lịch sử đóng góp</NavLink>}
          {laTnv && <NavLink to="/ho-so">Hồ sơ và kỹ năng</NavLink>}
          {(laDaiDien || laDieuPhoi) && <NavLink to="/nhu-cau">Nhu cầu</NavLink>}
          {laDieuPhoi && <NavLink to="/dong-gop/cho-duyet">Duyệt đóng góp</NavLink>}
        </nav>
        <div className="tai-khoan">
          {nguoiDung ? (
            <>
              <span>
                <strong>{nguoiDung.tenHienThi}</strong>
                <small>{nguoiDung.vaiTro.map((v) => TEN_VAI_TRO[v]).join(', ')}</small>
              </span>
              <button className="phu" onClick={dangXuat}>Đăng xuất</button>
            </>
          ) : (
            daTai && <button onClick={() => dangNhap(viTri.pathname)}>Đăng nhập</button>
          )}
        </div>
      </header>
      <main>
        {loiDangNhap && <div className="thong-bao loi" role="alert">{loiDangNhap}</div>}
        <Routes>
          <Route path="/" element={<TimCa />} />
          <Route path="/dang-nhap" element={<DangNhap />} />
          <Route path="/ca/:ma" element={<ChiTietCa />} />
          <Route path="/ca/:ma/huong-dan-rieng" element={<HuongDanRieng />} />
          <Route path="/ca/:ma/xet-duyet" element={<XetDuyetDangKy />} />
          <Route path="/ho-so" element={<HoSo />} />
          <Route path="/dang-ky-cua-toi" element={<DangKyCuaToi />} />
          <Route path="/dang-ky/:ma/khai" element={<KhaiDongGop />} />
          <Route path="/nhu-cau" element={<DanhSachNhuCau />} />
          <Route path="/nhu-cau/moi" element={<FormNhuCau />} />
          <Route path="/nhu-cau/:ma" element={<FormNhuCau />} />
          <Route path="/nhu-cau/:ma/ca" element={<QuanLyCa />} />
          <Route path="/dong-gop/cho-duyet" element={<DuyetDongGop />} />
          <Route path="/dong-gop/cua-toi" element={<LichSuDongGop />} />
          <Route path="*" element={<p>Không tìm thấy trang. <Link to="/">Về trang tìm ca</Link></p>} />
        </Routes>
      </main>
    </NguoiDungContext.Provider>
  )
}
