import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, useTai } from '../chung'
import { gioVnSangUtc, hienKhoangGio } from '../tienIch'

// M02 Tìm ca làm: khách và tình nguyện viên xem các ca đang mở. Chỉ có thông tin công khai.
export default function TimCa() {
  const [loc, setLoc] = useState({ maKyNang: '', tuNgay: '', denNgay: '', khuVuc: '' })
  const [dangDung, setDangDung] = useState({ ...loc, trang: 1 })
  const kyNang = useTai(() => api.get('/api/ky-nang'), [])
  const ketQua = useTai(() => {
    const q = new URLSearchParams({ trang: dangDung.trang, kichThuoc: 6 })
    if (dangDung.maKyNang) q.set('maKyNang', dangDung.maKyNang)
    if (dangDung.tuNgay) q.set('tuNgay', gioVnSangUtc(dangDung.tuNgay + 'T00:00'))
    if (dangDung.denNgay) q.set('denNgay', gioVnSangUtc(dangDung.denNgay + 'T23:59'))
    if (dangDung.khuVuc) q.set('khuVuc', dangDung.khuVuc)
    return api.get('/api/ca?' + q)
  }, [dangDung])

  const tim = (e) => { e.preventDefault(); setDangDung({ ...loc, trang: 1 }) }
  const kq = ketQua.duLieu
  const soTrang = kq ? Math.max(1, Math.ceil(kq.tongSo / kq.kichThuoc)) : 1

  return (
    <>
      <h1>Tìm ca làm</h1>
      <form className="bo-loc" onSubmit={tim}>
        <label>Kỹ năng
          <select value={loc.maKyNang} onChange={(e) => setLoc({ ...loc, maKyNang: e.target.value })}>
            <option value="">Tất cả</option>
            {kyNang.duLieu?.map((k) => <option key={k.maKyNang} value={k.maKyNang}>{k.tenKyNang}</option>)}
          </select>
        </label>
        <label>Từ ngày<input type="date" value={loc.tuNgay} onChange={(e) => setLoc({ ...loc, tuNgay: e.target.value })} /></label>
        <label>Đến ngày<input type="date" value={loc.denNgay} onChange={(e) => setLoc({ ...loc, denNgay: e.target.value })} /></label>
        <label>Khu vực<input value={loc.khuVuc} placeholder="Ví dụ: An Bình" onChange={(e) => setLoc({ ...loc, khuVuc: e.target.value })} /></label>
        <button type="submit">Tìm</button>
      </form>
      <ThongBao loi={ketQua.loi} />
      {ketQua.dangTai ? <DangTai /> : kq && (
        kq.danhSach.length === 0 ? <p className="mo">Không có ca nào phù hợp với bộ lọc.</p> : (
          <>
            <div className="luoi-the">
              {kq.danhSach.map((c) => (
                <Link key={c.maCaLam} to={'/ca/' + c.maCaLam} className="the the-ca">
                  <h3>{c.tieuDe}</h3>
                  <p className="mo">{c.khuVucChung}</p>
                  <p>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</p>
                  <p>Cần {c.soNguoiCan} người</p>
                  <p className="ds-nhan">{c.kyNangYeuCau.map((k) => <span key={k} className="nhan">{k}</span>)}</p>
                </Link>
              ))}
            </div>
            <div className="phan-trang">
              <button className="phu" disabled={dangDung.trang <= 1} onClick={() => setDangDung({ ...dangDung, trang: dangDung.trang - 1 })}>Trang trước</button>
              <span>Trang {dangDung.trang} / {soTrang} ({kq.tongSo} ca)</span>
              <button className="phu" disabled={dangDung.trang >= soTrang} onClick={() => setDangDung({ ...dangDung, trang: dangDung.trang + 1 })}>Trang sau</button>
            </div>
          </>
        )
      )}
    </>
  )
}
