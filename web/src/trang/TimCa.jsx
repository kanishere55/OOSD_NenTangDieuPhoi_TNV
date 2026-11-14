import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, ONgay, ThongBao, TrangThai, Trong, useNguoiDung, useTai } from '../chung'
import { cuoiNgayUtc, dauNgayUtc, docNgay, hienKhoangGio, hienSoCho } from '../tienIch'

const LOC_TRONG = { maKyNang: '', tuNgay: '', denNgay: '', khuVuc: '' }

// M02 Tìm ca làm: khách và tình nguyện viên xem các ca đang mở. Chỉ có thông tin công khai.
export default function TimCa() {
  const { laTnv } = useNguoiDung()
  const [loc, setLoc] = useState(LOC_TRONG)
  const [dangDung, setDangDung] = useState({ ...LOC_TRONG, trang: 1 })
  const [loiLoc, setLoiLoc] = useState(null)
  const kyNang = useTai(() => api.get('/api/ky-nang'), [])
  // Tình nguyện viên: lấy kỹ năng và các đăng ký còn hiệu lực để ghi chú ngay trên từng ca.
  const cuaToi = useTai(async () => {
    if (!laTnv) return null
    const [hs, dk] = await Promise.all([api.get('/api/ho-so'), api.get('/api/dang-ky/cua-toi')])
    return {
      kyNang: new Set(hs.kyNang.map((k) => k.tenKyNang)),
      dangKy: Object.fromEntries(dk.filter((d) => d.trangThai === 'CHO_DUYET' || d.trangThai === 'DA_PHAN_CONG').map((d) => [d.maCaLam, d.trangThai]))
    }
  }, [laTnv])
  const ketQua = useTai(() => {
    const q = new URLSearchParams({ trang: dangDung.trang, kichThuoc: 6 })
    if (dangDung.maKyNang) q.set('maKyNang', dangDung.maKyNang)
    if (dangDung.tuNgay) q.set('tuNgay', dauNgayUtc(dangDung.tuNgay))
    if (dangDung.denNgay) q.set('denNgay', cuoiNgayUtc(dangDung.denNgay))
    if (dangDung.khuVuc.trim()) q.set('khuVuc', dangDung.khuVuc.trim())
    return api.get('/api/ca?' + q)
  }, [dangDung])

  const tim = (e) => {
    e.preventDefault()
    if ((loc.tuNgay && !docNgay(loc.tuNgay)) || (loc.denNgay && !docNgay(loc.denNgay))) {
      setLoiLoc('Ngày trong bộ lọc chưa đúng. Hãy gõ theo dạng ngày/tháng/năm, ví dụ 20/11/2026.')
      return
    }
    setLoiLoc(null)
    setDangDung({ ...loc, trang: 1 })
  }
  const boLoc = () => { setLoc(LOC_TRONG); setLoiLoc(null); setDangDung({ ...LOC_TRONG, trang: 1 }) }
  const dangLoc = dangDung.maKyNang || dangDung.tuNgay || dangDung.denNgay || dangDung.khuVuc
  const kq = ketQua.duLieu
  const soTrang = kq ? Math.max(1, Math.ceil(kq.tongSo / kq.kichThuoc)) : 1

  const ghiChu = (c) => {
    if (!cuaToi.duLieu) return null
    const tt = cuaToi.duLieu.dangKy[c.maCaLam]
    if (tt) return <p>Bạn đã đăng ký: <TrangThai ma={tt} /></p>
    const thieu = c.kyNangYeuCau.filter((k) => !cuaToi.duLieu.kyNang.has(k))
    return thieu.length === 0
      ? <p className="hop-ky-nang">Hợp với kỹ năng của bạn</p>
      : <p className="thieu-ky-nang">Hồ sơ của bạn chưa có: {thieu.join(', ')}</p>
  }

  return (
    <>
      <DauTrang tieuDe="Tìm ca làm" />
      <form className="the" onSubmit={tim}>
        <div className="bo-loc">
          <label>Kỹ năng ca cần
            <select value={loc.maKyNang} onChange={(e) => setLoc({ ...loc, maKyNang: e.target.value })}>
              <option value="">Tất cả kỹ năng</option>
              {kyNang.duLieu?.map((k) => <option key={k.maKyNang} value={k.maKyNang}>{k.tenKyNang}</option>)}
            </select>
          </label>
          <ONgay nhan="Từ ngày" giaTri={loc.tuNgay} onDoi={(v) => setLoc({ ...loc, tuNgay: v })} />
          <ONgay nhan="Đến ngày" giaTri={loc.denNgay} onDoi={(v) => setLoc({ ...loc, denNgay: v })} />
          <label>Khu vực
            <input value={loc.khuVuc} placeholder="ví dụ: An Bình" onChange={(e) => setLoc({ ...loc, khuVuc: e.target.value })} />
          </label>
        </div>
        <div className="cuoi-bo-loc">
          <span style={{ marginRight: 'auto' }} />
          {dangLoc && <button type="button" className="phu" onClick={boLoc}>Bỏ lọc</button>}
          <button type="submit">Tìm ca</button>
        </div>
        {loiLoc && <div className="thong-bao loi" role="alert">{loiLoc}</div>}
      </form>

      <ThongBao loi={ketQua.loi} />
      {ketQua.dangTai ? <DangTai /> : kq && (
        kq.danhSach.length === 0 ? (
          <Trong>
            <p>{dangLoc ? 'Không có ca nào khớp với điều kiện bạn chọn.' : 'Hiện chưa có ca nào đang nhận đăng ký.'}</p>
            {dangLoc && <div className="hang-nut"><button className="phu" onClick={boLoc}>Bỏ lọc để xem tất cả</button></div>}
          </Trong>
        ) : (
          <>
            <p className="mo">Có {kq.tongSo} ca đang nhận đăng ký{dangLoc ? ' khớp với điều kiện lọc' : ''}.</p>
            <div className="luoi-the">
              {kq.danhSach.map((c) => (
                <article key={c.maCaLam} className="the the-ca">
                  <h3><Link to={'/ca/' + c.maCaLam}>{c.tieuDe}</Link></h3>
                  <p className="mo">{c.tenToChuc} · {c.khuVucChung}</p>
                  <p className="dong-chinh">{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</p>
                  <p>{hienSoCho(c.soNguoiCan, c.soDaPhanCong)}</p>
                  <p>Kỹ năng cần: {c.kyNangYeuCau.map((k) => <span key={k} className="nhan nhan-ky-nang">{k}</span>)}</p>
                  {ghiChu(c)}
                  <Link className="nut phu" to={'/ca/' + c.maCaLam}>Xem chi tiết</Link>
                </article>
              ))}
            </div>
            {soTrang > 1 && (
              <div className="phan-trang">
                <button className="phu" disabled={dangDung.trang <= 1} onClick={() => setDangDung({ ...dangDung, trang: dangDung.trang - 1 })}>‹ Trang trước</button>
                <span>Trang {dangDung.trang} trên {soTrang}</span>
                <button className="phu" disabled={dangDung.trang >= soTrang} onClick={() => setDangDung({ ...dangDung, trang: dangDung.trang + 1 })}>Trang sau ›</button>
              </div>
            )}
          </>
        )
      )}
    </>
  )
}
