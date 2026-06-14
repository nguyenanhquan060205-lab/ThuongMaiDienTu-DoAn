# Hệ Thống Thương Mại Điện Tử

Ứng dụng web thương mại điện tử theo mô hình **C2C (Customer-to-Customer)** — cho phép người dùng đăng bán sản phẩm, mua hàng, nhắn tin và đánh giá lẫn nhau. Quản trị viên có bảng điều khiển riêng để duyệt tin, quản lý người dùng và xử lý khiếu nại.

---

## Công nghệ sử dụng

| Layer | Công nghệ |
|---|---|
| Backend | ASP.NET MVC (C#) |
| Frontend | HTML, CSS, JavaScript |
| Database | SQL Server (T-SQL) |
| ORM / DB Access | ADO.NET / Entity Framework |
| IDE | Visual Studio |

---

## Tính năng chính

### 👤 Người dùng (User)
- Đăng ký, đăng nhập tài khoản
- Đăng tin bán sản phẩm (kèm hình ảnh, danh mục, giá, mô tả)
- Tìm kiếm sản phẩm theo tên, danh mục
- Thêm sản phẩm vào giỏ hàng, đặt hàng
- Theo dõi trạng thái đơn hàng (Đang chờ xử lý → Đang vận chuyển → Đã giao)
- Hủy / sửa đơn hàng (chỉ khi chưa vận chuyển)
- Đánh giá sản phẩm (1–5 sao + nhận xét) sau khi mua hàng
- Gửi khiếu nại về sản phẩm
- Nhắn tin trực tiếp với người bán / người mua
- Xem lịch sử mua hàng và lịch sử đánh giá

### 🔧 Quản trị viên (Admin)
- Duyệt tin đăng bán sản phẩm
- Quản lý người dùng (xem, ban, xóa tài khoản)
- Quản lý đơn hàng toàn hệ thống
- Xử lý khiếu nại từ người dùng
- Tìm kiếm sản phẩm, người dùng trong trang quản trị
- Dashboard tổng quan với các card thống kê nhanh

---

## Database Schema

Cơ sở dữ liệu gồm **10 bảng chính** với các trigger tự động:

```
NGUOIDUNG       — Tài khoản người dùng (Admin / User)
LOAISANPHAM     — Danh mục sản phẩm (Điện thoại, Laptop, Thời trang,...)
SANPHAM         — Sản phẩm đăng bán
HINHANHSP       — Hình ảnh sản phẩm (hỗ trợ nhiều ảnh, ảnh bìa)
GIOHANG         — Giỏ hàng (1 giỏ / 1 người dùng)
CT_GIOHANG      — Chi tiết giỏ hàng
HOADON          — Hóa đơn / đơn hàng
CT_HOADON       — Chi tiết hóa đơn
DANHGIA         — Đánh giá sản phẩm (1–5 sao)
KHIEUNAI        — Khiếu nại sản phẩm
TINNHAN         — Tin nhắn giữa người dùng
```

**Triggers tự động:**
- `TRG_UPDATETONGSOLUONG` — Cập nhật tổng số lượng giỏ hàng sau mỗi thay đổi
- `TRG_UPDATEDANHGIATB` — Cập nhật điểm đánh giá trung bình sản phẩm sau mỗi review

---

## Hướng dẫn cài đặt

### Yêu cầu
- Visual Studio 2022
- SQL Server 2019+
- .NET Framework 4.x hoặc .NET 6+

### Các bước
1. Clone repository:
   ```bash
   git clone https://github.com/nguyenanhquan060205-lab/ThuongMaiDienTu-DoAn.git
   ```
2. Mở file `ThuongMaiDienTu-DoAn.sln` bằng Visual Studio
3. Chạy file `TMDT.sql` trên SQL Server Management Studio (SSMS) để tạo database và dữ liệu mẫu
4. Cập nhật connection string trong `Web.config`:
   ```xml
   <connectionStrings>
     <add name="TMDTContext" connectionString="Server=.;Database=TMDT;Trusted_Connection=True;" ... />
   </connectionStrings>
   ```
5. Build và chạy project (F5)

### Tài khoản mẫu
| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Admin | `admin` | `123` |
| User | `minhhuy` | `123` |
| User | `hoapham` | `123` |

---

## Thành viên nhóm

> Dự án môn học — Trường Đại học Công Thương TP.HCM (HUIT)

---

## License

Dự án phục vụ mục đích học tập, không dùng cho mục đích thương mại.
