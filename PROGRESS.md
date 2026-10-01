# Checkpoint — scene gameplay Unity, 30/09/2026

**Trạng thái: đã hoàn thành scene thử đầu tiên, sẵn sàng mở Unity và Play.**

- Project riêng: `D:/Unity/testsand/CoreGameplayTest` (Unity 2022.3.62f3).
- Scene mới: `Assets/Scenes/SandJamGameplay3D.unity`.
- Player: `BuildScene3D/SandJam-Scene3D.exe`.
- Hướng dẫn: `SCENE_GUIDE_VI.md`.
- Scene lưu sẵn 13 nhân vật dạng mesh thật, 5 vùng màu, ô chướng ngại, 3 hàng, 6 ô chờ, camera, light và vị trí đạn.
- Nhân vật đi vào ô trước khi bắn; hết đạn rời ô; có tiến độ, mở vùng, thắng, thua, restart, pause, gợi ý và âm thanh.
- Model được bake từ asset gốc; chuyển động hiện tại là tween mới. Chưa khôi phục animation xương gốc hoặc vật lý từng hạt cát.
- Vẫn dùng luật mở vùng ANY đã ghi rõ là giả định prototype.

**Kiểm chứng:** 17 kiểm tra model cũ đạt; kiểm tra scene/editor đạt; bản Windows build 0 lỗi. Runtime kiểm tra raycast, chặn chọn phía sau, chặn bắn khi đang di chuyển, thắng đúng 9.000 đạn, restart, thua và resize đều đạt. Kết quả/ảnh: `TestResults/Scene3D/FinalSmoke`.

**Bảo toàn dữ liệu gốc:** đã đối chiếu SHA-256 của toàn bộ 11.049 file thuộc Assets, Packages, ProjectSettings ở dự án cha: không đổi, không thêm, không thiếu. Bằng chứng: `TestResults/Scene3D/original-integrity.json`. Chỉ thêm/sửa trong project CoreGameplayTest cho lần làm này.

**Tiếp tục:** lấy phản hồi khi người dùng chơi scene; sau đó cải thiện hình dáng/shader/animation, cơ chế mở vùng hoặc thêm level theo yêu cầu. Không cần làm lại prototype. Scene có thể chỉnh trực tiếp; lệnh Regenerate sẽ tạo lại bố cục, nên không chạy khi người dùng đã chỉnh scene mà chưa bảo toàn thay đổi.

Hạn mức kiểm tra gần cuối: dùng 38% trong 5 giờ, 23% trong tuần. Tiếp tục lưu và dừng khi gần hết theo yêu cầu trước đó. Mọi thao tác lên dữ liệu trích xuất gốc phải tránh ghi đè.

## Lịch sử checkpoint ngày 29/09/2026 — thử asset gốc

Yêu cầu: nâng cấp màn thử để dùng asset trích xuất Sand Jam; nếu gần hết hạn mức thì dừng và lưu tiến độ.

Trạng thái hiện tại: DỪNG THEO YÊU CẦU NGƯỜI DÙNG — hạn mức 5 giờ đã dùng 92%, còn 8%; tuần dùng 16%. Bản mới đã build thành công và kiểm tra hình ảnh/thắng/thua. Bản `Build/SandJam-CoreTest.exe` cũ vẫn dùng được.

Bản mới để chơi: `BuildAssets/SandJam-AssetsTest.exe`. Giữ nguyên thư mục cạnh exe.
Kết quả: `TestResults/AssetSmoke2/smoke-result.txt`, ảnh `01-start.png`, `02-painting.png`, `03-win.png`, `04-stalled.png`, và 5 ảnh `asset-character-*.png`.
Build: 0 lỗi, 17 kiểm tra gameplay đạt; prefab giữ đủ 41 bone và không có script cũ. Kiểm tra runtime đã thắng với đúng 9.000 đạn, restart và thua khi đầy ô chờ. Âm thanh được xác nhận load, chưa kiểm chứng bằng nghe thủ công.

