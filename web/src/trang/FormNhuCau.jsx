import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, ThongBao, TrangThai, useNguoiDung, useThaoTac } from '../chung'

const TRONG = { maToChuc: '', tieuDe: '', tomTatCongKhai: '', khuVucChung: '', huongDanRieng: '' }

// M07 Tạo và công bố nhu cầu. Nội dung công khai và hướng dẫn riêng tách thành hai phần (BR01, BR05).
export default function FormNhuCau() {
  const { ma } = useParams()
  const dieuHuong = useNavigate()
  const { nguoiDung } = useNguoiDung()
  const [nc, setNc] = useState(null)
  const [form, setForm] = useState(TRONG)
  const [dangTai, setDangTai] = useState(!!ma)
  const [daXacNhanRaSoat, setDaXacNhanRaSoat] = useState(false)
  const tt = useThaoTac()

  const toChucDaiDien = nguoiDung?.thanhVien.filter((t) => t.vaiTro === 'DAI_DIEN') ?? []

  const tai = async () => {
    const d = await api.get('/api/nhu-cau/' + ma)
    setNc(d)
    setForm({ maToChuc: d.maToChuc, tieuDe: d.tieuDe, tomTatCongKhai: d.tomTatCongKhai, khuVucChung: d.khuVucChung, huongDanRieng: d.huongDanRieng ?? '' })
    return d
  }
  useEffect(() => {
    if (ma) tai().catch((e) => tt.setLoi(e.message)).finally(() => setDangTai(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ma])
  useEffect(() => {
    if (!ma && toChucDaiDien.length && !form.maToChuc) setForm((f) => ({ ...f, maToChuc: toChucDaiDien[0].maToChuc }))
  }, [ma, toChucDaiDien, form.maToChuc])

  if (dangTai) return <DangTai />

  const laDaiDien = !ma ? toChucDaiDien.length > 0 : nc?.laDaiDien
  const laNhap = !nc || nc.trangThai === 'NHAP'
  const suaCongKhai = laDaiDien && laNhap
  const daSuaCongKhai = nc && (form.tieuDe !== nc.tieuDe || form.tomTatCongKhai !== nc.tomTatCongKhai || form.khuVucChung !== nc.khuVucChung)
  const daRaSoat = daXacNhanRaSoat && !daSuaCongKhai
  const doi = (truong) => (e) => setForm({ ...form, [truong]: e.target.value })

  // Lưu bản nháp; trả về dữ liệu mới nhất của nhu cầu.
  const luu = async () => {
    if (!ma) {
      const kq = await api.post('/api/nhu-cau', form)
      dieuHuong('/nhu-cau/' + kq.maNhuCau, { replace: true })
      return null
    }
    await api.put('/api/nhu-cau/' + ma, form)
    const moi = await tai()
    // Nội dung công khai đổi sang phiên bản mới thì phải rà soát lại.
    if (daSuaCongKhai) setDaXacNhanRaSoat(false)
    return moi
  }

  const congBo = async () => {
    tt.setThanhCong(null)
    if (daSuaCongKhai) {
      tt.setLoi('Nội dung công khai vừa sửa chưa được lưu. Hãy bấm Lưu, đọc lại rồi đánh dấu ô rà soát trước khi công bố.')
      return
    }
    if (!daRaSoat) {
      tt.setLoi('Hãy đọc lại nội dung công khai và đánh dấu ô xác nhận đã rà soát trước khi công bố.')
      return
    }
    // Gửi kèm số phiên bản đã rà soát; máy chủ từ chối nếu nội dung đã bị sửa sau đó (BR05).
    await tt.chay(async () => {
      await api.post(`/api/nhu-cau/${ma}/cong-bo`, { phienBan: nc.phienBanCongKhai })
      await tai()
    }, 'Đã công bố nhu cầu. Điều phối viên có thể tạo ca làm.')
  }

  return (
    <section className="the">
      <p><Link to="/nhu-cau">← Danh sách nhu cầu</Link></p>
      <div className="tieu-de-hang">
        <h1>{ma ? 'Nhu cầu hỗ trợ' : 'Tạo nhu cầu'}</h1>
        {nc && <span>Trạng thái: <TrangThai ma={nc.trangThai} /> · phiên bản nội dung {nc.phienBanCongKhai}</span>}
      </div>

      <fieldset className="phan cong-khai">
        <legend>Nội dung công khai</legend>
        <p className="mo">Ai cũng xem được phần này. Không ghi họ tên, địa chỉ cụ thể hay hoàn cảnh của người được hỗ trợ.</p>
        {!ma && (
          <label>Tổ chức
            <select value={form.maToChuc} onChange={doi('maToChuc')}>
              {toChucDaiDien.map((t) => <option key={t.maToChuc} value={t.maToChuc}>{t.tenToChuc}</option>)}
            </select>
          </label>
        )}
        <label>Tiêu đề<input value={form.tieuDe} onChange={doi('tieuDe')} disabled={!suaCongKhai} maxLength={150} /></label>
        <label>Tóm tắt công khai<textarea rows={3} value={form.tomTatCongKhai} onChange={doi('tomTatCongKhai')} disabled={!suaCongKhai} /></label>
        <label>Khu vực chung<input value={form.khuVucChung} onChange={doi('khuVucChung')} disabled={!suaCongKhai} placeholder="Ví dụ: Phường An Bình" maxLength={150} /></label>
      </fieldset>

      <fieldset className="phan rieng">
        <legend>Hướng dẫn điều phối riêng</legend>
        <p className="mo">Chỉ đại diện, điều phối viên của tổ chức và tình nguyện viên đã được phân công (trước khi ca kết thúc) mới xem được. Chỉ ghi những gì cần cho việc điều phối.</p>
        <textarea rows={3} value={form.huongDanRieng} onChange={doi('huongDanRieng')} disabled={!laDaiDien} placeholder="Ví dụ: địa điểm cụ thể, người liên hệ khi đến, lưu ý khi tiếp xúc" />
      </fieldset>

      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />

      {laDaiDien && (
        <div className="hang-nut">
          <button className="phu" disabled={tt.dangGui} onClick={() => tt.chay(luu, ma ? 'Đã lưu.' : null)}>
            {ma ? 'Lưu' : 'Lưu bản nháp'}
          </button>
          {ma && laNhap && (
            <>
              <label className="o-chon">
                <input type="checkbox" checked={daRaSoat} disabled={tt.dangGui} onChange={(e) => setDaXacNhanRaSoat(e.target.checked)} />
                <span>Tôi đã đọc lại nội dung công khai và xác nhận không để lộ thông tin của người được hỗ trợ</span>
              </label>
              <button disabled={tt.dangGui} onClick={congBo}>Công bố</button>
            </>
          )}
        </div>
      )}
      {nc?.trangThai === 'CONG_BO' && <p><Link className="nut phu" to={`/nhu-cau/${ma}/ca`}>Xem và tạo ca làm</Link></p>}
    </section>
  )
}
