import { Link } from 'react-router-dom'
import { api } from '../api'
import { DangTai, DauTrang, ThongBao, TrangThai, Trong, useNguoiDung, useTai } from '../chung'
import { GIAI_THICH_NHU_CAU } from '../tienIch'

// M06 Danh sách nhu cầu của tổ chức.
export default function DanhSachNhuCau() {
  const { laDaiDien } = useNguoiDung()
  const ds = useTai(() => api.get('/api/nhu-cau'), [])

  if (ds.dangTai) return <DangTai />
  if (ds.loi) return <ThongBao loi={ds.loi} />
  return (
    <>
      <DauTrang tieuDe="Nhu cầu của tổ chức">
        {laDaiDien && <Link className="nut" to="/nhu-cau/moi">Tạo nhu cầu mới</Link>}
      </DauTrang>
      {ds.duLieu.length === 0 ? (
        <Trong>
          <p>Tổ chức chưa có nhu cầu nào.</p>
          {laDaiDien
            ? <div className="hang-nut"><Link className="nut" to="/nhu-cau/moi">Tạo nhu cầu đầu tiên</Link></div>
            : <p>Đại diện của tổ chức là người tạo nhu cầu.</p>}
        </Trong>
      ) : (
        <ul className="ds-dong">
          {ds.duLieu.map((n) => (
            <li key={n.maNhuCau}>
              <div className="than">
                <p><strong>{n.tieuDe}</strong></p>
                <p className="mo">{n.tenToChuc} · {n.khuVucChung}{!n.coViTri && n.trangThai !== 'DA_HUY' && ' · chưa đặt vị trí trên bản đồ'}{n.soAnh > 0 && ` · ${n.soAnh} ảnh`}</p>
                <p><TrangThai ma={n.trangThai} /> <span className="mo">{GIAI_THICH_NHU_CAU[n.trangThai]}</span></p>
                {n.trangThai !== 'NHAP' && (
                  <p>
                    {n.soCa === 0 ? 'Chưa có ca làm' : n.soCaChuaXong === 0 ? `${n.soCa} ca làm, tất cả đã xong` : `${n.soCa} ca làm, ${n.soCaChuaXong} ca chưa xong`}
                    {n.soChoDuyet > 0 && <span className="thieu-ky-nang"> · {n.soChoDuyet} đăng ký chờ duyệt</span>}
                  </p>
                )}
              </div>
              <div className="hang-nut">
                {n.trangThai === 'NHAP' && laDaiDien && <Link className="nut" to={'/nhu-cau/' + n.maNhuCau}>Rà soát và công bố</Link>}
                {n.trangThai === 'NHAP' && !laDaiDien && <Link className="nut phu" to={'/nhu-cau/' + n.maNhuCau}>Xem nội dung</Link>}
                {n.trangThai !== 'NHAP' && <Link className="nut" to={`/nhu-cau/${n.maNhuCau}/ca`}>Ca làm</Link>}
                {n.trangThai !== 'NHAP' && <Link className="nut phu" to={'/nhu-cau/' + n.maNhuCau}>Nội dung nhu cầu</Link>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
