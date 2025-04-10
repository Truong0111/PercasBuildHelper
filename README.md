# Percas Build Helper

Đây là công cụ hỗ trợ build game Unity. Percas Build Helper giúp quản lý việc build game Android một cách dễ dàng, theo
dõi phiên bản và kiểm tra hiệu suất game một cách hiệu quả.

## Tính năng

### Build Management

- Build game Android nhanh chóng và đơn giản
- Hỗ trợ các loại build phổ biến (Development, Clean Build, Final)
- Tự động xử lý keystore
- Quản lý thư mục build
- Đặt tên file build theo ý thích

### Version Management

- Tự động tạo mã phiên bản
- Quản lý tên phiên bản
- Chọn cách đánh số phiên bản (Tự động hoặc Tùy chỉnh)
- Tăng/giảm phiên bản với một cú nhấp chuột

### Performance Watching

- Xem FPS trực tiếp khi chơi game
- Kiểm tra số lượng draw call
- Theo dõi số lượng batch
- Xem số lượng tam giác và đỉnh
- Kiểm tra bộ nhớ:
    - Tổng bộ nhớ hệ thống
    - Bộ nhớ đang sử dụng
    - Bộ nhớ dành cho texture
    - Bộ nhớ dành cho mesh
    - Số lượng material đang dùng

### Utility 

- Điều chỉnh tốc độ game (Time scale)
- Tùy chỉnh fixed delta time
- Đặt giới hạn FPS
- On/Off hiển thị log
- Xem thống kê hiệu suất

## Cài đặt

### Import qua Package Manager
1. Mở Unity Package Manager (Window > Package Manager)
2. Nhấn nút "+" ở góc trên bên trái
3. Chọn "Add package from git URL"
4. Dán URL: `https://github.com/Truong0111/PercasBuildHelper.git`
5. Nhấn "Add"

### Kiểm tra cài đặt
1. Sau khi import, vào menu Unity
2. Tìm menu `Percas > Build Helper`
3. Nếu thấy cửa sổ hiện lên thì đã cài đặt thành công

## Hướng dẫn sử dụng

### Build game
1. Điền thông tin bản build (tên game, package name, v.v.)
2. Cài đặt keystore
3. Chọn kiểu build phù hợp
4. Nhấn "Build"

### Quản lý phiên bản
1. Đặt phiên bản chính và số phiên bản
2. Chọn cách đánh số phiên bản
3. Dùng nút tăng/giảm để điều chỉnh
4. Nhấn "Apply" để lưu

### Theo dõi hiệu suất
1. Bật thống kê hiệu suất trong tab Utility
2. Vào chế độ play để xem số liệu
3. Theo dõi hiệu suất game trong khi chơi

## Giấy phép

Copyright (c) Scary. All rights reserved.