Đã làm:
- Tách renderer chính của Character.prefab và 46 Transform tổ tiên/bone, giữ 41 bone references.
- Sao chép nguyên mesh character.asset, Sand.png, Shadow_0.png, button-green-v2.png, Input/pop/Victory.ogg.
- Lấy FinalColor từ ColorDataSO gốc; sinh prefab hình ảnh không phụ thuộc SDK/script trích xuất.
- Dùng shader preview mới vì shader gốc phụ thuộc URP/dissolve. Render 5 hình nhân vật từ model gốc ở runtime; chưa khôi phục animation gốc.
- Thêm âm chọn nhân vật, hoàn thành vùng, chiến thắng và nút tắt âm.
- Giữ nguyên luật gameplay và 17 kiểm tra logic đã có.

File quan trọng: Tools/Import-PrototypeAssets.ps1 (ở thư mục dự án cha), Assets/Scripts/OriginalAssetView.cs, Assets/Scripts/SandJamDemo.cs, Assets/Editor/BuildDemo.cs.

Đã sửa lỗi đầu tiên: prefab gốc có scale nhỏ, đặt stage ở tọa độ 1000 gây mất độ chính xác/ảnh trống. Renderer hiện bake pose gốc ra mesh tĩnh, đặt ở gốc tọa độ, chuẩn hóa cao 2 đơn vị và render riêng trên layer 30. Không có animation xương đang chạy. Lượt lỗi đầu tiên ở AssetSmoke; lượt đạt ở AssetSmoke2.

Làm tiếp khi người dùng yêu cầu và đủ hạn mức: kiểm tra hình dáng/góc nhìn và shader so với prefab gốc (ảnh hiện có model khối, một số mép tối), khôi phục animation phù hợp nếu muốn nhân vật chuyển động thực; cân nhắc dựng nhân vật trực tiếp trong scene 3D thay vì portrait. Không cần làm lại core logic. Source và checkpoint đều đã lưu.

Build lại bằng Unity batch executeMethod SandJamTest.Editor.BuildDemo.Build. Nếu exe bản mới đang chạy, không tự đóng phiên chơi của người dùng; đóng tiến trình test do mình tạo hoặc dùng thư mục build khác. Chạy --smoke-output với thư mục kết quả mới và cửa sổ bình thường để có ảnh. Kiểm tra usage trước khi mở rộng công việc.

Hạn mức lúc bắt đầu: 59% dùng trong khung 5 giờ, 10% dùng trong tuần. Lần kiểm tra cuối: 92%/16%; reset timestamp cho khung 5 giờ là 1790684791. Không tự tạo lịch chạy lại; chờ người dùng tiếp tục.

## 2026-09-30 — YouTube reference, UI amounts and flowing sand

Completed and verified in isolated CoreGameplayTest only.
Reference: https://www.youtube.com/watch?v=xV6nPstYIaQ (Empty Fellow, Sand Jam Gameplay). Viewed frames at 00:05 and 00:10: queued characters show 20; red remaining falls from 41 to 19; colored sand builds triangular piles. The video confirms the small display scale, not the exact internal physics.

- Scene3D uses ceil(raw / max(1, uiDivider)). Tutorial divider 40: standard ammo 800 -> 20; regions 59 / 35 / 41 / 58 / 32; total 9000 -> 225. Raw JSON and model accounting unchanged.
- SceneRegionView now animates batched grain quads falling and settling into three overlapping piles within each region mask. Deterministic pile ordering, short gravity fall, subtle sideways motion, original sand texture. Presentation approximation, NOT a cellular sand or collision simulation, NOT recovered original code. Local vertical mask clearance limits each grain's fall. Existing projectile travel remains.
- Restart clears all visual particles; pause stops them; win overlay waits until all grains settle. Meshes cloned at runtime; original/generated source meshes untouched at runtime.
- Editor scene labels updated by build, without regenerating scene objects. Old 2D demo retains its old visual style/counts; use SandJamGameplay3D scene / BuildScene3D executable.
- Build succeeded, zero errors; prior 17 model tests + 13 runtime checks passed, including 20/59/225 UI counts, visible grains in flight, all 9077 pixels settled, win/loss/restart and resized raycast.
- Results: TestResults/SandFlow/Smoke/scene-tests.txt and 01..05 PNGs. Build log: TestResults/SandFlow/build.log.
- Original 11049 files SHA256 verified unchanged, no added/deleted files: TestResults/Scene3D/original-integrity.json.
- Latest quota check: 81% used / 19% remaining in 5-hour window; weekly 30%. Stop expanding work now, checkpoint saved. No automatic continuation or reset requested.

