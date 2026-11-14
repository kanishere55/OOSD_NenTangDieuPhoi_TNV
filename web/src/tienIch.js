// Máy chủ lưu và trả về giờ UTC; giao diện hiển thị và nhận giờ Việt Nam (UTC+07:00).
const VUNG_GIO = 'Asia/Ho_Chi_Minh'
const THU = ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy']
const hai = (n) => String(n).padStart(2, '0')

// Các thành phần ngày giờ theo giờ Việt Nam của một thời điểm UTC.
function theoGioVn(utc) {
  const d = new Date(new Date(utc).getTime() + 7 * 3600 * 1000)
  return {
    thu: THU[d.getUTCDay()], ngay: d.getUTCDate(), thang: d.getUTCMonth() + 1, nam: d.getUTCFullYear(),
    gio: d.getUTCHours(), phut: d.getUTCMinutes()
  }
}

export function hienNgay(utc) {
  if (!utc) return ''
  const t = theoGioVn(utc)
  return `${t.thu}, ${hai(t.ngay)}/${hai(t.thang)}/${t.nam}`
}

export function hienGio(utc) {
  if (!utc) return ''
  const t = theoGioVn(utc)
  return `${hai(t.gio)}:${hai(t.phut)} ngày ${hai(t.ngay)}/${hai(t.thang)}/${t.nam}`
}

// "Thứ ba, 17/11/2026, từ 08:00 đến 10:00". Ca kéo dài qua ngày thì ghi đủ ngày kết thúc.
export function hienKhoangGio(batDau, ketThuc) {
  const a = theoGioVn(batDau), b = theoGioVn(ketThuc)
  const dau = `${a.thu}, ${hai(a.ngay)}/${hai(a.thang)}/${a.nam}, từ ${hai(a.gio)}:${hai(a.phut)}`
  if (a.ngay === b.ngay && a.thang === b.thang && a.nam === b.nam) return `${dau} đến ${hai(b.gio)}:${hai(b.phut)}`
  return `${dau} đến ${hai(b.gio)}:${hai(b.phut)} ngày ${hai(b.ngay)}/${hai(b.thang)}/${b.nam}`
}

// Tách thời điểm UTC thành giá trị cho ô chọn ngày giờ: { ngay: "17/11/2026", gio: "08", phut: "00" }.
export function tachNgayGio(utc) {
  if (!utc) return { ngay: '', gio: '', phut: '' }
  const t = theoGioVn(utc)
  return { ngay: `${hai(t.ngay)}/${hai(t.thang)}/${t.nam}`, gio: hai(t.gio), phut: hai(t.phut) }
}

// Đọc ngày người dùng gõ ("17/11/2026", "17-11-2026", "7/1/2027"). Trả về null nếu không phải một ngày có thật.
export function docNgay(chuoi) {
  const m = /^\s*(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{4})\s*$/.exec(chuoi || '')
  if (!m) return null
  const ngay = Number(m[1]), thang = Number(m[2]), nam = Number(m[3])
  const d = new Date(Date.UTC(nam, thang - 1, ngay))
  if (d.getUTCFullYear() !== nam || d.getUTCMonth() !== thang - 1 || d.getUTCDate() !== ngay) return null
  return { ngay, thang, nam }
}

// Ghép giá trị ô chọn ngày giờ (giờ Việt Nam) thành chuỗi UTC gửi lên máy chủ. Thiếu hoặc sai thì trả về null.
export function ghepNgayGio(o) {
  const n = docNgay(o?.ngay)
  if (!n || o.gio === '' || o.phut === '') return null
  return new Date(Date.UTC(n.nam, n.thang - 1, n.ngay, Number(o.gio) - 7, Number(o.phut))).toISOString()
}

// Đầu ngày và cuối ngày (giờ Việt Nam) của một ngày người dùng gõ, dùng cho bộ lọc tìm ca.
export function dauNgayUtc(chuoi) {
  const n = docNgay(chuoi)
  return n ? new Date(Date.UTC(n.nam, n.thang - 1, n.ngay, -7, 0)).toISOString() : null
}
export function cuoiNgayUtc(chuoi) {
  const n = docNgay(chuoi)
  return n ? new Date(Date.UTC(n.nam, n.thang - 1, n.ngay + 1, -7, 0)).toISOString() : null
}

export const TEN_TRANG_THAI = {
  NHAP: 'Bản nháp',
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

// Câu giải thích đi kèm trạng thái, để người dùng biết điều gì đang xảy ra và việc tiếp theo.
export const GIAI_THICH_NHU_CAU = {
  NHAP: 'Chưa ai ngoài tổ chức nhìn thấy. Cần rà soát rồi công bố.',
  CONG_BO: 'Điều phối viên có thể tạo ca làm cho nhu cầu này.',
  DONG: 'Các ca đã xong, nhu cầu đã khép lại.',
  DA_HUY: 'Nhu cầu đã hủy, không tạo thêm ca.'
}

export const GIAI_THICH_DANG_KY = {
  CHO_DUYET: 'Điều phối viên chưa duyệt. Bạn chưa có chỗ trong ca.',
  DA_PHAN_CONG: 'Bạn đã có chỗ trong ca.',
  TU_CHOI: 'Điều phối viên không chọn đăng ký này. Bạn có thể tìm ca khác.',
  DA_RUT: 'Bạn đã rút đăng ký này.',
  DA_HUY: 'Đăng ký không còn hiệu lực: ca bị hủy, bạn bị hủy phân công, hoặc đến giờ bắt đầu mà đăng ký chưa được duyệt.'
}

export const GIAI_THICH_DONG_GOP = {
  CHO_DUYET: 'Điều phối viên chưa xem bản khai.',
  CAN_BO_SUNG: 'Điều phối viên trả lại để bạn sửa.',
  DA_XAC_NHAN: 'Số giờ đã được cộng vào tổng giờ của bạn.'
}

export const TEN_VAI_TRO = { TNV: 'Tình nguyện viên', DAI_DIEN: 'Đại diện cộng đồng', DIEU_PHOI: 'Điều phối viên' }

export const soGio = (n) => Number(n).toLocaleString('vi-VN', { maximumFractionDigits: 2 })

// Số chỗ còn lại của ca; ca đủ người vẫn nhận đăng ký chờ duyệt (BR08).
export function hienSoCho(soNguoiCan, soDaPhanCong) {
  const con = soNguoiCan - soDaPhanCong
  return con > 0 ? `Cần ${soNguoiCan} người, còn ${con} chỗ` : `Cần ${soNguoiCan} người, đã đủ (vẫn nhận đăng ký dự bị)`
}
