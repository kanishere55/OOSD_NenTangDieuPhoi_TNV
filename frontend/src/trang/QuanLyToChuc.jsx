import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, HopXacNhan, TheMuc, ThongBao, Trong, useTai, useThaoTac } from '../chung'
import { TEN_TRANG_THAI_TO_CHUC, TEN_VAI_TRO } from '../tienIch'
import KyNangDangThieu from './KyNangDangThieu'
import NhatKyTruyCap from './NhatKyTruyCap'

// M18 Tổ chức: đại diện quản lý thành viên, xem kỹ năng đang thiếu và nhật ký xem dữ liệu nhạy cảm (BR26, BR28).
export default function QuanLyToChuc() {
  const { ma } = useParams()
  const [muc, setMuc] = useState('thanh-vien')
  const tv = useTai(() => api.get(`/api/to-chuc/${ma}/thanh-vien`), [ma])
  if (tv.dangTai && !tv.duLieu) return <DangTai />
  if (tv.loi) return <ThongBao loi={tv.loi} />
  const t = tv.duLieu
  return (
    <>
      <DauTrang tieuDe={t.tenToChuc} moTa={`Tổ chức ${TEN_TRANG_THAI_TO_CHUC[t.trangThai].toLowerCase()}.`} />
      <TheMuc dangChon={muc} onChon={setMuc} ds={[
        { ma: 'thanh-vien', ten: 'Thành viên' },
        { ma: 'ky-nang', ten: 'Kỹ năng đang thiếu' },
        { ma: 'nhat-ky', ten: 'Nhật ký truy cập' }
      ]} />
      {muc === 'thanh-vien' && <ThanhVien t={t} taiLai={tv.taiLai} />}
      {muc === 'ky-nang' && <KyNangDangThieu maToChuc={ma} />}
      {muc === 'nhat-ky' && <NhatKyTruyCap duongDan={`/api/to-chuc/${ma}/nhat-ky`} />}
    </>
  )
}

function ThanhVien({ t, taiLai }) {
  const [form, setForm] = useState({ tenDangNhap: '', vaiTro: 'DIEU_PHOI' })
  const [hoi, setHoi] = useState(null)
  const tt = useThaoTac()
  const them = async (e) => {
    e.preventDefault()
    if (!form.tenDangNhap.trim()) { tt.setLoi('Hãy nhập tên đăng nhập hoặc email.'); return }
    const ok = await tt.chay(() => api.post(`/api/to-chuc/${t.maToChuc}/thanh-vien`, form),
      `Đã thêm ${form.tenDangNhap.trim()} làm ${TEN_VAI_TRO[form.vaiTro].toLowerCase()}. Người này nhận được thông báo.`)
    if (ok) { setForm({ ...form, tenDangNhap: '' }); taiLai() }
  }
  const ngung = async () => {
    const m = hoi
    const ok = await tt.chay(() => api.post(`/api/to-chuc/${t.maToChuc}/thanh-vien/${m.maNguoiDung}/ngung?vaiTro=${m.vaiTro}`),
      `${m.tenHienThi} không còn là ${TEN_VAI_TRO[m.vaiTro].toLowerCase()} của tổ chức.`)
    setHoi(null)
    if (ok) taiLai()
  }
  const dangHoatDong = t.thanhVien.filter((m) => m.dangHoatDong)
  const daNgung = t.thanhVien.filter((m) => !m.dangHoatDong)
  return (
    <>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      <h2 style={{ marginTop: 0 }}>Thành viên đang hoạt động</h2>
      {dangHoatDong.length === 0 ? <Trong><p>Chưa có thành viên.</p></Trong> : (
        <ul className="ds-dong">
          {dangHoatDong.map((m) => (
            <li key={m.maNguoiDung + m.vaiTro}>
              <div className="than">
                <p><strong>{m.tenHienThi}</strong> · {TEN_VAI_TRO[m.vaiTro]}</p>
                <p className="mo">Tên đăng nhập: {m.tenDangNhap ?? 'chưa rõ'}{!m.taiKhoanHoatDong && ' · tài khoản đang bị khóa'}</p>
              </div>
              {(m.vaiTro === 'DIEU_PHOI' || t.laQuanTri) && (
                <div className="hang-nut"><button className="phu nguy-hiem" onClick={() => setHoi(m)}>Ngừng vai trò</button></div>
              )}
            </li>
          ))}
        </ul>
      )}
      <form className="the hep" onSubmit={them}>
        <h2 style={{ marginTop: 0 }}>Thêm điều phối viên</h2>
        <p className="mo">Người được thêm cần tạo tài khoản và đăng nhập vào nền tảng ít nhất một lần. Sau đó nhập tên đăng nhập hoặc email của họ ở đây.</p>
        <label>Tên đăng nhập hoặc email
          <input value={form.tenDangNhap} onChange={(e) => setForm({ ...form, tenDangNhap: e.target.value })} />
        </label>
        {t.laQuanTri && (
          <label style={{ maxWidth: 300 }}>Vai trò
            <select value={form.vaiTro} onChange={(e) => setForm({ ...form, vaiTro: e.target.value })}>
              <option value="DIEU_PHOI">Điều phối viên</option>
              <option value="DAI_DIEN">Đại diện</option>
            </select>
          </label>
        )}
        <button type="submit" disabled={tt.dangGui}>Thêm vào tổ chức</button>
      </form>
      {daNgung.length > 0 && (
        <>
          <h2>Đã ngừng</h2>
          <ul className="ds-dong">
            {daNgung.map((m) => <li key={m.maNguoiDung + m.vaiTro}><div className="than"><p>{m.tenHienThi} · {TEN_VAI_TRO[m.vaiTro]}</p></div></li>)}
          </ul>
        </>
      )}
      {hoi && (
        <HopXacNhan tieuDe="Ngừng vai trò của người này?" nhanDongY="Ngừng vai trò" dangGui={tt.dangGui} onDongY={ngung} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.tenHienThi}</strong> sẽ không còn là {TEN_VAI_TRO[hoi.vaiTro].toLowerCase()} của tổ chức và không xem được dữ liệu của tổ chức nữa.</p>
          <p>Lịch sử ca, phân công và duyệt đóng góp của người này vẫn được giữ. Bạn có thể thêm lại sau.</p>
        </HopXacNhan>
      )}
    </>
  )
}
