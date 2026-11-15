import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, LuoiAnh, ONgay, ThongBao, TrangThai, Trong, useTai, useThaoTac } from '../chung'
import { GIAI_THICH_DONG_GOP, docNgay, hienKhoangGio, hienNgay, hienNgayThuong, ngayGuiDi, soGio } from '../tienIch'

// M13 Lịch sử và tổng giờ đóng góp: tổng chỉ cộng các bản khai đã xác nhận (BR14);
// tạo giấy xác nhận giờ tình nguyện có mã kiểm tra (BR29).
export default function LichSuDongGop() {
  const kq = useTai(() => api.get('/api/dong-gop/cua-toi'), [])
  if (kq.dangTai) return <DangTai />
  if (kq.loi) return <ThongBao loi={kq.loi} />
  const d = kq.duLieu
  return (
    <>
      <DauTrang tieuDe="Lịch sử và tổng giờ đóng góp" moTa="Chỉ bản khai đã được xác nhận mới tính vào tổng giờ và vào giấy xác nhận." />
      <div className="o-tong">
        <span>Tổng giờ đã được xác nhận</span>
        <strong>{soGio(d.tongGioDaXacNhan)} giờ</strong>
        <span>từ {d.soBanDaXacNhan} bản khai</span>
      </div>
      <GiayXacNhan coGio={d.soBanDaXacNhan > 0} />
      <h2>Các bản khai</h2>
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
                <p><strong>{g.tieuDe}</strong> <span className="mo">· {g.tenToChuc}</span></p>
                <p>{hienKhoangGio(g.batDauThucTe, g.ketThucThucTe)} · <strong>{soGio(g.soGio)} giờ</strong></p>
                <p><TrangThai ma={g.trangThai} /> <span className="mo">{GIAI_THICH_DONG_GOP[g.trangThai]}</span></p>
                {g.trangThai === 'CAN_BO_SUNG' && <p>Lý do trả lại: {g.lyDoTraLai}</p>}
                <p className="mo">Công việc: {g.moTaCongViec}</p>
                <LuoiAnh ds={g.anh.map((m) => ({ khoa: m, src: '/api/anh/minh-chung/' + m }))} />
              </div>
              {g.trangThai === 'CAN_BO_SUNG' && <div className="hang-nut"><Link className="nut" to={`/dang-ky/${g.maDangKy}/khai`}>Bổ sung bản khai</Link></div>}
            </li>
          ))}
        </ul>
      )}
    </>
  )
}

function GiayXacNhan({ coGio }) {
  const ds = useTai(() => api.get('/api/giay-xac-nhan/cua-toi'), [])
  const [khoang, setKhoang] = useState({ tu: '', den: '' })
  const tt = useThaoTac()
  const tao = async () => {
    if ((khoang.tu && !docNgay(khoang.tu)) || (khoang.den && !docNgay(khoang.den))) { tt.setLoi('Ngày chưa đúng. Hãy gõ dạng 20/11/2026.'); return }
    const ok = await tt.chay(() => api.post('/api/giay-xac-nhan', { tuNgay: ngayGuiDi(khoang.tu), denNgay: ngayGuiDi(khoang.den) }),
      'Đã tạo giấy xác nhận. Bấm Tải PDF để lưu hoặc in.')
    if (ok) ds.taiLai()
  }
  return (
    <section className="the">
      <h2 style={{ marginTop: 0 }}>Giấy xác nhận giờ tình nguyện</h2>
      <p className="mo-ta">Dùng để nộp cho nhà trường hoặc nơi làm việc, ví dụ khi tính ngày công tác xã hội. Giấy có mã kiểm tra:
        người nhận giấy quét mã QR hoặc mở trang <Link to="/kiem-tra">Kiểm tra giấy xác nhận</Link> để biết giấy là thật.</p>
      {!coGio ? <p className="mo">Bạn chưa có bản khai nào được xác nhận nên chưa tạo được giấy.</p> : (
        <>
          <div className="bo-loc" style={{ maxWidth: 640 }}>
            <ONgay nhan="Từ ngày" giaTri={khoang.tu} onDoi={(v) => setKhoang({ ...khoang, tu: v })} />
            <ONgay nhan="Đến ngày" giaTri={khoang.den} onDoi={(v) => setKhoang({ ...khoang, den: v })} />
          </div>
          <p className="mo">Để trống cả hai ô thì giấy gồm toàn bộ thời gian bạn tham gia.</p>
          <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
          <button onClick={tao} disabled={tt.dangGui}>Tạo giấy xác nhận</button>
        </>
      )}
      {ds.duLieu?.length > 0 && (
        <div className="bang-cuon" style={{ marginTop: 16 }}>
          <table>
            <thead><tr><th>Ngày cấp</th><th>Khoảng thời gian</th><th>Số ca</th><th>Tổng giờ</th><th>Mã kiểm tra</th><th></th></tr></thead>
            <tbody>
              {ds.duLieu.map((g) => (
                <tr key={g.maGiay}>
                  <td>{hienNgay(g.ngayCap)}</td>
                  <td>{!g.tuNgay && !g.denNgay ? 'Toàn bộ' : `${g.tuNgay ? hienNgayThuong(g.tuNgay) : '...'} đến ${g.denNgay ? hienNgayThuong(g.denNgay) : 'nay'}`}</td>
                  <td>{g.soCa}</td>
                  <td>{soGio(g.tongGio)} giờ</td>
                  <td><span className="ma-chu" style={{ fontSize: '1rem' }}>{g.maKiemTra}</span></td>
                  <td className="thao-tac"><a className="nut nho" href={`/api/giay-xac-nhan/${g.maGiay}/pdf`}>Tải PDF</a></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
