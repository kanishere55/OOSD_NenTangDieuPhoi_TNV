import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useTai, useThaoTac } from '../chung'
import { gioVnSangUtc, hienKhoangGio } from '../tienIch'

// M08 Quản lý ca làm của một nhu cầu đã công bố; điều phối viên tạo ca kèm kỹ năng bắt buộc.
export default function QuanLyCa() {
  const { ma } = useParams()
  const nc = useTai(() => api.get('/api/nhu-cau/' + ma), [ma])
  const ds = useTai(() => api.get(`/api/nhu-cau/${ma}/ca`), [ma])
  const danhMuc = useTai(() => api.get('/api/ky-nang'), [])
  const [form, setForm] = useState({ batDau: '', ketThuc: '', soNguoiCan: 1, kyNang: [] })
  const [loiO, setLoiO] = useState({})
  const [caDangHuy, setCaDangHuy] = useState(null)
  const tt = useThaoTac()
  const ttHuy = useThaoTac()

  const kiemTra = () => {
    const l = {}
    if (!form.batDau) l.batDau = 'Hãy chọn giờ bắt đầu.'
    if (!form.ketThuc) l.ketThuc = 'Hãy chọn giờ kết thúc.'
    else if (form.batDau && form.ketThuc <= form.batDau) l.ketThuc = 'Giờ kết thúc phải sau giờ bắt đầu.'
    if (!(form.soNguoiCan >= 1)) l.soNguoiCan = 'Ca cần ít nhất một người.'
    if (form.kyNang.length === 0) l.kyNang = 'Phải chọn ít nhất một kỹ năng.'
    setLoiO(l)
    return Object.keys(l).length === 0
  }

  const taoCa = async (e) => {
    e.preventDefault()
    if (!kiemTra()) return
    const ok = await tt.chay(() => api.post(`/api/nhu-cau/${ma}/ca`, {
      batDauDuKien: gioVnSangUtc(form.batDau), ketThucDuKien: gioVnSangUtc(form.ketThuc),
      soNguoiCan: Number(form.soNguoiCan), kyNang: form.kyNang
    }), 'Đã tạo ca.')
    if (ok) { setForm({ batDau: '', ketThuc: '', soNguoiCan: 1, kyNang: [] }); ds.taiLai() }
  }
  const huyCa = async (maCaLam) => {
    const ok = await ttHuy.chay(() => api.post(`/api/ca/${maCaLam}/huy`), 'Đã hủy ca. Các đăng ký còn hiệu lực của ca cũng đã bị hủy.')
    setCaDangHuy(null)
    if (ok) ds.taiLai()
  }
  const doiKyNang = (m) => setForm({ ...form, kyNang: form.kyNang.includes(m) ? form.kyNang.filter((x) => x !== m) : [...form.kyNang, m] })

  if (nc.dangTai || ds.dangTai) return <DangTai />
  if (nc.loi || ds.loi) return <ThongBao loi={nc.loi || ds.loi} />
  return (
    <>
      <p><Link to={'/nhu-cau/' + ma}>← {nc.duLieu.tieuDe}</Link></p>
      <h1>Ca làm</h1>
      <ThongBao loi={ttHuy.loi} thanhCong={ttHuy.thanhCong} />
      {ds.duLieu.length === 0 ? <p className="mo">Nhu cầu chưa có ca nào.</p> : (
        <table>
          <thead><tr><th>Thời gian</th><th>Kỹ năng</th><th>Cần</th><th>Đã phân công</th><th>Chờ duyệt</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>
            {ds.duLieu.map((c) => (
              <tr key={c.maCaLam}>
                <td>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</td>
                <td>{c.kyNangYeuCau.join(', ')}</td>
                <td>{c.soNguoiCan}</td>
                <td>{c.soDaPhanCong}</td>
                <td>{c.soChoDuyet}</td>
                <td><TrangThai ma={c.trangThai} /></td>
                <td className="hang-nut">
                  {nc.duLieu.laDieuPhoi && <Link to={`/ca/${c.maCaLam}/xet-duyet`}>Xét duyệt</Link>}
                  {nc.duLieu.laDieuPhoi && c.trangThai === 'MO' && new Date(c.batDauDuKien) > new Date() && (
                    caDangHuy === c.maCaLam ? (
                      <span className="xac-nhan">
                        Hủy ca này?
                        <button className="nguy-hiem" disabled={ttHuy.dangGui} onClick={() => huyCa(c.maCaLam)}>Hủy ca</button>
                        <button className="phu" disabled={ttHuy.dangGui} onClick={() => setCaDangHuy(null)}>Không</button>
                      </span>
                    ) : (
                      <button className="phu" onClick={() => setCaDangHuy(c.maCaLam)}>Hủy ca</button>
                    )
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {nc.duLieu.laDieuPhoi && nc.duLieu.trangThai === 'CONG_BO' && (
        <form className="the hep" onSubmit={taoCa} noValidate>
          <h2>Tạo ca</h2>
          <label>Giờ bắt đầu (giờ Việt Nam)
            <input type="datetime-local" value={form.batDau} onChange={(e) => setForm({ ...form, batDau: e.target.value })} />
            {loiO.batDau && <small className="loi-o">{loiO.batDau}</small>}
          </label>
          <label>Giờ kết thúc
            <input type="datetime-local" value={form.ketThuc} onChange={(e) => setForm({ ...form, ketThuc: e.target.value })} />
            {loiO.ketThuc && <small className="loi-o">{loiO.ketThuc}</small>}
          </label>
          <label>Số người cần
            <input type="number" min={1} value={form.soNguoiCan} onChange={(e) => setForm({ ...form, soNguoiCan: e.target.value })} />
            {loiO.soNguoiCan && <small className="loi-o">{loiO.soNguoiCan}</small>}
          </label>
          <div>Kỹ năng bắt buộc
            <div className="ds-o-chon">
              {danhMuc.duLieu?.map((k) => (
                <label key={k.maKyNang} className="o-chon">
                  <input type="checkbox" checked={form.kyNang.includes(k.maKyNang)} onChange={() => doiKyNang(k.maKyNang)} />
                  <span>{k.tenKyNang}</span>
                </label>
              ))}
            </div>
            {loiO.kyNang && <small className="loi-o">{loiO.kyNang}</small>}
          </div>
          <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
          <button type="submit" disabled={tt.dangGui}>Tạo ca</button>
        </form>
      )}
    </>
  )
}
