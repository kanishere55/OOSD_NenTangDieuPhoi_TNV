import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Khi phát triển: giao diện chạy ở 127.0.0.1:5173, các yêu cầu /api và đăng nhập được chuyển tới máy chủ ứng dụng.
// Khi build: kết quả ghi vào wwwroot của máy chủ ứng dụng để chạy chung một địa chỉ.
const mayChu = 'http://127.0.0.1:5000'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    proxy: {
      '/api': mayChu,
      '/signin-oidc': mayChu,
      '/signout-callback-oidc': mayChu
    }
  },
  build: {
    outDir: '../backend/Civ09.Api/wwwroot',
    emptyOutDir: true,
    // Tách thư viện bản đồ thành tệp riêng để trình duyệt lưu đệm.
    rollupOptions: { output: { manualChunks: { 'ban-do': ['leaflet'] } } }
  }
})
