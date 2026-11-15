import { useEffect, useRef, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { ThongBao, useTai, useThaoTac } from '../chung'
import { hienGioPhut, hienKhoangGio } from '../tienIch'

// M16 Điểm danh tại ca: tình nguyện viên quét mã QR của điều phối viên (trang mở sẵn mã) hoặc gõ mã (BR23).
// Lần đầu là điểm danh vào, lần sau là điểm danh ra.
export default function DiemDanh() {
  const { ma } = useParams()
  const [thamSo] = useSearchParams()
  const [maNhap, setMaNhap] = useState(thamSo.get('ma') ?? '')
  const [ketQua, setKetQua] = useState(null)
  const tt = useThaoTac()
  const daGui = useRef(false)
  // Ca của chính người dùng, để hiện tên ca và biết đây là lần điểm danh vào hay ra.
  const dk = useTai(() => api.get('/api/dang-ky/cua-toi'), [ketQua])
  const cua = dk.duLieu?.find((d) => d.maCaLam === ma && d.trangThai === 'DA_PHAN_CONG')
  const daVao = cua?.diemDanh?.ketQua === 'CO_MAT'

  const gui = async (maGui) => {
    setKetQua(null)
    const kq = await tt.chay(() => api.post(`/api/ca/${ma}/diem-danh`, { ma: maGui.replace(/\s/g, '') }))
    if (kq && kq !== true) setKetQua(kq)
  }
  // Mở bằng mã QR thì gửi ngay, người dùng không phải bấm thêm.
  useEffect(() => {
    const m = thamSo.get('ma')
    if (m && !daGui.current) { daGui.current = true; gui(m) }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <section className="the hep">
      <h1>Điểm danh tại ca</h1>
      {cua && (
        <dl className="thong-tin">
          <dt>Ca làm</dt><dd>{cua.tieuDe}</dd>
          <dt>Thời gian</dt><dd>{hienKhoangGio(cua.batDauDuKien, cua.ketThucDuKien)}</dd>
          {daVao && <><dt>Đã điểm danh</dt><dd>Vào lúc {hienGioPhut(cua.diemDanh.gioVao)}{cua.diemDanh.gioRa && `, ra lúc ${hienGioPhut(cua.diemDanh.gioRa)}`}</dd></>}
        </dl>
      )}
      {dk.duLieu && !cua && <div className="thong-bao canh-bao">Bạn không có chỗ trong ca này nên không điểm danh được. Hãy kiểm tra lại ở Đăng ký của tôi.</div>}
      {ketQua ? (
        <div className={'thong-bao thanh-cong ket-qua-lon'} role="status">
          {ketQua.loai === 'VAO' ? 'Đã điểm danh vào' : 'Đã điểm danh ra'}
          <strong>{hienGioPhut(ketQua.gio)}</strong>
          {ketQua.tieuDe}
        </div>
      ) : (
        <form onSubmit={(e) => { e.preventDefault(); if (maNhap.trim()) gui(maNhap) }}>
          <p>Quét mã QR trên màn hình của điều phối viên bằng máy ảnh điện thoại. Nếu không quét được, gõ mã chữ hiện dưới mã QR.</p>
          <label>Mã điểm danh
            <input className="ma-chu" value={maNhap} maxLength={14} autoCapitalize="characters" autoComplete="off"
              onChange={(e) => setMaNhap(e.target.value.toUpperCase())} />
          </label>
          <button type="submit" className="lon" disabled={tt.dangGui || !maNhap.trim()}>{daVao ? 'Điểm danh ra' : 'Điểm danh vào'}</button>
        </form>
      )}
      <ThongBao loi={tt.loi} />
      {ketQua?.loai === 'VAO' && <p>Khi làm xong, quét lại mã của điều phối viên để điểm danh ra. Giờ vào và ra được dùng để tính giờ đóng góp.</p>}
      {ketQua?.loai === 'RA' && <p>Sau khi ca kết thúc, vào Đăng ký của tôi để khai đóng góp. Giờ làm được điền theo điểm danh.</p>}
      <p><Link to="/dang-ky-cua-toi">Về Đăng ký của tôi</Link></p>
    </section>
  )
}
