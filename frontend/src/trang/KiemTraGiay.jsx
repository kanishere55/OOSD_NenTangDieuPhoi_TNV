import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, useTai } from '../chung'
import { hienNgay, hienNgayThuong, soGio } from '../tienIch'

// M22 Kiểm tra giấy xác nhận: ai có mã đều kiểm tra được giấy là thật, không cần đăng nhập (BR29).
export default function KiemTraGiay() {
  const { ma } = useParams()
  const dieuHuong = useNavigate()
  const [maNhap, setMaNhap] = useState(ma ?? '')
  const kq = useTai(() => ma ? api.get('/api/kiem-tra-giay/' + encodeURIComponent(ma)) : Promise.resolve(null), [ma])
  return (
    <section className="the hep">
      <DauTrang tieuDe="Kiểm tra giấy xác nhận" moTa="Nhập mã kiểm tra in trên giấy xác nhận giờ tình nguyện." />
      <form className="hang-tim" onSubmit={(e) => { e.preventDefault(); if (maNhap.trim()) dieuHuong('/kiem-tra/' + maNhap.trim().toUpperCase()) }}>
        <input className="ma-chu" value={maNhap} maxLength={12} aria-label="Mã kiểm tra" placeholder="Mã 10 ký tự" onChange={(e) => setMaNhap(e.target.value.toUpperCase())} />
        <button type="submit">Kiểm tra</button>
      </form>
      {ma && kq.dangTai && <DangTai />}
      {ma && kq.loi && <div className="thong-bao loi" role="alert" style={{ marginTop: 16 }}>Không tìm thấy giấy xác nhận với mã {ma}. Hãy kiểm tra lại mã.</div>}
      {kq.duLieu && (
        <div className="thong-bao thanh-cong" style={{ marginTop: 16 }}>
          <p><strong>Giấy xác nhận hợp lệ.</strong></p>
          <dl className="thong-tin">
            <dt>Người tham gia</dt><dd>{kq.duLieu.tenNguoiNhan}</dd>
            <dt>Tổng giờ đã xác nhận</dt><dd>{soGio(kq.duLieu.tongGio)} giờ, {kq.duLieu.soCa} ca</dd>
            <dt>Khoảng thời gian</dt><dd>{!kq.duLieu.tuNgay && !kq.duLieu.denNgay ? 'Toàn bộ thời gian tham gia'
              : `${kq.duLieu.tuNgay ? hienNgayThuong(kq.duLieu.tuNgay) : '...'} đến ${kq.duLieu.denNgay ? hienNgayThuong(kq.duLieu.denNgay) : 'ngày cấp'}`}</dd>
            <dt>Tổ chức xác nhận</dt><dd>{kq.duLieu.toChuc.join(', ')}</dd>
            <dt>Ngày cấp</dt><dd>{hienNgay(kq.duLieu.ngayCap)}</dd>
          </dl>
        </div>
      )}
    </section>
  )
}
