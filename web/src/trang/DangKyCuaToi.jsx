import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, HopXacNhan, ThongBao, TrangThai, Trong, useTai, useThaoTac } from '../chung'
import { GIAI_THICH_DANG_KY, GIAI_THICH_DONG_GOP, hienKhoangGio } from '../tienIch'

// Đăng ký đã phân công của ca đã kết thúc thì ghi là đã tham gia, không ghi "đã có chỗ".
const giaiThich = (d) => d.trangThai === 'DA_PHAN_CONG' && d.trangThaiCa === 'DA_KET_THUC'
  ? 'Ca đã kết thúc.'
  : d.trangThai === 'DA_PHAN_CONG' && d.trangThaiCa === 'DA_HUY' ? 'Ca đã bị hủy.' : GIAI_THICH_DANG_KY[d.trangThai]

// M05 Đăng ký của tôi: rút đăng ký trước giờ bắt đầu, xem hướng dẫn riêng, khai đóng góp sau khi ca kết thúc.
export default function DangKyCuaToi() {
  const ds = useTai(() => api.get('/api/dang-ky/cua-toi'), [])
  const [dangHoiRut, setDangHoiRut] = useState(null)
  const tt = useThaoTac()

  const rut = async () => {
    const d = dangHoiRut
    const ok = await tt.chay(() => api.post(`/api/dang-ky/${d.maDangKy}/rut`), `Đã rút đăng ký ca "${d.tieuDe}".`)
    setDangHoiRut(null)
    if (ok) ds.taiLai()
  }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  const tatCa = ds.duLieu
  const canKhai = tatCa.filter((d) => d.duocKhai)
  const sapToi = tatCa.filter((d) => d.duocRut || d.duocXemRieng).sort((a, b) => a.batDauDuKien.localeCompare(b.batDauDuKien))
  const conLai = tatCa.filter((d) => !d.duocKhai && !d.duocRut && !d.duocXemRieng)

  const dong = (d) => (
    <li key={d.maDangKy} className={d.duocKhai ? 'noi-bat' : ''}>
      <div className="than">
        <p><strong>{d.tieuDe}</strong></p>
        <p className="mo">{d.tenToChuc} · {d.khuVucChung}</p>
        <p>{hienKhoangGio(d.batDauDuKien, d.ketThucDuKien)}</p>
        <p>Đăng ký: <TrangThai ma={d.trangThai} /> <span className="mo">{giaiThich(d)}</span></p>
        {d.trangThai === 'DA_PHAN_CONG' && d.trangThaiCa !== 'DA_HUY' && !d.duocRut && (
          <p>Đóng góp: {d.dongGop
            ? <><TrangThai ma={d.dongGop.trangThai} /> <span className="mo">{GIAI_THICH_DONG_GOP[d.dongGop.trangThai]}</span></>
            : <span className="mo">{d.duocKhai ? 'Chưa khai' : 'Khai được sau khi ca kết thúc'}</span>}
          </p>
        )}
        {d.dongGop?.trangThai === 'CAN_BO_SUNG' && <p>Lý do trả lại: {d.dongGop.lyDoTraLai}</p>}
      </div>
      <div className="hang-nut">
        {d.duocKhai && <Link className="nut" to={`/dang-ky/${d.maDangKy}/khai`}>{d.dongGop ? 'Bổ sung bản khai' : 'Khai đóng góp'}</Link>}
        {d.duocXemRieng && <Link className="nut" to={`/ca/${d.maCaLam}/huong-dan-rieng`}>Xem hướng dẫn riêng</Link>}
        {d.duocRut && <button className="phu nguy-hiem" onClick={() => setDangHoiRut(d)}>Rút đăng ký</button>}
      </div>
    </li>
  )

  return (
    <>
      <DauTrang tieuDe="Đăng ký của tôi" moTa="Các ca bạn đã đăng ký, tình trạng của từng đăng ký và việc bạn cần làm tiếp.">
        <Link className="nut phu" to="/tim-ca">Tìm thêm ca</Link>
      </DauTrang>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      {tatCa.length === 0 && (
        <Trong>
          <p>Bạn chưa đăng ký ca nào.</p>
          <div className="hang-nut"><Link className="nut" to="/tim-ca">Tìm ca làm</Link></div>
        </Trong>
      )}
      {canKhai.length > 0 && (
        <>
          <h2>Cần khai đóng góp</h2>
          <p className="mo">Ca đã kết thúc. Hãy khai giờ bạn đã làm để điều phối viên xác nhận.</p>
          <ul className="ds-dong">{canKhai.map(dong)}</ul>
        </>
      )}
      {sapToi.length > 0 && (
        <>
          <h2>Ca sắp tới và đang diễn ra</h2>
          <p className="mo">Bạn rút được đăng ký cho đến trước giờ ca bắt đầu.</p>
          <ul className="ds-dong">{sapToi.map(dong)}</ul>
        </>
      )}
      {conLai.length > 0 && (
        <>
          <h2>Đã qua hoặc không còn hiệu lực</h2>
          <ul className="ds-dong">{conLai.map(dong)}</ul>
        </>
      )}
      {dangHoiRut && (
        <HopXacNhan tieuDe="Rút đăng ký này?" nhanDongY="Rút đăng ký" dangGui={tt.dangGui} onDongY={rut} onThoi={() => setDangHoiRut(null)}>
          <p><strong>{dangHoiRut.tieuDe}</strong><br />{hienKhoangGio(dangHoiRut.batDauDuKien, dangHoiRut.ketThucDuKien)}</p>
          {dangHoiRut.trangThai === 'DA_PHAN_CONG'
            ? <p>Bạn đã được phân công ca này. Nếu rút, chỗ của bạn sẽ được trả lại cho ca.</p>
            : <p>Đăng ký đang chờ duyệt sẽ bị rút khỏi danh sách.</p>}
          <p>Sau khi rút, bạn vẫn có thể đăng ký lại nếu ca còn mở.</p>
        </HopXacNhan>
      )}
    </>
  )
}
