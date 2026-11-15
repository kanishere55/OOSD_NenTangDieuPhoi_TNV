import { hienKm } from '../tienIch'

// Khoảng cách gần đúng từ nơi ở đã lưu tới tâm vùng công khai của ca.
export function ViTriCuaToi({ viTri, vung }) {
  if (!viTri || vung?.viDoVung == null) return null
  const R = 6371, rad = (d) => d * Math.PI / 180
  const a = Math.sin(rad(vung.viDoVung - viTri.viDo) / 2) ** 2
    + Math.cos(rad(viTri.viDo)) * Math.cos(rad(vung.viDoVung)) * Math.sin(rad(vung.kinhDoVung - viTri.kinhDo) / 2) ** 2
  const km = 2 * R * Math.asin(Math.min(1, Math.sqrt(a)))
  return <span className="mo"> · cách nơi ở của bạn {hienKm(km)}</span>
}
