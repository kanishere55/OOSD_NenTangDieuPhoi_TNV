import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { ChonNgayGio, DangTai, DuongDan, ThongBao, TrangThai, useThaoTac } from '../chung'
import { GIAI_THICH_DONG_GOP, ghepNgayGio, hienKhoangGio, soGio, tachNgayGio } from '../tienIch'

// M10 Khai đóng góp: nhập thời gian thực tế và mô tả công việc; số giờ do hệ thống tính (BR13).
export default function KhaiDongGop() {
  const { ma } = useParams()
  const [dk, setDk] = useState(null)
  const [banKhai, setBanKhai] = useState(null)
  const [form, setForm] = useState({ batDau: tachNgayGio(null), ketThuc: tachNgayGio(null), moTa: '' })
  const [loiTai, setLoiTai] = useState(null)
  const tt = useThaoTac()

  const tai = async () => {
    const [dsDk, dg] = await Promise.all([api.get('/api/dang-ky/cua-toi'), api.get('/api/dong-gop/cua-toi')])
    const d = dsDk.find((x) => x.maDangKy === ma)
    if (!d) throw new Error('Không tìm thấy đăng ký này trong các đăng ký của bạn.')
    const b = dg.danhSach.find((x) => x.maDangKy === ma) ?? null
    setDk(d)
    setBanKhai(b)
    return { d, b }
  }
  useEffect(() => {
    tai().then(({ d, b }) => setForm(b
      ? { batDau: tachNgayGio(b.batDauThucTe), ketThuc: tachNgayGio(b.ketThucThucTe), moTa: b.moTaCongViec }
      : { batDau: tachNgayGio(d.batDauDuKien), ketThuc: tachNgayGio(d.ketThucDuKien), moTa: '' }))
      .catch((e) => setLoiTai(e.message))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ma])

  const a = ghepNgayGio(form.batDau), b = ghepNgayGio(form.ketThuc)
  const soGioTam = a && b && b > a ? (new Date(b) - new Date(a)) / 3600000 : null

  const gui = async (e) => {
    e.preventDefault()
    if (!a || !b) { tt.setLoi('Hãy điền đủ ngày, giờ và phút cho cả giờ bắt đầu và giờ kết thúc.'); return }
    if (b <= a) { tt.setLoi('Giờ kết thúc thực tế phải sau giờ bắt đầu.'); return }
    if (!form.moTa.trim()) { tt.setLoi('Hãy mô tả ngắn gọn công việc bạn đã làm.'); return }
    await tt.chay(async () => {
      await api.post(`/api/dang-ky/${ma}/dong-gop`, { batDauThucTe: a, ketThucThucTe: b, moTaCongViec: form.moTa })
      const { b: moi } = await tai()
      tt.setThanhCong(`Đã gửi bản khai (${soGio(moi.soGio)} giờ). Bản khai đang chờ điều phối viên duyệt.`)
    })
  }

  const duongDan = <DuongDan muc={[{ ten: 'Đăng ký của tôi', den: '/dang-ky-cua-toi' }, { ten: 'Khai đóng góp' }]} />
  if (loiTai) return <>{duongDan}<ThongBao loi={loiTai} /></>
  if (!dk) return <DangTai />
  const choSua = dk.duocKhai && (!banKhai || banKhai.trangThai === 'CAN_BO_SUNG')
  return (
    <>
      {duongDan}
      <section className="the hep">
        <h1>Khai đóng góp</h1>
        <p className="mo-ta">Số giờ do hệ thống tính từ thời gian bạn khai.</p>
        <dl className="thong-tin">
          <dt>Ca làm</dt><dd>{dk.tieuDe}</dd>
          <dt>Thời gian của ca</dt><dd>{hienKhoangGio(dk.batDauDuKien, dk.ketThucDuKien)}</dd>
          {banKhai && <><dt>Bản khai</dt><dd><TrangThai ma={banKhai.trangThai} /> {soGio(banKhai.soGio)} giờ. <span className="mo">{GIAI_THICH_DONG_GOP[banKhai.trangThai]}</span></dd></>}
        </dl>
        {banKhai?.trangThai === 'CAN_BO_SUNG' && (
          <div className="thong-bao canh-bao"><p><strong>Điều phối viên trả lại bản khai.</strong> Lý do: {banKhai.lyDoTraLai}</p><p>Hãy sửa theo lý do trên rồi bấm Gửi lại.</p></div>
        )}
        {!dk.duocKhai && !banKhai && <div className="thong-bao tin">Chỉ khai được đóng góp khi bạn đã được phân công và ca đã kết thúc.</div>}
        <form onSubmit={gui} noValidate>
          <ChonNgayGio nhan="Bạn bắt đầu làm lúc" giaTri={form.batDau} tatDi={!choSua} onDoi={(v) => setForm({ ...form, batDau: v })}
            goiY="Thời gian thực tế phải nằm trong khung giờ của ca." />
          <ChonNgayGio nhan="Bạn kết thúc lúc" giaTri={form.ketThuc} tatDi={!choSua} onDoi={(v) => setForm({ ...form, ketThuc: v })} />
          {choSua && soGioTam !== null && <p>Số giờ sẽ được tính: <strong>{soGio(soGioTam)} giờ</strong></p>}
          <label>Công việc bạn đã làm
            <small>Ví dụ: Kèm Toán cho 3 em lớp 4.</small>
            <textarea rows={3} value={form.moTa} disabled={!choSua} onChange={(e) => setForm({ ...form, moTa: e.target.value })} />
          </label>
          <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
          <div className="hang-nut">
            {choSua && <button type="submit" className="lon" disabled={tt.dangGui}>{banKhai ? 'Gửi lại bản khai' : 'Gửi bản khai'}</button>}
            <Link to="/dong-gop/cua-toi">Xem lịch sử đóng góp</Link>
          </div>
        </form>
      </section>
    </>
  )
}
