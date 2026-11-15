import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import { ChonViTri } from '../BanDo'
import { ChonCapDo, DangTai, DauTrang, NutChonAnh, ThongBao, TrangThai, useTai, useThaoTac } from '../chung'
import DeXuatKyNang from './DeXuatKyNang'
import { BUOI, CAP_DO, GIAI_THICH_CAP_DO, THU_TRONG_TUAN, hienGio } from '../tienIch'

// M04 Hồ sơ và kỹ năng: kỹ năng kèm cấp độ và chứng chỉ, lịch rảnh, nơi ở gần đúng, đề xuất kỹ năng mới.
export default function HoSo() {
  const hoSo = useTai(() => api.get('/api/ho-so'), [])
  const danhMuc = useTai(() => api.get('/api/ky-nang'), [])

  if ((hoSo.dangTai && !hoSo.duLieu) || (danhMuc.dangTai && !danhMuc.duLieu)) return <DangTai />
  if (hoSo.loi || danhMuc.loi) return <ThongBao loi={hoSo.loi || danhMuc.loi} />
  const h = hoSo.duLieu
  const tc = h.mucTinCay
  const tong = tc.soLanCoMat + tc.soLanVang
  return (
    <>
      <DauTrang tieuDe="Hồ sơ và kỹ năng"
        moTa="Bạn chỉ đăng ký được ca khi có đủ kỹ năng, đúng cấp độ mà ca yêu cầu." />
      <section className="the">
        <dl className="thong-tin">
          <dt>Họ tên</dt><dd>{h.tenHienThi}</dd>
          <dt>Kỹ năng đã lưu</dt><dd>{h.kyNang.length === 0 ? 'Chưa có' : h.kyNang.map((k) => `${k.tenKyNang} (${k.tenCapDo.toLowerCase()})`).join(', ')}</dd>
          <dt>Điểm danh</dt><dd>{tong === 0 ? 'Chưa có ca nào được ghi nhận' : `Có mặt ${tc.soLanCoMat} trên ${tong} ca đã ghi nhận`}</dd>
        </dl>
        <p className="mo">Đi tới: <a href="#ky-nang">Kỹ năng</a> · <a href="#lich-ranh">Lịch rảnh</a> · <a href="#noi-o">Nơi ở</a></p>
      </section>
      <PhanKyNang hoSo={h} danhMuc={danhMuc.duLieu} taiLai={hoSo.taiLai} />
      <PhanLichRanh hoSo={h} taiLai={hoSo.taiLai} />
      <PhanNoiO hoSo={h} taiLai={hoSo.taiLai} />
    </>
  )
}

