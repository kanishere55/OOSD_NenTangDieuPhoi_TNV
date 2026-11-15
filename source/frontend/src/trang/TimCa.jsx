import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api'
import { BanDoVung } from '../BanDo'
import { DangTai, DauTrang, ONgay, ThongBao, TrangThai, Trong, useNguoiDung, useTai } from '../chung'
import { conThieu } from '../kiemTraKyNang'
import { cuoiNgayUtc, dauNgayUtc, docNgay, hienKhoangGio, hienKm, hienSoCho, hienYeuCau } from '../tienIch'

const LOC_TRONG = { maKyNang: '', tuNgay: '', denNgay: '', khuVuc: '', banKinhKm: '', sapXep: 'som-nhat' }

// M02 Tìm ca làm: khách và tình nguyện viên xem các ca đang mở dạng danh sách hoặc bản đồ.
// Chỉ có thông tin công khai; bản đồ chỉ khoanh vùng gần đúng, không có địa chỉ cụ thể (BR21).
export default function TimCa() {
  const { laTnv } = useNguoiDung()
  const dieuHuong = useNavigate()
  const [loc, setLoc] = useState(LOC_TRONG)
  const [dangDung, setDangDung] = useState({ ...LOC_TRONG, trang: 1 })
  const [cachXem, setCachXem] = useState('danh-sach')
  const [viTriThietBi, setViTriThietBi] = useState(null)
  const [loiLoc, setLoiLoc] = useState(null)
  const kyNang = useTai(() => api.get('/api/ky-nang'), [])
  // Tình nguyện viên: lấy hồ sơ và các đăng ký còn hiệu lực để ghi chú ngay trên từng ca.
  const cuaToi = useTai(async () => {
    if (!laTnv) return null
    const [hs, dk] = await Promise.all([api.get('/api/ho-so'), api.get('/api/dang-ky/cua-toi')])
    return {
      hoSo: hs,
      dangKy: Object.fromEntries(dk.filter((d) => d.trangThai === 'CHO_DUYET' || d.trangThai === 'DA_PHAN_CONG').map((d) => [d.maCaLam, d.trangThai]))
    }
  }, [laTnv])
  const hoSo = cuaToi.duLieu?.hoSo
  const viTri = viTriThietBi ?? (hoSo?.viDo != null ? { viDo: hoSo.viDo, kinhDo: hoSo.kinhDo, tu: 'ho-so' } : null)

  const ketQua = useTai(() => {
    const q = new URLSearchParams({ trang: cachXem === 'ban-do' ? 1 : dangDung.trang, kichThuoc: cachXem === 'ban-do' ? 50 : 6 })
    if (dangDung.maKyNang) q.set('maKyNang', dangDung.maKyNang)
    if (dangDung.tuNgay) q.set('tuNgay', dauNgayUtc(dangDung.tuNgay))
    if (dangDung.denNgay) q.set('denNgay', cuoiNgayUtc(dangDung.denNgay))
    if (dangDung.khuVuc.trim()) q.set('khuVuc', dangDung.khuVuc.trim())
    if (viTri) {
      q.set('viDo', viTri.viDo); q.set('kinhDo', viTri.kinhDo)
      if (dangDung.banKinhKm) q.set('banKinhKm', dangDung.banKinhKm)
      if (dangDung.sapXep === 'gan-nhat') q.set('sapXep', 'gan-nhat')
    }
    return api.get('/api/ca?' + q)
  }, [dangDung, cachXem, viTri?.viDo, viTri?.kinhDo])

  const tim = (e) => {
    e.preventDefault()
    if ((loc.tuNgay && !docNgay(loc.tuNgay)) || (loc.denNgay && !docNgay(loc.denNgay))) {
      setLoiLoc('Ngày trong bộ lọc chưa đúng. Hãy gõ theo dạng ngày/tháng/năm, ví dụ 20/11/2026.')
      return
    }
    if ((loc.banKinhKm || loc.sapXep === 'gan-nhat') && !viTri) {
      setLoiLoc('Để lọc theo khoảng cách, hãy bấm "Dùng vị trí hiện tại" hoặc lưu nơi ở trong hồ sơ.')
      return
    }
    setLoiLoc(null)
    setDangDung({ ...loc, trang: 1 })
  }
  const boLoc = () => { setLoc(LOC_TRONG); setLoiLoc(null); setDangDung({ ...LOC_TRONG, trang: 1 }) }
  const layViTri = () => {
    if (!navigator.geolocation) { setLoiLoc('Trình duyệt này không lấy được vị trí.'); return }
    navigator.geolocation.getCurrentPosition(
      (p) => { setViTriThietBi({ viDo: p.coords.latitude, kinhDo: p.coords.longitude, tu: 'thiet-bi' }); setLoiLoc(null) },
      () => setLoiLoc('Không lấy được vị trí. Hãy cho phép trình duyệt dùng vị trí rồi thử lại.'),
      { timeout: 10000 })
  }
  const dangLoc = dangDung.maKyNang || dangDung.tuNgay || dangDung.denNgay || dangDung.khuVuc || dangDung.banKinhKm
  const kq = ketQua.duLieu
  const soTrang = kq ? Math.max(1, Math.ceil(kq.tongSo / kq.kichThuoc)) : 1

  const ghiChu = (c) => {
    if (!cuaToi.duLieu) return null
    const tt = cuaToi.duLieu.dangKy[c.maCaLam]
    if (tt) return <p>Bạn đã đăng ký: <TrangThai ma={tt} /></p>
    const thieu = conThieu(c.yeuCau, hoSo)
    return thieu.length === 0
      ? <p className="hop-ky-nang">Hợp với kỹ năng của bạn</p>
      : <p className="thieu-ky-nang">Chưa đủ điều kiện: {thieu.join('; ')}</p>
  }

  return (
    <>
      <DauTrang tieuDe="Tìm ca làm" moTa="Bản đồ chỉ khoanh vùng gần đúng. Địa chỉ cụ thể gửi riêng cho người đã được phân công." />
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
          <label>Khoảng cách
            <select value={loc.banKinhKm} onChange={(e) => setLoc({ ...loc, banKinhKm: e.target.value })}>
              <option value="">Không giới hạn</option>
              {[2, 5, 10, 20].map((k) => <option key={k} value={k}>Trong vòng {k} km</option>)}
            </select>
          </label>
          <label>Xếp theo
            <select value={loc.sapXep} onChange={(e) => setLoc({ ...loc, sapXep: e.target.value })}>
              <option value="som-nhat">Ca sớm nhất</option>
              <option value="gan-nhat">Gần tôi nhất</option>
            </select>
          </label>
        </div>
        <div className="cuoi-bo-loc">
          <span className="mo" style={{ marginRight: 'auto' }}>
            {viTri ? (viTri.tu === 'ho-so' ? 'Khoảng cách tính từ nơi ở trong hồ sơ của bạn.' : 'Khoảng cách tính từ vị trí hiện tại của bạn.') : 'Chưa có vị trí để tính khoảng cách.'}
          </span>
          <button type="button" className="phu" onClick={layViTri}>Dùng vị trí hiện tại</button>
          {dangLoc && <button type="button" className="phu" onClick={boLoc}>Bỏ lọc</button>}
          <button type="submit">Tìm ca</button>
        </div>
        {loiLoc && <div className="thong-bao loi" role="alert">{loiLoc}</div>}
      </form>

      <div className="the-muc" role="tablist">
        <button type="button" role="tab" aria-selected={cachXem === 'danh-sach'} className={cachXem === 'danh-sach' ? 'dang-chon' : ''} onClick={() => setCachXem('danh-sach')}>Xem dạng danh sách</button>
        <button type="button" role="tab" aria-selected={cachXem === 'ban-do'} className={cachXem === 'ban-do' ? 'dang-chon' : ''} onClick={() => setCachXem('ban-do')}>Xem trên bản đồ</button>
      </div>

      <ThongBao loi={ketQua.loi} />
      {ketQua.dangTai && !kq ? <DangTai /> : kq && (
        kq.danhSach.length === 0 ? (
          <Trong>
            <p>{dangLoc ? 'Không có ca nào khớp với điều kiện bạn chọn.' : 'Hiện chưa có ca nào đang nhận đăng ký.'}</p>
            {dangLoc && <div className="hang-nut"><button className="phu" onClick={boLoc}>Bỏ lọc để xem tất cả</button></div>}
          </Trong>
        ) : cachXem === 'ban-do' ? (
          <>
            <p className="mo">Có {kq.tongSo} ca đang nhận đăng ký. Mỗi vòng tròn là khu vực gần đúng của một nhu cầu, bấm vào vòng tròn để xem ca.</p>
            <BanDoVung cao={460} viTriCuaToi={viTri}
              vung={kq.danhSach.filter((c) => c.viDoVung != null).map((c) => ({
                viDo: c.viDoVung, kinhDo: c.kinhDoVung, banKinh: c.banKinhVung,
                nhan: `${c.tieuDe} · ${hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}`,
                onBam: () => dieuHuong('/ca/' + c.maCaLam)
              }))} />
            {kq.danhSach.some((c) => c.viDoVung == null) && <p className="chu-thich-ban-do">Một số ca chưa có vị trí trên bản đồ, xem ở dạng danh sách.</p>}
          </>
        ) : (
          <>
            <p className="mo">Có {kq.tongSo} ca đang nhận đăng ký{dangLoc ? ' khớp với điều kiện lọc' : ''}.</p>
            <div className="luoi-the">
              {kq.danhSach.map((c) => (
                <article key={c.maCaLam} className="the the-ca">
                  {c.anh.length > 0 && <img className="anh-dai-dien" src={'/api/anh/nhu-cau/' + c.anh[0]} alt="" loading="lazy" />}
                  <h3><Link to={'/ca/' + c.maCaLam}>{c.tieuDe}</Link></h3>
                  <p className="mo">{c.tenToChuc} · {c.khuVucChung}{c.khoangCachKm != null && ` · cách bạn ${hienKm(c.khoangCachKm)}`}</p>
                  <p className="dong-chinh">{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</p>
                  <p>{hienSoCho(c.soNguoiCan, c.soDaPhanCong)}</p>
                  <p>Kỹ năng cần: {c.yeuCau.map((y) => <span key={y.maKyNang} className="nhan nhan-ky-nang">{hienYeuCau(y)}</span>)}</p>
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
