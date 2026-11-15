import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { ChonNgayGio, DangTai, DuongDan, LuoiAnh, NutChonAnh, ThongBao, TrangThai, useThaoTac } from '../chung'
import { GIAI_THICH_DONG_GOP, ghepNgayGio, hienGioPhut, hienKhoangGio, soGio, tachNgayGio } from '../tienIch'

// Giờ làm theo điểm danh, cắt trong khung giờ của ca (BR23).
function gioTheoDiemDanh(dk) {
  const d = dk.diemDanh
  if (d?.ketQua !== 'CO_MAT' || !d.gioVao || !d.gioRa) return null
  const a = d.gioVao < dk.batDauDuKien ? dk.batDauDuKien : d.gioVao
  const b = d.gioRa > dk.ketThucDuKien ? dk.ketThucDuKien : d.gioRa
  return b > a ? { a, b } : null
}

// M10 Khai đóng góp: thời gian thực tế (lấy theo điểm danh nếu có), mô tả công việc và ảnh minh chứng;
// số giờ do hệ thống tính (BR13, BR23, BR24).
export default function KhaiDongGop() {
  const { ma } = useParams()
  const [dk, setDk] = useState(null)
  const [banKhai, setBanKhai] = useState(null)
  const [form, setForm] = useState({ batDau: tachNgayGio(null), ketThuc: tachNgayGio(null), moTa: '' })
  const [anhCho, setAnhCho] = useState([])   // ảnh đã chọn, gửi cùng lúc với bản khai mới
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
    tai().then(({ d, b }) => {
      const dd = gioTheoDiemDanh(d)
      setForm(b
        ? { batDau: tachNgayGio(b.batDauThucTe), ketThuc: tachNgayGio(b.ketThucThucTe), moTa: b.moTaCongViec }
        : { batDau: tachNgayGio(dd?.a ?? d.batDauDuKien), ketThuc: tachNgayGio(dd?.b ?? d.ketThucDuKien), moTa: '' })
    }).catch((e) => setLoiTai(e.message))
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
      const kq = await api.post(`/api/dang-ky/${ma}/dong-gop`, { batDauThucTe: a, ketThucThucTe: b, moTaCongViec: form.moTa })
      for (const x of anhCho) await api.taiTep(`/api/dong-gop/${kq.maDongGop}/anh`, x.tep)
      setAnhCho([])
      const { b: moi } = await tai()
      tt.setThanhCong(`Đã gửi bản khai (${soGio(moi.soGio)} giờ). Bản khai đang chờ điều phối viên duyệt.`)
    })
  }
  const themAnh = async (ds) => {
    if (!banKhai) { setAnhCho([...anhCho, ...ds.map((t) => ({ tep: t, src: URL.createObjectURL(t), khoa: t.name + t.size + Math.random() }))].slice(0, 5)); return }
    await tt.chay(async () => {
      for (const t of ds) await api.taiTep(`/api/dong-gop/${banKhai.maDongGop}/anh`, t)
      await tai()
    }, 'Đã thêm ảnh minh chứng.')
  }
  const boAnh = async (x) => {
    if (!banKhai) { setAnhCho(anhCho.filter((y) => y.khoa !== x.khoa)); return }
    await tt.chay(async () => { await api.post(`/api/dong-gop/${banKhai.maDongGop}/anh/${x.khoa}/bo`); await tai() }, 'Đã bỏ ảnh.')
  }

  const duongDan = <DuongDan muc={[{ ten: 'Đăng ký của tôi', den: '/dang-ky-cua-toi' }, { ten: 'Khai đóng góp' }]} />
  if (loiTai) return <>{duongDan}<ThongBao loi={loiTai} /></>
  if (!dk) return <DangTai />
  const choSua = dk.duocKhai && (!banKhai || banKhai.trangThai === 'CAN_BO_SUNG')
  const suaAnh = choSua || banKhai?.trangThai === 'CHO_DUYET'
  const dd = gioTheoDiemDanh(dk)
  const dsAnh = banKhai ? banKhai.anh.map((m) => ({ khoa: m, src: '/api/anh/minh-chung/' + m })) : anhCho
  return (
    <>
      {duongDan}
      <section className="the hep">
        <h1>Khai đóng góp</h1>
        <p className="mo-ta">Số giờ do hệ thống tính từ thời gian làm thực tế.</p>
        <dl className="thong-tin">
          <dt>Ca làm</dt><dd>{dk.tieuDe}</dd>
          <dt>Thời gian của ca</dt><dd>{hienKhoangGio(dk.batDauDuKien, dk.ketThucDuKien)}</dd>
          {banKhai && <><dt>Bản khai</dt><dd><TrangThai ma={banKhai.trangThai} /> {soGio(banKhai.soGio)} giờ. <span className="mo">{GIAI_THICH_DONG_GOP[banKhai.trangThai]}</span></dd></>}
        </dl>
        {banKhai?.trangThai === 'CAN_BO_SUNG' && (
          <div className="thong-bao canh-bao"><p><strong>Điều phối viên trả lại bản khai.</strong> Lý do: {banKhai.lyDoTraLai}</p><p>Hãy sửa theo lý do trên rồi bấm Gửi lại.</p></div>
        )}
        {dk.diemDanh?.ketQua === 'VANG_MAT' && <div className="thong-bao canh-bao">Bạn được ghi nhận vắng mặt ở ca này nên không khai đóng góp được. Nếu có nhầm lẫn, hãy liên hệ điều phối viên để sửa lại.</div>}
        {!dk.duocKhai && !banKhai && dk.diemDanh?.ketQua !== 'VANG_MAT' && <div className="thong-bao tin">Chỉ khai được đóng góp khi bạn đã được phân công và ca đã kết thúc.</div>}
        {dd && choSua && (
          <div className="thong-bao tin">
            Bạn đã điểm danh vào lúc {hienGioPhut(dk.diemDanh.gioVao)} và ra lúc {hienGioPhut(dk.diemDanh.gioRa)}.
            Thời gian làm được lấy theo điểm danh, trong khung giờ của ca: từ {hienGioPhut(dd.a)} đến {hienGioPhut(dd.b)}.
          </div>
        )}
        <form onSubmit={gui} noValidate>
          <ChonNgayGio nhan="Bạn bắt đầu làm lúc" giaTri={form.batDau} tatDi={!choSua || !!dd} onDoi={(v) => setForm({ ...form, batDau: v })}
            goiY={dd ? null : 'Thời gian thực tế phải nằm trong khung giờ của ca.'} />
          <ChonNgayGio nhan="Bạn kết thúc lúc" giaTri={form.ketThuc} tatDi={!choSua || !!dd} onDoi={(v) => setForm({ ...form, ketThuc: v })} />
          {choSua && soGioTam !== null && <p>Số giờ sẽ được tính: <strong>{soGio(soGioTam)} giờ</strong></p>}
          <label>Công việc bạn đã làm
            <small>Ví dụ: Kèm Toán cho 3 em lớp 4.</small>
            <textarea rows={3} value={form.moTa} disabled={!choSua} onChange={(e) => setForm({ ...form, moTa: e.target.value })} />
          </label>
          <p style={{ marginBottom: 4 }}><strong>Ảnh minh chứng (không bắt buộc)</strong></p>
          <p className="mo" style={{ marginTop: 0 }}>Tối đa 5 ảnh. Chỉ bạn, điều phối viên và đại diện của tổ chức xem được. Không chụp mặt người được hỗ trợ khi chưa được đồng ý.</p>
          <LuoiAnh ds={dsAnh} onBo={suaAnh || !banKhai ? boAnh : null} tatDi={tt.dangGui} />
          {(suaAnh || (!banKhai && choSua)) && dsAnh.length < 5 && (
            <p><NutChonAnh nhan={dsAnh.length ? 'Thêm ảnh' : 'Chọn ảnh'} nhieu tatDi={tt.dangGui} onChon={(ds) => themAnh(ds.slice(0, 5 - dsAnh.length))} /></p>
          )}
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
