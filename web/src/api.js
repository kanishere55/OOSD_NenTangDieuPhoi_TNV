// Gọi API của máy chủ ứng dụng. Phiên đăng nhập nằm trong cookie nên không cần gửi token.

export class LoiApi extends Error {
  constructor(maHttp, thongBao) {
    super(thongBao)
    this.maHttp = maHttp
  }
}

async function goi(phuongThuc, duongDan, duLieu) {
  const r = await fetch(duongDan, {
    method: phuongThuc,
    credentials: 'same-origin',
    headers: duLieu ? { 'Content-Type': 'application/json' } : {},
    body: duLieu ? JSON.stringify(duLieu) : undefined
  })
  if (r.status === 204) return null
  const vanBan = await r.text()
  const json = vanBan ? JSON.parse(vanBan) : null
  if (!r.ok) {
    const thongBao = json?.loi ?? (r.status === 401 ? 'Bạn cần đăng nhập.' : `Lỗi ${r.status}`)
    throw new LoiApi(r.status, thongBao)
  }
  return json
}

export const api = {
  get: (d) => goi('GET', d),
  post: (d, dl) => goi('POST', d, dl ?? {}),
  put: (d, dl) => goi('PUT', d, dl)
}

export const dangNhap = (quayVe) => {
  window.location.href = '/api/auth/dang-nhap?quayVe=' + encodeURIComponent(quayVe || '/')
}

export const dangXuat = () => {
  window.location.href = '/api/auth/dang-xuat'
}
