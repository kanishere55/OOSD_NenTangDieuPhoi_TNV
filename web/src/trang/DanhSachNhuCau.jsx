import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useNguoiDung, useTai } from '../chung'

// M06 Danh sách nhu cầu của tổ chức.
export default function DanhSachNhuCau() {
  const { nguoiDung } = useNguoiDung()
  const ds = useTai(() => api.get('/api/nhu-cau'), [])
  const laDaiDien = nguoiDung?.vaiTro.includes('DAI_DIEN')

  if (ds.dangTai) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      <div className="tieu-de-hang">
        <h1>Nhu cầu của tổ chức</h1>
        {laDaiDien && <Link className="nut" to="/nhu-cau/moi">Tạo nhu cầu</Link>}
      </div>
      {ds.duLieu.length === 0 ? <p className="mo">Tổ chức chưa có nhu cầu nào.</p> : (
        <table>
          <thead><tr><th>Tiêu đề</th><th>Tổ chức</th><th>Khu vực</th><th>Trạng thái</th><th>Rà soát</th><th>Số ca</th><th></th></tr></thead>
          <tbody>
            {ds.duLieu.map((n) => (
              <tr key={n.maNhuCau}>
                <td><Link to={'/nhu-cau/' + n.maNhuCau}>{n.tieuDe}</Link></td>
                <td>{n.tenToChuc}</td>
                <td>{n.khuVucChung}</td>
                <td><TrangThai ma={n.trangThai} /></td>
                <td>{n.daRaSoatCongKhai ? `Đã rà soát (phiên bản ${n.phienBanCongKhai})` : `Chưa rà soát (phiên bản ${n.phienBanCongKhai})`}</td>
                <td>{n.soCa}</td>
                <td>{n.trangThai === 'CONG_BO' && <Link to={`/nhu-cau/${n.maNhuCau}/ca`}>Ca làm</Link>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
