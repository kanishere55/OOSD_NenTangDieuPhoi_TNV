import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useTai, useThaoTac } from '../chung'
import { hienKhoangGio } from '../tienIch'

// M05 Đăng ký của tôi: rút đăng ký trước giờ bắt đầu, khai đóng góp sau khi ca kết thúc.
export default function DangKyCuaToi() {
  const ds = useTai(() => api.get('/api/dang-ky/cua-toi'), [])
  const [dangHoiRut, setDangHoiRut] = useState(null)
  const tt = useThaoTac()

  const rut = async (ma) => {
    setDangHoiRut(null)
    if (await tt.chay(() => api.post(`/api/dang-ky/${ma}/rut`), 'Đã rút đăng ký.')) ds.taiLai()
  }

  if (ds.dangTai) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      <h1>Đăng ký của tôi</h1>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {ds.duLieu.length === 0 ? <p className="mo">Bạn chưa đăng ký ca nào. <Link to="/">Tìm ca làm</Link></p> : (
        <table>
          <thead><tr><th>Ca</th><th>Thời gian</th><th>Trạng thái đăng ký</th><th>Đóng góp</th><th></th></tr></thead>
          <tbody>
            {ds.duLieu.map((d) => (
              <tr key={d.maDangKy}>
                <td>{d.tieuDe}<br /><small className="mo">{d.khuVucChung}</small></td>
                <td>{hienKhoangGio(d.batDauDuKien, d.ketThucDuKien)}</td>
                <td><TrangThai ma={d.trangThai} /></td>
                <td>{d.dongGop ? <TrangThai ma={d.dongGop.trangThai} /> : 'Chưa khai'}</td>
                <td className="hang-nut">
                  {d.duocRut && (dangHoiRut === d.maDangKy ? (
                    <span className="xac-nhan">Rút đăng ký này?
                      <button className="nguy-hiem" onClick={() => rut(d.maDangKy)}>Rút</button>
                      <button className="phu" onClick={() => setDangHoiRut(null)}>Không</button>
                    </span>
                  ) : <button className="phu" onClick={() => setDangHoiRut(d.maDangKy)}>Rút đăng ký</button>)}
                  {d.duocKhai && <Link className="nut" to={`/dang-ky/${d.maDangKy}/khai`}>Khai đóng góp</Link>}
                  {d.duocXemRieng && (
                    <Link className="nut phu" to={`/ca/${d.maCaLam}/huong-dan-rieng`}>Hướng dẫn riêng</Link>
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
