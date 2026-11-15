import { useState } from 'react'
import { api } from '../api'
import { ThongBao, TrangThai, useTai, useThaoTac } from '../chung'
import { hienNgay } from '../tienIch'

// Đề xuất thêm kỹ năng chưa có trong danh mục (BR17), dùng ở Hồ sơ và khi tạo ca.
export default function DeXuatKyNang({ tieuDe = 'Không thấy kỹ năng của bạn trong danh sách?' }) {
  const [mo, setMo] = useState(false)
  const [form, setForm] = useState({ tenKyNang: '', moTa: '', lyDo: '' })
  const ds = useTai(() => api.get('/api/ky-nang/de-xuat/cua-toi'), [])
  const tt = useThaoTac()
  const gui = async () => {
    if (!form.tenKyNang.trim()) { tt.setLoi('Hãy nhập tên kỹ năng.'); return }
    const ok = await tt.chay(() => api.post('/api/ky-nang/de-xuat', form),
      'Đã gửi đề xuất. Khi quản trị viên duyệt, bạn sẽ nhận được thông báo và chọn được kỹ năng này.')
    if (ok) { setForm({ tenKyNang: '', moTa: '', lyDo: '' }); setMo(false); ds.taiLai() }
  }
  return (
    <div style={{ marginTop: 22 }}>
      <h3>{tieuDe}</h3>
      {!mo && <button className="phu" onClick={() => { setMo(true); tt.setThanhCong(null) }}>Đề xuất thêm kỹ năng</button>}
      {mo && (
        <div className="xem-truoc" onKeyDown={(e) => { if (e.key === 'Enter' && e.target.tagName === 'INPUT') { e.preventDefault(); gui() } }}>
          <label>Tên kỹ năng
            <small>Ví dụ: Cắt tóc, Ngôn ngữ ký hiệu, Sửa xe đạp.</small>
            <input value={form.tenKyNang} maxLength={100} onChange={(e) => setForm({ ...form, tenKyNang: e.target.value })} />
          </label>
          <label>Mô tả ngắn (không bắt buộc)
            <input value={form.moTa} maxLength={300} onChange={(e) => setForm({ ...form, moTa: e.target.value })} />
          </label>
          <label>Kỹ năng này giúp được việc gì (không bắt buộc)
            <textarea rows={2} value={form.lyDo} maxLength={500} onChange={(e) => setForm({ ...form, lyDo: e.target.value })} />
          </label>
          <div className="hang-nut">
            <button type="button" disabled={tt.dangGui} onClick={gui}>Gửi đề xuất</button>
            <button type="button" className="phu" onClick={() => setMo(false)}>Thôi</button>
          </div>
        </div>
      )}
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {ds.duLieu?.length > 0 && (
        <ul className="ds-dong" style={{ marginTop: 10 }}>
          {ds.duLieu.map((d) => (
            <li key={d.maDeXuat}>
              <div className="than">
                <p><strong>{d.tenKyNang}</strong> <TrangThai ma={d.trangThai} /></p>
                <p className="mo">Gửi ngày {hienNgay(d.ngayDeXuat)}
                  {d.trangThai === 'DA_DUYET' && d.tenKyNangDuocDung && ` · Dùng kỹ năng "${d.tenKyNangDuocDung}"`}
                  {d.trangThai === 'TU_CHOI' && ` · Lý do: ${d.lyDoTuChoi}`}</p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
