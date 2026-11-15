// Gọi API của máy chủ ứng dụng. Phiên đăng nhập nằm trong cookie nên không cần gửi token.

export class LoiApi extends Error {
  constructor(maHttp, thongBao) {
    super(thongBao)
    this.maHttp = maHttp
  }
}

async function goi(phuongThuc, duongDan, duLieu) {
  let r
  const laBieuMau = duLieu instanceof FormData
  try {
    r = await fetch(duongDan, {
      method: phuongThuc,
      credentials: 'same-origin',
      headers: duLieu && !laBieuMau ? { 'Content-Type': 'application/json' } : {},
      body: laBieuMau ? duLieu : duLieu ? JSON.stringify(duLieu) : undefined
    })
  } catch {
    throw new LoiApi(0, 'Không kết nối được với máy chủ. Hãy kiểm tra mạng rồi thử lại.')
  }
  if (r.status === 204) return null
  const vanBan = await r.text()
  let json = null
  try { json = vanBan ? JSON.parse(vanBan) : null } catch { json = null }
  if (!r.ok) {
    const macDinh = r.status === 401 ? 'Phiên đăng nhập đã hết. Hãy đăng nhập lại.'
      : r.status === 403 ? 'Bạn không có quyền thực hiện thao tác này.'
      : 'Có lỗi xảy ra, thao tác chưa được thực hiện. Hãy thử lại.'
    throw new LoiApi(r.status, json?.loi ?? macDinh)
  }
  return json
}

export const api = {
  get: (d) => goi('GET', d),
  post: (d, dl) => goi('POST', d, dl ?? {}),
  put: (d, dl) => goi('PUT', d, dl),
  // Gửi một tệp ảnh lên máy chủ (trường "tep"). Ảnh được lưu trong CSDL.
  taiTep: (d, tep) => {
    const f = new FormData()
    f.append('tep', tep)
    return goi('POST', d, f)
  }
}

export const dangNhap = (quayVe) => {
  window.location.href = '/api/auth/dang-nhap?quayVe=' + encodeURIComponent(quayVe || '/')
}

// Mở trang tạo tài khoản của Keycloak; tạo xong thì quay lại nền tảng như một lần đăng nhập.
export const dangKyTaiKhoan = (quayVe) => {
  window.location.href = '/api/auth/dang-nhap?taoMoi=true&quayVe=' + encodeURIComponent(quayVe || '/ho-so')
}

export const dangXuat = () => {
  window.location.href = '/api/auth/dang-xuat'
}
