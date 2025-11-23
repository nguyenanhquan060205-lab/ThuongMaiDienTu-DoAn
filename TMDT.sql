CREATE DATABASE TMDT
GO
USE TMDT
GO

---------------------------------------------------
-- 1. TẠO CÁC BẢNG (ĐÃ GOM GỌN)
---------------------------------------------------

-- Bảng USER (KHÁCHHÀNG + ADMIN)
CREATE TABLE NGUOIDUNG
(
    MaKH INT PRIMARY KEY IDENTITY(1,1),
    HoTen NVARCHAR(100) NOT NULL,
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    VaiTro NVARCHAR(10) CHECK (VaiTro IN ('Admin','User')) NOT NULL,
    MatKhau NVARCHAR(100) NOT NULL, 
    TaiKhoan NVARCHAR(50) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE,
    SDT VARCHAR(20) UNIQUE,
    DiaChi NVARCHAR(200),
    AnhDaiDien NVARCHAR(255) DEFAULT 'default.jpg',
    NgayTao DATETIME DEFAULT GETDATE()
);

-- Bảng LOẠI SẢN PHẨM
CREATE TABLE LOAISANPHAM (
    MaLoai INT PRIMARY KEY IDENTITY(1,1),
    TenLoai NVARCHAR(100) NOT NULL
);

-- Bảng SẢN PHẨM (Đã thêm DanhGiaTB và TongDanhGia)
CREATE TABLE SANPHAM (
    MaSP INT PRIMARY KEY IDENTITY(1,1),
    MaKH INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),        
    MaLoai INT FOREIGN KEY REFERENCES LoaiSanPham(MaLoai),
    TenSP NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    Gia DECIMAL(18,2) CHECK (Gia >= 0),    
    SoLuong INT CHECK (SoLuong >= 0),
    DanhGiaTB FLOAT DEFAULT 0, -- Đã gom vào đây
    TongDanhGia INT DEFAULT 0, -- Đã gom vào đây
    TrangThai NVARCHAR(20) DEFAULT N'Đã duyệt' 
        CHECK (TrangThai IN (N'Đã duyệt', N'Đã bán', N'Ẩn')),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- Bảng HÌNH ẢNH SP
CREATE TABLE HINHANHSP (
    MaHA INT PRIMARY KEY IDENTITY,
    Masp INT FOREIGN KEY REFERENCES SanPham(MaSP) ON DELETE CASCADE,
    URLAnh NVARCHAR(255) NOT NULL,
    AnhBia BIT DEFAULT 0  
);

-- Bảng GIỎ HÀNG (Đã thêm TongSoLuong và ràng buộc UNIQUE MaKH)
CREATE TABLE GIOHANG (
    MaGH INT PRIMARY KEY IDENTITY(1,1),
    MaKH INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),
    TongSoLuong INT DEFAULT 0, -- Đã gom vào đây
    CONSTRAINT UQ_GioHang_MaKH UNIQUE (MaKH) -- Mỗi người chỉ 1 giỏ hàng
);

-- Bảng CHI TIẾT GIỎ HÀNG
CREATE TABLE CT_GIOHANG (
    MaGH INT FOREIGN KEY REFERENCES GioHang(MaGH),
    MaSP INT FOREIGN KEY REFERENCES SanPham(MaSP) ON DELETE CASCADE,
    SoLuong INT CHECK (SoLuong > 0),
    ThanhTien DECIMAL(18,2) CHECK (ThanhTien >= 0),
    PRIMARY KEY (MaGH, MaSP)
);

-- Bảng HÓA ĐƠN (Đã thêm DiaChiGiaoHang)
CREATE TABLE HOADON (
    MaHD INT PRIMARY KEY IDENTITY(1,1),
    MaKH INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),
    TongTien DECIMAL(18,2) CHECK (TongTien >= 0),
    PhuongThucTT NVARCHAR(50),
    DiaChiGiaoHang NVARCHAR(200), -- Đã gom vào đây
    NgayTT DATETIME,
    NgayDat DATETIME DEFAULT GETDATE(),
    TrangThai NVARCHAR(20) DEFAULT N'Đang chờ xử lý'
        CHECK (TrangThai IN (N'Đang chờ xử lý', N'Đã thanh toán', N'Đang vận chuyển', N'Đã Huỷ'))                
);

