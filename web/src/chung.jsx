import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import { TEN_TRANG_THAI } from './tienIch'

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

export function TrangThai({ ma }) {
  return <span className={'nhan nhan-' + (ma || '').toLowerCase()}>{TEN_TRANG_THAI[ma] ?? ma}</span>
}

export function ThongBao({ loi, thanhCong }) {
  if (loi) return <div className="thong-bao loi" role="alert">{loi}</div>
  if (thanhCong) return <div className="thong-bao thanh-cong" role="status">{thanhCong}</div>
  return null
}

export function DangTai() {
  return <p className="mo">Đang tải...</p>
}

// Chạy một thao tác gửi lên máy chủ và hiện thông báo kết quả.
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
