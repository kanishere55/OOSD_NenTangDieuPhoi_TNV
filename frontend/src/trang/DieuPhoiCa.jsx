import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao, TrangThai, Trong, useNguoiDung, useTai } from '../chung'
import { hienKhoangGio, hienYeuCau } from '../tienIch'
import KyNangDangThieu from './KyNangDangThieu'

// M14 Điều phối ca: mọi ca của các nhu cầu đã công bố trong tổ chức, gom về một trang để điều phối viên
// thấy ngay ca nào đang có đăng ký chờ duyệt, ca nào còn thiếu người.
export default function DieuPhoiCa() {
  const { nguoiDung } = useNguoiDung()
  const ds = useTai(async () => {
    const nhuCau = (await api.get('/api/nhu-cau')).filter((n) => n.trangThai !== 'NHAP' && n.soCa > 0)
    const ca = await Promise.all(nhuCau.map((n) => api.get(`/api/nhu-cau/${n.maNhuCau}/ca`)))
    return nhuCau.flatMap((n, i) => ca[i].map((c) => ({ ...c, maNhuCau: n.maNhuCau, tieuDe: n.tieuDe, maToChuc: n.maToChuc })))
  }, [])

  if (ds.dangTai) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  const laDieuPhoiCua = (maToChuc) => nguoiDung.thanhVien.some((t) => t.maToChuc === maToChuc && t.vaiTro === 'DIEU_PHOI')
  const bayGio = new Date()
  const sapToi = ds.duLieu.filter((c) => c.trangThai === 'MO' && new Date(c.batDauDuKien) > bayGio)
    .sort((a, b) => a.batDauDuKien.localeCompare(b.batDauDuKien))
  const dangDienRa = ds.duLieu.filter((c) => c.trangThai === 'DANG_DIEN_RA' || (c.trangThai === 'MO' && new Date(c.batDauDuKien) <= bayGio))
  // Ca đang trong khung giờ điểm danh: từ 30 phút trước giờ bắt đầu đến 2 giờ sau giờ kết thúc.
  const ganDiemDanh = (c) => c.trangThai !== 'DA_HUY' && bayGio >= new Date(new Date(c.batDauDuKien).getTime() - 30 * 60000)
    && bayGio <= new Date(new Date(c.ketThucDuKien).getTime() + 2 * 3600000)
  const daQua = ds.duLieu.filter((c) => c.trangThai === 'DA_KET_THUC' || c.trangThai === 'DA_HUY')
    .sort((a, b) => b.batDauDuKien.localeCompare(a.batDauDuKien)).slice(0, 10)

  const dong = (c, coNut) => {
    const thieu = c.soNguoiCan - c.soDaPhanCong
    return (
      <li key={c.maCaLam} className={coNut && c.soChoDuyet > 0 ? 'noi-bat' : ''}>
        <div className="than">
          <p><strong>{c.tieuDe}</strong></p>
          <p>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)} <TrangThai ma={c.trangThai} /></p>
          <p>
            Đã phân công {c.soDaPhanCong} / {c.soNguoiCan} người
            {coNut && thieu > 0 && <span className="thieu-ky-nang"> · còn thiếu {thieu} người</span>}
            {coNut && c.soChoDuyet > 0 && <strong className="thieu-ky-nang"> · {c.soChoDuyet} đăng ký chờ duyệt</strong>}
          </p>
          <p className="mo">Kỹ năng: {c.yeuCau.map(hienYeuCau).join(', ')}</p>
        </div>
        <div className="hang-nut">
          {laDieuPhoiCua(c.maToChuc) && ganDiemDanh(c) && <Link className="nut" to={`/ca/${c.maCaLam}/diem-danh`}>Điểm danh</Link>}
          {laDieuPhoiCua(c.maToChuc) && (
            <Link className={'nut' + (coNut && c.soChoDuyet > 0 ? '' : ' phu')} to={`/ca/${c.maCaLam}/xet-duyet`}>{coNut ? 'Xét duyệt đăng ký' : 'Xem người đăng ký'}</Link>
          )}
          <Link className="nut phu" to={`/nhu-cau/${c.maNhuCau}/ca`}>Ca của nhu cầu</Link>
        </div>
      </li>
    )
  }

  return (
    <>
      <DauTrang tieuDe="Điều phối ca">
        <Link className="nut phu" to="/nhu-cau">Tạo ca từ một nhu cầu</Link>
      </DauTrang>
      {ds.duLieu.length === 0 && (
        <Trong>
          <p>Tổ chức chưa có ca làm nào. Ca được tạo từ một nhu cầu đã công bố.</p>
          <div className="hang-nut"><Link className="nut" to="/nhu-cau">Mở danh sách nhu cầu</Link></div>
        </Trong>
      )}
      {dangDienRa.length > 0 && (<><h2>Đang diễn ra</h2><ul className="ds-dong">{dangDienRa.map((c) => dong(c, false))}</ul></>)}
      {ds.duLieu.length > 0 && (
        <>
          <h2>Sắp diễn ra ({sapToi.length})</h2>
          {sapToi.length === 0 ? <Trong><p>Không có ca nào sắp diễn ra.</p></Trong> : <ul className="ds-dong">{sapToi.map((c) => dong(c, true))}</ul>}
        </>
      )}
      {daQua.length > 0 && (<><h2>Đã kết thúc hoặc đã hủy gần đây</h2><ul className="ds-dong">{daQua.map((c) => dong(c, false))}</ul></>)}
      {nguoiDung.thanhVien.filter((t) => t.vaiTro === 'DIEU_PHOI' || t.vaiTro === 'DAI_DIEN')
        .filter((t, i, a) => a.findIndex((x) => x.maToChuc === t.maToChuc) === i)
        .map((t) => (
          <section key={t.maToChuc}>
            <h2>Kỹ năng đang thiếu{nguoiDung.thanhVien.length > 1 ? ` · ${t.tenToChuc}` : ''}</h2>
            <KyNangDangThieu maToChuc={t.maToChuc} />
          </section>
        ))}
    </>
  )
}
