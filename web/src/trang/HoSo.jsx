import { useEffect, useState } from 'react'
import { api } from '../api'
import { DangTai, ThongBao, useTai, useThaoTac } from '../chung'
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
  }
  const luu = async () => {
    if (await tt.chay(() => api.put('/api/ho-so/ky-nang', { kyNang: [...chon] }), 'Đã lưu kỹ năng.')) hoSo.taiLai()
    else hoSo.taiLai()
  }

  if (hoSo.dangTai || danhMuc.dangTai) return <DangTai />
  if (hoSo.loi) return <ThongBao loi={hoSo.loi} />
  const h = hoSo.duLieu
  return (
    <section className="the hep">
      <h1>Hồ sơ và kỹ năng</h1>
      <dl className="thong-tin">
        <dt>Tên hiển thị</dt><dd>{h.tenHienThi}</dd>
        <dt>Cập nhật kỹ năng lần cuối</dt><dd>{hienGio(h.ngayCapNhatKyNang)}</dd>
      </dl>
      <h2>Kỹ năng của tôi</h2>
      <div className="ds-o-chon">
        {danhMuc.duLieu.map((k) => (
          <label key={k.maKyNang} className="o-chon">
            <input type="checkbox" checked={chon.has(k.maKyNang)} onChange={() => doi(k.maKyNang)} />
            <span><strong>{k.tenKyNang}</strong>{k.moTaKyNang && <small>{k.moTaKyNang}</small>}</span>
          </label>
        ))}
      </div>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      <button onClick={luu} disabled={tt.dangGui}>Lưu</button>
    </section>
  )
}
