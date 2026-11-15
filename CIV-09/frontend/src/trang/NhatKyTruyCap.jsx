import { useState } from 'react'
import { api } from '../api'
import { DangTai, ThongBao, Trong, useTai } from '../chung'
import { TEN_LOAI_DU_LIEU, hienGio } from '../tienIch'

// Bảng nhật ký xem dữ liệu nhạy cảm (BR26). duongDan: API trả nhật ký (của một tổ chức hoặc toàn nền tảng).
export default function NhatKyTruyCap({ duongDan, coLoc }) {
  const [trang, setTrang] = useState(1)
  const [loai, setLoai] = useState('')
  const kq = useTai(() => api.get(`${duongDan}?trang=${trang}${loai ? '&loai=' + loai : ''}`), [duongDan, trang, loai])
  if (kq.dangTai && !kq.duLieu) return <DangTai />
  if (kq.loi) return <ThongBao loi={kq.loi} />
  const d = kq.duLieu
  const soTrang = Math.max(1, Math.ceil(d.tongSo / d.kichThuoc))
  return (
    <>
      <p className="mo-ta">Mỗi lần ai đó mở hướng dẫn riêng, ảnh minh chứng hoặc ảnh chứng chỉ đều được ghi lại ở đây, kể cả lần bị từ chối. Nhật ký không sửa, không xóa được.</p>
      {coLoc && (
        <label style={{ maxWidth: 320 }}>Loại dữ liệu
          <select value={loai} onChange={(e) => { setLoai(e.target.value); setTrang(1) }}>
            <option value="">Tất cả</option>
            {Object.entries(TEN_LOAI_DU_LIEU).map(([m, t]) => <option key={m} value={m}>{t}</option>)}
          </select>
        </label>
      )}
      {d.danhSach.length === 0 ? <Trong><p>Chưa có lần xem nào được ghi.</p></Trong> : (
        <div className="bang-cuon">
          <table>
            <thead><tr><th>Thời điểm</th><th>Người xem</th><th>Dữ liệu</th><th>Kết quả</th></tr></thead>
            <tbody>
              {d.danhSach.map((n) => (
                <tr key={n.maNhatKy}>
                  <td>{hienGio(n.thoiDiem)}</td>
                  <td>{n.tenNguoiXem}{n.tenDangNhap && <small className="mo"><br />{n.tenDangNhap}</small>}</td>
                  <td>{TEN_LOAI_DU_LIEU[n.loaiDuLieu]}<small className="mo"><br />{n.tenDoiTuong}</small></td>
                  <td>{n.duocPhep ? 'Được xem' : <strong className="thieu-ky-nang">Bị từ chối</strong>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {soTrang > 1 && (
        <div className="phan-trang">
          <button className="phu" disabled={trang <= 1} onClick={() => setTrang(trang - 1)}>‹ Trang trước</button>
          <span>Trang {trang} trên {soTrang}</span>
          <button className="phu" disabled={trang >= soTrang} onClick={() => setTrang(trang + 1)}>Trang sau ›</button>
        </div>
      )}
    </>
  )
}
