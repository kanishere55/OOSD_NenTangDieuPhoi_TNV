import { Link } from 'react-router-dom'
import { api, dangKyTaiKhoan, dangNhap } from '../api'
import { DangTai, ThongBao, useNguoiDung, useTai } from '../chung'
import { hienKhoangGio, hienSoCho } from '../tienIch'

// M00 Trang chủ. Khách: giới thiệu và cách tham gia. Người đã đăng nhập: các việc cần làm theo vai trò.
export default function TrangChu() {
  const { nguoiDung, daTai, laTnv, laDaiDien, laDieuPhoi } = useNguoiDung()
  if (!daTai) return <DangTai />
  if (!nguoiDung) return <ChoKhach />
  return (
    <>
      <h1>Xin chào, {nguoiDung.tenHienThi}</h1>
      {laTnv && <ViecCuaTinhNguyenVien />}
      {laDaiDien && <ViecCuaDaiDien />}
      {laDieuPhoi && <ViecCuaDieuPhoi />}
      {!laTnv && !laDaiDien && !laDieuPhoi && (
        <div className="thong-bao canh-bao">Tài khoản của bạn chưa có vai trò nào trên nền tảng. Hãy liên hệ tổ chức của bạn để được cấp.</div>
      )}
    </>
  )
}

function ChoKhach() {
  const ca = useTai(() => api.get('/api/ca?trang=1&kichThuoc=3'), [])
  return (
    <>
      <section className="gioi-thieu">
        <h1>Tìm ca tình nguyện hợp với kỹ năng của bạn</h1>
        <p>
          Các tổ chức cộng đồng đăng việc cần người hỗ trợ theo từng ca làm.
          Bạn chọn ca phù hợp, đến làm và được ghi nhận số giờ đóng góp.
        </p>
        <div className="hang-nut">
          <Link className="nut lon" to="/tim-ca">Xem các ca đang cần người</Link>
          <button className="phu lon" onClick={() => dangKyTaiKhoan()}>Tạo tài khoản tình nguyện viên</button>
        </div>
        <p className="mo">Đã có tài khoản? <a href="/dang-nhap" onClick={(e) => { e.preventDefault(); dangNhap('/') }}>Đăng nhập</a></p>
      </section>

      <h2>Tham gia như thế nào</h2>
      <ol className="ds-buoc">
        <li>Tạo tài khoản và chọn kỹ năng bạn có.</li>
        <li>Tìm ca hợp với kỹ năng và thời gian, bấm Đăng ký.</li>
        <li>Chờ điều phối viên phân công, rồi xem hướng dẫn để đến làm.</li>
        <li>Sau buổi làm, khai giờ đã làm để được xác nhận.</li>
      </ol>

      <h2>Các ca sắp diễn ra</h2>
      <ThongBao loi={ca.loi} />
      {ca.dangTai ? <DangTai /> : ca.duLieu && (ca.duLieu.danhSach.length === 0
        ? <p className="mo">Hiện chưa có ca nào đang mở. Bạn hãy quay lại sau.</p>
        : (
          <>
            <div className="luoi-the">
              {ca.duLieu.danhSach.map((c) => (
                <article key={c.maCaLam} className="the the-ca">
                  <h3><Link to={'/ca/' + c.maCaLam}>{c.tieuDe}</Link></h3>
                  <p className="mo">{c.tenToChuc} · {c.khuVucChung}</p>
                  <p className="dong-chinh">{hienKhoangGio(c.batDauDuKien, c.ketThucDuKien)}</p>
                  <p>{hienSoCho(c.soNguoiCan, c.soDaPhanCong)}</p>
                </article>
              ))}
            </div>
            <p><Link to="/tim-ca">Xem tất cả {ca.duLieu.tongSo} ca đang mở</Link></p>
          </>
        ))}

      <h2>Dành cho tổ chức cộng đồng</h2>
      <p className="mo-ta">
        Đại diện và điều phối viên dùng tài khoản được cấp cho tổ chức, đăng nhập ở góc trên bên phải.
      </p>
    </>
  )
}

function Viec({ loai = '', tieuDe, moTa, children }) {
  return (
    <div className={'viec ' + loai}>
      <div className="than">
        <p><strong>{tieuDe}</strong></p>
        {moTa && <p className="mo">{moTa}</p>}
      </div>
      <div className="hang-nut">{children}</div>
    </div>
  )
}