Test: open BuildScene3D/SandJam-Scene3D.exe, click the first red character or press 1. Unity: open this nested project, Assets/Scenes/SandJamGameplay3D.unity, Play.
Next optional refinement: compare longer real-time playback with reference, tune emitter/projectile stream and pile slope; replace art-directed grain paths with true grid simulation if requested. Current build is ready for user testing.

## 2026-09-30 — Single pour correction (supersedes three-pile effect)
User requested one falling stream per selected color instead of three simultaneous streams. Replaced three pile centers with one central inlet per region and one central pile. All grains start at the same inlet, fall vertically for the first 70% of flight, then spread toward destination cells near the pile. Projectile target now follows the inlet. Staggered grain birth times soften shot batches. Still a presentation approximation, not collision-based sand simulation.
BuildScene3D/SandJam-Scene3D.exe rebuilt successfully. All 13 runtime smoke checks passed; screenshot TestResults/SingleFlow/Smoke/02-scene-shooting.png visually verified one red stream. Win settles all 9077 grains. Original 11049 files verified unchanged. Quota at start of this correction: 88% used (12% left), weekly31%; checkpoint saved and stop after this focused fix.

## 2026-09-30 — Hide locked regions and wait for actual coverage
Latest playable build: BuildLockedRegions/SandJam-Scene3D.exe (use this instead of older BuildScene3D exe).
- Locked regions have identical neutral tint; number and backing hidden; sidebar hides palette/name until opened.
- SandGame optional coverageCheck gates opening linked regions and win/loss evaluation while completed regions still have sand in flight. Scene3D checks SettledCount against full region pixel count. Existing neighbour links and ANY completed prerequisite rule retained; no new sequential level order invented.
- Old callers without coverageCheck retain immediate model-only completion for compatibility.
- All 15 runtime checks pass, including hidden amounts/colors, unlock only after covered prerequisite, full win/loss/restart. Original 11049 files SHA256 unchanged.
- Verification output: TestResults/LockedRegions/Smoke. Start screenshot visually inspected.
- Main prototype Unity editor was open, so build used copied Assets/Packages/ProjectSettings in TestResults/LockedRegions/ValidationProject. Did not close user's editor or overwrite scene edits. Source changes are in main CoreGameplayTest/Assets/Scripts. Main editor scene will show new runtime hiding when Play is started; saved edit-mode labels may still be visible.
- Quota at start: new 5-hour window, 2% used; weekly32%.

## 2026-09-30 — Exactly five stash slots
Latest build: BuildFiveSlots/SandJam-Scene3D.exe. Scene3D now uses exactly five slots, centered at x=-3,-1.5,0,1.5,3. Old six-slot scenes migrate at runtime (sixth platform/anchor/index disabled); generator builds five directly. HUD uses actual slot count. No original assets edited. Validation build used existing isolated copy because main Unity project is open. Runtime assertion requires five slots; full win, loss, restart and locked-region checks passed. Screenshot TestResults/FiveSlots/01-scene-start.png visually verified five slots. Source changes in main project apply on next Play.

## 2026-09-30 — Even downward flow, no overlapping grains, serialized five slots
Latest playable build: BuildEvenFlow/SandJam-Scene3D.exe.
Replaced independent tweened grains with SandPourSimulation: fixed 300 steps/sec, one inlet above the highest part of each region, occupied grid cells shared by moving and settled grains. A blocked grain waits. Grains move only downward or sideways, spread one cell above target surface, and fill lower rows before higher rows. This is controlled grid routing for visual fill, not a recovered original physics engine. Rendering clips grains to their own mask so they cannot visually cover other regions. Runtime cloned meshes preserve source assets. Completion checkmark waits until grains settle.
Scene now serializes exactly five stash anchors/platforms/labels (sixth removed), centered. ConfigureFiveSlots also cleans up stale sixth-slot objects when entering Play in an already-open editor. Targeted scene patch backup: TestResults/EvenFlow/scene-before.unity.txt; unrelated scene objects retained.
Tests: SandFlowChecks covers all five original tutorial masks, partial/full pours, no overlap, no upward movement, conservation, and even coverage of all 32 columns in a rectangle. Full final runtime smoke test passed win/loss/restart/locked regions/five-slot assertion/occupancy; screenshot visually verified. Results: TestResults/EvenFlow/FinalSmoke and grid-tests.txt. Build final-build.log succeeded. All original 11049 files hash-verified unchanged.
Main source and scene updated; compiled in separate validation project to preserve user's open Unity session. Source files: Scene3D/SandPourSimulation.cs, SceneRegionView.cs, SandJamSceneController.cs; Editor/Scene3D/SandFlowChecks.cs.
Quota checked at start: 27% used of five-hour window, weekly36%. Work saved.

