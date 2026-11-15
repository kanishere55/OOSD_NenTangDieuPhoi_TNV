import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, HopLyDo, HopXacNhan, TheMuc, ThongBao, TrangThai, Trong, useNguoiDung, useTai, useThaoTac } from '../chung'
import { TEN_TRANG_THAI_TO_CHUC, TEN_VAI_TRO, hienNgay } from '../tienIch'
import NhatKyTruyCap from './NhatKyTruyCap'
import XacMinhChungChi from './XacMinhChungChi'

// M20 Quản trị: duyệt tổ chức, quản lý danh mục kỹ năng và đề xuất, xác minh chứng chỉ, khóa tài khoản,
// xem nhật ký truy cập (BR17, BR19, BR26, BR28).
export default function QuanTri() {
  const { muc = 'to-chuc' } = useParams()
  const dieuHuong = useNavigate()
  const tq = useTai(() => api.get('/api/quan-tri/tong-quan'), [muc])
  if (tq.loi) return <ThongBao loi={tq.loi} />
  return (
    <>
      <DauTrang tieuDe="Quản trị nền tảng" />
      <TheMuc dangChon={muc} onChon={(m) => dieuHuong('/quan-tri/' + m)} ds={[
        { ma: 'to-chuc', ten: 'Tổ chức', so: tq.duLieu?.toChucChoDuyet },
        { ma: 'ky-nang', ten: 'Kỹ năng', so: tq.duLieu?.deXuatKyNangChoDuyet },
        { ma: 'chung-chi', ten: 'Chứng chỉ', so: tq.duLieu?.chungChiChoXacMinh },
        { ma: 'tai-khoan', ten: 'Tài khoản' },
        { ma: 'nhat-ky', ten: 'Nhật ký truy cập' }
      ]} />
      {muc === 'to-chuc' && <ToChuc capNhat={tq.taiLai} />}
      {muc === 'ky-nang' && <KyNang capNhat={tq.taiLai} />}
      {muc === 'chung-chi' && <XacMinhChungChi nhung />}
      {muc === 'tai-khoan' && <TaiKhoan />}
      {muc === 'nhat-ky' && <NhatKyTruyCap duongDan="/api/quan-tri/nhat-ky" coLoc />}
    </>
  )
}

