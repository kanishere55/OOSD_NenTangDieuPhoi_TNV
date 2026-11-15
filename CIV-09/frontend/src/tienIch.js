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
  DA_XAC_NHAN: 'Đã xác nhận',
  CHO_XAC_MINH: 'Chờ xác minh',
  DA_XAC_MINH: 'Đã xác minh',
  HET_HIEU_LUC: 'Hết hiệu lực',
  DA_DUYET: 'Đã duyệt',
  CO_MAT: 'Có mặt',
  VANG_MAT: 'Vắng mặt'
}

// Ghi chú đi kèm trạng thái ở những chỗ tên trạng thái chưa đủ rõ.
export const GIAI_THICH_NHU_CAU = {
  NHAP: 'Chỉ tổ chức nhìn thấy.'
}

export const GIAI_THICH_DANG_KY = {
  CHO_DUYET: 'Bạn chưa có chỗ trong ca.',
  DA_PHAN_CONG: 'Bạn đã có chỗ trong ca.',
  DA_HUY: 'Ca bị hủy, bạn bị hủy phân công, hoặc đăng ký chưa được duyệt khi ca bắt đầu.'
}

export const GIAI_THICH_DONG_GOP = {
  CHO_DUYET: 'Điều phối viên chưa duyệt.',
  DA_XAC_NHAN: 'Đã cộng vào tổng giờ.'
}

export const TEN_VAI_TRO = { TNV: 'Tình nguyện viên', DAI_DIEN: 'Đại diện cộng đồng', DIEU_PHOI: 'Điều phối viên', QUAN_TRI: 'Quản trị viên' }

// Cấp độ kỹ năng (BR18).
export const CAP_DO = { 1: 'Cơ bản', 2: 'Khá', 3: 'Thành thạo' }
export const GIAI_THICH_CAP_DO = {
  1: 'Đã biết, làm được việc đơn giản khi có người hướng dẫn',
  2: 'Làm được độc lập phần lớn công việc',
  3: 'Làm thành thạo, hướng dẫn được người khác'
}

// "Sơ cứu (khá trở lên, cần chứng chỉ đã xác minh)". Cấp cơ bản không cần ghi thêm.
export function hienYeuCau(y) {
  const ghiChu = []
  if (y.capDoToiThieu > 1) ghiChu.push(`${CAP_DO[y.capDoToiThieu].toLowerCase()} trở lên`)
  if (y.canXacMinh) ghiChu.push('cần chứng chỉ đã xác minh')
  return ghiChu.length ? `${y.tenKyNang} (${ghiChu.join(', ')})` : y.tenKyNang
}

// Thứ trong tuần (1 là thứ hai) và buổi trong ngày dùng cho lịch rảnh.
export const THU_TRONG_TUAN = ['Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy', 'Chủ nhật']
export const BUOI = [
  { ma: 'SANG', ten: 'Sáng', gio: 'trước 12 giờ' },
  { ma: 'CHIEU', ten: 'Chiều', gio: '12 đến 18 giờ' },
  { ma: 'TOI', ten: 'Tối', gio: 'từ 18 giờ' }
]

export const hienKm = (km) => km == null ? '' : km < 1 ? 'dưới 1 km' : `khoảng ${Number(km).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} km`

// Đường dẫn chỉ đường của Google Maps tới một điểm; mở được trên máy tính và ứng dụng Google Maps trên điện thoại.
export const duongDanChiDuong = (viDo, kinhDo) => `https://www.google.com/maps/dir/?api=1&destination=${viDo},${kinhDo}`

export const TEN_TRANG_THAI_TO_CHUC = {
  CHO_DUYET: 'Chờ duyệt',
  HOAT_DONG: 'Đang hoạt động',
  TU_CHOI: 'Không được duyệt',
  NGUNG_HOAT_DONG: 'Ngừng hoạt động'
}

export const TEN_LOAI_DU_LIEU = {
  HUONG_DAN_RIENG: 'Hướng dẫn riêng',
  ANH_MINH_CHUNG: 'Ảnh minh chứng',
  CHUNG_CHI: 'Ảnh chứng chỉ'
}

// "HH:mm" theo giờ Việt Nam.
export function hienGioPhut(utc) {
  if (!utc) return ''
  const t = theoGioVn(utc)
  return `${hai(t.gio)}:${hai(t.phut)}`
}

// Ngày dạng dd/mm/yyyy cho giá trị ngày không có giờ ("2026-11-20").
export function hienNgayThuong(ngay) {
  if (!ngay) return ''
  const [n, t, d] = ngay.split('-').reverse()
  return `${n}/${t}/${d}`
}

// Đổi ngày người dùng gõ (20/11/2026) thành "2026-11-20" để gửi lên máy chủ.
export function ngayGuiDi(chuoi) {
  const n = docNgay(chuoi)
  return n ? `${n.nam}-${hai(n.thang)}-${hai(n.ngay)}` : null
}

export const soGio = (n) => Number(n).toLocaleString('vi-VN', { maximumFractionDigits: 2 })

// Số chỗ còn lại của ca; ca đủ người vẫn nhận đăng ký chờ duyệt (BR08).
export function hienSoCho(soNguoiCan, soDaPhanCong) {
  const con = soNguoiCan - soDaPhanCong
  return con > 0 ? `Cần ${soNguoiCan} người, còn ${con} chỗ` : `Cần ${soNguoiCan} người, đã đủ (vẫn nhận đăng ký dự bị)`
}