function PhanKyNang({ hoSo, danhMuc, taiLai }) {
  // chon: { maKyNang: capDo }
  const [chon, setChon] = useState({})
  const [xemAnh, setXemAnh] = useState(null)
  const tt = useThaoTac()
  const ttCc = useThaoTac()
  useEffect(() => { setChon(Object.fromEntries(hoSo.kyNang.map((k) => [k.maKyNang, k.capDo]))) }, [hoSo])

  const daLuu = Object.fromEntries(hoSo.kyNang.map((k) => [k.maKyNang, k]))
  const coThayDoi = Object.keys(chon).length !== hoSo.kyNang.length
    || Object.entries(chon).some(([ma, c]) => daLuu[ma]?.capDo !== c)
  // Kỹ năng đã ngừng dùng nhưng còn trong hồ sơ vẫn hiện để bạn giữ hoặc bỏ.
  const tatCa = useMemo(() => {
    const ds = [...danhMuc]
    for (const k of hoSo.kyNang) if (!ds.some((x) => x.maKyNang === k.maKyNang))
      ds.push({ maKyNang: k.maKyNang, tenKyNang: k.tenKyNang, moTaKyNang: k.moTaKyNang, tenNhom: 'Kỹ năng khác', canChungChi: k.canChungChi })
    return ds
  }, [danhMuc, hoSo])
  const nhom = [...new Set(tatCa.map((k) => k.tenNhom ?? 'Kỹ năng khác'))]

  const doi = (ma) => {
    const moi = { ...chon }
    if (ma in moi) delete moi[ma]; else moi[ma] = 1
    setChon(moi); tt.setThanhCong(null)
  }
  const luu = async () => {
    const ok = await tt.chay(() => api.put('/api/ho-so/ky-nang', { kyNang: Object.entries(chon).map(([maKyNang, capDo]) => ({ maKyNang, capDo })) }),
      'Đã lưu kỹ năng của bạn.')
    if (ok) taiLai()
  }
  const taiChungChi = async (maKyNang, tep) => {
    const ok = await ttCc.chay(() => api.taiTep(`/api/ho-so/chung-chi/${maKyNang}`, tep),
      'Đã gửi chứng chỉ. Điều phối viên của tổ chức bạn đăng ký hoặc quản trị viên sẽ xác minh.')
    if (ok) taiLai()
  }

  return (
    <section className="the" id="ky-nang">
      <h2 style={{ marginTop: 0 }}>Kỹ năng của bạn</h2>
      <p className="mo-ta">Đánh dấu kỹ năng bạn có và chọn mức phù hợp. {Object.entries(GIAI_THICH_CAP_DO).map(([c, g]) => `${CAP_DO[c]}: ${g.toLowerCase()}.`).join(' ')}</p>
      {nhom.map((n) => (
        <div key={n} className="nhom-ky-nang">
          <h3>{n}</h3>
          {tatCa.filter((k) => (k.tenNhom ?? 'Kỹ năng khác') === n).map((k) => {
            const daChon = k.maKyNang in chon
            const luuRoi = daLuu[k.maKyNang]
            return (
              <div key={k.maKyNang} className={'dong-ky-nang' + (daChon ? ' da-chon' : '')}>
                <label className="o-chon">
                  <input type="checkbox" checked={daChon} onChange={() => doi(k.maKyNang)} />
                  <span><strong>{k.tenKyNang}</strong>{k.moTaKyNang && <small>{k.moTaKyNang}</small>}</span>
                </label>
                <span>{daChon && <ChonCapDo nhan={'Cấp độ ' + k.tenKyNang} giaTri={chon[k.maKyNang]} onDoi={(c) => setChon({ ...chon, [k.maKyNang]: c })} />}</span>
                <span className="trang-thai-chung-chi">
                  {k.canChungChi && !luuRoi && <span className="mo">Cần chứng chỉ. Lưu kỹ năng trước rồi gửi ảnh chứng chỉ.</span>}
                  {k.canChungChi && luuRoi && (
                    <>
                      {luuRoi.daXacMinh && <><TrangThai ma="DA_XAC_MINH" /> </>}
                      {luuRoi.chungChiDangCho && <><TrangThai ma="CHO_XAC_MINH" /> </>}
                      {luuRoi.chungChiBiTuChoi && <span className="thieu-ky-nang">Chứng chỉ chưa được chấp nhận: {luuRoi.chungChiBiTuChoi.lyDoTuChoi} </span>}
                      {!luuRoi.daXacMinh && !luuRoi.chungChiDangCho && !luuRoi.chungChiBiTuChoi && <span className="mo">Chưa gửi chứng chỉ. </span>}
                      <span className="hang-nut" style={{ marginTop: 4 }}>
                        <NutChonAnh nho nhan={luuRoi.daXacMinh || luuRoi.chungChiDangCho ? 'Gửi ảnh mới' : 'Gửi ảnh chứng chỉ'} tatDi={ttCc.dangGui}
                          onChon={(tep) => taiChungChi(k.maKyNang, tep)} />
                        {(luuRoi.chungChiDangCho || luuRoi.chungChiDaXacMinh) && (
                          <button type="button" className="phu nho" onClick={() => setXemAnh((luuRoi.chungChiDangCho ?? luuRoi.chungChiDaXacMinh).maChungChi)}>Xem ảnh đã gửi</button>
                        )}
                      </span>
                    </>
                  )}
                </span>
              </div>
            )
          })}
        </div>
      ))}
      <ThongBao loi={tt.loi || ttCc.loi} thanhCong={tt.thanhCong || ttCc.thanhCong} />
      <div className="hang-nut">
        <button className="lon" onClick={luu} disabled={tt.dangGui || !coThayDoi}>Lưu kỹ năng</button>
        {coThayDoi ? <span className="thieu-ky-nang">Bạn có thay đổi chưa lưu.</span>
          : <Link to="/tim-ca">Tìm ca hợp với kỹ năng</Link>}
        {hoSo.ngayCapNhatKyNang && <span className="mo">Lưu lần cuối lúc {hienGio(hoSo.ngayCapNhatKyNang)}</span>}
      </div>
      <DeXuatKyNang />
      {xemAnh && (
        <div className="nen-hop" onClick={() => setXemAnh(null)}>
          <div className="hop hop-anh" role="dialog" aria-modal="true" aria-label="Ảnh chứng chỉ">
            <img src={`/api/chung-chi/${xemAnh}/anh`} alt="Ảnh chứng chỉ đã gửi" />
            <div className="hang-nut cuoi-hop"><button className="phu" onClick={() => setXemAnh(null)}>Đóng</button></div>
          </div>
        </div>
      )}
    </section>
  )
}

