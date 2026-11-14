import { Link, useParams } from 'react-router-dom'
import { api, dangNhap } from '../api'
import { DangTai, ThongBao, useNguoiDung, useTai, useThaoTac } from '../chung'
import { hienKhoangGio } from '../tienIch'

// M03 Chi tiết ca làm: thông tin công khai, nút Đăng ký và nút Xem hướng dẫn riêng.
export default function ChiTietCa() {
  const { ma } = useParams()
  const { nguoiDung } = useNguoiDung()
  const ca = useTai(() => api.get('/api/ca/' + ma), [ma])
  const tt = useThaoTac()

  const dangKy = () => {
    if (!nguoiDung) return dangNhap('/ca/' + ma)
    tt.chay(() => api.post(`/api/ca/${ma}/dang-ky`),
      'Đã gửi đăng ký. Đăng ký đang ở trạng thái Chờ duyệt, điều phối viên sẽ xem xét và phân công.')
  }

  if (ca.dangTai) return <DangTai />
  if (ca.loi) return <ThongBao loi={ca.loi} />
  const c = ca.duLieu
  return (
    <section className="the">
      <p><Link to="/">← Danh sách ca</Link></p>
      <h1>{c.tieuDe}</h1>
      <p>{c.tomTatCongKhai}</p>
      <dl className="thong-tin">
        <dt>Khu vực</dt><dd>{c.khuVucChung}</dd>
        <dt>Thời gian</dt><dd>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</dd>
        <dt>Số người cần</dt><dd>{c.soNguoiCan}</dd>
        <dt>Kỹ năng yêu cầu</dt><dd>{c.kyNangYeuCau.join(', ')}</dd>
      </dl>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {nguoiDung?.laTinhNguyenVien && (tt.loi || tt.thanhCong) && (
        <p><Link to="/dang-ky-cua-toi">Xem Đăng ký của tôi</Link></p>
      )}
      <div className="hang-nut">
        {(!nguoiDung || nguoiDung.laTinhNguyenVien) && (
          <button onClick={dangKy} disabled={tt.dangGui || !!tt.thanhCong}>Đăng ký</button>
        )}
        {nguoiDung && <Link className="nut phu" to={`/ca/${ma}/huong-dan-rieng`}>Xem hướng dẫn riêng</Link>}
      </div>
      {!nguoiDung && <p className="mo">Khách cần đăng nhập bằng tài khoản tình nguyện viên để đăng ký.</p>}
    </section>
  )
}