-- Bảng CHI TIẾT HÓA ĐƠN
CREATE TABLE CT_HOADON (
    MaHD INT FOREIGN KEY REFERENCES HoaDon(MaHD),
    MaSP INT FOREIGN KEY REFERENCES SanPham(MaSP),
    SoLuong INT CHECK (SoLuong > 0),
    ThanhTien DECIMAL(18,2) CHECK (ThanhTien >= 0),
    PRIMARY KEY (MaHD, MaSP)
);

-- Bảng ĐÁNH GIÁ
CREATE TABLE DANHGIA (
    MaDG INT PRIMARY KEY IDENTITY(1,1),
    MaKH INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),
    MaSP INT FOREIGN KEY REFERENCES SanPham(MaSP) ON DELETE CASCADE,
    SoSao INT CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung NVARCHAR(MAX),
    NgayDG DATETIME DEFAULT GETDATE()
);

-- Bảng KHIẾU NẠI (Đã gom NgayGui, PhanHoi và fix TrangThai chỉ có 2 loại)
CREATE TABLE KHIEUNAI (
    MaKN INT PRIMARY KEY IDENTITY(1,1),
    MaKH INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),
    MaSP INT FOREIGN KEY REFERENCES SanPham(MaSP) ON DELETE CASCADE,
    MoTa NVARCHAR(MAX),
    PhanHoi NVARCHAR(MAX) NULL, -- Đã gom vào đây
    NgayGui DATETIME DEFAULT GETDATE(), -- Đã gom vào đây
    TrangThai NVARCHAR(20) DEFAULT N'Chưa xử lý'
    CHECK (TrangThai IN (N'Chưa xử lý', N'Đã giải quyết')) -- Chuẩn chỉ 2 trạng thái
);

-- Bảng TIN NHẮN
CREATE TABLE TINNHAN (
    MaTN INT PRIMARY KEY IDENTITY(1,1),
    NguoiGui INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),
    NguoiNhan INT FOREIGN KEY REFERENCES NGUOIDUNG(MaKH),
    NgayGui DATETIME DEFAULT GETDATE(),
    NoiDung NVARCHAR(MAX),
    MaSP INT NULL FOREIGN KEY REFERENCES SanPham(MaSP) ON DELETE SET NULL,
    DaDoc BIT DEFAULT 0
);
GO

---------------------------------------------------
-- 2. TẠO TRIGGERS
---------------------------------------------------

-- Trigger cập nhật tổng số lượng giỏ hàng
CREATE TRIGGER TRG_UPDATETONGSOLUONG
ON CT_GIOHANG
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    UPDATE GIOHANG
    SET TONGSOLUONG = (
        SELECT ISNULL(SUM(SOLUONG),0)
        FROM CT_GIOHANG
        WHERE CT_GIOHANG.MaGH = GIOHANG.MaGH
    )
    WHERE MaGH IN (SELECT MaGH FROM inserted UNION SELECT MaGH FROM deleted);
END;
GO

-- Trigger cập nhật đánh giá trung bình
CREATE TRIGGER TRG_UPDATEDANHGIATB
ON DANHGIA
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    UPDATE SANPHAM
    SET DANHGIATB = (
        SELECT AVG(SOSAO*1.0)
        FROM DANHGIA
        WHERE DANHGIA.MaSP = SANPHAM.MaSP
    ),
    TONGDANHGIA = (
        SELECT COUNT(*)
        FROM DANHGIA
        WHERE DANHGIA.MaSP = SANPHAM.MaSP
    )
    WHERE MaSP IN (SELECT MaSP FROM inserted UNION SELECT MaSP FROM deleted);
END;
GO

---------------------------------------------------
-- 3. INSERT DỮ LIỆU MẪU
---------------------------------------------------

