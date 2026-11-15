import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DuongDan, HopXacNhan, ThongBao, TrangThai, Trong, useTai, useThaoTac } from '../chung'
import { hienGioPhut, hienKhoangGio } from '../tienIch'

// Giờ, phút (làm tròn xuống 5 phút) theo giờ Việt Nam của lần điểm danh đã ghi, để điền sẵn vào hộp sửa.
function tach(utc) {
  if (!utc) return ['', '00']
  const d = new Date(new Date(utc).getTime() + 7 * 3600 * 1000)
  return [String(d.getUTCHours()).padStart(2, '0'), String(Math.floor(d.getUTCMinutes() / 5) * 5).padStart(2, '0')]
}
function gioDaGhi(dd) {
  const co = dd?.ketQua === 'CO_MAT'
  const [vaoGio, vaoPhut] = tach(co ? dd.gioVao : null)
  const [raGio, raPhut] = tach(co ? dd.gioRa : null)
  return { vaoGio, vaoPhut, raGio, raPhut }
}
const GIO = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'))
const PHUT = Array.from({ length: 12 }, (_, i) => String(i * 5).padStart(2, '0'))

// M17 Điểm danh của ca: điều phối viên đưa mã QR cho người tham gia quét, xem ai đã đến,
// ghi nhận có mặt cho người không quét được hoặc ghi nhận vắng mặt (BR23, BR27).
export default function DiemDanhCa() {
  const { ma } = useParams()
  const ds = useTai(() => api.get(`/api/ca/${ma}/diem-danh`), [ma])
  const [qr, setQr] = useState(null)
  const [loiQr, setLoiQr] = useState(null)
  const [conLai, setConLai] = useState(0)
  const [hoi, setHoi] = useState(null)   // { viec: 'vang' | 'co-mat', n }
  const [gio, setGio] = useState({ vaoGio: '', vaoPhut: '00', raGio: '', raPhut: '00' })
  const tt = useThaoTac()

  const trongGio = ds.duLieu?.trongGioDiemDanh
  const layQr = useCallback(async () => {
    try { setQr(await api.get(`/api/ca/${ma}/diem-danh/ma-qr`)); setLoiQr(null) } catch (e) { setLoiQr(e.message) }
  }, [ma])
  // Mã đổi mỗi 2 phút: lấy mã mới khi mã cũ hết hạn, đồng thời cập nhật danh sách người đã điểm danh.
  useEffect(() => { if (trongGio) layQr() }, [trongGio, layQr])
  useEffect(() => {
    if (!qr) return
    const t = setInterval(() => {
      const s = Math.max(0, Math.round((new Date(qr.hetHan) - new Date()) / 1000))
      setConLai(s)
      if (s <= 0) { layQr(); ds.taiLai() }
    }, 1000)
    return () => clearInterval(t)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [qr, layQr])

  const ghiVang = async () => {
    const n = hoi.n
    const ok = await tt.chay(() => api.post(`/api/dang-ky/${n.maDangKy}/vang-mat`), `Đã ghi nhận ${n.tenHienThi} vắng mặt.`)
    setHoi(null)
    if (ok) ds.taiLai()
  }
  const ghiCoMat = async () => {
    const n = hoi.n
    if (!gio.vaoGio) { tt.setLoi('Hãy chọn giờ vào.'); return }
    const c = ds.duLieu
    const ngay = (utc) => new Date(new Date(utc).getTime() + 7 * 3600 * 1000).toISOString().slice(0, 10)
    const ghep = (g, p) => new Date(`${ngay(c.batDauDuKien)}T${g}:${p}:00+07:00`)
    const vao = ghep(gio.vaoGio, gio.vaoPhut)
    let ra = gio.raGio ? ghep(gio.raGio, gio.raPhut) : null
    // Ca qua nửa đêm: giờ ra nhỏ hơn giờ vào thì là ngày hôm sau.
    if (ra && ra <= vao) ra = new Date(ra.getTime() + 24 * 3600 * 1000)
    const ok = await tt.chay(() => api.post(`/api/dang-ky/${n.maDangKy}/co-mat`, {
      gioVao: vao.toISOString(), gioRa: ra ? ra.toISOString() : null
    }), `Đã ghi nhận ${n.tenHienThi} có mặt.`)
    if (ok) { setHoi(null); ds.taiLai() }
  }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  const c = ds.duLieu
  const daDen = c.nguoiThamGia.filter((n) => n.diemDanh?.ketQua === 'CO_MAT').length
  return (
    <>
      <DuongDan muc={[{ ten: 'Điều phối ca', den: '/dieu-phoi' }, { ten: 'Xét duyệt đăng ký', den: `/ca/${ma}/xet-duyet` }, { ten: 'Điểm danh' }]} />
      <h1>Điểm danh</h1>
      <p className="mo-ta"><strong>{c.tieuDe}</strong> · {hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)} <TrangThai ma={c.trangThai} /></p>

      <section className="the khung-qr">
        {trongGio ? (
          qr ? (
            <>
              <p>Người tham gia quét mã này bằng máy ảnh điện thoại để điểm danh vào và ra.</p>
              <img src={qr.anhQr} alt="Mã QR điểm danh" />
              <p>Hoặc gõ mã: <span className="ma-chu">{qr.ma}</span></p>
              <p className="mo">Mã tự đổi sau {Math.floor(conLai / 60)}:{String(conLai % 60).padStart(2, '0')} để ảnh chụp mã không dùng lại được từ xa.</p>
            </>
          ) : loiQr ? <ThongBao loi={loiQr} /> : <DangTai />
        ) : (
          <p className="mo">Mã điểm danh chỉ hiện từ 30 phút trước giờ bắt đầu đến 2 giờ sau giờ kết thúc ca.</p>
        )}
      </section>

      <ThongBao loi={tt.loi && !hoi ? tt.loi : null} thanhCong={tt.thanhCong} />
      <h2>Người được phân công ({daDen}/{c.nguoiThamGia.length} đã đến)</h2>
      {c.nguoiThamGia.length === 0 ? <Trong><p>Ca chưa có ai được phân công.</p></Trong> : (
        <ul className="ds-dong">
          {c.nguoiThamGia.map((n) => (
            <li key={n.maDangKy} className={!n.diemDanh && c.duocGhiVang ? 'noi-bat' : ''}>
              <div className="than">
                <p><strong>{n.tenHienThi}</strong></p>
                {n.diemDanh ? (
                  <p><TrangThai ma={n.diemDanh.ketQua} />{' '}
                    {n.diemDanh.ketQua === 'CO_MAT' && <span className="mo">
                      Vào {hienGioPhut(n.diemDanh.gioVao)}{n.diemDanh.gioRa ? `, ra ${hienGioPhut(n.diemDanh.gioRa)}` : ', chưa ra'}
                      {n.diemDanh.cachGhi === 'DIEU_PHOI' ? ' (điều phối viên ghi nhận)' : ' (quét mã)'}</span>}
                  </p>
                ) : <p className="mo">Chưa điểm danh</p>}
                {n.daKhai && <p className="mo">Đã khai đóng góp nên không đổi điểm danh được nữa.</p>}
              </div>
              {!n.daKhai && (
                <div className="hang-nut">
                  {c.duocGhiCoMat && <button className="phu" onClick={() => { setHoi({ viec: 'co-mat', n }); setGio(gioDaGhi(n.diemDanh)); tt.setLoi(null) }}>
                    {n.diemDanh?.ketQua === 'CO_MAT' ? 'Sửa giờ vào, ra' : 'Ghi nhận có mặt'}</button>}
                  {c.duocGhiVang && n.diemDanh?.ketQua !== 'VANG_MAT' && <button className="phu nguy-hiem" onClick={() => setHoi({ viec: 'vang', n })}>Ghi nhận vắng mặt</button>}
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
      <p><Link to={`/ca/${ma}/xet-duyet`}>‹ Về trang xét duyệt của ca</Link></p>

      {hoi?.viec === 'vang' && (
        <HopXacNhan tieuDe="Ghi nhận vắng mặt?" nhanDongY="Ghi nhận vắng mặt" dangGui={tt.dangGui} onDongY={ghiVang} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.n.tenHienThi}</strong> sẽ được ghi là vắng mặt ở ca này và không khai đóng góp được.</p>
          <p>Người này nhận được thông báo. Nếu ghi nhầm, bạn sửa lại bằng nút Ghi nhận có mặt.</p>
        </HopXacNhan>
      )}
      {hoi?.viec === 'co-mat' && (
        <div className="nen-hop" onClick={(e) => { if (e.target === e.currentTarget) setHoi(null) }}>
          <div className="hop" role="dialog" aria-modal="true" aria-label="Ghi nhận có mặt">
            <h2>{hoi.n.diemDanh?.ketQua === 'CO_MAT' ? 'Sửa giờ vào, ra' : 'Ghi nhận có mặt'}</h2>
            <p><strong>{hoi.n.tenHienThi}</strong>, ca {hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</p>
            {[['Giờ vào', 'vaoGio', 'vaoPhut'], ['Giờ ra (để trống nếu chưa về)', 'raGio', 'raPhut']].map(([nhan, g, p]) => (
              <fieldset key={g} className="ngay-gio">
                <legend>{nhan}</legend>
                <div className="hang-ngay-gio" style={{ gridTemplateColumns: '1fr 1fr' }}>
                  <label>Giờ
                    <select value={gio[g]} onChange={(e) => setGio({ ...gio, [g]: e.target.value })}>
                      <option value="">--</option>{GIO.map((x) => <option key={x} value={x}>{x} giờ</option>)}
                    </select>
                  </label>
                  <label>Phút
                    <select value={gio[p]} onChange={(e) => setGio({ ...gio, [p]: e.target.value })}>
                      {PHUT.map((x) => <option key={x} value={x}>{x} phút</option>)}
                    </select>
                  </label>
                </div>
              </fieldset>
            ))}
            <ThongBao loi={tt.loi} />
            <div className="hang-nut cuoi-hop">
              <button className="phu" disabled={tt.dangGui} onClick={() => setHoi(null)}>Thôi</button>
              <button disabled={tt.dangGui} onClick={ghiCoMat}>Lưu</button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
