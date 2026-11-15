import { api } from '../api'
import { DangTai, ThongBao, Trong, useTai } from '../chung'

// Kỹ năng đang thiếu ở các ca sắp tới của tổ chức, lấy từ thủ tục sp_ThongKeKyNangThieu.
export default function KyNangDangThieu({ maToChuc, tenToChuc }) {
  const kq = useTai(() => api.get(`/api/to-chuc/${maToChuc}/ky-nang-thieu`), [maToChuc])
  if (kq.dangTai && !kq.duLieu) return <DangTai />
  if (kq.loi) return <ThongBao loi={kq.loi} />
  return (
    <>
      <p className="mo-ta">Các ca sắp tới{tenToChuc ? ` của ${tenToChuc}` : ''} còn thiếu người, theo từng kỹ năng ca yêu cầu. Cột cuối là số tình nguyện viên trên nền tảng đã khai kỹ năng đó, để biết nên tìm thêm người ở đâu.</p>
      {kq.duLieu.length === 0 ? <Trong><p>Các ca sắp tới đều đã đủ người.</p></Trong> : (
        <div className="bang-cuon">
          <table>
            <thead><tr><th>Kỹ năng</th><th>Số ca cần</th><th>Còn thiếu</th><th>Đang chờ duyệt</th><th>Tình nguyện viên có kỹ năng</th></tr></thead>
            <tbody>
              {kq.duLieu.map((k) => (
                <tr key={k.maKyNang}>
                  <td><strong>{k.tenKyNang}</strong></td>
                  <td>{k.soCa}</td>
                  <td><strong className="thieu-ky-nang">{k.soChoThieu} người</strong></td>
                  <td>{k.soDangKyChoDuyet}</td>
                  <td>{k.soTinhNguyenVienCoKyNang === 0 ? <span className="thieu-ky-nang">Chưa có ai</span> : k.soTinhNguyenVienCoKyNang}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
