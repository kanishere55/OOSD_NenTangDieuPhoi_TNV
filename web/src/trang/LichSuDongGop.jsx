import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao, TrangThai, Trong, useTai } from '../chung'
import { GIAI_THICH_DONG_GOP, hienKhoangGio, soGio } from '../tienIch'

// M13 Lịch sử và tổng giờ đóng góp: tổng chỉ cộng các bản đã xác nhận (BR14).
export default function LichSuDongGop() {
  const kq = useTai(() => api.get('/api/dong-gop/cua-toi'), [])
  if (kq.dangTai) return <DangTai />
  if (kq.loi) return <ThongBao loi={kq.loi} />
  const d = kq.duLieu
  return (
    <>
      <DauTrang tieuDe="Lịch sử và tổng giờ đóng góp" moTa="Các bản khai đóng góp của bạn. Chỉ bản đã được điều phối viên xác nhận mới tính vào tổng giờ." />
      <div className="o-tong">
        <span>Tổng giờ đã được xác nhận</span>
        <strong>{soGio(d.tongGioDaXacNhan)} giờ</strong>
        <span>từ {d.soBanDaXacNhan} bản khai</span>
      </div>
      {d.danhSach.length === 0 ? (
        <Trong>
          <p>Bạn chưa có bản khai nào. Sau khi làm xong một ca được phân công, bạn khai đóng góp ở mục Đăng ký của tôi.</p>
          <div className="hang-nut"><Link className="nut phu" to="/dang-ky-cua-toi">Mở Đăng ký của tôi</Link></div>
        </Trong>
      ) : (
        <ul className="ds-dong">
          {d.danhSach.map((g) => (
            <li key={g.maDongGop} className={g.trangThai === 'CAN_BO_SUNG' ? 'noi-bat' : ''}>
              <div className="than">
                <p><strong>{g.tieuDe}</strong></p>
                <p>{hienKhoangGio(g.batDauThucTe, g.ketThucThucTe)} · <strong>{soGio(g.soGio)} giờ</strong></p>
                <p><TrangThai ma={g.trangThai} /> <span className="mo">{GIAI_THICH_DONG_GOP[g.trangThai]}</span></p>
                {g.trangThai === 'CAN_BO_SUNG' && <p>Lý do trả lại: {g.lyDoTraLai}</p>}
                <p className="mo">Công việc: {g.moTaCongViec}</p>
              </div>
              {g.trangThai === 'CAN_BO_SUNG' && <div className="hang-nut"><Link className="nut" to={`/dang-ky/${g.maDangKy}/khai`}>Bổ sung bản khai</Link></div>}
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
