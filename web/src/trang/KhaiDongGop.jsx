import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useThaoTac } from '../chung'
import { gioVnSangUtc, hienKhoangGio, soGio, utcSangGioVn } from '../tienIch'

// M10 Khai đóng góp: nhập thời gian thực tế và mô tả công việc; số giờ do hệ thống tính (BR13).
export default function KhaiDongGop() {
  const { ma } = useParams()
  const [dk, setDk] = useState(null)
  const [banKhai, setBanKhai] = useState(null)
  const [form, setForm] = useState({ batDau: '', ketThuc: '', moTa: '' })
  const [loiTai, setLoiTai] = useState(null)
  const tt = useThaoTac()

  const tai = async () => {
    const [dsDk, dg] = await Promise.all([api.get('/api/dang-ky/cua-toi'), api.get('/api/dong-gop/cua-toi')])
    const d = dsDk.find((x) => x.maDangKy === ma)
    if (!d) throw new Error('Không tìm thấy đăng ký.')
    const b = dg.danhSach.find((x) => x.maDangKy === ma) ?? null
    setDk(d)
    setBanKhai(b)
    return { d, b }
  }
  useEffect(() => {
    tai().then(({ d, b }) => setForm(b
      ? { batDau: utcSangGioVn(b.batDauThucTe), ketThuc: utcSangGioVn(b.ketThucThucTe), moTa: b.moTaCongViec }
      : { batDau: utcSangGioVn(d.batDauDuKien), ketThuc: utcSangGioVn(d.ketThucDuKien), moTa: '' }))
      .catch((e) => setLoiTai(e.message))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ma])

  const gui = async (e) => {
    e.preventDefault()
    await tt.chay(async () => {
      await api.post(`/api/dang-ky/${ma}/dong-gop`, {
        batDauThucTe: gioVnSangUtc(form.batDau), ketThucThucTe: gioVnSangUtc(form.ketThuc), moTaCongViec: form.moTa
      })
      const { b } = await tai()
      tt.setThanhCong(`Đã gửi bản khai. Số giờ hệ thống tính được: ${soGio(b.soGio)} giờ. Bản khai đang chờ điều phối viên duyệt.`)
    })
  }

  if (loiTai) return <ThongBao loi={loiTai} />
  if (!dk) return <DangTai />
  const choSua = !banKhai || banKhai.trangThai === 'CAN_BO_SUNG'
  return (
    <section className="the hep">
      <p><Link to="/dang-ky-cua-toi">← Đăng ký của tôi</Link></p>
      <h1>Khai đóng góp</h1>
      <p><strong>{dk.tieuDe}</strong></p>
      <p>Thời gian của ca: {hienKhoangGio(dk.batDauDuKien, dk.ketThucDuKien)}</p>
      {banKhai && <p>Trạng thái bản khai: <TrangThai ma={banKhai.trangThai} /> · {soGio(banKhai.soGio)} giờ</p>}
      {banKhai?.trangThai === 'CAN_BO_SUNG' && (
        <div className="thong-bao canh-bao">Bản khai bị trả lại. Lý do: {banKhai.lyDoTraLai}</div>
      )}
      <form onSubmit={gui}>
        <label>Giờ bắt đầu thực tế
          <input type="datetime-local" value={form.batDau} disabled={!choSua} onChange={(e) => setForm({ ...form, batDau: e.target.value })} />
        </label>
        <label>Giờ kết thúc thực tế
          <input type="datetime-local" value={form.ketThuc} disabled={!choSua} onChange={(e) => setForm({ ...form, ketThuc: e.target.value })} />
        </label>
        <label>Mô tả công việc đã làm
          <textarea rows={3} value={form.moTa} disabled={!choSua} onChange={(e) => setForm({ ...form, moTa: e.target.value })} />
        </label>
        <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
        {choSua && <button type="submit" disabled={tt.dangGui}>{banKhai ? 'Gửi lại' : 'Gửi'}</button>}
      </form>
    </section>
  )
}