## 2026-09-30 — Shared grid texture + SpriteRenderer
Latest build: BuildPixelSand/SandJam-Scene3D.exe. SandBoardTextureView owns a shared 84x112 RGBA32 texture, persistent base/frame buffers, point filtering, no mipmaps, dirty upload once per LateUpdate. Every cell maps to one texel; one SpriteRenderer full-rect quad renders all regions and obstacles. SceneRegionView now writes grid pixels, no per-grain/per-region runtime mesh clones. Legacy mesh components retained only as scene/editor references and disabled during play. Shader Resources/PixelSandSprite.shader. Grain changed from class to struct, moving list preallocated for level.
All model/grid tests and 19 runtime checks pass; images visually inspected in TestResults/PixelSand/Smoke. Build log TestResults/PixelSand/build.log. Original 11049 files hash verified unchanged. No Android/iOS deployment or device performance measurements; only Windows tested. Details: PIXEL_SAND_VI.md. Main sources updated, build ran using isolated validation copy to preserve user's Unity session. Quota at start 55% used; progress saved.

## 2026-09-30 — Script organization based on AssetRipper inspection
Scanned all 1120 original C# files into Documentation/EXTRACTED_SCRIPT_INDEX.csv; examined gameplay controllers/data declarations. Report and responsibilities: Documentation/SCRIPT_ARCHITECTURE_VI.md. Extracted methods are largely stubs; hierarchy is reference, not recovered implementation.
Moved existing prototype scripts with their .meta GUIDs to Runtime/{Data,Gameplay,Sand,Board,Characters,Flow,UI}, Development/Validation, Legacy, Editor/{Build,Validation,Legacy}. Split level DTOs and game entities out of SandGame; split UI and smoke harness into partial controller files. Namespaces/serialized field names unchanged. No asmdef introduced. UI partial and test harness were reconstituted after a directory-creation error during extraction; compilation and runtime validation then passed. Existing pure gameplay and sand simulation unchanged by this organization.
Build: TestResults/ScriptStructure/build.log success. Runtime: TestResults/ScriptStructure/Smoke validates selection, five slots, masked locked regions, sprite texture, full win/loss/restart/resize. Original 11049 files hash verified unchanged. Main source tree reorganized; prior playable BuildPixelSand remains the last published executable. Current validation executable lives under TestResults/LockedRegions/ValidationProject/BuildScene3D. Source changes apply in main Unity project on refresh.
Quota at start 74%; focused preparation completed, stop extending this turn. Follow new architecture document for next features; old progress paths under Scripts/Scene3D are now historical.

## 2026-10-01 — Screenshot reference scene
Latest visual build: BuildReferenceUI/SandJam-ReferenceUI.exe; new main scene Assets/Scenes/SandJamReferenceUI.unity. Existing prototype scene retained. Correct deer mask found in wildlife_11 JSON. Uses copied original icons, RoundedCube mesh, Sand texture; generated frame/rails/pads; 5 serialized slots. Settings/coin/three boosters are UI-only placeholders. Header 111/210 and prices 150/500/1 are preview values. Queue reordered in separate generated playable JSON; ropes and question covers are cosmetic, not recovered special mechanics.
Asset folder inventory and usage/limitations: Documentation/ReferenceUI/README_VI.md, folders.csv, copied-assets.csv. Scripts under Editor/ReferenceUI and Runtime/UI/ReferenceUI. Main controller gained default-compatible HidePrototypeHud/ViewAspect; region view gained default-compatible empty/open tint; palette supports additional original ColorType IDs. New reference screen sets obstacle tint and restores it on destroy. Scene includes editor-only preview, hidden at runtime.
Build completed in isolated validation project then new scene/assets copied with GUID resolution checks. Generated sprite material's shader GUID remapped to main project's PixelSandSprite shader GUID where needed. No missing scene/material references. Tests: TestResults/ReferenceUI/FinalSmoke. Original integrity checked: 11049 unchanged files. Quota latest check: 63% used in five-hour window, weekly57%; stop extending after current deliverable.
