# Cát dạng grid → texture → SpriteRenderer

Bản chạy mới: `BuildPixelSand/SandJam-Scene3D.exe`.

- `SandPourSimulation`: mô phỏng ô chiếm chỗ, một luồng rót, chỉ xuống/ngang; dữ liệu hạt dùng struct, danh sách cấp dung lượng khi tạo level để tránh tạo object mới mỗi hạt.
- `SandBoardTextureView`: cả bảng dùng chung một Texture2D RGBA32 84 × 112. Cell (column,row) ánh xạ pixel tại index = row * width + column. 1 cell = 1 pixel. Point filtering, Clamp, không mipmap.
- Texture dùng hai Color32 buffer tái sử dụng: nền/cát đã ổn định và ảnh hiện tại. Pixel hạt ở frame trước được khôi phục trước khi vẽ vị trí mới, tránh vệt cát và chồng hình.
- Chỉ upload khi thay đổi, tối đa một lần trong LateUpdate mỗi frame, dù mô phỏng chạy nhiều bước. Một lần upload tối đa 37.632 byte (36,75 KiB) ở kích thước tutorial.
- Một SpriteRenderer, sprite FullRect (4 vertex / 2 triangle), shader unlit PixelSandSprite. Vật cản nằm cùng texture. Các MeshRenderer cũ của vùng và vật cản bị tắt khi Play; reference mesh được giữ cho editor/scene tương thích, không tạo bản mesh runtime.
- Sprite và texture/material runtime được giải phóng khi đóng scene. Không tạo GameObject cho từng hạt.

Giữ nguyên 5 ô chờ, quy đổi uiDivider, giấu màu/số của vùng khóa và chờ phủ kín mới mở vùng liên quan.

Kiểm tra Windows: build thành công, mô phỏng 5 vùng/không chồng/không đi lên/bảo toàn hạt đạt; kiểm tra runtime xác nhận texture 84×112, sprite 4 vertex, mesh cũ tắt, upload texture, thắng/thua/chơi lại/resize đạt. Ảnh đã kiểm tra trong TestResults/PixelSand/Smoke.

Chưa đo CPU/GPU/FPS trên điện thoại và chưa build Android/iOS. Tối ưu lần này tập trung vào renderer cát; nhân vật, UI và logic gameplay ngoài mô phỏng cát vẫn theo bản prototype hiện tại. Khi đo trên máy thật cần theo dõi thời gian SandPourSimulation.Step, Texture2D.Apply, GC Alloc, draw calls và mức tiêu thụ pin.
