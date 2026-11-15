// So kỹ năng của tình nguyện viên với yêu cầu của ca (BR18). hoSo lấy từ /api/ho-so.
// Trả về danh sách điều còn thiếu, rỗng nghĩa là đủ điều kiện.
export function conThieu(yeuCau, hoSo) {
  const cua = Object.fromEntries((hoSo?.kyNang ?? []).map((k) => [k.maKyNang, k]))
  const thieu = []
  for (const y of yeuCau) {
    const k = cua[y.maKyNang]
    if (!k) thieu.push(`chưa có kỹ năng ${y.tenKyNang}`)
    else if (k.capDo < y.capDoToiThieu) thieu.push(`${y.tenKyNang} cần mức ${['', 'cơ bản', 'khá', 'thành thạo'][y.capDoToiThieu]} trở lên`)
    else if (y.canXacMinh && !k.daXacMinh) {
      thieu.push(k.chungChiDangCho
        ? `${y.tenKyNang} cần chứng chỉ đã được xác minh. Chứng chỉ bạn gửi đang chờ xác minh`
        : `${y.tenKyNang} cần chứng chỉ đã được xác minh. Hãy gửi ảnh chứng chỉ ở trang Hồ sơ`)
    }
  }
  return thieu
}
