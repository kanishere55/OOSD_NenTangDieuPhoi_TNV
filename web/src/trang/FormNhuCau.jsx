import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DuongDan, HopXacNhan, ThongBao, TrangThai, useNguoiDung, useThaoTac } from '../chung'
import { GIAI_THICH_NHU_CAU } from '../tienIch'

const TRONG = { maToChuc: '', tieuDe: '', tomTatCongKhai: '', khuVucChung: '', huongDanRieng: '' }

// M07 Nhu cầu: tạo, sửa, rà soát và công bố, đóng hoặc hủy. Nội dung công khai và hướng dẫn riêng
// tách thành hai phần (BR01, BR05, BR16).
export default function FormNhuCau() {
  const { ma } = useParams()
  const dieuHuong = useNavigate()
  const { nguoiDung } = useNguoiDung()
  const [nc, setNc] = useState(null)
  const [form, setForm] = useState(TRONG)
  const [dangTai, setDangTai] = useState(!!ma)
  const [daXacNhanRaSoat, setDaXacNhanRaSoat] = useState(false)
  const [hoi, setHoi] = useState(null)   // 'dong' hoặc 'huy'
  const tt = useThaoTac()

  const toChucDaiDien = nguoiDung?.thanhVien.filter((t) => t.vaiTro === 'DAI_DIEN') ?? []

  const tai = async () => {
    const d = await api.get('/api/nhu-cau/' + ma)
    setNc(d)
    setForm({ maToChuc: d.maToChuc, tieuDe: d.tieuDe, tomTatCongKhai: d.tomTatCongKhai, khuVucChung: d.khuVucChung, huongDanRieng: d.huongDanRieng ?? '' })
    return d
  }
  useEffect(() => {
    setNc(null); setForm(TRONG); setDaXacNhanRaSoat(false)
    if (ma) { setDangTai(true); tai().catch((e) => tt.setLoi(e.message)).finally(() => setDangTai(false)) }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ma])
  useEffect(() => {
    if (!ma && toChucDaiDien.length && !form.maToChuc) setForm((f) => ({ ...f, maToChuc: toChucDaiDien[0].maToChuc }))
  }, [ma, toChucDaiDien, form.maToChuc])

  if (dangTai) return <DangTai />
  const duongDan = <DuongDan muc={[{ ten: 'Nhu cầu của tổ chức', den: '/nhu-cau' }, { ten: ma ? (nc?.tieuDe ?? 'Nhu cầu') : 'Tạo nhu cầu mới' }]} />
  if (ma && !nc) return <>{duongDan}<ThongBao loi={tt.loi} /></>

  const laDaiDien = !ma ? toChucDaiDien.length > 0 : nc.laDaiDien
  const trangThai = nc?.trangThai ?? 'NHAP'
  const laNhap = trangThai === 'NHAP'
  const daKetThuc = trangThai === 'DONG' || trangThai === 'DA_HUY'
  // BR05: phần công khai sửa được khi là bản nháp, hoặc đã công bố mà chưa có ca.
  const suaCongKhai = laDaiDien && (laNhap || (trangThai === 'CONG_BO' && nc.soCa === 0))
  const suaRieng = laDaiDien && !daKetThuc
  const daSuaCongKhai = nc && (form.tieuDe !== nc.tieuDe || form.tomTatCongKhai !== nc.tomTatCongKhai || form.khuVucChung !== nc.khuVucChung)
  const daSuaRieng = nc && form.huongDanRieng !== (nc.huongDanRieng ?? '')
  const daRaSoat = daXacNhanRaSoat && !daSuaCongKhai
  const doi = (truong) => (e) => { setForm({ ...form, [truong]: e.target.value }); tt.setThanhCong(null) }
  const thieu = !form.tieuDe.trim() || !form.tomTatCongKhai.trim() || !form.khuVucChung.trim()

  const luu = async () => {
    if (thieu) { tt.setLoi('Hãy điền đủ tiêu đề, tóm tắt công khai và khu vực chung.'); return }
    if (!ma) {
      const kq = await tt.chay(() => api.post('/api/nhu-cau', form))
      if (kq) dieuHuong('/nhu-cau/' + kq.maNhuCau, { replace: true })
      return
    }
    const veNhap = trangThai === 'CONG_BO' && daSuaCongKhai
    await tt.chay(async () => {
      await api.put('/api/nhu-cau/' + ma, form)
      await tai()
      setDaXacNhanRaSoat(false)
    }, veNhap ? 'Đã lưu. Vì nội dung công khai thay đổi, nhu cầu trở về bản nháp. Hãy rà soát và công bố lại.' : 'Đã lưu.')
  }

  const congBo = async () => {
    // Gửi kèm số phiên bản đã rà soát; máy chủ từ chối nếu nội dung đã bị sửa sau đó (BR05).
    await tt.chay(async () => {
      await api.post(`/api/nhu-cau/${ma}/cong-bo`, { phienBan: nc.phienBanCongKhai })
      await tai()
    }, 'Đã công bố nhu cầu. Điều phối viên có thể tạo ca làm từ bây giờ.')
  }

  const ketThuc = async () => {
    const viec = hoi
    await tt.chay(async () => {
      await api.post(`/api/nhu-cau/${ma}/${viec}`)
      await tai()
    }, viec === 'dong' ? 'Đã đóng nhu cầu.' : 'Đã hủy nhu cầu. Các ca còn mở và đăng ký của chúng cũng đã bị hủy.')
    setHoi(null)
  }

  const buoc = !ma ? 1 : laNhap ? 2 : 3
  const duocDong = laDaiDien && trangThai === 'CONG_BO' && nc.soCa > 0 && nc.soCaChuaXong === 0
  const duocHuy = laDaiDien && nc && !daKetThuc && !nc.coCaDaBatDau

  return (
    <>
      {duongDan}
      <h1>{ma ? nc.tieuDe : 'Tạo nhu cầu mới'}</h1>
      {nc && <p><TrangThai ma={trangThai} /> <span className="mo">{GIAI_THICH_NHU_CAU[trangThai]}</span></p>}
      {!daKetThuc && (
        <ol className="cac-buoc">
          <li className={buoc > 1 ? 'xong' : 'dang'}>Soạn nội dung và lưu nháp</li>
          <li className={buoc > 2 ? 'xong' : buoc === 2 ? 'dang' : ''}>Rà soát và công bố</li>
          <li className={buoc === 3 ? 'dang' : ''}>Điều phối viên tạo ca làm</li>
        </ol>
      )}
      {!laDaiDien && nc && <div className="thong-bao tin">Bạn đang xem ở vai trò điều phối viên. Chỉ đại diện của tổ chức mới sửa được nội dung nhu cầu.</div>}

      <fieldset>
        <legend>Nội dung công khai</legend>
        <p className="mo">Ai cũng xem được phần này, kể cả người chưa đăng nhập. <strong>Không ghi họ tên, địa chỉ cụ thể hay hoàn cảnh của người được hỗ trợ.</strong></p>
        {!ma && toChucDaiDien.length > 1 && (
          <label>Tổ chức
            <select value={form.maToChuc} onChange={doi('maToChuc')}>
              {toChucDaiDien.map((t) => <option key={t.maToChuc} value={t.maToChuc}>{t.tenToChuc}</option>)}
            </select>
          </label>
        )}
        {!ma && toChucDaiDien.length === 1 && <p>Tổ chức: <strong>{toChucDaiDien[0].tenToChuc}</strong></p>}
        <label>Tiêu đề
          <small>Nêu ngắn gọn việc cần hỗ trợ, ví dụ: Hướng dẫn dùng điện thoại thông minh cho người lớn tuổi.</small>
          <input value={form.tieuDe} onChange={doi('tieuDe')} disabled={!suaCongKhai} maxLength={150} />
        </label>
        <label>Tóm tắt công khai
          <small>Mô tả công việc tình nguyện viên sẽ làm, một đến ba câu.</small>
          <textarea rows={3} value={form.tomTatCongKhai} onChange={doi('tomTatCongKhai')} disabled={!suaCongKhai} />
        </label>
        <label>Khu vực chung
          <small>Chỉ ghi đến phường, xã, ví dụ: Phường An Bình. Địa chỉ cụ thể ghi ở phần hướng dẫn riêng bên dưới.</small>
          <input value={form.khuVucChung} onChange={doi('khuVucChung')} disabled={!suaCongKhai} maxLength={150} />
        </label>
        {laDaiDien && trangThai === 'CONG_BO' && nc.soCa === 0 && (
          <p className="thieu-ky-nang">Nhu cầu đã công bố. Nếu bạn sửa phần này rồi lưu, nhu cầu sẽ trở về bản nháp và phải rà soát, công bố lại.</p>
        )}
        {laDaiDien && trangThai === 'CONG_BO' && nc.soCa > 0 && (
          <p className="mo">Nhu cầu đã có ca làm nên phần công khai không sửa được nữa. Phần hướng dẫn riêng vẫn sửa được.</p>
        )}
      </fieldset>

      <fieldset className="rieng">
        <legend>Hướng dẫn điều phối riêng (không bắt buộc)</legend>
        <p className="mo">Chỉ đại diện, điều phối viên của tổ chức và tình nguyện viên đã được phân công mới xem được, và chỉ đến khi ca kết thúc. Chỉ ghi những gì cần cho việc đến làm.</p>
        <label>Nội dung hướng dẫn
          <small>Ví dụ: địa điểm cụ thể, gặp ai khi đến, cần mang theo gì, lưu ý khi tiếp xúc.</small>
          <textarea rows={4} value={form.huongDanRieng} onChange={doi('huongDanRieng')} disabled={!suaRieng} />
        </label>
      </fieldset>

      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />

      {laDaiDien && !daKetThuc && (
        <div className="hang-nut">
          <button className={laNhap ? 'phu' : ''} disabled={tt.dangGui || (ma && !daSuaCongKhai && !daSuaRieng)} onClick={luu}>
            {ma ? 'Lưu thay đổi' : 'Lưu bản nháp'}
          </button>
          {ma && (daSuaCongKhai || daSuaRieng) && <span className="thieu-ky-nang">Bạn có thay đổi chưa lưu.</span>}
          {!ma && <Link to="/nhu-cau">Thôi, quay lại danh sách</Link>}
        </div>
      )}

      {ma && laNhap && laDaiDien && (
        <section className="the" style={{ marginTop: 22 }}>
          <h2 style={{ marginTop: 0 }}>Rà soát và công bố</h2>
          <p>Trước khi công bố, hãy đọc lại đúng những gì người ngoài sẽ nhìn thấy:</p>
          <div className="xem-truoc">
            <p><strong>{nc.tieuDe}</strong></p>
            <p>{nc.tomTatCongKhai}</p>
            <p className="mo">Khu vực: {nc.khuVucChung}</p>
          </div>
          {daSuaCongKhai && <p className="thieu-ky-nang">Bạn vừa sửa nội dung công khai. Hãy bấm Lưu thay đổi trước, rồi rà soát bản đã lưu.</p>}
          <label className="o-chon" style={{ marginBottom: 14 }}>
            <input type="checkbox" checked={daRaSoat} disabled={tt.dangGui || daSuaCongKhai} onChange={(e) => setDaXacNhanRaSoat(e.target.checked)} />
            <span>Tôi đã đọc lại nội dung trên và xác nhận không để lộ thông tin của người được hỗ trợ.</span>
          </label>
          <button className="lon" disabled={tt.dangGui || !daRaSoat} onClick={congBo}>Công bố nhu cầu</button>
          {!daRaSoat && <p className="mo">Nút Công bố bấm được sau khi bạn đánh dấu ô xác nhận ở trên.</p>}
        </section>
      )}

      {nc && trangThai !== 'NHAP' && (
        <section className="the" style={{ marginTop: 22 }}>
          <h2 style={{ marginTop: 0 }}>Ca làm của nhu cầu</h2>
          <p>{nc.soCa === 0 ? 'Nhu cầu chưa có ca làm nào.' : nc.soCaChuaXong === 0 ? `Nhu cầu có ${nc.soCa} ca làm, tất cả đã kết thúc hoặc đã hủy.` : `Nhu cầu có ${nc.soCa} ca làm, trong đó ${nc.soCaChuaXong} ca chưa xong.`}</p>
          <Link className="nut" to={`/nhu-cau/${ma}/ca`}>{nc.laDieuPhoi && trangThai === 'CONG_BO' ? 'Xem và tạo ca làm' : 'Xem ca làm'}</Link>
        </section>
      )}

      {(duocDong || duocHuy) && (
        <section className="the" style={{ marginTop: 22 }}>
          <h2 style={{ marginTop: 0 }}>Kết thúc nhu cầu</h2>
          {duocDong && (
            <>
              <p>Tất cả các ca đã kết thúc hoặc đã hủy. Đóng nhu cầu để khép lại việc này; sau khi đóng không tạo thêm ca được.</p>
              <button onClick={() => setHoi('dong')}>Đóng nhu cầu</button>
            </>
          )}
          {duocHuy && (
            <>
              <p>Nếu việc này không còn cần nữa, bạn có thể hủy nhu cầu khi chưa có ca nào bắt đầu.</p>
              <button className="phu nguy-hiem" onClick={() => setHoi('huy')}>Hủy nhu cầu</button>
            </>
          )}
        </section>
      )}

      {hoi === 'dong' && (
        <HopXacNhan tieuDe="Đóng nhu cầu này?" nhanDongY="Đóng nhu cầu" nguyHiem={false} dangGui={tt.dangGui} onDongY={ketThuc} onThoi={() => setHoi(null)}>
          <p><strong>{nc.tieuDe}</strong></p>
          <p>Sau khi đóng, nhu cầu không tạo thêm ca được. Lịch sử ca, đăng ký và đóng góp vẫn được giữ.</p>
        </HopXacNhan>
      )}
      {hoi === 'huy' && (
        <HopXacNhan tieuDe="Hủy nhu cầu này?" nhanDongY="Hủy nhu cầu" dangGui={tt.dangGui} onDongY={ketThuc} onThoi={() => setHoi(null)}>
          <p><strong>{nc.tieuDe}</strong></p>
          {nc.soCaChuaXong > 0
            ? <p>Nhu cầu đang có {nc.soCaChuaXong} ca còn mở. Các ca này và mọi đăng ký của tình nguyện viên vào chúng sẽ bị hủy theo.</p>
            : <p>Nhu cầu sẽ chuyển sang Đã hủy và không tạo thêm ca được.</p>}
          <p>Thao tác này không hoàn tác được.</p>
        </HopXacNhan>
      )}
    </>
  )
}
