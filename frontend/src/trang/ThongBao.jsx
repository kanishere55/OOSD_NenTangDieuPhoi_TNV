import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao as HienThongBao, Trong, useNguoiDung, useTai, useThaoTac } from '../chung'
import { hienGio } from '../tienIch'

// M15 Thông báo: những việc liên quan tới bạn (được phân công, ca đổi giờ, bản khai được duyệt...). Bấm để mở đúng chỗ (BR25).
export default function ThongBao() {
  const ds = useTai(() => api.get('/api/thong-bao'), [])
  const { demThongBao } = useNguoiDung()
  const dieuHuong = useNavigate()
  const tt = useThaoTac()

  const mo = async (t) => {
    if (!t.daDoc) { try { await api.post(`/api/thong-bao/${t.maThongBao}/da-doc`) } catch { /* vẫn mở trang */ } demThongBao?.() }
    if (t.duongDan) dieuHuong(t.duongDan); else ds.taiLai()
  }
  const docHet = async () => {
    if (await tt.chay(() => api.post('/api/thong-bao/da-doc-het'))) { ds.taiLai(); demThongBao?.() }
  }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <HienThongBao loi={ds.loi} />
  const d = ds.duLieu
  return (
    <>
      <DauTrang tieuDe="Thông báo" moTa="Bấm vào một thông báo để mở đúng trang cần xem.">
        {d.soChuaDoc > 0 && <button className="phu" onClick={docHet} disabled={tt.dangGui}>Đánh dấu đã đọc tất cả</button>}
      </DauTrang>
      <HienThongBao loi={tt.loi} />
      {d.danhSach.length === 0 ? <Trong><p>Bạn chưa có thông báo nào.</p></Trong> : (
        <ul className="ds-dong">
          {d.danhSach.map((t) => (
            <li key={t.maThongBao} className={'dong-thong-bao' + (t.daDoc ? '' : ' chua-doc')} onClick={() => mo(t)}>
              <div className="than">
                <p>{!t.daDoc && <strong>Mới · </strong>}{t.noiDung}</p>
                <p className="thoi-gian">{hienGio(t.ngayTao)}</p>
              </div>
              {t.duongDan && <div className="hang-nut"><button className="phu nho" onClick={(e) => { e.stopPropagation(); mo(t) }}>Mở</button></div>}
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