function PhanLichRanh({ hoSo, taiLai }) {
  const khoa = (t, b) => `${t}-${b}`
  const [chon, setChon] = useState(new Set())
  const tt = useThaoTac()
  useEffect(() => { setChon(new Set(hoSo.lichRanh.map((l) => khoa(l.thu, l.buoi)))) }, [hoSo])
  const daLuu = new Set(hoSo.lichRanh.map((l) => khoa(l.thu, l.buoi)))
  const coThayDoi = chon.size !== daLuu.size || [...chon].some((k) => !daLuu.has(k))
  const doi = (k) => { const m = new Set(chon); m.has(k) ? m.delete(k) : m.add(k); setChon(m); tt.setThanhCong(null) }
  const luu = async () => {
    const ok = await tt.chay(() => api.put('/api/ho-so/lich-ranh', {
      lichRanh: [...chon].map((k) => { const [thu, buoi] = k.split('-'); return { thu: Number(thu), buoi } })
    }), 'Đã lưu lịch rảnh.')
    if (ok) taiLai()
  }
  return (
    <section className="the" id="lich-ranh">
      <h2 style={{ marginTop: 0 }}>Lịch rảnh trong tuần</h2>
      <p className="mo-ta">Đánh dấu những buổi bạn thường rảnh. Lịch rảnh chỉ dùng để gợi ý ca hợp với bạn, không bắt buộc.</p>
      <div className="bang-cuon">
        <table className="bang-lich-ranh">
          <thead><tr><th></th>{BUOI.map((b) => <th key={b.ma}>{b.ten}<br /><small className="mo">{b.gio}</small></th>)}</tr></thead>
          <tbody>
            {THU_TRONG_TUAN.map((t, i) => (
              <tr key={t}>
                <th style={{ textAlign: 'left' }}>{t}</th>
                {BUOI.map((b) => (
                  <td key={b.ma}>
                    <label><input type="checkbox" aria-label={`${t} buổi ${b.ten.toLowerCase()}`} checked={chon.has(khoa(i + 1, b.ma))} onChange={() => doi(khoa(i + 1, b.ma))} /></label>
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      <div className="hang-nut">
        <button onClick={luu} disabled={tt.dangGui || !coThayDoi}>Lưu lịch rảnh</button>
        {coThayDoi && <span className="thieu-ky-nang">Bạn có thay đổi chưa lưu.</span>}
      </div>
    </section>
  )
}

const KHOANG_CACH = [2, 5, 10, 15, 20, 30]

function PhanNoiO({ hoSo, taiLai }) {
  const [viTri, setViTri] = useState(null)
  const [noiO, setNoiO] = useState('')
  const [km, setKm] = useState(10)
  const tt = useThaoTac()
  useEffect(() => {
    setViTri(hoSo.viDo != null ? { viDo: hoSo.viDo, kinhDo: hoSo.kinhDo } : null)
    setNoiO(hoSo.noiO ?? '')
    setKm(hoSo.khoangCachToiDa ?? 10)
  }, [hoSo])
  const coThayDoi = (noiO.trim() !== (hoSo.noiO ?? '')) || km !== (hoSo.khoangCachToiDa ?? 10)
    || (viTri?.viDo ?? null) !== (hoSo.viDo ?? null) || (viTri?.kinhDo ?? null) !== (hoSo.kinhDo ?? null)
  const luu = async () => {
    const ok = await tt.chay(() => api.put('/api/ho-so/vi-tri', {
      noiO, viDo: viTri?.viDo ?? null, kinhDo: viTri?.kinhDo ?? null, khoangCachToiDa: km
    }), 'Đã lưu nơi ở và khoảng cách.')
    if (ok) taiLai()
  }
  return (
    <section className="the" id="noi-o">
      <h2 style={{ marginTop: 0 }}>Nơi ở và khoảng cách đi lại</h2>
      <div className="thong-bao tin">
        Vị trí này chỉ dùng để tính khoảng cách tới ca. Tổ chức và người khác không thấy vị trí của bạn,
        điều phối viên chỉ thấy khoảng cách gần đúng. Bạn có thể đặt ghim ở chỗ gần nhà, ví dụ đầu hẻm hoặc chợ.
      </div>
      <label>Khu vực bạn ở (không bắt buộc)
        <small>Ví dụ: Khu phố 2, Phường An Bình. Chỉ bạn thấy dòng này.</small>
        <input value={noiO} maxLength={150} onChange={(e) => setNoiO(e.target.value)} />
      </label>
      <ChonViTri giaTri={viTri} onDoi={setViTri} banKinhXemTruoc={viTri ? km * 1000 : null} goiYTimKiem="ví dụ: Chợ An Bình" />
      <label style={{ maxWidth: 320 }}>Chỉ gợi ý ca trong vòng
        <select value={km} onChange={(e) => setKm(Number(e.target.value))}>
          {KHOANG_CACH.map((k) => <option key={k} value={k}>{k} km</option>)}
        </select>
      </label>
      <ThongBao loi={tt.loi} thanhCong={tt.thanhCong} />
      <div className="hang-nut">
        <button onClick={luu} disabled={tt.dangGui || !coThayDoi}>Lưu nơi ở</button>
        {coThayDoi && <span className="thieu-ky-nang">Bạn có thay đổi chưa lưu.</span>}
      </div>
    </section>
  )
}