function ViecCuaTinhNguyenVien() {
  const hoSo = useTai(() => api.get('/api/ho-so'), [])
  const dk = useTai(() => api.get('/api/dang-ky/cua-toi'), [])
  if (hoSo.dangTai || dk.dangTai) return <DangTai />
  if (hoSo.loi || dk.loi) return <ThongBao loi={hoSo.loi || dk.loi} />
  const ds = dk.duLieu
  const sapToi = ds.filter((d) => d.trangThai === 'DA_PHAN_CONG' && d.duocXemRieng).sort((a, b) => a.batDauDuKien.localeCompare(b.batDauDuKien))
  const canKhai = ds.filter((d) => d.duocKhai)
  const choDuyet = ds.filter((d) => d.trangThai === 'CHO_DUYET')
  const chuaCoKyNang = hoSo.duLieu.kyNang.length === 0
  const khongCoViec = !chuaCoKyNang && sapToi.length === 0 && canKhai.length === 0 && choDuyet.length === 0
  return (
    <section>
      <h2>Việc của tình nguyện viên</h2>
      {chuaCoKyNang && (
        <Viec loai="gap" tieuDe="Bạn chưa chọn kỹ năng nào"
          moTa="Cần có kỹ năng trong hồ sơ mới đăng ký được ca.">
          <Link className="nut" to="/ho-so">Chọn kỹ năng</Link>
        </Viec>
      )}
      {canKhai.map((d) => (
        <Viec key={d.maDangKy} loai="gap"
          tieuDe={d.dongGop ? 'Bản khai bị trả lại, cần bổ sung' : 'Ca đã kết thúc, bạn chưa khai giờ đã làm'}
          moTa={`${d.tieuDe} · ${hienKhoangGio(d.batDauDuKien, d.ketThucDuKien)}`}>
          <Link className="nut" to={`/dang-ky/${d.maDangKy}/khai`}>{d.dongGop ? 'Bổ sung bản khai' : 'Khai đóng góp'}</Link>
        </Viec>
      ))}
      {sapToi.map((d) => (
        <Viec key={d.maDangKy} tieuDe="Bạn đã được phân công ca này"
          moTa={`${d.tieuDe} · ${hienKhoangGio(d.batDauDuKien, d.ketThucDuKien)}`}>
          <Link className="nut" to={`/ca/${d.maCaLam}/huong-dan-rieng`}>Xem hướng dẫn để đến làm</Link>
        </Viec>
      ))}
      {choDuyet.length > 0 && (
        <Viec loai="xong" tieuDe={`${choDuyet.length} đăng ký đang chờ điều phối viên duyệt`}
          moTa="Khi được phân công, ca sẽ hiện ở đây.">
          <Link className="nut phu" to="/dang-ky-cua-toi">Xem đăng ký của tôi</Link>
        </Viec>
      )}
      {khongCoViec && (
        <Viec loai="xong" tieuDe="Bạn chưa có ca nào sắp tới">
          <Link className="nut" to="/tim-ca">Tìm ca làm</Link>
        </Viec>
      )}
    </section>
  )
}

function ViecCuaDaiDien() {
  const ds = useTai(() => api.get('/api/nhu-cau'), [])
  const { nguoiDung } = useNguoiDung()
  if (ds.dangTai) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  const cuaToi = new Set(nguoiDung.thanhVien.filter((t) => t.vaiTro === 'DAI_DIEN').map((t) => t.maToChuc))
  const nc = ds.duLieu.filter((n) => cuaToi.has(n.maToChuc))
  const nhap = nc.filter((n) => n.trangThai === 'NHAP')
  const dongDuoc = nc.filter((n) => n.trangThai === 'CONG_BO' && n.soCa > 0 && n.soCaChuaXong === 0)
  return (
    <section>
      <h2>Việc của đại diện cộng đồng</h2>
      {nhap.map((n) => (
        <Viec key={n.maNhuCau} loai="gap" tieuDe="Nhu cầu còn là bản nháp, chưa công bố" moTa={n.tieuDe}>
          <Link className="nut" to={'/nhu-cau/' + n.maNhuCau}>Rà soát và công bố</Link>
        </Viec>
      ))}
      {dongDuoc.map((n) => (
        <Viec key={n.maNhuCau} tieuDe="Các ca của nhu cầu đã xong, có thể đóng nhu cầu" moTa={n.tieuDe}>
          <Link className="nut phu" to={'/nhu-cau/' + n.maNhuCau}>Mở nhu cầu</Link>
        </Viec>
      ))}
      <Viec loai="xong" tieuDe="Nhu cầu của tổ chức">
        <Link className="nut" to="/nhu-cau/moi">Tạo nhu cầu mới</Link>
        <Link className="nut phu" to="/nhu-cau">Xem các nhu cầu</Link>
      </Viec>
    </section>
  )
}

function ViecCuaDieuPhoi() {
  const ds = useTai(() => api.get('/api/nhu-cau'), [])
  const banKhai = useTai(() => api.get('/api/dong-gop/cho-duyet'), [])
  const { nguoiDung } = useNguoiDung()
  if (ds.dangTai || banKhai.dangTai) return <DangTai />
  if (ds.loi || banKhai.loi) return <ThongBao loi={ds.loi || banKhai.loi} />
  const cuaToi = new Set(nguoiDung.thanhVien.filter((t) => t.vaiTro === 'DIEU_PHOI').map((t) => t.maToChuc))
  const nc = ds.duLieu.filter((n) => cuaToi.has(n.maToChuc) && n.trangThai === 'CONG_BO')
  const choDuyet = nc.reduce((t, n) => t + n.soChoDuyet, 0)
  const chuaCoCa = nc.filter((n) => n.soCa === 0)
  const soBanKhai = banKhai.duLieu.filter((g) => !g.laCuaToi).length
  return (
    <section>
      <h2>Việc của điều phối viên</h2>
      {choDuyet > 0 && (
        <Viec loai="gap" tieuDe={`${choDuyet} đăng ký đang chờ bạn duyệt`}
          moTa="Cần duyệt trước giờ ca bắt đầu.">
          <Link className="nut" to="/dieu-phoi">Xét duyệt đăng ký</Link>
        </Viec>
      )}
      {soBanKhai > 0 && (
        <Viec loai="gap" tieuDe={`${soBanKhai} bản khai đóng góp đang chờ duyệt`}>
          <Link className="nut" to="/dong-gop/cho-duyet">Duyệt đóng góp</Link>
        </Viec>
      )}
      {chuaCoCa.map((n) => (
        <Viec key={n.maNhuCau} tieuDe="Nhu cầu đã công bố nhưng chưa có ca làm" moTa={n.tieuDe}>
          <Link className="nut" to={`/nhu-cau/${n.maNhuCau}/ca`}>Tạo ca làm</Link>
        </Viec>
      ))}
      {choDuyet === 0 && soBanKhai === 0 && chuaCoCa.length === 0 && (
        <Viec loai="xong" tieuDe="Hiện không có việc nào chờ bạn xử lý">
          <Link className="nut phu" to="/dieu-phoi">Xem các ca</Link>
        </Viec>
      )}
    </section>
  )
}