-- 1️⃣ Bảng NGUOIDUNG
INSERT INTO NGUOIDUNG (HoTen, GioiTinh, NgaySinh, VaiTro, MatKhau, TaiKhoan, Email, SDT, DiaChi, AnhDaiDien)
VALUES
(N'Nguyễn Văn Admin', N'Nam', '1990-01-10', 'Admin', '123', 'admin', 'admin@gmail.com', '0901000001', N'Quận 1, TP.HCM', 'admin.jpg'),
(N'Lê Minh Huy', N'Nam', '2002-07-20', 'User', '123', 'minhhuy', 'minhhuy@gmail.com', '0902000002', N'Quận 5, TP.HCM', 'default.jpg'),
(N'Phạm Thị Hoa', N'Nữ', '2001-12-03', 'User', '123', 'hoapham', 'hoapham@gmail.com', '0903000003', N'Quận 7, TP.HCM', 'default.jpg'),
(N'Trần Quốc Bảo', N'Nam', '2001-09-14', 'User', '123', 'quocbao', 'quocbao@gmail.com', '0904000004', N'Quận 10, TP.HCM', 'default.jpg');
GO

-- 2️⃣ Bảng LOAISANPHAM
INSERT INTO LOAISANPHAM (TenLoai)
VALUES
(N'Điện thoại'), (N'Máy tính & Laptop'), (N'Thời trang'),
(N'Đồ gia dụng'), (N'Phụ kiện công nghệ'), (N'Khác');
GO

-- 3️⃣ Bảng SANPHAM
INSERT INTO SANPHAM (MaKH, MaLoai, TenSP, MoTa, Gia, SoLuong, TrangThai)
VALUES
(2, 1, N'iPhone 13 Pro 128GB', N'Hàng chính hãng, pin 93%, màu bạc', 18000000, 1, N'Đã duyệt'),
(2, 1, N'Samsung Galaxy S21 FE', N'Máy đẹp 99%, tặng ốp lưng và cáp sạc', 9500000, 1, N'Đã duyệt'),
(3, 2, N'Laptop Dell XPS 13', N'Core i7, SSD 512GB, RAM 16GB, mỏng nhẹ', 23000000, 1, N'Đã duyệt'),
(3, 3, N'Áo hoodie unisex', N'Form rộng, chất vải cotton dày, đủ size', 350000, 5, N'Đã duyệt'),
(4, 4, N'Máy xay sinh tố Philips', N'Công suất 600W, cối thủy tinh, mới 95%', 890000, 2, N'Đã duyệt'),
(4, 5, N'Tai nghe Bluetooth Sony WF-1000XM4', N'Hàng chính hãng, chống ồn chủ động', 4800000, 1, N'Đã duyệt');
GO

-- 4️⃣ Bảng HINHANHSP
INSERT INTO HINHANHSP (MaSP, URLAnh, AnhBia)
VALUES
(1, N'iphone13.jpg', 1), (2, N's21fe.jpg', 1), (3, N'xps13.jpg', 1),
(4, N'hoodie.jpg', 1), (5, N'philips_blender.jpg', 1), (6, N'sony_xm4.jpg', 1);
GO


USE TMDT;
GO

-- XÓA DỮ LIỆU THEO THỨ TỰ RÀNG BUỘC
DELETE FROM TINNHAN;
DELETE FROM KHIEUNAI;
DELETE FROM DANHGIA;
DELETE FROM CT_HOADON;
DELETE FROM HOADON;
DELETE FROM CT_GIOHANG;
DELETE FROM GIOHANG;
DELETE FROM HINHANHSP;
DELETE FROM SANPHAM;
DELETE FROM LOAISANPHAM;
DELETE FROM NGUOIDUNG;
GO

-- RESET IDENTITY CHO TOÀN BỘ BẢNG
DBCC CHECKIDENT ('NGUOIDUNG', RESEED, 0);
DBCC CHECKIDENT ('LOAISANPHAM', RESEED, 0);
DBCC CHECKIDENT ('SANPHAM', RESEED, 0);
DBCC CHECKIDENT ('HINHANHSP', RESEED, 0);
DBCC CHECKIDENT ('GIOHANG', RESEED, 0);
DBCC CHECKIDENT ('HOADON', RESEED, 0);
DBCC CHECKIDENT ('DANHGIA', RESEED, 0);
DBCC CHECKIDENT ('KHIEUNAI', RESEED, 0);
DBCC CHECKIDENT ('TINNHAN', RESEED, 0);
GO

SELECT * FROM NGUOIDUNG
SELECT * FROM SANPHAM
SELECT * FROM HINHANHSP
SELECT * FROM GIOHANG
SELECT * FROM CT_GIOHANG
SELECT * FROM HOADON
SELECT * FROM CT_HOADON
SELECT * FROM LOAISANPHAM
SELECT * FROM DANHGIA
