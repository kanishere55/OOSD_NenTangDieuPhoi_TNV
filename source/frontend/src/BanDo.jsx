import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import anhGhim from 'leaflet/dist/images/marker-icon.png'
import anhGhim2x from 'leaflet/dist/images/marker-icon-2x.png'
import anhBong from 'leaflet/dist/images/marker-shadow.png'

// Bản đồ dùng Leaflet với nền OpenStreetMap (miễn phí, không cần khóa API).
const GHIM = L.icon({
  iconUrl: anhGhim, iconRetinaUrl: anhGhim2x, shadowUrl: anhBong,
  iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41]
})
// Khi chưa có vị trí nào thì mở bản đồ ở khu vực Thành phố Hồ Chí Minh.
const TAM_MAC_DINH = [10.8, 106.7]
const MAU_VUNG = '#17604e'

function useBanDo(cao, onBam) {
  const khung = useRef(null)
  const banDo = useRef(null)
  const lop = useRef(null)
  const bam = useRef(onBam)
  bam.current = onBam
  useEffect(() => {
    const m = L.map(khung.current, { scrollWheelZoom: false, attributionControl: true }).setView(TAM_MAC_DINH, 12)
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '&copy; OpenStreetMap'
    }).addTo(m)
    lop.current = L.layerGroup().addTo(m)
    m.on('click', (e) => bam.current?.(e.latlng))
    banDo.current = m
    return () => { m.remove(); banDo.current = null }
  }, [])
  useEffect(() => { banDo.current?.invalidateSize() }, [cao])
  return { khung, banDo, lop }
}

// Các vùng tròn công khai của ca hoặc nhu cầu. Mỗi vùng: { viDo, kinhDo, banKinh, nhan, onBam }.
// Vị trí của người xem (nếu có) hiện bằng một ghim.
export function BanDoVung({ vung, viTriCuaToi, cao = 320, nhanViTriCuaToi = 'Vị trí của bạn' }) {
  const { khung, banDo, lop } = useBanDo(cao)
  useEffect(() => {
    const m = banDo.current
    if (!m) return
    lop.current.clearLayers()
    const diem = []
    for (const v of vung.filter((x) => x.viDo != null)) {
      const vong = L.circle([v.viDo, v.kinhDo], { radius: v.banKinh ?? 1000, color: MAU_VUNG, weight: 2, fillOpacity: 0.15 })
      if (v.nhan) vong.bindTooltip(v.nhan, { sticky: true })
      if (v.onBam) vong.on('click', v.onBam)
      vong.addTo(lop.current)
      diem.push(vong.getBounds())
    }
    if (viTriCuaToi?.viDo != null) {
      L.marker([viTriCuaToi.viDo, viTriCuaToi.kinhDo], { icon: GHIM }).bindTooltip(nhanViTriCuaToi).addTo(lop.current)
      diem.push(L.latLngBounds([[viTriCuaToi.viDo, viTriCuaToi.kinhDo], [viTriCuaToi.viDo, viTriCuaToi.kinhDo]]))
    }
    if (diem.length) m.fitBounds(diem.reduce((a, b) => a.extend(b)), { padding: [24, 24], maxZoom: 15 })
  }, [vung, viTriCuaToi, banDo, lop, nhanViTriCuaToi])
  return <div ref={khung} className="ban-do" style={{ height: cao }} role="img" aria-label="Bản đồ khu vực" />
}

// Bản đồ có một ghim ở vị trí chính xác (chỉ dùng ở phần thông tin riêng).
export function BanDoGhim({ viDo, kinhDo, nhan, cao = 300 }) {
  const { khung, banDo, lop } = useBanDo(cao)
  useEffect(() => {
    const m = banDo.current
    if (!m || viDo == null) return
    lop.current.clearLayers()
    const g = L.marker([viDo, kinhDo], { icon: GHIM }).addTo(lop.current)
    if (nhan) g.bindTooltip(nhan, { permanent: true, direction: 'top', offset: [0, -36] })
    m.setView([viDo, kinhDo], 16)
  }, [viDo, kinhDo, nhan, banDo, lop])
  return <div ref={khung} className="ban-do" style={{ height: cao }} role="img" aria-label="Bản đồ vị trí" />
}

