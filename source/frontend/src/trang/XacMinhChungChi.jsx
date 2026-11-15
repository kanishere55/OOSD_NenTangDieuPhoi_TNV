import { useState } from 'react'
import { api } from '../api'
import { DangTai, DauTrang, HopLyDo, ThongBao, Trong, useTai, useThaoTac } from '../chung'
import { CAP_DO, hienGio } from '../tienIch'

// M21 Xác minh chứng chỉ: điều phối viên xem chứng chỉ của người đang đăng ký ca của tổ chức mình,
// quản trị viên xem tất cả. Không tự xác minh chứng chỉ của mình (BR19). Mỗi lần mở ảnh đều được ghi nhật ký.
export default function XacMinhChungChi({ nhung = false }) {
  const ds = useTai(() => api.get('/api/chung-chi/cho-xac-minh'), [])
  const [dangXem, setDangXem] = useState(null)
  const [tuChoi, setTuChoi] = useState(false)
  const tt = useThaoTac()
  const xacMinh = async (hopLe, lyDo) => {
    const c = dangXem
    const ok = await tt.chay(() => api.post(`/api/chung-chi/${c.maChungChi}/xac-minh`, { hopLe, lyDo }),
      hopLe ? `Đã xác minh chứng chỉ ${c.tenKyNang} của ${c.tenTinhNguyenVien}.` : `Đã báo chứng chỉ của ${c.tenTinhNguyenVien} chưa hợp lệ.`)
    setDangXem(null); setTuChoi(false)
    if (ok) ds.taiLai()
  }
  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      {!nhung && <DauTrang tieuDe="Xác minh chứng chỉ" moTa="Chứng chỉ của những người đang đăng ký ca của tổ chức bạn. Xác minh xong, người đó đăng ký được các ca yêu cầu chứng chỉ ở mọi tổ chức." />}
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {ds.duLieu.length === 0 ? <Trong><p>Không có chứng chỉ nào đang chờ xác minh.</p></Trong> : (
        <ul className="ds-dong">
          {ds.duLieu.map((c) => (
            <li key={c.maChungChi}>
              <div className="than">
                <p><strong>{c.tenTinhNguyenVien}</strong> · {c.tenKyNang}{c.capDo && <span className="mo"> (tự khai mức {CAP_DO[c.capDo].toLowerCase()})</span>}</p>
                <p className="mo">Gửi lúc {hienGio(c.ngayGui)}</p>
              </div>
              <div className="hang-nut"><button onClick={() => setDangXem(c)}>Xem và xác minh</button></div>
            </li>
          ))}
        </ul>
      )}
      {dangXem && !tuChoi && (
        <div className="nen-hop" onClick={(e) => { if (e.target === e.currentTarget) setDangXem(null) }}>
          <div className="hop hop-anh" role="dialog" aria-modal="true" aria-label="Chứng chỉ">
            <h2>Chứng chỉ {dangXem.tenKyNang} của {dangXem.tenTinhNguyenVien}</h2>
            <img src={`/api/chung-chi/${dangXem.maChungChi}/anh`} alt={'Chứng chỉ ' + dangXem.tenKyNang} />
            <p className="mo">Kiểm tra họ tên, nội dung chứng chỉ và ngày cấp. Không chụp lại hay chia sẻ ảnh này.</p>
            <div className="hang-nut cuoi-hop">
              <button className="phu" onClick={() => setDangXem(null)}>Đóng</button>
              <button className="phu nguy-hiem" disabled={tt.dangGui} onClick={() => setTuChoi(true)}>Chưa hợp lệ</button>
              <button disabled={tt.dangGui} onClick={() => xacMinh(true)}>Xác nhận hợp lệ</button>
            </div>
          </div>
        </div>
      )}
      {dangXem && tuChoi && (
        <HopLyDo tieuDe="Chứng chỉ chưa hợp lệ" nhanDongY="Gửi" dangGui={tt.dangGui}
          moTa={`${dangXem.tenTinhNguyenVien} sẽ nhận thông báo kèm lý do để gửi lại.`}
          goiY="Ví dụ: Ảnh bị mờ, không đọc được ngày cấp." onDongY={(l) => xacMinh(false, l)} onThoi={() => setTuChoi(false)} />
      )}
    </>
  )
}
