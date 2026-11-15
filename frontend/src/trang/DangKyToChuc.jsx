import { useState } from 'react'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao, Trong, useNguoiDung, useTai, useThaoTac } from '../chung'
import { TEN_TRANG_THAI_TO_CHUC, hienNgay } from '../tienIch'

const TRONG = { tenToChuc: '', khuVucHoatDong: '', soDienThoai: '', moTa: '' }

// M19 Đăng ký tổ chức: người dùng đã đăng nhập gửi thông tin nhóm thiện nguyện, quản trị viên duyệt.
// Được duyệt thì người gửi trở thành đại diện của tổ chức (BR28).
export default function DangKyToChuc() {
  const { nguoiDung } = useNguoiDung()
  const ds = useTai(() => api.get('/api/to-chuc/dang-ky-cua-toi'), [])
  const [form, setForm] = useState(TRONG)
  const tt = useThaoTac()
  const doi = (t) => (e) => setForm({ ...form, [t]: e.target.value })
  const gui = async (e) => {
    e.preventDefault()
    if (!form.tenToChuc.trim() || !form.khuVucHoatDong.trim() || !form.soDienThoai.trim()) {
      tt.setLoi('Hãy điền tên tổ chức, khu vực hoạt động và số điện thoại.'); return
    }
    const ok = await tt.chay(() => api.post('/api/to-chuc/dang-ky', form),
      'Đã gửi đăng ký. Quản trị viên sẽ gọi điện xác minh rồi duyệt. Bạn sẽ nhận được thông báo khi có kết quả.')
    if (ok) { setForm(TRONG); ds.taiLai() }
  }
  if (ds.dangTai && !ds.duLieu) return <DangTai />
  const dangCho = ds.duLieu?.some((t) => t.trangThai === 'CHO_DUYET')
  return (
    <>
      <DauTrang tieuDe="Đăng ký tổ chức" moTa="Dành cho nhóm thiện nguyện, câu lạc bộ, tổ dân phố muốn đăng nhu cầu và tìm tình nguyện viên." />
      {ds.duLieu?.length > 0 && (
        <>
          <h2>Đăng ký của bạn</h2>
          <ul className="ds-dong">
            {ds.duLieu.map((t) => (
              <li key={t.maToChuc}>
                <div className="than">
                  <p><strong>{t.tenToChuc}</strong> <span className={'nhan nhan-' + t.trangThai.toLowerCase()}>{TEN_TRANG_THAI_TO_CHUC[t.trangThai]}</span></p>
                  <p className="mo">Gửi ngày {hienNgay(t.ngayDangKy)} · {t.khuVucHoatDong}</p>
                  {t.trangThai === 'TU_CHOI' && <p>Lý do: {t.lyDoTuChoi}</p>}
                  {t.trangThai === 'HOAT_DONG' && <p className="hop-ky-nang">Bạn là đại diện của tổ chức. Đăng xuất rồi đăng nhập lại nếu chưa thấy mục Nhu cầu và Tổ chức.</p>}
                </div>
              </li>
            ))}
          </ul>
        </>
      )}
      {dangCho ? <Trong><p>Bạn đang có một đăng ký chờ duyệt. Hãy chờ kết quả trước khi gửi đăng ký khác.</p></Trong> : (
        <form className="the hep" onSubmit={gui}>
          <h2 style={{ marginTop: 0 }}>Thông tin tổ chức</h2>
          <p className="mo">Người gửi ({nguoiDung?.tenHienThi}) sẽ là đại diện của tổ chức. Sau khi được duyệt, bạn thêm điều phối viên ở trang Tổ chức.</p>
          <label>Tên tổ chức
            <small>Ví dụ: Nhóm Thiện nguyện Khu phố 5.</small>
            <input value={form.tenToChuc} maxLength={150} onChange={doi('tenToChuc')} />
          </label>
          <label>Khu vực hoạt động
            <small>Ví dụ: Phường An Bình và các phường lân cận.</small>
            <input value={form.khuVucHoatDong} maxLength={150} onChange={doi('khuVucHoatDong')} />
          </label>
          <label>Số điện thoại liên hệ
            <small>Quản trị viên gọi số này để xác minh, không hiện công khai.</small>
            <input value={form.soDienThoai} inputMode="tel" maxLength={20} onChange={doi('soDienThoai')} />
          </label>
          <label>Giới thiệu ngắn (không bắt buộc)
            <textarea rows={3} value={form.moTa} maxLength={500} onChange={doi('moTa')} />
          </label>
          <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
          <button type="submit" className="lon" disabled={tt.dangGui}>Gửi đăng ký</button>
        </form>
      )}
      {dangCho && <ThongBao thanhCong={tt.thanhCong} />}
    </>
  )
}
