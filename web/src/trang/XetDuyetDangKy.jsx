import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useTai, useThaoTac } from '../chung'
import { hienGio, hienKhoangGio } from '../tienIch'

// M09 Xét duyệt đăng ký của một ca: phân công hoặc từ chối. Điều phối viên là người quyết định (BR04).
export default function XetDuyetDangKy() {
  const { ma } = useParams()
  const ds = useTai(() => api.get(`/api/ca/${ma}/dang-ky`), [ma])
  const tt = useThaoTac()

  const lam = async (maDangKy, viec, thongBao) => {
    if (await tt.chay(() => api.post(`/api/dang-ky/${maDangKy}/${viec}`), thongBao)) ds.taiLai()
  }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  const c = ds.duLieu
  const conLai = c.soNguoiCan - c.soDaPhanCong
  return (
    <>
      <h1>Xét duyệt đăng ký</h1>
      <section className="the">
        <h2>{c.tieuDe}</h2>
        <p>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)} · <TrangThai ma={c.trangThai} /></p>
        <p>Kỹ năng yêu cầu: {c.kyNangYeuCau.join(', ')}</p>
        <p className="noi-bat">Đã phân công {c.soDaPhanCong}/{c.soNguoiCan} · còn {Math.max(0, conLai)} chỗ</p>
      </section>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {c.dangKy.length === 0 ? <p className="mo">Chưa có ai đăng ký ca này.</p> : (
        <table>
          <thead><tr><th>Tình nguyện viên</th><th>Kỹ năng đã khai</th><th>Gợi ý</th><th>Ngày đăng ký</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>
            {c.dangKy.map((d) => (
              <tr key={d.maDangKy}>
                <td>{d.tenHienThi}</td>
                <td>{d.kyNangDaKhai.join(', ')}</td>
                <td>
                  {d.trangThai === 'CHO_DUYET' && (
                    <>
                      <span className={d.duKyNang ? 'goi-y tot' : 'goi-y xau'}>{d.duKyNang ? 'Đủ kỹ năng' : 'Thiếu kỹ năng'}</span>
                      {d.trungLich && <span className="goi-y xau">Trùng lịch</span>}
                    </>
                  )}
                </td>
                <td>{hienGio(d.ngayDangKy)}</td>
                <td><TrangThai ma={d.trangThai} /></td>
                <td className="hang-nut">
                  {d.trangThai === 'CHO_DUYET' && (
                    <>
                      <button disabled={tt.dangGui} onClick={() => lam(d.maDangKy, 'phan-cong', `Đã phân công ${d.tenHienThi}.`)}>Phân công</button>
                      <button className="phu" disabled={tt.dangGui} onClick={() => lam(d.maDangKy, 'tu-choi', `Đã từ chối đăng ký của ${d.tenHienThi}.`)}>Từ chối</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <p><Link to={`/ca/${ma}/huong-dan-rieng`}>Xem hướng dẫn riêng của ca</Link></p>
    </>
  )
}
