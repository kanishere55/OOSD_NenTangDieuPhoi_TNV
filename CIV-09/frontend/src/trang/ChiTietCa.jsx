import { Link, useParams } from 'react-router-dom'
import { api, dangKyTaiKhoan, dangNhap } from '../api'
import { BanDoVung } from '../BanDo'
import { DangTai, DuongDan, LuoiAnh, ThongBao, TrangThai, useNguoiDung, useTai, useThaoTac } from '../chung'
import { conThieu } from '../kiemTraKyNang'
import { hienKhoangGio, hienSoCho, hienYeuCau } from '../tienIch'
import { ViTriCuaToi } from './viTriCuaToi'

// M03 Chi tiết ca làm: thông tin công khai của ca, ảnh, vùng trên bản đồ và phần đăng ký tham gia.
export default function ChiTietCa() {
  const { ma } = useParams()
  const { nguoiDung, laTnv } = useNguoiDung()
  const ca = useTai(() => api.get('/api/ca/' + ma), [ma])
  const cuaToi = useTai(async () => {
    if (!laTnv) return null
    const [hs, dk] = await Promise.all([api.get('/api/ho-so'), api.get('/api/dang-ky/cua-toi')])
    return {
      hoSo: hs,
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
  const hoSo = cuaToi.duLieu?.hoSo
  const thieu = hoSo ? conThieu(c.yeuCau, hoSo) : []
  const dk = cuaToi.duLieu?.dangKy
  const viTri = hoSo?.viDo != null ? { viDo: hoSo.viDo, kinhDo: hoSo.kinhDo } : null

  return (
    <>
      {duongDan}
      <section className="the">
        <h1>{c.tieuDe}</h1>
        <p className="mo">Do {c.tenToChuc} đăng</p>
        <LuoiAnh ds={c.anh.map((m) => ({ khoa: m, src: '/api/anh/nhu-cau/' + m, ten: c.tieuDe }))} />
        <p>{c.tomTatCongKhai}</p>
        <dl className="thong-tin">
          <dt>Thời gian</dt><dd>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</dd>
          <dt>Khu vực</dt><dd>{c.khuVucChung}<ViTriCuaToi viTri={viTri} vung={c} /></dd>
          <dt>Số người</dt><dd>{hienSoCho(c.soNguoiCan, c.soDaPhanCong)}</dd>
          <dt>Kỹ năng cần có</dt><dd>{c.yeuCau.map((y) => <span key={y.maKyNang} className="nhan nhan-ky-nang">{hienYeuCau(y)}</span>)}</dd>
        </dl>
        {c.viDoVung != null && (
          <>
            <BanDoVung cao={260} vung={[{ viDo: c.viDoVung, kinhDo: c.kinhDoVung, banKinh: c.banKinhVung, nhan: 'Khu vực gần đúng' }]} viTriCuaToi={viTri} nhanViTriCuaToi="Nơi ở bạn đã lưu" />
            <p className="chu-thich-ban-do">Vòng tròn là khu vực gần đúng, bán kính khoảng {(c.banKinhVung / 1000).toLocaleString('vi-VN')} km. Địa điểm cụ thể không nằm ở tâm vòng tròn.{viTri && ' Ghim xanh là nơi ở bạn đã lưu, chỉ bạn thấy.'}</p>
          </>
        )}
        <p className="mo">Địa chỉ cụ thể và hướng dẫn chi tiết chỉ hiện cho người đã được phân công vào ca.</p>
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
            {dk.trangThai === 'CHO_DUYET' && <p className="mo">Bạn có chỗ trong ca khi được điều phối viên phân công. Bạn sẽ nhận được thông báo.</p>}
            <div className="hang-nut">
              {dk.trangThai === 'DA_PHAN_CONG' && <Link className="nut" to={`/ca/${ma}/huong-dan-rieng`}>Xem hướng dẫn riêng</Link>}
              <Link className="nut phu" to="/dang-ky-cua-toi">Xem đăng ký của tôi</Link>
            </div>
          </>
        )}
        {laTnv && cuaToi.duLieu && !dk && thieu.length > 0 && (
          <>
            <div className="thong-bao canh-bao">
              <p>Bạn chưa đủ điều kiện kỹ năng của ca này:</p>
              <ul style={{ margin: '4px 0 0' }}>{thieu.map((t) => <li key={t}>{t}</li>)}</ul>
            </div>
            <Link className="nut" to="/ho-so">Cập nhật kỹ năng</Link>
          </>
        )}
        {laTnv && cuaToi.duLieu && !dk && thieu.length === 0 && (
          <>
            <p>Bạn có đủ kỹ năng mà ca yêu cầu.</p>
            <button className="lon" onClick={dangKy} disabled={tt.dangGui}>Đăng ký tham gia</button>
          </>
        )}
      </section>
    </>
  )
}