function ToChuc({ capNhat }) {
  const ds = useTai(() => api.get('/api/quan-tri/to-chuc'), [])
  const [hoi, setHoi] = useState(null)   // { viec, t }
  const tt = useThaoTac()
  const lam = async (viec, t, lyDo) => {
    const thongBao = { duyet: `Đã duyệt ${t.tenToChuc}. ${t.tenNguoiDangKy} là đại diện của tổ chức.`, 'tu-choi': `Đã từ chối ${t.tenToChuc}.`,
      ngung: `${t.tenToChuc} đã ngừng hoạt động.`, 'mo-lai': `${t.tenToChuc} hoạt động trở lại.` }[viec]
    const ok = await tt.chay(() => api.post(`/api/quan-tri/to-chuc/${t.maToChuc}/${viec}`, lyDo ? { lyDo } : {}), thongBao)
    setHoi(null)
    if (ok) { ds.taiLai(); capNhat() }
  }
  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      <p className="mo-ta">Trước khi duyệt, hãy gọi số điện thoại liên hệ để xác minh tổ chức có thật.</p>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      <ul className="ds-dong">
        {ds.duLieu.map((t) => (
          <li key={t.maToChuc} className={t.trangThai === 'CHO_DUYET' ? 'noi-bat' : ''}>
            <div className="than">
              <p><strong>{t.tenToChuc}</strong> <span className={'nhan nhan-' + t.trangThai.toLowerCase()}>{TEN_TRANG_THAI_TO_CHUC[t.trangThai]}</span></p>
              {t.moTa && <p>{t.moTa}</p>}
              <p className="mo">{t.khuVucHoatDong}{t.soDienThoai && ` · Điện thoại: ${t.soDienThoai}`}</p>
              {t.tenNguoiDangKy && <p className="mo">Người đăng ký: {t.tenNguoiDangKy} ({t.tenDangNhapNguoiDangKy}), ngày {hienNgay(t.ngayDangKy)}</p>}
              {t.trangThai !== 'CHO_DUYET' && <p className="mo">{t.soThanhVien} thành viên đang hoạt động · {t.soNhuCau} nhu cầu</p>}
              {t.trangThai === 'TU_CHOI' && <p>Lý do từ chối: {t.lyDoTuChoi}</p>}
            </div>
            <div className="hang-nut">
              {t.trangThai === 'CHO_DUYET' && <>
                <button onClick={() => setHoi({ viec: 'duyet', t })}>Duyệt</button>
                <button className="phu nguy-hiem" onClick={() => setHoi({ viec: 'tu-choi', t })}>Từ chối</button>
              </>}
              {t.trangThai === 'HOAT_DONG' && <>
                <Link className="nut phu" to={'/to-chuc/' + t.maToChuc}>Thành viên</Link>
                <button className="phu nguy-hiem" onClick={() => setHoi({ viec: 'ngung', t })}>Ngừng hoạt động</button>
              </>}
              {t.trangThai === 'NGUNG_HOAT_DONG' && <button className="phu" onClick={() => setHoi({ viec: 'mo-lai', t })}>Cho hoạt động lại</button>}
            </div>
          </li>
        ))}
      </ul>
      {hoi?.viec === 'duyet' && (
        <HopXacNhan tieuDe="Duyệt tổ chức này?" nhanDongY="Duyệt" nguyHiem={false} dangGui={tt.dangGui} onDongY={() => lam('duyet', hoi.t)} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.t.tenToChuc}</strong> sẽ được tạo nhu cầu. {hoi.t.tenNguoiDangKy} trở thành đại diện của tổ chức.</p>
        </HopXacNhan>
      )}
      {hoi?.viec === 'tu-choi' && (
        <HopLyDo tieuDe="Từ chối đăng ký tổ chức" nhanDongY="Từ chối" dangGui={tt.dangGui} moTa={`Người đăng ký ${hoi.t.tenToChuc} nhận được thông báo kèm lý do.`}
          goiY="Ví dụ: Chưa liên lạc được số điện thoại đã ghi." onDongY={(l) => lam('tu-choi', hoi.t, l)} onThoi={() => setHoi(null)} />
      )}
      {hoi?.viec === 'ngung' && (
        <HopXacNhan tieuDe="Ngừng hoạt động tổ chức này?" nhanDongY="Ngừng hoạt động" dangGui={tt.dangGui} onDongY={() => lam('ngung', hoi.t)} onThoi={() => setHoi(null)}>
          <p>Các ca của <strong>{hoi.t.tenToChuc}</strong> không còn hiện công khai, thành viên của tổ chức mất quyền thao tác. Dữ liệu cũ vẫn được giữ và có thể mở lại.</p>
        </HopXacNhan>
      )}
      {hoi?.viec === 'mo-lai' && (
        <HopXacNhan tieuDe="Cho tổ chức hoạt động lại?" nhanDongY="Cho hoạt động lại" nguyHiem={false} dangGui={tt.dangGui} onDongY={() => lam('mo-lai', hoi.t)} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.t.tenToChuc}</strong> và các thành viên đang hoạt động sẽ dùng lại được nền tảng.</p>
        </HopXacNhan>
      )}
    </>
  )
}

const KN_TRONG = { tenKyNang: '', moTa: '', maNhom: '', canChungChi: false, dangSuDung: true }

