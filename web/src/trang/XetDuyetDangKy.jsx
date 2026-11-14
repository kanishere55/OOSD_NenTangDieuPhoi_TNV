import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DuongDan, HopXacNhan, ThongBao, TrangThai, Trong, useTai, useThaoTac } from '../chung'
import { hienGio, hienKhoangGio } from '../tienIch'

// M09 Xét duyệt đăng ký của một ca: phân công, từ chối, hủy phân công. Điều phối viên là người quyết định (BR04, BR08).
export default function XetDuyetDangKy() {
  const { ma } = useParams()
  const ds = useTai(() => api.get(`/api/ca/${ma}/dang-ky`), [ma])
  const [hoi, setHoi] = useState(null)   // { viec: 'tu-choi' | 'huy-phan-cong', d }
  const tt = useThaoTac()

  const lam = async (d, viec, thongBao) => {
    const ok = await tt.chay(() => api.post(`/api/dang-ky/${d.maDangKy}/${viec}`), thongBao)
    setHoi(null)
    if (ok) ds.taiLai()
  }

  if (ds.dangTai && !ds.duLieu) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  const c = ds.duLieu
  const conLai = Math.max(0, c.soNguoiCan - c.soDaPhanCong)
  const chuaBatDau = c.trangThai === 'MO' && new Date(c.batDauDuKien) > new Date()
  const cho = c.dangKy.filter((d) => d.trangThai === 'CHO_DUYET')
  const daPhanCong = c.dangKy.filter((d) => d.trangThai === 'DA_PHAN_CONG')
  const khac = c.dangKy.filter((d) => d.trangThai !== 'CHO_DUYET' && d.trangThai !== 'DA_PHAN_CONG')

  return (
    <>
      <DuongDan muc={[{ ten: 'Điều phối ca', den: '/dieu-phoi' }, { ten: 'Ca làm của nhu cầu', den: `/nhu-cau/${c.maNhuCau}/ca` }, { ten: 'Xét duyệt đăng ký' }]} />
      <h1>Xét duyệt đăng ký</h1>
      <section className="the">
        <h2 style={{ marginTop: 0 }}>{c.tieuDe}</h2>
        <dl className="thong-tin">
          <dt>Thời gian</dt><dd>{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)} <TrangThai ma={c.trangThai} /></dd>
          <dt>Kỹ năng yêu cầu</dt><dd>{c.kyNangYeuCau.join(', ')}</dd>
          <dt>Số người</dt><dd>Đã phân công {c.soDaPhanCong} / {c.soNguoiCan}, còn {conLai} chỗ</dd>
        </dl>
        <Link to={`/ca/${ma}/huong-dan-rieng`}>Xem hướng dẫn riêng của ca</Link>
      </section>
      {!chuaBatDau && <div className="thong-bao tin">Ca đã bắt đầu, đã kết thúc hoặc đã hủy nên không còn phân công hay hủy phân công được.</div>}
      {chuaBatDau && conLai === 0 && cho.length > 0 && (
        <div className="thong-bao canh-bao">Ca đã đủ người. Muốn nhận thêm, hãy tăng số người cần ở trang <Link to={`/nhu-cau/${c.maNhuCau}/ca`}>Ca làm của nhu cầu</Link>, hoặc từ chối các đăng ký còn lại.</div>
      )}
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />

      <h2>Đang chờ duyệt ({cho.length})</h2>
      {cho.length === 0 ? <Trong><p>Không có đăng ký nào đang chờ duyệt.</p></Trong> : (
        <ul className="ds-dong">
          {cho.map((d) => (
            <li key={d.maDangKy}>
              <div className="than">
                <p><strong>{d.tenHienThi}</strong></p>
                <p>Kỹ năng đã khai: {d.kyNangDaKhai.join(', ') || 'chưa khai'}</p>
                <p>
                  <span className={d.duKyNang ? 'goi-y tot' : 'goi-y xau'}>{d.duKyNang ? 'Đủ kỹ năng của ca' : 'Thiếu kỹ năng của ca'}</span>
                  {d.trungLich && <span className="goi-y xau">Trùng lịch với ca khác đã được phân công</span>}
                </p>
                <p className="mo">Đăng ký lúc {hienGio(d.ngayDangKy)}</p>
              </div>
              {chuaBatDau && (
                <div className="hang-nut">
                  <button disabled={tt.dangGui} onClick={() => lam(d, 'phan-cong', `Đã phân công ${d.tenHienThi} vào ca.`)}>Phân công</button>
                  <button className="phu nguy-hiem" disabled={tt.dangGui} onClick={() => setHoi({ viec: 'tu-choi', d })}>Từ chối</button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      <h2>Đã phân công ({daPhanCong.length})</h2>
      {daPhanCong.length === 0 ? <Trong><p>Chưa phân công ai vào ca này.</p></Trong> : (
        <ul className="ds-dong">
          {daPhanCong.map((d) => (
            <li key={d.maDangKy}>
              <div className="than">
                <p><strong>{d.tenHienThi}</strong> <TrangThai ma={d.trangThai} /></p>
                <p>Kỹ năng đã khai: {d.kyNangDaKhai.join(', ')}</p>
              </div>
              {chuaBatDau && (
                <div className="hang-nut">
                  <button className="phu nguy-hiem" disabled={tt.dangGui} onClick={() => setHoi({ viec: 'huy-phan-cong', d })}>Hủy phân công</button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {khac.length > 0 && (
        <>
          <h2>Đã từ chối, đã rút hoặc đã hủy ({khac.length})</h2>
          <ul className="ds-dong">
            {khac.map((d) => (
              <li key={d.maDangKy}>
                <div className="than"><p>{d.tenHienThi} <TrangThai ma={d.trangThai} /></p></div>
              </li>
            ))}
          </ul>
        </>
      )}

      {hoi?.viec === 'tu-choi' && (
        <HopXacNhan tieuDe="Từ chối đăng ký này?" nhanDongY="Từ chối" dangGui={tt.dangGui}
          onDongY={() => lam(hoi.d, 'tu-choi', `Đã từ chối đăng ký của ${hoi.d.tenHienThi}.`)} onThoi={() => setHoi(null)}>
          <p>Đăng ký của <strong>{hoi.d.tenHienThi}</strong> sẽ chuyển sang Bị từ chối.</p>
          <p>Thao tác này không hoàn tác được. Người này vẫn có thể đăng ký lại nếu ca còn mở.</p>
        </HopXacNhan>
      )}
      {hoi?.viec === 'huy-phan-cong' && (
        <HopXacNhan tieuDe="Hủy phân công người này?" nhanDongY="Hủy phân công" dangGui={tt.dangGui}
          onDongY={() => lam(hoi.d, 'huy-phan-cong', `Đã hủy phân công ${hoi.d.tenHienThi}. Ca có thêm một chỗ trống.`)} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.d.tenHienThi}</strong> sẽ không còn chỗ trong ca và không xem được hướng dẫn riêng nữa. Chỗ này được trả lại cho ca.</p>
          <p>Thao tác này không hoàn tác được. Hãy báo cho người đó biết nếu có thể.</p>
        </HopXacNhan>
      )}
    </>
  )
}
