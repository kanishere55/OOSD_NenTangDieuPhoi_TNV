import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, useTai } from '../chung'
import { hienKhoangGio } from '../tienIch'

// M12 Hướng dẫn điều phối riêng: máy chủ kiểm tra quyền theo BR15 trước khi trả nội dung.
export default function HuongDanRieng() {
  const { ma } = useParams()
  const kq = useTai(() => api.get(`/api/ca/${ma}/huong-dan-rieng`), [ma])
  const dieuHuong = useNavigate()
  const quayLai = <p><button className="phu" onClick={() => dieuHuong(-1)}>← Quay lại</button></p>

  if (kq.dangTai) return <DangTai />
  if (kq.loi) return (
    <section className="the hep">
      <h1>Hướng dẫn điều phối riêng</h1>
      <div className="thong-bao loi">{kq.loi}</div>
      {quayLai}
    </section>
  )
  const d = kq.duLieu
  return (
    <section className="the hep">
      <h1>Hướng dẫn điều phối riêng</h1>
      <p><strong>{d.tieuDe}</strong></p>
      <p>{hienKhoangGio(d.batDauDuKien, d.ketThucDuKien)}</p>
      <div className="khung-rieng">
        {d.huongDanRieng ? d.huongDanRieng : <span className="mo">Nhu cầu này chưa có hướng dẫn riêng.</span>}
      </div>
      <p className="mo">Nội dung chỉ dành cho người tham gia điều phối. Không chia sẻ ra ngoài.</p>
      {quayLai}
    </section>
  )
}
