import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { ChonCapDo, ChonNgayGio, DangTai, DuongDan, HopXacNhan, ThongBao, TrangThai, Trong, useTai, useThaoTac } from '../chung'
import { ghepNgayGio, hienKhoangGio, hienYeuCau, tachNgayGio } from '../tienIch'
import DeXuatKyNang from './DeXuatKyNang'

const GIO_TRONG = { ngay: '', gio: '', phut: '00' }
const FORM_TRONG = { batDau: GIO_TRONG, ketThuc: GIO_TRONG, soNguoiCan: 1, kyNang: [] }

// M08 Ca làm của một nhu cầu: điều phối viên tạo ca, sửa ca và hủy ca (BR06, BR11, BR12).
export default function QuanLyCa() {
  const { ma } = useParams()
  const nc = useTai(() => api.get('/api/nhu-cau/' + ma), [ma])
  const ds = useTai(() => api.get(`/api/nhu-cau/${ma}/ca`), [ma])
  const danhMuc = useTai(() => api.get('/api/ky-nang'), [])
  const [form, setForm] = useState(FORM_TRONG)
  const [dangSua, setDangSua] = useState(null)   // ca đang sửa, null khi tạo mới
  const [loiO, setLoiO] = useState({})
  const [caDangHuy, setCaDangHuy] = useState(null)
  const tt = useThaoTac()
  const ttDs = useThaoTac()

  const kiemTra = () => {
    const l = {}
    const a = ghepNgayGio(form.batDau), b = ghepNgayGio(form.ketThuc)
    if (!a) l.batDau = 'Hãy điền đủ ngày, giờ và phút bắt đầu.'
    if (!b) l.ketThuc = 'Hãy điền đủ ngày, giờ và phút kết thúc.'
    else if (a && b <= a) l.ketThuc = 'Giờ kết thúc phải sau giờ bắt đầu.'
    if (!(Number(form.soNguoiCan) >= 1)) l.soNguoiCan = 'Ca cần ít nhất một người.'
    if (form.kyNang.length === 0) l.kyNang = 'Hãy chọn ít nhất một kỹ năng mà ca yêu cầu.'
    setLoiO(l)
    return Object.keys(l).length === 0
  }

  const luu = async (e) => {
    e.preventDefault()
    ttDs.setThanhCong(null)
    if (!kiemTra()) return
    const dl = {
      batDauDuKien: ghepNgayGio(form.batDau), ketThucDuKien: ghepNgayGio(form.ketThuc),
      soNguoiCan: Number(form.soNguoiCan), kyNang: form.kyNang
    }
    // form.kyNang: [{ maKyNang, capDoToiThieu, canXacMinh }]
    const ok = dangSua
      ? await tt.chay(() => api.put('/api/ca/' + dangSua.maCaLam, dl))
      : await tt.chay(() => api.post(`/api/nhu-cau/${ma}/ca`, dl))
    if (ok) {
      ttDs.setThanhCong(dangSua ? 'Đã lưu thay đổi của ca.' : 'Đã tạo ca.')
      thoiSua()
      ds.taiLai(); nc.taiLai()
    }
  }
  const moSua = (c) => {
    setDangSua(c)
    setForm({
      batDau: tachNgayGio(c.batDauDuKien), ketThuc: tachNgayGio(c.ketThucDuKien), soNguoiCan: c.soNguoiCan,
      kyNang: c.yeuCau.map((y) => ({ maKyNang: y.maKyNang, capDoToiThieu: y.capDoToiThieu, canXacMinh: y.canXacMinh }))
    })
    setLoiO({}); tt.setLoi(null)
    setTimeout(() => document.getElementById('form-ca')?.scrollIntoView({ behavior: 'smooth' }), 0)
  }
  const thoiSua = () => { setDangSua(null); setForm(FORM_TRONG); setLoiO({}); tt.setLoi(null) }
  const huyCa = async () => {
    const c = caDangHuy
    const ok = await ttDs.chay(() => api.post(`/api/ca/${c.maCaLam}/huy`), 'Đã hủy ca. Các đăng ký còn hiệu lực của ca cũng đã bị hủy.')
    setCaDangHuy(null)
    if (ok) { if (dangSua?.maCaLam === c.maCaLam) thoiSua(); ds.taiLai(); nc.taiLai() }
  }
  const yc = (m) => form.kyNang.find((x) => x.maKyNang === m)
  const doiKyNang = (m) => setForm({ ...form, kyNang: yc(m) ? form.kyNang.filter((x) => x.maKyNang !== m) : [...form.kyNang, { maKyNang: m, capDoToiThieu: 1, canXacMinh: false }] })
  const suaYeuCau = (m, thay) => setForm({ ...form, kyNang: form.kyNang.map((x) => x.maKyNang === m ? { ...x, ...thay } : x) })
  // Chọn ngày bắt đầu thì điền sẵn cùng ngày cho giờ kết thúc.
  const doiBatDau = (v) => setForm({ ...form, batDau: v, ketThuc: form.ketThuc.ngay === '' || form.ketThuc.ngay === form.batDau.ngay ? { ...form.ketThuc, ngay: v.ngay } : form.ketThuc })

  if ((nc.dangTai && !nc.duLieu) || (ds.dangTai && !ds.duLieu)) return <DangTai />
  if (nc.loi || ds.loi) return <ThongBao loi={nc.loi || ds.loi} />
  const n = nc.duLieu
  const duocTao = n.laDieuPhoi && n.trangThai === 'CONG_BO'
  const chuaBatDau = (c) => c.trangThai === 'MO' && new Date(c.batDauDuKien) > new Date()
  const khoaGio = dangSua && dangSua.soDaPhanCong > 0

  return (
    <>
      <DuongDan muc={[{ ten: 'Nhu cầu của tổ chức', den: '/nhu-cau' }, { ten: n.tieuDe, den: '/nhu-cau/' + ma }, { ten: 'Ca làm' }]} />
      <h1>Ca làm của nhu cầu</h1>
      <p className="mo-ta"><strong>{n.tieuDe}</strong> · <TrangThai ma={n.trangThai} /></p>
      {n.trangThai === 'NHAP' && <div className="thong-bao canh-bao">Nhu cầu còn là bản nháp. Đại diện cần công bố nhu cầu trước khi tạo ca.</div>}
      {(n.trangThai === 'DONG' || n.trangThai === 'DA_HUY') && <div className="thong-bao tin">Nhu cầu đã đóng hoặc đã hủy nên không tạo thêm ca.</div>}
      {n.trangThai === 'CONG_BO' && !n.laDieuPhoi && <div className="thong-bao tin">Bạn đang xem ở vai trò đại diện. Điều phối viên của tổ chức là người tạo ca và phân công.</div>}

      <ThongBao loi={ttDs.loi} thanhCong={ttDs.thanhCong} />
      <h2>Danh sách ca</h2>
      {ds.duLieu.length === 0 ? (
        <Trong><p>Nhu cầu chưa có ca nào.{duocTao && ' Hãy tạo ca đầu tiên ở biểu mẫu bên dưới.'}</p></Trong>
      ) : (
        <div className="bang-cuon">
          <table>
            <thead><tr><th>Thời gian</th><th>Kỹ năng yêu cầu</th><th>Đã phân công</th><th>Chờ duyệt</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
            <tbody>
              {ds.duLieu.map((c) => (
                <tr key={c.maCaLam}>
                  <td>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</td>
                  <td>{c.yeuCau.map(hienYeuCau).join(', ')}</td>
                  <td>{c.soDaPhanCong} / {c.soNguoiCan} người</td>
                  <td>{c.soChoDuyet > 0 ? <strong className="thieu-ky-nang">{c.soChoDuyet} người</strong> : '0'}</td>
                  <td><TrangThai ma={c.trangThai} /></td>
                  <td className="thao-tac">
                    {n.laDieuPhoi && <Link className={'nut' + (c.soChoDuyet > 0 && chuaBatDau(c) ? '' : ' phu')} to={`/ca/${c.maCaLam}/xet-duyet`}>{chuaBatDau(c) ? 'Xét duyệt đăng ký' : 'Xem người đăng ký'}</Link>}
                    {n.laDieuPhoi && c.trangThai !== 'DA_HUY' && <Link className="nut phu" to={`/ca/${c.maCaLam}/diem-danh`}>Điểm danh</Link>}
                    {n.laDieuPhoi && chuaBatDau(c) && <button className="phu" onClick={() => moSua(c)}>Sửa</button>}
                    {n.laDieuPhoi && chuaBatDau(c) && <button className="phu nguy-hiem" onClick={() => setCaDangHuy(c)}>Hủy ca</button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {duocTao && (
        <form id="form-ca" className="the hep" onSubmit={luu} noValidate>
          <h2 style={{ marginTop: 0 }}>{dangSua ? 'Sửa ca' : 'Tạo ca mới'}</h2>
          {dangSua && <p className="mo">Đang sửa ca: {hienKhoangGio(dangSua.batDauDuKien, dangSua.ketThucDuKien)}</p>}
          {khoaGio && <div className="thong-bao tin">Ca đã có {dangSua.soDaPhanCong} người được phân công nên chỉ sửa được số người cần. Muốn đổi giờ hoặc kỹ năng, hãy hủy phân công trước.</div>}
          <ChonNgayGio nhan="Bắt đầu" giaTri={form.batDau} onDoi={doiBatDau} loi={loiO.batDau} tatDi={khoaGio} goiY="Giờ Việt Nam." />
          <ChonNgayGio nhan="Kết thúc" giaTri={form.ketThuc} onDoi={(v) => setForm({ ...form, ketThuc: v })} loi={loiO.ketThuc} tatDi={khoaGio} />
          <label style={{ maxWidth: 220 }}>Số người cần
            <input type="number" min={dangSua ? Math.max(1, dangSua.soDaPhanCong) : 1} value={form.soNguoiCan} onChange={(e) => setForm({ ...form, soNguoiCan: e.target.value })} />
            {loiO.soNguoiCan && <small className="loi-o">{loiO.soNguoiCan}</small>}
          </label>
          <fieldset className="ngay-gio" disabled={khoaGio}>
            <legend>Kỹ năng ca yêu cầu</legend>
            <small className="goi-y-o">Chỉ người có đủ tất cả kỹ năng được chọn, ở mức không thấp hơn mức tối thiểu, mới đăng ký được ca.</small>
            <div style={{ marginTop: 8 }}>
              {danhMuc.duLieu?.map((k) => {
                const y = yc(k.maKyNang)
                return (
                  <div key={k.maKyNang} className={'dong-ky-nang' + (y ? ' da-chon' : '')}>
                    <label className="o-chon">
                      <input type="checkbox" checked={!!y} onChange={() => doiKyNang(k.maKyNang)} />
                      <span><strong>{k.tenKyNang}</strong>{k.tenNhom && <small>{k.tenNhom}</small>}</span>
                    </label>
                    <span>{y && <>Mức tối thiểu <ChonCapDo nhan={'Mức tối thiểu ' + k.tenKyNang} giaTri={y.capDoToiThieu} onDoi={(c) => suaYeuCau(k.maKyNang, { capDoToiThieu: c })} /></>}</span>
                    <span>{y && k.canChungChi && (
                      <label className="o-chon" style={{ border: 0, padding: 0 }}>
                        <input type="checkbox" checked={y.canXacMinh} onChange={(e) => suaYeuCau(k.maKyNang, { canXacMinh: e.target.checked })} />
                        <span>Chỉ nhận người đã xác minh chứng chỉ</span>
                      </label>
                    )}</span>
                  </div>
                )
              })}
            </div>
            {loiO.kyNang && <small className="loi-o">{loiO.kyNang}</small>}
          </fieldset>
          <ThongBao loi={tt.loi} />
          <div className="hang-nut">
            <button type="submit" disabled={tt.dangGui}>{dangSua ? 'Lưu thay đổi' : 'Tạo ca'}</button>
            {dangSua && <button type="button" className="phu" onClick={thoiSua}>Thôi không sửa</button>}
          </div>
          <DeXuatKyNang tieuDe="Ca cần kỹ năng chưa có trong danh sách?" />
        </form>
      )}

      {caDangHuy && (
        <HopXacNhan tieuDe="Hủy ca này?" nhanDongY="Hủy ca" dangGui={ttDs.dangGui} onDongY={huyCa} onThoi={() => setCaDangHuy(null)}>
          <p><strong>{hienKhoangGio(caDangHuy.batDauDuKien, caDangHuy.ketThucDuKien)}</strong></p>
          <p>Ca đang có {caDangHuy.soDaPhanCong} người đã phân công và {caDangHuy.soChoDuyet} đăng ký chờ duyệt. Tất cả các đăng ký này sẽ bị hủy theo.</p>
          <p>Thao tác này không hoàn tác được.</p>
        </HopXacNhan>
      )}
    </>
  )
}