function KyNang({ capNhat }) {
  const dx = useTai(() => api.get('/api/quan-tri/de-xuat-ky-nang'), [])
  const dm = useTai(() => api.get('/api/quan-tri/ky-nang'), [])
  const [sua, setSua] = useState(null)   // { loai: 'duyet' | 'tao' | 'sua' | 'trung' | 'tu-choi', dx?, kn?, form }
  const [nhomMoi, setNhomMoi] = useState('')
  const tt = useThaoTac()
  const taiHet = () => { dx.taiLai(); dm.taiLai(); capNhat() }

  const luu = async () => {
    const f = sua.form
    if (sua.loai !== 'trung' && !f.tenKyNang.trim()) { tt.setLoi('Hãy nhập tên kỹ năng.'); return }
    const goi = {
      duyet: () => api.post(`/api/quan-tri/de-xuat-ky-nang/${sua.dx.maDeXuat}/duyet`, { ...f, maNhom: f.maNhom || null }),
      tao: () => api.post('/api/quan-tri/ky-nang', { ...f, maNhom: f.maNhom || null }),
      sua: () => api.put('/api/quan-tri/ky-nang/' + sua.kn.maKyNang, { ...f, maNhom: f.maNhom || null }),
      trung: () => api.post(`/api/quan-tri/de-xuat-ky-nang/${sua.dx.maDeXuat}/dung-co-san`, { maKyNang: f.maKyNang })
    }[sua.loai]
    const ok = await tt.chay(goi, { duyet: 'Đã thêm kỹ năng vào danh mục và báo cho người đề xuất.', tao: 'Đã thêm kỹ năng.', sua: 'Đã lưu kỹ năng.', trung: 'Đã báo người đề xuất dùng kỹ năng có sẵn.' }[sua.loai])
    if (ok) { setSua(null); taiHet() }
  }
  const tuChoi = async (lyDo) => {
    const ok = await tt.chay(() => api.post(`/api/quan-tri/de-xuat-ky-nang/${sua.dx.maDeXuat}/tu-choi`, { lyDo }), 'Đã từ chối đề xuất.')
    setSua(null)
    if (ok) taiHet()
  }
  const themNhom = async (e) => {
    e.preventDefault()
    if (!nhomMoi.trim()) return
    const ok = await tt.chay(() => api.post('/api/quan-tri/nhom-ky-nang', { tenNhom: nhomMoi, thuTu: (dm.duLieu?.nhom.length ?? 0) + 1 }), 'Đã thêm nhóm kỹ năng.')
    if (ok) { setNhomMoi(''); dm.taiLai() }
  }

  if ((dx.dangTai && !dx.duLieu) || (dm.dangTai && !dm.duLieu)) return <DangTai />
  if (dx.loi || dm.loi) return <ThongBao loi={dx.loi || dm.loi} />
  const choDuyet = dx.duLieu.filter((d) => d.trangThai === 'CHO_DUYET')
  const daXuLy = dx.duLieu.filter((d) => d.trangThai !== 'CHO_DUYET').slice(0, 10)
  const f = sua?.form
  const doi = (t) => (e) => setSua({ ...sua, form: { ...f, [t]: e.target.type === 'checkbox' ? e.target.checked : e.target.value } })
  return (
    <>
      <ThongBao loi={!sua ? tt.loi : null} thanhCong={tt.thanhCong} />
      <h2 style={{ marginTop: 0 }}>Đề xuất chờ duyệt ({choDuyet.length})</h2>
      {choDuyet.length === 0 ? <Trong><p>Không có đề xuất nào đang chờ.</p></Trong> : (
        <ul className="ds-dong">
          {choDuyet.map((d) => (
            <li key={d.maDeXuat} className="noi-bat">
              <div className="than">
                <p><strong>{d.tenKyNang}</strong></p>
                {d.moTa && <p>{d.moTa}</p>}
                {d.lyDo && <p className="mo">Vì sao cần: {d.lyDo}</p>}
                <p className="mo">{d.tenNguoiDeXuat} đề xuất ngày {hienNgay(d.ngayDeXuat)}</p>
              </div>
              <div className="hang-nut">
                <button onClick={() => { tt.setLoi(null); setSua({ loai: 'duyet', dx: d, form: { ...KN_TRONG, tenKyNang: d.tenKyNang, moTa: d.moTa ?? '' } }) }}>Thêm vào danh mục</button>
                <button className="phu" onClick={() => { tt.setLoi(null); setSua({ loai: 'trung', dx: d, form: { maKyNang: '' } }) }}>Trùng kỹ năng có sẵn</button>
                <button className="phu nguy-hiem" onClick={() => setSua({ loai: 'tu-choi', dx: d })}>Từ chối</button>
              </div>
            </li>
          ))}
        </ul>
      )}
      {daXuLy.length > 0 && (
        <details style={{ marginBottom: 18 }}>
          <summary>Đề xuất đã xử lý gần đây</summary>
          <ul className="ds-dong" style={{ marginTop: 8 }}>
            {daXuLy.map((d) => (
              <li key={d.maDeXuat}><div className="than">
                <p>{d.tenKyNang} <TrangThai ma={d.trangThai} /> <span className="mo">{d.tenNguoiDeXuat}{d.tenKyNangDuocDung && ` · dùng "${d.tenKyNangDuocDung}"`}{d.lyDoTuChoi && ` · ${d.lyDoTuChoi}`}</span></p>
              </div></li>
            ))}
          </ul>
        </details>
      )}

      <DauTrang tieuDe="Danh mục kỹ năng">
        <button onClick={() => { tt.setLoi(null); setSua({ loai: 'tao', form: { ...KN_TRONG } }) }}>Thêm kỹ năng</button>
      </DauTrang>
      <div className="bang-cuon">
        <table>
          <thead><tr><th>Kỹ năng</th><th>Nhóm</th><th>Cần chứng chỉ</th><th>Số người khai</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>
            {dm.duLieu.kyNang.map((k) => (
              <tr key={k.maKyNang}>
                <td><strong>{k.tenKyNang}</strong>{k.moTaKyNang && <small className="mo"><br />{k.moTaKyNang}</small>}</td>
                <td>{k.tenNhom ?? <span className="mo">Chưa xếp nhóm</span>}</td>
                <td>{k.canChungChi ? 'Có' : 'Không'}</td>
                <td>{k.soNguoiDaKhai}</td>
                <td style={{ whiteSpace: 'nowrap' }}>{k.dangSuDung ? 'Đang dùng' : <span className="thieu-ky-nang">Ngừng dùng</span>}</td>
                <td className="thao-tac"><button className="phu nho" onClick={() => { tt.setLoi(null); setSua({ loai: 'sua', kn: k, form: { tenKyNang: k.tenKyNang, moTa: k.moTaKyNang ?? '', maNhom: k.maNhom ?? '', canChungChi: k.canChungChi, dangSuDung: k.dangSuDung } }) }}>Sửa</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <form className="hang-tim" style={{ maxWidth: 520 }} onSubmit={themNhom}>
        <input value={nhomMoi} placeholder="Tên nhóm kỹ năng mới" aria-label="Tên nhóm kỹ năng mới" onChange={(e) => setNhomMoi(e.target.value)} />
        <button type="submit" className="phu" disabled={tt.dangGui}>Thêm nhóm</button>
      </form>

      {sua && sua.loai !== 'tu-choi' && (
        <div className="nen-hop" onClick={(e) => { if (e.target === e.currentTarget) setSua(null) }}>
          <div className="hop" role="dialog" aria-modal="true" aria-label="Kỹ năng">
            <h2>{{ duyet: 'Thêm kỹ năng từ đề xuất', tao: 'Thêm kỹ năng', sua: 'Sửa kỹ năng', trung: 'Dùng kỹ năng có sẵn' }[sua.loai]}</h2>
            {sua.loai === 'trung' ? (
              <label>Kỹ năng có sẵn trùng nghĩa với "{sua.dx.tenKyNang}"
                <select value={f.maKyNang} onChange={doi('maKyNang')}>
                  <option value="">-- chọn --</option>
                  {dm.duLieu.kyNang.filter((k) => k.dangSuDung).map((k) => <option key={k.maKyNang} value={k.maKyNang}>{k.tenKyNang}</option>)}
                </select>
              </label>
            ) : (
              <>
                <label>Tên kỹ năng<input value={f.tenKyNang} maxLength={100} onChange={doi('tenKyNang')} /></label>
                <label>Mô tả<input value={f.moTa} maxLength={300} onChange={doi('moTa')} /></label>
                <label>Nhóm
                  <select value={f.maNhom} onChange={doi('maNhom')}>
                    <option value="">Chưa xếp nhóm</option>
                    {dm.duLieu.nhom.map((n) => <option key={n.maNhom} value={n.maNhom}>{n.tenNhom}</option>)}
                  </select>
                </label>
                <label className="o-chon"><input type="checkbox" checked={f.canChungChi} onChange={doi('canChungChi')} />
                  <span>Cần chứng chỉ<small>Người khai kỹ năng này được gửi ảnh chứng chỉ để xác minh; ca có thể chỉ nhận người đã xác minh.</small></span></label>
                {sua.loai === 'sua' && (
                  <label className="o-chon" style={{ marginTop: 8 }}><input type="checkbox" checked={f.dangSuDung} onChange={doi('dangSuDung')} />
                    <span>Đang dùng<small>Bỏ chọn để ngừng dùng: không ai chọn mới được, dữ liệu cũ vẫn giữ.</small></span></label>
                )}
              </>
            )}
            <ThongBao loi={tt.loi} />
            <div className="hang-nut cuoi-hop">
              <button className="phu" disabled={tt.dangGui} onClick={() => setSua(null)}>Thôi</button>
              <button disabled={tt.dangGui || (sua.loai === 'trung' && !f.maKyNang)} onClick={luu}>Lưu</button>
            </div>
          </div>
        </div>
      )}
      {sua?.loai === 'tu-choi' && (
        <HopLyDo tieuDe="Từ chối đề xuất kỹ năng" nhanDongY="Từ chối" dangGui={tt.dangGui} moTa={`Người đề xuất "${sua.dx.tenKyNang}" nhận được thông báo kèm lý do.`}
          goiY="Ví dụ: Đây là công việc, chưa phải kỹ năng riêng." onDongY={tuChoi} onThoi={() => setSua(null)} />
      )}
    </>
  )
}

function TaiKhoan() {
  const { nguoiDung } = useNguoiDung()
  const [tuKhoa, setTuKhoa] = useState('')
  const [dangTim, setDangTim] = useState('')
  const ds = useTai(() => api.get('/api/quan-tri/nguoi-dung?tuKhoa=' + encodeURIComponent(dangTim)), [dangTim])
  const [hoi, setHoi] = useState(null)
  const tt = useThaoTac()
  const lam = async () => {
    const n = hoi
    const ok = await tt.chay(() => api.post(`/api/quan-tri/nguoi-dung/${n.maNguoiDung}/${n.dangHoatDong ? 'khoa' : 'mo-khoa'}`),
      n.dangHoatDong ? `Đã khóa tài khoản ${n.tenHienThi}.` : `Đã mở khóa tài khoản ${n.tenHienThi}.`)
    setHoi(null)
    if (ok) ds.taiLai()
  }
  return (
    <>
      <form className="hang-tim" style={{ maxWidth: 560, marginBottom: 14 }} onSubmit={(e) => { e.preventDefault(); setDangTim(tuKhoa.trim()) }}>
        <input value={tuKhoa} placeholder="Tìm theo họ tên, tên đăng nhập hoặc email" aria-label="Tìm tài khoản" onChange={(e) => setTuKhoa(e.target.value)} />
        <button type="submit">Tìm</button>
      </form>
      <ThongBao loi={tt.loi || ds.loi} thanhCong={tt.thanhCong} />
      {ds.dangTai && !ds.duLieu ? <DangTai /> : ds.duLieu && (
        <div className="bang-cuon">
          <table>
            <thead><tr><th>Họ tên</th><th>Tên đăng nhập, email</th><th>Vai trò</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>
              {ds.duLieu.map((n) => (
                <tr key={n.maNguoiDung}>
                  <td><strong>{n.tenHienThi}</strong><small className="mo"><br />Tạo ngày {new Date(n.ngayTao).toLocaleDateString('vi-VN')}</small></td>
                  <td>{n.tenDangNhap}<small className="mo"><br />{n.email}</small></td>
                  <td>{n.vaiTro.map((v) => TEN_VAI_TRO[v]).join(', ')}{n.toChuc.length > 0 && <small className="mo"><br />{n.toChuc.join('; ')}</small>}</td>
                  <td style={{ whiteSpace: 'nowrap' }}>{n.dangHoatDong ? 'Hoạt động' : <strong className="thieu-ky-nang">Đã khóa</strong>}</td>
                  <td className="thao-tac">{n.maNguoiDung !== nguoiDung.maNguoiDung && <button className={n.dangHoatDong ? 'phu nguy-hiem nho' : 'phu nho'} onClick={() => setHoi(n)}>{n.dangHoatDong ? 'Khóa' : 'Mở khóa'}</button>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {hoi && (
        <HopXacNhan tieuDe={hoi.dangHoatDong ? 'Khóa tài khoản này?' : 'Mở khóa tài khoản này?'} nhanDongY={hoi.dangHoatDong ? 'Khóa tài khoản' : 'Mở khóa'}
          nguyHiem={hoi.dangHoatDong} dangGui={tt.dangGui} onDongY={lam} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.tenHienThi}</strong> ({hoi.tenDangNhap})</p>
          <p>{hoi.dangHoatDong ? 'Người này sẽ không đăng nhập và không dùng được nền tảng. Dữ liệu cũ vẫn được giữ.' : 'Người này dùng lại được nền tảng với các vai trò đang có.'}</p>
        </HopXacNhan>
      )}
    </>
  )
}
