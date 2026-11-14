import { useState } from 'react'
import { api } from '../api'
import { DangTai, ThongBao, useTai, useThaoTac } from '../chung'
import { hienKhoangGio, soGio } from '../tienIch'

// M11 Duyệt đóng góp: xác nhận hoặc trả lại kèm lý do; không tự duyệt bản của mình (BR14).
export default function DuyetDongGop() {
  const ds = useTai(() => api.get('/api/dong-gop/cho-duyet'), [])
  const [lyDo, setLyDo] = useState({})
  const [moTraLai, setMoTraLai] = useState(null)
  const tt = useThaoTac()

  const duyet = async (g, xacNhan) => {
    const ok = await tt.chay(() => api.post(`/api/dong-gop/${g.maDongGop}/duyet`, { xacNhan, lyDoTraLai: lyDo[g.maDongGop] ?? '' }),
      xacNhan ? `Đã xác nhận bản khai của ${g.tenTinhNguyenVien}.` : `Đã trả lại bản khai của ${g.tenTinhNguyenVien}.`)
    if (ok) { setMoTraLai(null); ds.taiLai() }
  }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      <h1>Duyệt đóng góp</h1>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {ds.duLieu.length === 0 ? <p className="mo">Không có bản khai nào đang chờ duyệt.</p> : (
        <table>
          <thead><tr><th>Người khai</th><th>Ca</th><th>Thời gian thực tế</th><th>Số giờ</th><th>Mô tả công việc</th><th></th></tr></thead>
          <tbody>
            {ds.duLieu.map((g) => (
              <tr key={g.maDongGop}>
                <td>{g.tenTinhNguyenVien}</td>
                <td>{g.tieuDe}<br /><small className="mo">{hienKhoangGio(g.batDauDuKien, g.ketThucDuKien)}</small></td>
                <td>{hienKhoangGio(g.batDauThucTe, g.ketThucThucTe)}</td>
                <td>{soGio(g.soGio)}</td>
                <td>{g.moTaCongViec}</td>
                <td className="hang-nut">
                  {g.laCuaToi ? <small className="mo">Bản khai của bạn, người khác sẽ duyệt</small> : moTraLai === g.maDongGop ? (
                    <span className="tra-lai">
                      <input placeholder="Lý do trả lại" value={lyDo[g.maDongGop] ?? ''} onChange={(e) => setLyDo({ ...lyDo, [g.maDongGop]: e.target.value })} />
                      <button className="nguy-hiem" disabled={tt.dangGui} onClick={() => duyet(g, false)}>Gửi trả lại</button>
                      <button className="phu" onClick={() => setMoTraLai(null)}>Hủy</button>
                    </span>
                  ) : (
                    <>
                      <button disabled={tt.dangGui} onClick={() => duyet(g, true)}>Xác nhận</button>
                      <button className="phu" onClick={() => setMoTraLai(g.maDongGop)}>Trả lại</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
