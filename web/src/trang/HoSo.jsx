import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao, useTai, useThaoTac } from '../chung'
import { hienGio } from '../tienIch'

// M04 Hồ sơ và kỹ năng: tình nguyện viên chọn kỹ năng từ danh mục chung.
export default function HoSo() {
  const hoSo = useTai(() => api.get('/api/ho-so'), [])
  const danhMuc = useTai(() => api.get('/api/ky-nang'), [])
  const [chon, setChon] = useState(new Set())
  const tt = useThaoTac()

  useEffect(() => {
    if (hoSo.duLieu) setChon(new Set(hoSo.duLieu.kyNang.map((k) => k.maKyNang)))
  }, [hoSo.duLieu])

  const doi = (ma) => {
    const moi = new Set(chon)
    moi.has(ma) ? moi.delete(ma) : moi.add(ma)
    setChon(moi)
    tt.setThanhCong(null)
  }
  const luu = async () => {
    await tt.chay(() => api.put('/api/ho-so/ky-nang', { kyNang: [...chon] }), 'Đã lưu kỹ năng của bạn.')
    hoSo.taiLai()
  }

  if ((hoSo.dangTai && !hoSo.duLieu) || danhMuc.dangTai) return <DangTai />
  if (hoSo.loi || danhMuc.loi) return <ThongBao loi={hoSo.loi || danhMuc.loi} />
  const h = hoSo.duLieu
  const daLuu = new Set(h.kyNang.map((k) => k.maKyNang))
  const coThayDoi = daLuu.size !== chon.size || [...chon].some((m) => !daLuu.has(m))
  return (
    <>
      <DauTrang tieuDe="Hồ sơ và kỹ năng"
        moTa="Đánh dấu những kỹ năng bạn có. Bạn chỉ đăng ký được ca khi có đủ các kỹ năng mà ca yêu cầu." />
      <section className="the">
        <dl className="thong-tin">
          <dt>Họ tên</dt><dd>{h.tenHienThi}</dd>
          <dt>Kỹ năng đã lưu</dt><dd>{h.kyNang.length === 0 ? 'Chưa có' : h.kyNang.map((k) => k.tenKyNang).join(', ')}</dd>
          <dt>Lần lưu gần nhất</dt><dd>{hienGio(h.ngayCapNhatKyNang)}</dd>
        </dl>
        <h2>Chọn kỹ năng của bạn</h2>
        <div className="ds-o-chon">
          {danhMuc.duLieu.map((k) => (
            <label key={k.maKyNang} className={'o-chon' + (chon.has(k.maKyNang) ? ' da-chon' : '')}>
              <input type="checkbox" checked={chon.has(k.maKyNang)} onChange={() => doi(k.maKyNang)} />
              <span><strong>{k.tenKyNang}</strong>{k.moTaKyNang && <small>{k.moTaKyNang}</small>}</span>
            </label>
          ))}
        </div>
        <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
        <div className="hang-nut">
          <button className="lon" onClick={luu} disabled={tt.dangGui || !coThayDoi}>Lưu kỹ năng</button>
          {coThayDoi ? <span className="thieu-ky-nang">Bạn có thay đổi chưa lưu.</span>
            : <Link to="/tim-ca">Tìm ca hợp với kỹ năng</Link>}
        </div>
      </section>
    </>
  )
}
