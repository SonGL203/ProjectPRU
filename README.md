# 🌾 GRUMPY ACRES - TÀI LIỆU KINH TẾ & HƯỚNG DẪN TEST

Tài liệu này tổng hợp toàn bộ hệ thống kinh tế trong game (Nông sản, Thủy sản, Thuê Cowboy) và danh mục các phím tắt ẩn phục vụ việc phát triển, cân bằng game và kiểm thử (Testing/QA).

---

## 📑 MỤC LỤC
1. [Hệ Thống Phím Tắt Ẩn & Điều Khiển (Debug / Hotkeys)](#1-hệ-thống-phím-tắt-ẩn--điều-khiển-debug--hotkeys)
2. [Hệ Thống Kinh Tế: Nông Nghiệp (Hạt Giống & Nông Sản)](#2-hệ-thống-kinh-tế-nông-nghiệp-hạt-giống--nông-sản)
3. [Hệ Thống Kinh Tế: Thủy Sản (Cá & Dụng Cụ Nuôi)](#3-hệ-thống-kinh-tế-thủy-sản-cá--dụng-cụ-nuôi)
4. [Hệ Thống Thuê Cowboy Phòng Thủ (Cowboy Vendors)](#4-hệ-thống-thuê-cowboy-phòng-thủ-cowboy-vendors)
5. [Chu Trình Ngày Đêm & Mùa Vụ (Day-Night & Seasons)](#5-chu-trình-ngày-đêm--mùa-vụ-day-night--seasons)

---

## 1. Hệ Thống Phím Tắt Ẩn & Điều Khiển (Debug / Hotkeys)

Dành riêng cho Team Dev và QA Tester thao tác nhanh trong màn chơi (`SampleScene`):

| Phím Tắt | Chức Năng Chi Tiết | Nơi Xử Lý (Script) |
| :--- | :--- | :--- |
| **`F1`** | **Bộ Đồ Khởi Nghiệp (Starter Kit)**:<br>- Đầy đủ công cụ nông trại (Cuốc, Bình tưới, Rìu, Cần câu, v.v.)<br>- Tặng mỗi loại 10 hạt giống (Lúa mì, Cà rốt, Cà chua)<br>- Bộ dụng cụ thủy sản cơ bản | `InventorySystem.cs` |
| **`F2`** | **Cheat Tiền (+100 Vàng)**:<br>- Cộng ngay 100 Gold vào ví (`PlayerWallet`) và tự động cập nhật lên UI góc dưới | `PlayerWallet.cs` |
| **`F3`** | **Bộ Sưu Tập Thủy Sản**:<br>- Nhận toàn bộ các loại cá trưởng thành, cá con và thức ăn cho cá | `InventorySystem.cs` |
| **`F4`** | **Bỏ Qua Ngày (Skip Day)**:<br>- Lập tức tăng 1 ngày (`Day + 1`), tua nhanh chu kỳ phát triển của cây trồng | `SeasonManager.cs` |
| **`F5`** | **Bỏ Qua Mùa (Skip Season)**:<br>- Chuyển ngay sang mùa tiếp theo: **Xuân (Spring) ➔ Hạ (Summer) ➔ Thu (Autumn) ➔ Đông (Winter)** | `SeasonManager.cs` |
| **`E`** hoặc **`Tab`** | **Mở / Đóng Túi Đồ (Inventory Window)** | `InventoryUI.cs` |
| **`1` – `9`, `0`** | **Chọn nhanh ô trang bị** trên thanh Hotbar (Action Bar) | `InventoryUI.cs` |
| **`F`** | **Tương tác thuê Cowboy**: Đứng gần quầy tuyển dụng Cowboy và nhấn `F` | `CowboyVendor.cs` |
| **Click vào Nhà** | Click chuột vào Ngôi nhà chính (`house`) để mở nhanh túi đồ | `InventoryUI.cs` |

---

## 2. Hệ Thống Kinh Tế: Nông Nghiệp (Hạt Giống & Nông Sản)

> **Cơ chế**: Người chơi mua hạt giống, dùng cuốc xới đất, gieo hạt, tưới nước và thu hoạch khi cây chín. Nông sản thu hoạch có thể đem bán lấy vàng.

| Tên Cây Trồng | Giá Mua Hạt | Giá Bán Hạt | Thời Gian Chín | Giá Bán Nông Sản | Lợi Nhuận Ròng / Cây | ROI (Tỉ suất LN) | Đánh Giá & Vai Trò |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Lúa Mì (Wheat)** | **8 Vàng** | 3 Vàng | 30 giây *(~2 ngày)* | **15 Vàng** | **+7 Vàng** | **87.5%** | **Early Game**: Vốn ít, thời gian thu hoạch nhanh nhất, xoay vòng vốn ban đầu. |
| **Cà Rốt (Carrot)** | **12 Vàng** | 4 Vàng | 35 giây *(~3 ngày)* | **20 Vàng** | **+8 Vàng** | **66.7%** | **Mid Game**: Lợi nhuận ổn định, thích hợp canh tác số lượng vừa phải. |
| **Cà Chua (Tomato)** | **15 Vàng** | 5 Vàng | 45 giây *(~4 ngày)* | **25 Vàng** | **+10 Vàng** | **66.7%** | **High Yield**: Lợi nhuận cao nhất trên mỗi ô đất thu hoạch. |

---

## 3. Hệ Thống Kinh Tế: Thủy Sản (Cá & Dụng Cụ Nuôi)

> **Cơ chế**: Mua cá giống từ NPC Ngư Dân (Fish Vendor), thả vào Bể cá nhân tạo (`Artificial Pond`), cho ăn hàng ngày. Khi cá trưởng thành, dùng Vợt bắt và bán lại cho Ngư Dân bằng phím **`X`**.

### A. Vật Phẩm & Con Giống (Mua từ NPC Ngư Dân)
| Phím Mua | Tên Vật Phẩm | Giá Mua | Mô Tả & Công Dụng |
| :---: | :--- | :---: | :--- |
| **`1`** | **Cá Chép Giống (Carp Fry)** | **15 Vàng** | Cá giống cấp 1, lớn nhanh, dễ nuôi. |
| **`2`** | **Cá Vàng Giống (Goldfish Fry)** | **25 Vàng** | Cá cảnh có giá trị thẩm mỹ và kinh tế tầm trung. |
| **`3`** | **Cá Hồi Giống (Salmon Fry)** | **50 Vàng** | Cá giống cao cấp, giá thành đầu tư cao, siêu lợi nhuận. |
| **`4`** | **Thức Ăn Cho Cá (x5 Gói)** | **25 Vàng** | Gói 5 túi thức ăn *(5 Vàng / gói)* dùng cho cá ăn để lớn. |
| **`5`** | **Vợt Bắt Cá (Fishing Net)** | **30 Vàng** | Dụng cụ cần thiết để vớt cá trưởng thành từ bể lên túi đồ. |
| **`6`** | **Bể Cá Nhân Tạo (Fish Pond)** | **100 Vàng** | Công trình đặt trên nông trại để nuôi thả cá. |

### B. Bảng Giá Bán Cá Trưởng Thành (Nhấn `X` tại Ngư Dân để bán)
| Loại Cá | Chi Phí Con Giống | Chi Phí Thức Ăn Ước Tính | Giá Bán Cá Lớn | Lợi Nhuận Thuần | ROI |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Cá Chép (Adult Carp)** | 15 Vàng | ~5 Vàng | **45 Vàng** | **+25 Vàng** | **125%** |
| **Cá Vàng (Adult Goldfish)** | 25 Vàng | ~10 Vàng | **80 Vàng** | **+45 Vàng** | **128%** |
| **Cá Hồi (Adult Salmon)** | 50 Vàng | ~15 Vàng | **180 Vàng** | **+115 Vàng** | **176%** |

---

## 4. Hệ Thống Thuê Cowboy Phòng Thủ (Cowboy Vendors)

> **Cơ chế**: Đêm xuống, quái vật sẽ tấn công nông trại. Người chơi xây dựng **Chòi Canh (`GuardFrame`)**, sau đó đến quầy Cowboy Market nhấn **`F`** để thuê xạ thủ về chốt giữ.

### Bảng Chỉ Số & Chi Phí Thuê Cowboy
| Cấp Bậc (Tier) | Giá Thuê (Gold) | Máu (HP) | Sát Thương (Damage) | Tốc Độ Bắn (Fire Rate) | Tầm Bắn (Range) | Đặc Điểm & Vai Trò Chiến Thuật |
| :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Tier 1 (Cowboy 1)**<br>*(Tập sự / Tuần tra)* | **50 Vàng** | 65 | 15 | 1.0 giây / phát | 5.5 ô | Giá rẻ, trang bị đầu game chống các loại quái cơ bản di chuyển chậm. |
| **Tier 2 (Cowboy 2)**<br>*(Thiện xạ / Súng đôi)* | **90 Vàng** | 90 | 19 | 0.8 giây / phát | 6.5 ô | Tốc độ bắn nhanh, tầm bao quát tốt, trụ vững các đợt quái vừa. |
| **Tier 3 (Cowboy 3)**<br>*(Đại tướng / Sát thủ)* | **150 Vàng** | 115 | 25 | 0.5 giây / phát *(Rất nhanh)* | 7.5 ô | DPS cực khủng, bắn tỉa từ khoảng cách rất xa, chống chịu trùm và bầy đàn. |

> [!IMPORTANT]
> **Điều kiện tiên quyết khi thuê Cowboy**:
> 1. Phải có đủ Vàng trong ví (`PlayerWallet`).
> 2. Số lượng Cowboy đang thuê không được vượt quá số Chòi Canh hiện có (`CowboyRoster.Count < GuardFrame.All.Count`). Nếu hết chòi, thông báo `msgNoSlot` sẽ hiện lên yêu cầu người chơi xây thêm chòi canh.

---

## 5. Chu Trình Ngày Đêm & Mùa Vụ (Day-Night & Seasons)

- **Thời gian một mùa**: Mặc định **30 Ngày** (hoặc theo cấu hình `daysPerSeason`).
- **Thời gian một ngày**: Chu kỳ Ngày / Đêm tự động xoay tua.
- **Hiệu ứng thời tiết theo mùa**:
  - **Mùa Xuân (Xuân)**: Thời tiết ôn hòa, cây trồng phát triển chuẩn.
  - **Mùa Hạ (Hạ)**: Nắng gắt, cây tiêu thụ nhiều nước tưới hơn.
  - **Mùa Thu (Thu)**: Mùa thu hoạch vàng, sương mù nhẹ.
  - **Mùa Đông (Đông)**: Giá lạnh, đêm dài hơn ngày, quái vật hung hãn hơn.

