import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { TEN_TRANG_THAI, docNgay } from './tienIch'

export const NguoiDungContext = createContext({ nguoiDung: null, daTai: false })
export const useNguoiDung = () => useContext(NguoiDungContext)

// Tải dữ liệu bất đồng bộ, trả về dữ liệu, lỗi và hàm tải lại.
export function useTai(hamTai, phuThuoc = []) {
  const [duLieu, setDuLieu] = useState(null)
  const [loi, setLoi] = useState(null)
  const [dangTai, setDangTai] = useState(true)
  const taiLai = useCallback(async () => {
    setDangTai(true)
    try {
      setDuLieu(await hamTai())
      setLoi(null)
    } catch (e) {
      setLoi(e.message)
    } finally {
      setDangTai(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, phuThuoc)
  useEffect(() => { taiLai() }, [taiLai])
  return { duLieu, loi, dangTai, taiLai }
}

// Chạy một thao tác gửi lên máy chủ và giữ thông báo kết quả.
export function useThaoTac() {
  const [loi, setLoi] = useState(null)
  const [thanhCong, setThanhCong] = useState(null)
  const [dangGui, setDangGui] = useState(false)
  const chay = async (hamGui, thongBaoThanhCong) => {
    setDangGui(true)
    setLoi(null)
    setThanhCong(null)
    try {
      const kq = await hamGui()
      if (thongBaoThanhCong) setThanhCong(thongBaoThanhCong)
      return kq ?? true
    } catch (e) {
      setLoi(e.message)
      return null
    } finally {
      setDangGui(false)
    }
  }
  return { loi, thanhCong, dangGui, chay, setLoi, setThanhCong }
}

export function TrangThai({ ma }) {
  return <span className={'nhan nhan-' + (ma || '').toLowerCase()}>{TEN_TRANG_THAI[ma] ?? ma}</span>
}

// Thông báo kết quả; tự cuộn vào tầm nhìn để người dùng không bỏ sót.
export function ThongBao({ loi, thanhCong }) {
  const o = useRef(null)
  useEffect(() => {
    if ((loi || thanhCong) && o.current) o.current.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
  }, [loi, thanhCong])
  if (loi) return <div ref={o} className="thong-bao loi" role="alert"><strong>Chưa thực hiện được.</strong> {loi}</div>
  if (thanhCong) return <div ref={o} className="thong-bao thanh-cong" role="status">{thanhCong}</div>
  return null
}

export function DangTai() {
  return <p className="mo">Đang tải dữ liệu...</p>
}

// Đường dẫn "bạn đang ở đâu" đặt đầu trang: [{ ten, den }], mục cuối là trang hiện tại.
export function DuongDan({ muc }) {
  return (
    <nav className="duong-dan" aria-label="Vị trí trang">
      {muc.map((m, i) => (
        <span key={i}>
          {i > 0 && <span className="ngan" aria-hidden="true">›</span>}
          {m.den ? <Link to={m.den}>{m.ten}</Link> : <span className="hien-tai">{m.ten}</span>}
        </span>
      ))}
    </nav>
  )
}

// Tiêu đề trang kèm một câu cho biết trang này dùng để làm gì.
export function DauTrang({ tieuDe, moTa, children }) {
  return (
    <div className="dau-muc">
      <div>
        <h1>{tieuDe}</h1>
        {moTa && <p className="mo-ta">{moTa}</p>}
      </div>
      {children && <div className="hang-nut">{children}</div>}
    </div>
  )
}

// Khung hiện khi danh sách chưa có gì, kèm gợi ý việc nên làm.
export function Trong({ children }) {
  return <div className="trong">{children}</div>
}

// Hộp hỏi lại trước các thao tác không hoàn tác được (rút, hủy, từ chối...).
export function HopXacNhan({ tieuDe, children, nhanDongY, nguyHiem = true, dangGui, onDongY, onThoi }) {
  const nutThoi = useRef(null)
  useEffect(() => {
    nutThoi.current?.focus()
    const phim = (e) => { if (e.key === 'Escape') onThoi() }
    window.addEventListener('keydown', phim)
    return () => window.removeEventListener('keydown', phim)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])
  return (
    <div className="nen-hop" onClick={(e) => { if (e.target === e.currentTarget) onThoi() }}>
      <div className="hop" role="dialog" aria-modal="true" aria-label={tieuDe}>
        <h2>{tieuDe}</h2>
        <div className="noi-dung-hop">{children}</div>
        <div className="hang-nut cuoi-hop">
          <button ref={nutThoi} className="phu" disabled={dangGui} onClick={onThoi}>Không, quay lại</button>
          <button className={nguyHiem ? 'nguy-hiem' : ''} disabled={dangGui} onClick={onDongY}>{nhanDongY}</button>
        </div>
      </div>
    </div>
  )
}

const GIO = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'))
const PHUT = Array.from({ length: 12 }, (_, i) => String(i * 5).padStart(2, '0'))

// Ô chọn ngày và giờ theo giờ Việt Nam: ngày gõ dạng ngày/tháng/năm, giờ và phút chọn từ danh sách.
// giaTri = { ngay: "17/11/2026", gio: "08", phut: "00" }
export function ChonNgayGio({ nhan, giaTri, onDoi, loi, tatDi, goiY }) {
  const phut = PHUT.includes(giaTri.phut) || giaTri.phut === '' ? PHUT : [...PHUT, giaTri.phut].sort()
  const saiNgay = giaTri.ngay && !docNgay(giaTri.ngay)
  return (
    <fieldset className="ngay-gio" disabled={tatDi}>
      <legend>{nhan}</legend>
      <div className="hang-ngay-gio">
        <label>Ngày
          <input value={giaTri.ngay} placeholder="ví dụ 20/11/2026" inputMode="numeric"
            onChange={(e) => onDoi({ ...giaTri, ngay: e.target.value })} />
        </label>
        <label>Giờ
          <select value={giaTri.gio} onChange={(e) => onDoi({ ...giaTri, gio: e.target.value })}>
            <option value="">--</option>
            {GIO.map((g) => <option key={g} value={g}>{g} giờ</option>)}
          </select>
        </label>
        <label>Phút
          <select value={giaTri.phut} onChange={(e) => onDoi({ ...giaTri, phut: e.target.value })}>
            <option value="">--</option>
            {phut.map((p) => <option key={p} value={p}>{p} phút</option>)}
          </select>
        </label>
      </div>
      {goiY && <small className="goi-y-o">{goiY}</small>}
      {saiNgay && !loi && <small className="loi-o">Ngày chưa đúng. Hãy gõ theo dạng ngày/tháng/năm, ví dụ 20/11/2026.</small>}
      {loi && <small className="loi-o">{loi}</small>}
    </fieldset>
  )
}

// Ô gõ ngày cho bộ lọc.
export function ONgay({ nhan, giaTri, onDoi }) {
  const sai = giaTri && !docNgay(giaTri)
  return (
    <label>{nhan}
      <input value={giaTri} placeholder="ngày/tháng/năm" inputMode="numeric" onChange={(e) => onDoi(e.target.value)} />
      {sai && <small className="loi-o">Gõ dạng 20/11/2026</small>}
    </label>
  )
}
