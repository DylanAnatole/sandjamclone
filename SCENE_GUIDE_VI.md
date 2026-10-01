# Scene gameplay Unity

## Mở và chơi

1. Trong Unity Hub, chọn Add project và trỏ tới **D:/Unity/testsand/CoreGameplayTest**. Dùng Unity **2022.3.62f3**.
2. Mở **Assets/Scenes/SandJamGameplay3D.unity** hoặc menu **Sand Jam → 3D Scene → Open scene**.
3. Nhấn **Play**. Chọn nhân vật đứng đầu hàng bằng chuột hoặc phím **1 / 2 / 3**.
4. **R** để chơi lại, **Space** để tạm dừng. Có nút gợi ý, tốc độ ×2 và bật/tắt âm.

Có thể chạy trực tiếp bản Windows tại **BuildScene3D/SandJam-Scene3D.exe**. Không tách exe khỏi thư mục dữ liệu và DLL bên cạnh.

## Những gì đã nằm sẵn trong scene

- **Main Camera** và **Key Light**.
- **Board**: 5 vùng màu riêng, mesh các ô chướng ngại và bộ đếm. Dữ liệu dùng tutorial 112 × 84.
- **Characters**: 13 đối tượng 3D có mesh từ asset nhân vật gốc, nhãn đạn và collider để chọn.
- **Lanes**: 3 vị trí đầu hàng, các panel phía sau.
- **Stash**: 6 vị trí chờ.
- **Projectiles**: nơi chứa đạn được tái sử dụng khi chạy.
- **SandJamSceneController**: liên kết JSON, camera, nhân vật, vùng màu, âm thanh và các tham số bắn.

Đây là các đối tượng thật được lưu trong file scene và xem được trong Hierarchy/Scene View, không phải các ảnh portrait của bản thử trước. UI điều khiển trên màn hình vẫn được vẽ bằng IMGUI; số đạn và số vùng dùng TextMesh trong scene.

## Chỉnh trong Inspector

Chọn **Sand Jam Gameplay 3D**:

- `QueueSpacing`: khoảng cách dọc giữa các nhân vật trong hàng.
- `ShotInterval`: khoảng thời gian giữa hai lần bắn, mặc định 0,08 giây; cần lớn hơn 0.
- `AmmoPerShot`: đạn tiêu hao mỗi lần, mặc định 20; cần lớn hơn 0.
- `LaneStarts` và `StashSlots`: tham chiếu các vị trí. Di chuyển những anchor này trong scene để đổi bố cục.

Đổi JSON sang level khác cần dựng lại số vùng/nhân vật tương ứng; scene hiện được thiết kế và kiểm tra cho tutorial này. Không thay JSON tùy ý rồi kỳ vọng scene tự sinh thêm tất cả đối tượng.

Menu **Build Windows player** build scene hiện tại. Menu **Regenerate scene from tutorial** tạo lại scene và asset sinh tự động từ tutorial; thao tác này thay bố cục scene thử, nên lưu một bản sao trước nếu đã chỉnh bằng tay.

## Luật thử và giới hạn

- Chọn đầu hàng → giữ một ô chờ → di chuyển đến ô → bắt đầu bắn đúng màu.
- Vùng được mở khi một vùng trong `prerequests` hoàn tất. Đây vẫn là giả định cho prototype, chưa xác nhận luật gốc.
- Nhân vật hết đạn rời ô. Đủ tất cả vùng thì thắng; đầy ô chờ và không nhân vật nào tiến triển được thì thua.
- Mesh được bake từ pose gốc rồi chuẩn hóa kích thước. Chuyển động đi/rời ô và giật khi bắn là hiệu ứng mới, **chưa khôi phục animation xương gốc**.
- Shader hiển thị tương thích được dùng với texture/palette trích xuất; chưa tái tạo toàn bộ shader URP/dissolve.
- Chưa có vật lý hạt cát, chain/freeze/secret/half/unlocker hay lưu tiến độ campaign.

## Bảo toàn dữ liệu trích xuất

Mọi mã, scene, material và mesh sinh thêm cho lần dựng này ở trong **CoreGameplayTest**. `Assets`, `Packages`, `ProjectSettings` của dự án cha được đọc để đối chiếu, không chỉnh sửa.

Danh mục SHA-256 trước khi dựng: **TestResults/Scene3D/original-before.csv**. Kết quả đối chiếu sau khi dựng: **TestResults/Scene3D/original-integrity.json**. Có thể kiểm tra lại bằng **Tools/Verify-OriginalData.ps1** trong project thử.

Các thư mục Generated và Resources/Original đều thuộc project thử; bản gốc bên ngoài vẫn riêng biệt.

### Bản cát chảy 30/09/2026
Bản 3D hiện hiển thị đạn theo uiDivider=40: 800 -> 20, cả màn 225 đơn vị màu. Hạt cát rơi và chất thành đụn; hiệu ứng mô phỏng, chưa tái tạo vật lý gốc. Mở BuildScene3D/SandJam-Scene3D.exe để test. Scene: Assets/Scenes/SandJamGameplay3D.unity trong project CoreGameplayTest. Dữ liệu APK gốc được kiểm tra giữ nguyên.

### Bản ô khóa 30/09/2026
Mở BuildLockedRegions/SandJam-Scene3D.exe để thử bản mới nhất: ô chưa mở giấu màu và số cát, chỉ mở ô liên quan khi cát đã phủ kín ô trước. Source đã cập nhật trong project này; nếu Unity đang Play hãy thoát Play và chạy lại.

### Bản cát dàn đều — mới nhất
Chạy BuildEvenFlow/SandJam-Scene3D.exe. Cát đi xuống từ một điểm rót, tràn sát bề mặt và phủ đều từ thấp lên cao; hạt không chiếm trùng ô. Scene đã lưu đúng 5 ô chờ. Nếu Unity đang Play, thoát Play rồi chạy lại để nhận code mới.

### Bản grid texture + SpriteRenderer — mới nhất
Mở BuildPixelSand/SandJam-Scene3D.exe. Mỗi ô grid tương ứng 1 pixel trên texture chung 84×112; một SpriteRenderer vẽ toàn bộ bảng cát. Chi tiết ở PIXEL_SAND_VI.md. Trong Unity thoát Play rồi chạy lại để nhận code mới.
