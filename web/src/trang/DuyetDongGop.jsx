import { useState } from 'react'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao, Trong, useTai, useThaoTac } from '../chung'
import { hienKhoangGio, soGio } from '../tienIch'

// M11 Duyệt đóng góp: xác nhận hoặc trả lại kèm lý do; không tự duyệt bản của mình (BR14).
export default function DuyetDongGop() {
  const ds = useTai(() => api.get('/api/dong-gop/cho-duyet'), [])
  const [traLai, setTraLai] = useState(null)   // bản khai đang được trả lại
  const [lyDo, setLyDo] = useState('')
  const [loiLyDo, setLoiLyDo] = useState(null)
  const tt = useThaoTac()

  const xacNhan = async (g) => {
    const ok = await tt.chay(() => api.post(`/api/dong-gop/${g.maDongGop}/duyet`, { xacNhan: true, lyDoTraLai: '' }),
      `Đã xác nhận ${soGio(g.soGio)} giờ đóng góp của ${g.tenTinhNguyenVien}.`)
    if (ok) ds.taiLai()
  }
  const guiTraLai = async () => {
    if (!lyDo.trim()) { setLoiLyDo('Hãy ghi lý do để tình nguyện viên biết cần sửa gì.'); return }
    const g = traLai
    const ok = await tt.chay(() => api.post(`/api/dong-gop/${g.maDongGop}/duyet`, { xacNhan: false, lyDoTraLai: lyDo }),
      `Đã trả lại bản khai của ${g.tenTinhNguyenVien} để bổ sung.`)
    setTraLai(null)
    if (ok) ds.taiLai()
  }
  const moTraLai = (g) => { setTraLai(g); setLyDo(''); setLoiLyDo(null) }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      <DauTrang tieuDe="Duyệt đóng góp"
        moTa="Sau mỗi ca, tình nguyện viên khai giờ đã làm. Bạn đối chiếu với thực tế rồi xác nhận, hoặc trả lại kèm lý do để họ sửa." />
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {ds.duLieu.length === 0 ? <Trong><p>Không có bản khai nào đang chờ duyệt.</p></Trong> : (
        <ul className="ds-dong">
          {ds.duLieu.map((g) => (
            <li key={g.maDongGop}>
              <div className="than">
                <p><strong>{g.tenTinhNguyenVien}</strong> khai <strong>{soGio(g.soGio)} giờ</strong></p>
                <p className="mo">{g.tieuDe}</p>
                <dl className="thong-tin">
                  <dt>Giờ của ca</dt><dd>{hienKhoangGio(g.batDauDuKien, g.ketThucDuKien)}</dd>
                  <dt>Giờ làm thực tế</dt><dd>{hienKhoangGio(g.batDauThucTe, g.ketThucThucTe)}</dd>
                  <dt>Công việc đã làm</dt><dd>{g.moTaCongViec}</dd>
                </dl>
              </div>
              <div className="hang-nut">
                {g.laCuaToi ? <span className="mo">Đây là bản khai của chính bạn.<br />Điều phối viên khác sẽ duyệt.</span> : (
                  <>
                    <button disabled={tt.dangGui} onClick={() => xacNhan(g)}>Xác nhận</button>
                    <button className="phu" disabled={tt.dangGui} onClick={() => moTraLai(g)}>Trả lại để bổ sung</button>
                  </>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {traLai && (
        <div className="nen-hop">
          <div className="hop" role="dialog" aria-modal="true" aria-label="Trả lại bản khai">
            <h2>Trả lại bản khai</h2>
            <p>Bản khai của <strong>{traLai.tenTinhNguyenVien}</strong> sẽ chuyển sang Cần bổ sung. Người khai sửa rồi gửi lại cho bạn duyệt.</p>
            <label>Lý do trả lại
              <small>Ví dụ: Giờ kết thúc chưa đúng, buổi hôm đó kết thúc lúc 09:30.</small>
              <textarea rows={3} value={lyDo} autoFocus onChange={(e) => { setLyDo(e.target.value); setLoiLyDo(null) }} />
              {loiLyDo && <small className="loi-o">{loiLyDo}</small>}
            </label>
            <div className="hang-nut cuoi-hop">
              <button className="phu" disabled={tt.dangGui} onClick={() => setTraLai(null)}>Thôi</button>
              <button disabled={tt.dangGui} onClick={guiTraLai}>Gửi trả lại</button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
