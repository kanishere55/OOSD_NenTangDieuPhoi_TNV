// Hiển thị giờ Việt Nam (UTC+07:00); máy chủ lưu và trả về giờ UTC.
const VUNG_GIO = 'Asia/Ho_Chi_Minh'

export function hienGio(utc) {
  if (!utc) return ''
  return new Date(utc).toLocaleString('vi-VN', {
    timeZone: VUNG_GIO, hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric'
  })
}

export function hienKhoangGio(batDau, ketThuc) {
  const a = new Date(batDau), b = new Date(ketThuc)
  const ngay = a.toLocaleDateString('vi-VN', { timeZone: VUNG_GIO, weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric' })
  const gio = (d) => d.toLocaleTimeString('vi-VN', { timeZone: VUNG_GIO, hour: '2-digit', minute: '2-digit' })
  return `${ngay}, ${gio(a)} - ${gio(b)}`
}

// Giá trị ô datetime-local ("2026-11-20T08:00") được hiểu là giờ Việt Nam, đổi sang chuỗi UTC.
export function gioVnSangUtc(giaTriO) {
  if (!giaTriO) return null
  return new Date(giaTriO + ':00+07:00').toISOString()
}

// Đổi giờ UTC sang giá trị cho ô datetime-local theo giờ Việt Nam.
export function utcSangGioVn(utc) {
  if (!utc) return ''
  const d = new Date(new Date(utc).getTime() + 7 * 3600 * 1000)
  return d.toISOString().slice(0, 16)
}

export const TEN_TRANG_THAI = {
  NHAP: 'Nháp',
  CONG_BO: 'Đã công bố',
  DONG: 'Đã đóng',
  MO: 'Đang mở',
  DANG_DIEN_RA: 'Đang diễn ra',
  DA_KET_THUC: 'Đã kết thúc',
  CHO_DUYET: 'Chờ duyệt',
  DA_PHAN_CONG: 'Đã phân công',
  TU_CHOI: 'Bị từ chối',
  DA_RUT: 'Đã rút',
  DA_HUY: 'Đã hủy',
  CAN_BO_SUNG: 'Cần bổ sung',
  DA_XAC_NHAN: 'Đã xác nhận'
}

export const TEN_VAI_TRO = { TNV: 'Tình nguyện viên', DAI_DIEN: 'Đại diện cộng đồng', DIEU_PHOI: 'Điều phối viên' }

export const soGio = (n) => Number(n).toLocaleString('vi-VN', { maximumFractionDigits: 2 })
