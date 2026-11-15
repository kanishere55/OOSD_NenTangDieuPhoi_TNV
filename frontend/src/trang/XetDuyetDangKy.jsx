import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DuongDan, HopLyDo, HopXacNhan, ThongBao, TrangThai, Trong, useTai, useThaoTac } from '../chung'
import { hienGio, hienKhoangGio, hienKm, hienYeuCau } from '../tienIch'

const mucDiem = (d) => d >= 75 ? 'cao' : d >= 55 ? 'vua' : 'thap'

// Lý do và lưu ý của điểm phù hợp, viết thành từng dòng ngắn.
function DiemPhuHop({ d }) {
  return (
    <>
      <p><span className={'diem ' + mucDiem(d.diem)}>Phù hợp {d.diem}/100</span></p>
      <ul className="ly-do">
        {d.lyDo.map((x) => <li key={x}>{x}</li>)}
        {d.luuY.map((x) => <li key={x} className="luu-y">{x}</li>)}
      </ul>
    </>
  )
}

// M09 Xét duyệt đăng ký của một ca: phân công, từ chối, hủy phân công. Điều phối viên là người quyết định (BR04, BR08).
export default function XetDuyetDangKy() {
  const { ma } = useParams()
  const ds = useTai(() => api.get(`/api/ca/${ma}/dang-ky`), [ma])
  const [hoi, setHoi] = useState(null)   // { viec: 'tu-choi' | 'huy-phan-cong', d }
  const [chungChi, setChungChi] = useState(null)   // chứng chỉ đang xem: { maChungChi, tenKyNang, trangThai, ten }
  const [tuChoiCc, setTuChoiCc] = useState(false)
  const tt = useThaoTac()
  const xacMinh = async (hopLe, lyDo) => {
    const cc = chungChi
    const ok = await tt.chay(() => api.post(`/api/chung-chi/${cc.maChungChi}/xac-minh`, { hopLe, lyDo }),
      hopLe ? `Đã xác minh chứng chỉ ${cc.tenKyNang} của ${cc.ten}.` : `Đã báo chứng chỉ ${cc.tenKyNang} của ${cc.ten} chưa hợp lệ.`)
    setTuChoiCc(false); setChungChi(null)
    if (ok) ds.taiLai()
  }

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
          <dt>Kỹ năng yêu cầu</dt><dd>{c.yeuCau.map((y) => <span key={y.maKyNang} className="nhan nhan-ky-nang">{hienYeuCau(y)}</span>)}</dd>
          <dt>Số người</dt><dd>Đã phân công {c.soDaPhanCong} / {c.soNguoiCan}, còn {conLai} chỗ</dd>
        </dl>
        <div className="hang-nut">
          <Link to={`/ca/${ma}/huong-dan-rieng`}>Xem hướng dẫn riêng của ca</Link>
          <Link className="nut phu" to={`/ca/${ma}/diem-danh`}>Điểm danh</Link>
        </div>
      </section>
      {chuaBatDau && cho.length > 1 && <p className="mo">Danh sách chờ duyệt xếp theo điểm phù hợp: kỹ năng, chứng chỉ, khoảng cách, lịch rảnh và số lần có mặt trước đây. Điểm chỉ để tham khảo, bạn là người quyết định.</p>}
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
                <p><strong>{d.tenHienThi}</strong>{d.khoangCachKm != null && <span className="mo"> · cách địa điểm {hienKm(d.khoangCachKm)}</span>}</p>
                {d.duKyNang ? <DiemPhuHop d={d} /> : (
                  <p><span className="goi-y xau">{d.thieuXacMinh ? 'Chứng chỉ chưa được xác minh' : 'Thiếu kỹ năng hoặc cấp độ của ca'}</span></p>
                )}
                {d.trungLich && <p><span className="goi-y xau">Trùng lịch với ca khác đã được phân công</span></p>}
                <p>Kỹ năng đã khai: {d.kyNangDaKhai.join('; ') || 'chưa khai'}</p>
                {d.chungChi.length > 0 && (
                  <p>Chứng chỉ: {d.chungChi.map((cc) => (
                    <button key={cc.maChungChi} type="button" className="phu nho" style={{ marginRight: 6 }}
                      onClick={() => setChungChi({ ...cc, ten: d.tenHienThi })}>
                      {cc.tenKyNang} ({cc.trangThai === 'DA_XAC_MINH' ? 'đã xác minh' : 'chờ xác minh'})
                    </button>
                  ))}</p>
                )}
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
                <p>Kỹ năng đã khai: {d.kyNangDaKhai.join('; ')}</p>
                {d.soLanVang > 0 && <p className="thieu-ky-nang">Đã vắng không báo {d.soLanVang} lần trước đây.</p>}
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

      {chungChi && !tuChoiCc && (
        <div className="nen-hop" onClick={(e) => { if (e.target === e.currentTarget) setChungChi(null) }}>
          <div className="hop hop-anh" role="dialog" aria-modal="true" aria-label="Chứng chỉ">
            <h2>Chứng chỉ {chungChi.tenKyNang} của {chungChi.ten}</h2>
            <img src={`/api/chung-chi/${chungChi.maChungChi}/anh`} alt={'Chứng chỉ ' + chungChi.tenKyNang} />
            <p className="mo">Lần xem này được ghi vào nhật ký truy cập. Không chụp lại hay chia sẻ ảnh chứng chỉ.</p>
            <div className="hang-nut cuoi-hop">
              <button className="phu" onClick={() => setChungChi(null)}>Đóng</button>
              {chungChi.trangThai === 'CHO_XAC_MINH' && (
                <>
                  <button className="phu nguy-hiem" disabled={tt.dangGui} onClick={() => setTuChoiCc(true)}>Chưa hợp lệ</button>
                  <button disabled={tt.dangGui} onClick={() => xacMinh(true)}>Xác nhận hợp lệ</button>
                </>
              )}
            </div>
          </div>
        </div>
      )}
      {chungChi && tuChoiCc && (
        <HopLyDo tieuDe="Chứng chỉ chưa hợp lệ" nhanDongY="Gửi" dangGui={tt.dangGui}
          moTa={`${chungChi.ten} sẽ nhận thông báo kèm lý do để gửi lại ảnh khác.`}
          goiY="Ví dụ: Ảnh mờ, không đọc được tên và ngày cấp."
          onDongY={(lyDo) => xacMinh(false, lyDo)} onThoi={() => setTuChoiCc(false)} />
      )}
      {hoi?.viec === 'tu-choi' && (
        <HopXacNhan tieuDe="Từ chối đăng ký này?" nhanDongY="Từ chối" dangGui={tt.dangGui}
          onDongY={() => lam(hoi.d, 'tu-choi', `Đã từ chối đăng ký của ${hoi.d.tenHienThi}.`)} onThoi={() => setHoi(null)}>
          <p>Đăng ký của <strong>{hoi.d.tenHienThi}</strong> sẽ chuyển sang Bị từ chối.</p>
          <p>Người này nhận được thông báo và vẫn có thể đăng ký lại nếu ca còn mở. Thao tác này không hoàn tác được.</p>
        </HopXacNhan>
      )}
      {hoi?.viec === 'huy-phan-cong' && (
        <HopXacNhan tieuDe="Hủy phân công người này?" nhanDongY="Hủy phân công" dangGui={tt.dangGui}
          onDongY={() => lam(hoi.d, 'huy-phan-cong', `Đã hủy phân công ${hoi.d.tenHienThi}. Ca có thêm một chỗ trống.`)} onThoi={() => setHoi(null)}>
          <p><strong>{hoi.d.tenHienThi}</strong> sẽ không còn chỗ trong ca và không xem được hướng dẫn riêng nữa. Chỗ này được trả lại cho ca.</p>
          <p>Thao tác này không hoàn tác được.</p>
        </HopXacNhan>
      )}
    </>
  )
}
