# TINY MONSTER KEEPER - TODO LIST

Danh sách các đầu việc (Todo List) cần triển khai tiếp theo để hoàn thiện bản MVP (Minimum Viable Product) theo thiết kế Game Design Document v0.2.

---

## 📋 Trạng thái hiện tại (Current Status)
*   **Đã hoàn thành:** Hệ thống di chuyển NavMesh 2D cơ bản, State Machine của Monster (Idle, Walk, Happy, Sleep), sự kiện Click/Touch mở UI Popup của monster bám theo màn hình, và hiệu ứng tim khi nhấn Feed/Play.
*   **Cần làm tiếp theo:** Hiện tại tất cả dữ liệu (tên monster, giá trị coin, friendship) đều đang được hardcode. Cần chuyển sang hệ thống quản lý dữ liệu động và hoàn thiện core loop.

---

## 🛠️ Danh sách việc cần làm (Task Backlog)

### 1. Hệ thống Dữ liệu & ScriptableObjects (Độ ưu tiên: P0)
Thiết lập các cấu trúc dữ liệu để tránh hardcode trong code.
- [ ] **Task 1.1:** Tạo class `MonsterData` kế thừa từ `ScriptableObject`.
  - *Thuộc tính:* `id` (string), `monsterName` (string), `idlePrefab` (GameObject), `favoriteFoodId` (string), `favoriteToyId` (string), `unlockAppealCost` (int), `unlockFriendshipCost` (int).
- [ ] **Task 1.2:** Tạo class `ItemData` (hoặc các lớp con `FoodData`, `ToyData`, `DecorationData`).
  - *Thuộc tính:* `id` (string), `itemName` (string), `price` (int), `value` (int - điểm cộng friendship/appeal), `icon` (Sprite).
- [ ] **Task 1.3:** Tạo các file asset ScriptableObject cụ thể cho 5 monster và các item theo bảng dữ liệu trong GDD (ví dụ: táo, kẹo, ao nhỏ, ghế gỗ...).

### 2. Quản lý Tiền tệ & Chỉ số (Currency & Stats) (Độ ưu tiên: P0)
- [ ] **Task 2.1:** Viết `CurrencyManager` (quản lý Coin).
  - *Tính năng:* Singleton quản lý Coin hiện có, cung cấp hàm `AddCoin(int amount)` và `TrySpendCoin(int amount)`. Phát Event khi Coin thay đổi để UI cập nhật.
- [ ] **Task 2.2:** Quản lý chỉ số Thân thiết (`Friendship`) cho từng monster.
  - *Tính năng:* Thay vì dùng biến static hoặc hardcode, mỗi Monster instance trong vườn sẽ lưu điểm friendship riêng.
- [ ] **Task 2.3:** Cập nhật `MonsterUIPanel` để hiển thị Friendship thực tế.
  - *Tính năng:* Khi mở panel, hiển thị đúng tên và thanh trượt (friendship bar fill) theo tỷ lệ friendship hiện tại của monster đó.
- [ ] **Task 2.4:** Thiết kế Main HUD UI.
  - *Tính năng:* Hiển thị số lượng Coin hiện tại ở góc trên màn hình, tự động cập nhật khi Coin thay đổi.

### 3. Hệ thống Cửa hàng & Túi đồ (Shop & Inventory) (Độ ưu tiên: P0)
- [ ] **Task 3.1:** Viết `InventoryManager` để quản lý số lượng vật phẩm đã mua.
  - *Tính năng:* Lưu danh sách vật phẩm người chơi sở hữu (`Dictionary<string, int>` cho Food/Toy và danh sách unique cho Decor).
- [ ] **Task 3.2:** Viết logic `ShopManager` và UI Cửa hàng.
  - *Tính năng:* UI dạng tab (Thức ăn, Đồ chơi, Trang trí). Khi nhấn nút mua: Kiểm tra số lượng coin, trừ coin, thêm vật phẩm vào Inventory, hiển thị thông báo thành công hoặc thất bại nếu thiếu tiền.

### 4. Hệ thống Đặt Đồ trang trí (Decoration Grid) (Độ ưu tiên: P0)
- [ ] **Task 4.1:** Xây dựng hệ thống Grid đơn giản.
  - *Tính năng:* Định nghĩa các ô có thể đặt đồ trong khu vườn. Quản lý trạng thái các ô (đã có đồ hay chưa).
- [ ] **Task 4.2:** Logic chọn và đặt vật phẩm.
  - *Tính năng:* Khi bấm nút "Trang trí", mở danh sách Decor có trong túi đồ. Chọn item -> hiển thị preview theo vị trí chuột/touch -> nhấn để đặt -> trừ vật phẩm trong túi -> cập nhật điểm Appeal tổng của vườn.

### 5. Tự động Lưu game (Save / Load System) (Độ ưu tiên: P0)
- [ ] **Task 5.1:** Định nghĩa `SaveData` Model.
  - *Thuộc tính cần lưu:* `coin` (int), danh sách monster đã unlock kèm điểm friendship, danh sách vật phẩm trong inventory, vị trí và ID các đồ trang trí đã đặt trong vườn.
- [ ] **Task 5.2:** Viết `SaveLoadManager` sử dụng JSON local (`Application.persistentDataPath`).
  - *Tính năng:* Tự động Save sau khi cho ăn, chơi, mua đồ, đặt đồ hoặc unlock monster. Tự động Load khi bắt đầu game để khôi phục trạng thái.

### 6. Hệ thống Mở khóa Monster (Unlock System) (Độ ưu tiên: P1)
- [ ] **Task 6.1:** Viết `UnlockManager`.
  - *Tính năng:* Lắng nghe các thay đổi về điểm Appeal tổng và tổng điểm Friendship. Nếu đạt điều kiện của monster chưa unlock -> hiển thị Popup chúc mừng -> spawn monster mới vào vườn.

---

> [!NOTE]
> Danh sách này được xây dựng bám sát theo sprint plan 4 tuần trong tài liệu `task_list.txt`.
