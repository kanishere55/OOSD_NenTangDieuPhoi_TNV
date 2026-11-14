import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useTai } from '../chung'
import { hienKhoangGio, soGio } from '../tienIch'

// M13 Lịch sử và tổng giờ đóng góp: tổng chỉ cộng các bản đã xác nhận (BR14).
export default function LichSuDongGop() {
  const kq = useTai(() => api.get('/api/dong-gop/cua-toi'), [])
  if (kq.dangTai) return <DangTai />
  if (kq.loi) return <ThongBao loi={kq.loi} />
  const d = kq.duLieu
  return (
    <>
      <h1>Lịch sử và tổng giờ đóng góp</h1>
      <div className="o-tong">
        <span>Tổng giờ đã được xác nhận</span>
        <strong>{soGio(d.tongGioDaXacNhan)} giờ</strong>
      </div>
      {d.danhSach.length === 0 ? <p className="mo">Bạn chưa có bản khai đóng góp nào.</p> : (
        <table>
          <thead><tr><th>Ca</th><th>Thời gian thực tế</th><th>Số giờ</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>
            {d.danhSach.map((g) => (
              <tr key={g.maDongGop}>
                <td>{g.tieuDe}</td>
                <td>{hienKhoangGio(g.batDauThucTe, g.ketThucThucTe)}</td>
                <td>{soGio(g.soGio)}</td>
                <td><TrangThai ma={g.trangThai} />{g.lyDoTraLai && g.trangThai === 'CAN_BO_SUNG' && <><br /><small>Lý do: {g.lyDoTraLai}</small></>}</td>
                <td>{g.trangThai === 'CAN_BO_SUNG' && <Link to={`/dang-ky/${g.maDangKy}/khai`}>Bổ sung</Link>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <p className="mo">Bản khai chờ duyệt hoặc cần bổ sung chưa được cộng vào tổng giờ.</p>
    </>
  )
}
