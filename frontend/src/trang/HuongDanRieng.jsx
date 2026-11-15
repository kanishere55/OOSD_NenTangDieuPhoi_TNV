import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { BanDoGhim } from '../BanDo'
import { DangTai, useTai } from '../chung'
import { duongDanChiDuong, hienKhoangGio } from '../tienIch'

// M12 Hướng dẫn điều phối riêng: máy chủ kiểm tra quyền theo BR15 trước khi trả nội dung và ghi nhật ký mỗi lần xem.
// Có địa chỉ cụ thể, vị trí chính xác và nút chỉ đường bằng Google Maps (BR21).
export default function HuongDanRieng() {
  const { ma } = useParams()
  const kq = useTai(() => api.get(`/api/ca/${ma}/huong-dan-rieng`), [ma])
  const dieuHuong = useNavigate()
  const quayLai = <p><button className="phu" onClick={() => dieuHuong(-1)}>‹ Quay lại trang trước</button></p>

  if (kq.dangTai) return <DangTai />
  if (kq.loi) return (
    <section className="the hep">
      <h1>Hướng dẫn điều phối riêng</h1>
      <div className="thong-bao loi" role="alert">{kq.loi}</div>
      <p className="mo">Hướng dẫn riêng có thể chứa địa điểm và thông tin về người được hỗ trợ nên chỉ hiện cho người tham gia ca.</p>
      {quayLai}
    </section>
  )
  const d = kq.duLieu
  const coViTri = d.viDo != null
  return (
    <section className="the hep">
      <h1>Hướng dẫn điều phối riêng</h1>
      <dl className="thong-tin">
        <dt>Ca làm</dt><dd>{d.tieuDe}</dd>
        <dt>Thời gian</dt><dd>{hienKhoangGio(d.batDauDuKien, d.ketThucDuKien)}</dd>
        <dt>Địa chỉ</dt><dd>{d.diaChiCuThe || <span className="mo">Tổ chức chưa ghi địa chỉ cụ thể.</span>}</dd>
      </dl>
      {coViTri && (
        <>
          <BanDoGhim viDo={d.viDo} kinhDo={d.kinhDo} nhan={d.diaChiCuThe || 'Địa điểm'} cao={280} />
          <p className="hang-nut" style={{ marginTop: 10 }}>
            <a className="nut lon" href={duongDanChiDuong(d.viDo, d.kinhDo)} target="_blank" rel="noreferrer">Chỉ đường bằng Google Maps</a>
          </p>
        </>
      )}
      <h2>Hướng dẫn</h2>
      <div className="khung-rieng">
        {d.huongDanRieng ? d.huongDanRieng : <span className="mo">Nhu cầu này chưa có hướng dẫn riêng. Nếu cần biết thêm, hãy hỏi điều phối viên của tổ chức.</span>}
      </div>
      <div className="thong-bao canh-bao">Nội dung này chỉ dành cho người tham gia ca. Xin không chụp lại hay chia sẻ ra ngoài. Mỗi lần xem trang này đều được ghi lại.</div>
      {quayLai}
    </section>
  )
}
