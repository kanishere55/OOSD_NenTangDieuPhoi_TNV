import { Link, useParams } from 'react-router-dom'
import { api, dangKyTaiKhoan, dangNhap } from '../api'
import { DangTai, DuongDan, ThongBao, TrangThai, useNguoiDung, useTai, useThaoTac } from '../chung'
import { hienKhoangGio, hienSoCho } from '../tienIch'

// M03 Chi tiết ca làm: thông tin công khai của ca và phần đăng ký tham gia.
export default function ChiTietCa() {
  const { ma } = useParams()
  const { nguoiDung, laTnv } = useNguoiDung()
  const ca = useTai(() => api.get('/api/ca/' + ma), [ma])
  const cuaToi = useTai(async () => {
    if (!laTnv) return null
    const [hs, dk] = await Promise.all([api.get('/api/ho-so'), api.get('/api/dang-ky/cua-toi')])
    return {
      kyNang: new Set(hs.kyNang.map((k) => k.tenKyNang)),
      dangKy: dk.find((d) => d.maCaLam === ma && (d.trangThai === 'CHO_DUYET' || d.trangThai === 'DA_PHAN_CONG')) ?? null
    }
  }, [ma, laTnv])
  const tt = useThaoTac()

  const dangKy = async () => {
    if (await tt.chay(() => api.post(`/api/ca/${ma}/dang-ky`))) { cuaToi.taiLai(); ca.taiLai() }
  }

  const duongDan = <DuongDan muc={[{ ten: 'Tìm ca làm', den: '/tim-ca' }, { ten: 'Chi tiết ca làm' }]} />
  if (ca.dangTai && !ca.duLieu) return <DangTai />
  if (ca.loi) {
    return (
      <>
        {duongDan}
        <div className="thong-bao canh-bao">
          <p>Ca này không còn nhận đăng ký (đã bắt đầu, đã kết thúc hoặc đã hủy) nên không còn hiện trong danh sách công khai.</p>
          <p><Link to="/tim-ca">Xem các ca đang mở</Link>{laTnv && <> · <Link to="/dang-ky-cua-toi">Xem đăng ký của tôi</Link></>}</p>
        </div>
      </>
    )
  }
  const c = ca.duLieu
  const thieu = cuaToi.duLieu ? c.kyNangYeuCau.filter((k) => !cuaToi.duLieu.kyNang.has(k)) : []
  const dk = cuaToi.duLieu?.dangKy

  return (
    <>
      {duongDan}
      <section className="the">
        <h1>{c.tieuDe}</h1>
        <p className="mo">Do {c.tenToChuc} đăng</p>
        <p>{c.tomTatCongKhai}</p>
        <dl className="thong-tin">
          <dt>Thời gian</dt><dd>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</dd>
          <dt>Khu vực</dt><dd>{c.khuVucChung}</dd>
          <dt>Số người</dt><dd>{hienSoCho(c.soNguoiCan, c.soDaPhanCong)}</dd>
          <dt>Kỹ năng cần có</dt><dd>{c.kyNangYeuCau.map((k) => <span key={k} className="nhan nhan-ky-nang">{k}</span>)}</dd>
        </dl>
        <p className="mo">Địa điểm cụ thể và hướng dẫn chi tiết chỉ hiện cho người đã được phân công vào ca.</p>
      </section>

      <section className="the">
        <h2 style={{ marginTop: 0 }}>Tham gia ca này</h2>
        <ThongBao loi={tt.loi} />
        {!nguoiDung && (
          <>
            <p>Để đăng ký, bạn cần đăng nhập bằng tài khoản tình nguyện viên.</p>
            <div className="hang-nut">
              <button onClick={() => dangNhap('/ca/' + ma)}>Đăng nhập để đăng ký</button>
              <button className="phu" onClick={() => dangKyTaiKhoan('/ca/' + ma)}>Tôi chưa có tài khoản</button>
            </div>
          </>
        )}
        {nguoiDung && !laTnv && <p className="mo">Tài khoản của bạn không có vai trò tình nguyện viên nên không đăng ký ca.</p>}
        {laTnv && cuaToi.dangTai && !cuaToi.duLieu && <DangTai />}
        {laTnv && cuaToi.duLieu && dk && (
          <>
            <p>Bạn đã đăng ký ca này. Tình trạng: <TrangThai ma={dk.trangThai} /></p>
            {dk.trangThai === 'CHO_DUYET'
              ? <p className="mo">Điều phối viên sẽ xem xét. Bạn chỉ chính thức có chỗ khi được phân công.</p>
              : <p className="mo">Bạn đã có chỗ trong ca. Hãy xem hướng dẫn riêng trước khi đến.</p>}
            <div className="hang-nut">
              {dk.trangThai === 'DA_PHAN_CONG' && <Link className="nut" to={`/ca/${ma}/huong-dan-rieng`}>Xem hướng dẫn riêng</Link>}
              <Link className="nut phu" to="/dang-ky-cua-toi">Xem đăng ký của tôi</Link>
            </div>
          </>
        )}
        {laTnv && cuaToi.duLieu && !dk && thieu.length > 0 && (
          <>
            <div className="thong-bao canh-bao">
              <p>Ca này cần kỹ năng <strong>{thieu.join(', ')}</strong> nhưng hồ sơ của bạn chưa có, nên chưa đăng ký được.</p>
              <p>Nếu bạn có kỹ năng này, hãy thêm vào hồ sơ rồi quay lại đăng ký.</p>
            </div>
            <Link className="nut" to="/ho-so">Cập nhật kỹ năng</Link>
          </>
        )}
        {laTnv && cuaToi.duLieu && !dk && thieu.length === 0 && (
          <>
            <p>Bạn có đủ kỹ năng mà ca yêu cầu. Sau khi đăng ký, điều phối viên sẽ xem xét và phân công.</p>
            <button className="lon" onClick={dangKy} disabled={tt.dangGui}>Đăng ký tham gia</button>
          </>
        )}
      </section>
    </>
  )
}