// Chọn một vị trí: bấm vào bản đồ, kéo ghim, tìm theo địa chỉ hoặc lấy vị trí hiện tại của thiết bị.
// giaTri = { viDo, kinhDo } hoặc null. banKinhXemTruoc (mét): vẽ thêm vòng tròn để người dùng thấy phạm vi.
export function ChonViTri({ giaTri, onDoi, cao = 340, tatDi, banKinhXemTruoc, goiYTimKiem = 'ví dụ: Nhà văn hóa khu phố 3, Phường An Bình' }) {
  const doi = useRef(onDoi)
  doi.current = onDoi
  const { khung, banDo, lop } = useBanDo(cao, (latlng) => { if (!tatDi) doi.current({ viDo: lam6(latlng.lat), kinhDo: lam6(latlng.lng) }) })
  const [tuKhoa, setTuKhoa] = useState('')
  const [ketQua, setKetQua] = useState(null)
  const [dangTim, setDangTim] = useState(false)
  const [loi, setLoi] = useState(null)
  const daCanGiua = useRef(false)

  useEffect(() => {
    const m = banDo.current
    if (!m) return
    lop.current.clearLayers()
    if (giaTri?.viDo == null) return
    const g = L.marker([giaTri.viDo, giaTri.kinhDo], { icon: GHIM, draggable: !tatDi }).addTo(lop.current)
    g.on('dragend', () => { const p = g.getLatLng(); doi.current({ viDo: lam6(p.lat), kinhDo: lam6(p.lng) }) })
    if (banKinhXemTruoc) L.circle([giaTri.viDo, giaTri.kinhDo], { radius: banKinhXemTruoc, color: MAU_VUNG, weight: 1, fillOpacity: 0.08 }).addTo(lop.current)
    if (!daCanGiua.current || !m.getBounds().contains([giaTri.viDo, giaTri.kinhDo])) {
      m.setView([giaTri.viDo, giaTri.kinhDo], Math.max(m.getZoom(), 15))
      daCanGiua.current = true
    }
  }, [giaTri, tatDi, banKinhXemTruoc, banDo, lop])

  const tim = async (e) => {
    e?.preventDefault()
    if (!tuKhoa.trim()) return
    setDangTim(true); setLoi(null); setKetQua(null)
    try {
      const r = await fetch('https://nominatim.openstreetmap.org/search?format=json&limit=5&countrycodes=vn&accept-language=vi&q=' + encodeURIComponent(tuKhoa.trim()))
      const ds = await r.json()
      setKetQua(ds)
      if (ds.length === 0) setLoi('Không tìm thấy địa chỉ này. Hãy thử gõ ngắn hơn, ví dụ tên đường và phường, hoặc bấm thẳng vào bản đồ.')
    } catch {
      setLoi('Không tìm được lúc này. Bạn hãy bấm thẳng vào vị trí trên bản đồ.')
    } finally {
      setDangTim(false)
    }
  }
  const chonKetQua = (k) => {
    onDoi({ viDo: lam6(Number(k.lat)), kinhDo: lam6(Number(k.lon)) })
    setKetQua(null)
    banDo.current?.setView([Number(k.lat), Number(k.lon)], 17)
  }
  const viTriHienTai = () => {
    if (!navigator.geolocation) { setLoi('Trình duyệt này không lấy được vị trí. Hãy bấm vào bản đồ.'); return }
    setLoi(null)
    navigator.geolocation.getCurrentPosition(
      (p) => { onDoi({ viDo: lam6(p.coords.latitude), kinhDo: lam6(p.coords.longitude) }); banDo.current?.setView([p.coords.latitude, p.coords.longitude], 16) },
      () => setLoi('Không lấy được vị trí hiện tại. Bạn hãy cho phép trình duyệt dùng vị trí, hoặc bấm vào bản đồ.'),
      { enableHighAccuracy: true, timeout: 10000 })
  }

  return (
    <div className="chon-vi-tri">
      {!tatDi && (
        <div className="tim-dia-chi">
          <label>Tìm địa chỉ
            <span className="hang-tim">
              <input value={tuKhoa} placeholder={goiYTimKiem} onChange={(e) => setTuKhoa(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') tim(e) }} />
              <button type="button" className="phu" onClick={tim} disabled={dangTim}>Tìm</button>
            </span>
          </label>
          {ketQua?.length > 0 && (
            <ul className="ds-ket-qua-tim">
              {ketQua.map((k) => (
                <li key={k.place_id}><button type="button" className="lien-ket" onClick={() => chonKetQua(k)}>{k.display_name}</button></li>
              ))}
            </ul>
          )}
          {loi && <small className="loi-o">{loi}</small>}
        </div>
      )}
      <div ref={khung} className="ban-do" style={{ height: cao }} role="application" aria-label="Bản đồ chọn vị trí" />
      {!tatDi && (
        <div className="hang-nut" style={{ marginTop: 8 }}>
          <span className="mo" style={{ marginRight: 'auto' }}>
            {giaTri?.viDo != null ? 'Đã chọn vị trí. Có thể kéo ghim để chỉnh lại.' : 'Bấm vào bản đồ để đặt ghim.'}
          </span>
          <button type="button" className="phu" onClick={viTriHienTai}>Dùng vị trí hiện tại</button>
          {giaTri?.viDo != null && <button type="button" className="phu" onClick={() => onDoi(null)}>Bỏ vị trí</button>}
        </div>
      )}
    </div>
  )
}

const lam6 = (x) => Math.round(x * 1e6) / 1e6
