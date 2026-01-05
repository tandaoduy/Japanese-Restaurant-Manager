CREATE DATABASE QuanLyNhaHangNhat_65133141;
GO

USE QuanLyNhaHangNhat_65133141;
GO

-- =============================================
-- 1. NHÓM QUẢN TRỊ & NHÂN SỰ
-- =============================================

-- 1.1. Bảng Vai Trò
CREATE TABLE VaiTro (
    VaiTroID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    MoTa NVARCHAR(255) NULL,
    IsActive BIT DEFAULT 1
);
GO

-- 1.2. Bảng Nhân Viên (Đã thêm NgaySinh và Avatar)
CREATE TABLE NhanVien (
    NhanVienID BIGINT IDENTITY(1,1) PRIMARY KEY,
    VaiTroID BIGINT NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE NULL, -- Cột mới thêm
    Avatar NVARCHAR(255) NULL, -- Cột mới thêm
    TaiKhoan NVARCHAR(50) UNIQUE NOT NULL,
    MatKhau NVARCHAR(255) NOT NULL, 
    Email NVARCHAR(100) NULL,
    SDT NVARCHAR(20) NULL,
    DiaChi NVARCHAR(255) NULL,
    NgayVaoLam DATE DEFAULT GETDATE(),
    TrangThai NVARCHAR(50) DEFAULT N'Đang làm việc',
    
    FOREIGN KEY (VaiTroID) REFERENCES VaiTro(VaiTroID)
);
GO

-- =============================================
-- 2. NHÓM KHÁCH HÀNG & TÀI NGUYÊN
-- =============================================

-- 2.1. Bảng Users (Đã thêm Avatar và NgaySinh)
CREATE TABLE Users (
    UserID BIGINT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) UNIQUE NULL,
    Password NVARCHAR(255) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE NULL, -- Thêm để đồng bộ với hồ sơ khách hàng
    Avatar NVARCHAR(255) NULL, -- Cột mới thêm
    Email NVARCHAR(100),
    SDT NVARCHAR(20),
    DiaChi NVARCHAR(255),
    DiemTichLuy INT DEFAULT 0,
    NgayTao DATETIME DEFAULT GETDATE(),
    TrangThai BIT DEFAULT 1 
);
GO

-- 2.2. Bảng Danh Mục Món Ăn
CREATE TABLE DanhMuc (
    DanhMucID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(MAX) NULL,
    HinhAnh NVARCHAR(255) NULL,
    IsHienThi BIT DEFAULT 1
);
GO

-- 2.3. Bảng Món Ăn
CREATE TABLE MonAn (
    MonAnID     BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenMon      NVARCHAR(255)    NOT NULL,
    DanhMucID   BIGINT           NOT NULL,
    GiaGoc      DECIMAL(18,2)    NOT NULL CHECK (GiaGoc >= 0),
    GiaGiam     DECIMAL(18,2)    NULL  CHECK (GiaGiam >= 0),
    Gia         AS (ISNULL(GiaGiam, GiaGoc)), -- cột tính toán: nếu có giảm thì dùng GiaGiam, không thì dùng GiaGoc
    MoTa        NVARCHAR(MAX)    NULL,
    HinhAnh     NVARCHAR(255)    NULL,
    DonViTinh   NVARCHAR(50)     NOT NULL DEFAULT N'Phần',
    TrangThai   NVARCHAR(50)     NOT NULL DEFAULT N'Đang kinh doanh',
    IsNoiBat    BIT              NOT NULL DEFAULT 0,
    NgayTao     DATETIME         NOT NULL DEFAULT GETDATE()
    -- ,FOREIGN KEY (DanhMucID) REFERENCES DanhMuc(DanhMucID) -- nếu bạn đã có bảng DanhMuc
);
GO
-- 2.4. Bảng Bàn Ăn
CREATE TABLE BanAn (
    BanID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenBan NVARCHAR(50) NOT NULL,
    SucChua INT CHECK (SucChua > 0),
    TrangThai NVARCHAR(50) DEFAULT N'Trống',
    ViTri NVARCHAR(100) NULL
);
GO

-- =============================================
-- 3. NHÓM MARKETING & VẬN HÀNH
-- =============================================

-- 3.1. Bảng Tin Tức
CREATE TABLE TinTuc (
    TinTucID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TieuDe NVARCHAR(255) NOT NULL,
    MoTaNgan NVARCHAR(500) NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    HinhAnh NVARCHAR(255) NULL,
    Slug VARCHAR(255) UNIQUE NOT NULL,
    NguoiDangID BIGINT NULL,
    NgayDang DATETIME DEFAULT GETDATE(),
    IsHienThi BIT DEFAULT 1,
    IsNoiBat BIT DEFAULT 0,

    FOREIGN KEY (NguoiDangID) REFERENCES NhanVien(NhanVienID) ON DELETE SET NULL
);
GO

-- 3.2. Bảng Thông Báo
CREATE TABLE ThongBao (
    ThongBaoID BIGINT IDENTITY(1,1) PRIMARY KEY,
    NguoiNhanID BIGINT NOT NULL,
    LoaiNguoiNhan NVARCHAR(50) NOT NULL DEFAULT N'KhachHang',
    TieuDe NVARCHAR(255) NOT NULL,
    NoiDung NVARCHAR(MAX) NULL,
    LienKet NVARCHAR(500) NULL,
    LoaiThongBao NVARCHAR(50) DEFAULT N'HeThong',
    DaDoc BIT NOT NULL DEFAULT 0,
    NgayTao DATETIME DEFAULT GETDATE()
);
GO
CREATE INDEX IX_ThongBao_NguoiNhan ON ThongBao(NguoiNhanID, LoaiNguoiNhan, DaDoc);
GO

-- 3.3. Bảng Đặt Bàn
CREATE TABLE DatBan (
    DatBanID BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserID BIGINT NULL,
    HoTenKhach NVARCHAR(100) NULL,
    SDTKhach NVARCHAR(20) NULL,
    BanID BIGINT NULL,
    ThoiGianDen DATETIME NOT NULL,
    SoNguoi INT CHECK (SoNguoi > 0),
    GhiChu NVARCHAR(MAX) NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Chờ xác nhận',
    NgayTao DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (BanID) REFERENCES BanAn(BanID)
);
GO

-- =============================================
-- 4. NHÓM KINH DOANH TẠI BÀN (DINE-IN)
-- =============================================

CREATE TABLE DonHang (
    DonHangID BIGINT IDENTITY(1,1) PRIMARY KEY,
    BanID BIGINT NULL,
    NhanVienID BIGINT NULL,
    UserID BIGINT NULL,
    NgayDat DATETIME DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (TongTien >= 0),
    TrangThai NVARCHAR(50) DEFAULT N'DangPhucVu',
    GhiChu NVARCHAR(MAX) NULL,

    FOREIGN KEY (BanID) REFERENCES BanAn(BanID),
    FOREIGN KEY (NhanVienID) REFERENCES NhanVien(NhanVienID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

CREATE TABLE ChiTietDonHang (
    ChiTietID BIGINT IDENTITY(1,1) PRIMARY KEY,
    DonHangID BIGINT NOT NULL,
    MonAnID BIGINT NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGia DECIMAL(18,2) NOT NULL,
    ThanhTien AS (SoLuong * DonGia),
    GhiChuMon NVARCHAR(255) NULL,

    FOREIGN KEY (DonHangID) REFERENCES DonHang(DonHangID) ON DELETE CASCADE,
    FOREIGN KEY (MonAnID) REFERENCES MonAn(MonAnID)
);
GO

CREATE TABLE HoaDon (
    HoaDonID BIGINT IDENTITY(1,1) PRIMARY KEY,
    DonHangID BIGINT NOT NULL,
    NhanVienThuNganID BIGINT NULL,
    NgayLap DATETIME DEFAULT GETDATE(),
    TongTienHang DECIMAL(18,2) NOT NULL,
    GiamGia DECIMAL(18,2) DEFAULT 0,
    ThueVAT DECIMAL(18,2) DEFAULT 0,
    PhiPhucVu DECIMAL(18,2) DEFAULT 0,
    TongThanhToan DECIMAL(18,2) NOT NULL,
    PhuongThucTT NVARCHAR(50) DEFAULT N'TienMat',

    FOREIGN KEY (DonHangID) REFERENCES DonHang(DonHangID),
    FOREIGN KEY (NhanVienThuNganID) REFERENCES NhanVien(NhanVienID)
);
GO

-- =============================================
-- 5. NHÓM KINH DOANH ONLINE (DELIVERY)
-- =============================================

CREATE TABLE DatHangOnline (
    DonOnlineID BIGINT IDENTITY(1,1) PRIMARY KEY,
    KhachHangID BIGINT NULL,
    HoTenNguoiNhan NVARCHAR(100) NOT NULL,
    SDTNguoiNhan NVARCHAR(20) NOT NULL,
    DiaChiGiaoHang NVARCHAR(255) NOT NULL,
    NgayDat DATETIME DEFAULT GETDATE(),
    TongTienHang DECIMAL(18,2) NOT NULL DEFAULT 0,
    PhiShip DECIMAL(18,2) DEFAULT 0,
    GiamGia DECIMAL(18,2) DEFAULT 0,
    TongThanhToan DECIMAL(18,2) NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'ChoXacNhan',
    GhiChu NVARCHAR(MAX) NULL,

    FOREIGN KEY (KhachHangID) REFERENCES Users(UserID)
);
GO

CREATE TABLE ChiTietDatHangOnline (
    ChiTietID BIGINT IDENTITY(1,1) PRIMARY KEY,
    DonOnlineID BIGINT NOT NULL,
    MonAnID BIGINT NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGia DECIMAL(18,2) NOT NULL,
    ThanhTien AS (SoLuong * DonGia),

    FOREIGN KEY (DonOnlineID) REFERENCES DatHangOnline(DonOnlineID) ON DELETE CASCADE,
    FOREIGN KEY (MonAnID) REFERENCES MonAn(MonAnID)
);
GO

CREATE TABLE ThanhToan (
    ThanhToanID BIGINT IDENTITY(1,1) PRIMARY KEY,
    DonOnlineID BIGINT NOT NULL,
    PhuongThuc NVARCHAR(50) NOT NULL,
    SoTien DECIMAL(18,2) NOT NULL CHECK (SoTien > 0),
    MaGiaoDich NVARCHAR(100) NULL,
    TrangThai NVARCHAR(50) DEFAULT N'ChoThanhToan',
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    GhiChu NVARCHAR(255) NULL,

    FOREIGN KEY (DonOnlineID) REFERENCES DatHangOnline(DonOnlineID) ON DELETE CASCADE
);
GO

-- =============================================
-- 6. BẢNG ĐÁNH GIÁ (FEEDBACK)
-- =============================================
CREATE TABLE DanhGia (
    DanhGiaID BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserID BIGINT NOT NULL,
    MonAnID BIGINT NULL,
    SoSao INT CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung NVARCHAR(MAX),
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (MonAnID) REFERENCES MonAn(MonAnID)
);
GO

-- =============================================
-- 7. DỮ LIỆU MẪU CƠ BẢN (SEED DATA)
-- =============================================

-- Vai trò
INSERT INTO VaiTro (TenVaiTro, MoTa) VALUES 
(N'Admin', N'Quản trị viên hệ thống'),
(N'Nhân viên', N'Nhân viên nhà hàng'),
(N'Khách hàng', N'Khách hàng');


-- Danh mục
INSERT INTO DanhMuc (TenDanhMuc) VALUES (N'SHUSHI'), (N'CƠM/MÌ'), (N'MÓN KHÁC'), (N'ĐỒ UỐNG'),(N'TRÁNG MIỆNG') ;

-- Bàn ăn
INSERT INTO BanAn (TenBan, SucChua, ViTri) VALUES 
(N'Bàn 1', 4, N'Sảnh chính'),
(N'Phòng Tatami VIP', 10, N'Tầng 2');

PRINT N'Cài đặt cơ sở dữ liệu QuanLyNhaHangNhat_65133141 thành công với thông tin cá nhân bổ sung!';
GO

ALTER TABLE DonHang
ADD SoDienThoai NVARCHAR(15) NULL;


SELECT VaiTroID, TenVaiTro
FROM VaiTro;

INSERT INTO NhanVien(HoTen, Email, SDT, MatKhau, VaiTroID, TaiKhoan, NgayVaoLam, TrangThai, DiaChi)
VALUES (
    N'Quản trị viên hệ thống',                                  -- HoTen
    'Admin@gmail.com',                                          -- Email
    NULL,                                                       -- SDT
    '231d084d91e7ddea62be82f1b07dca4d3f3d2c2e01270e069a25761609e2f823', -- MatKhau (SHA256)
    1,                                                          -- VaiTroID (Admin)
    'Admin@gmail.com',                                          -- TaiKhoan (username)
    GETDATE(),                                                  -- NgayVaoLam
    N'Hoạt động',                                               -- TrangThai
    NULL                                                        -- DiaChi
);


PRINT N'Cài đặt cơ sở dữ liệu QuanLyNhaHangNhat_65133141 thành công với thông tin cá nhân bổ sung!';
GO


INSERT INTO DanhMuc (TenDanhMuc, MoTa)
SELECT N'Sashimi', N'Cá sống Nhật Bản'
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc WHERE TenDanhMuc = N'Sashimi');

INSERT INTO DanhMuc (TenDanhMuc, MoTa)
SELECT N'Teishoku', N'Set ăn Nhật Bản'
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc WHERE TenDanhMuc = N'Teishoku');
GO

CREATE OR ALTER TRIGGER TRG_DonHang_LaySDT_User
ON DonHang
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dh
    SET dh.SoDienThoai = u.SDT
    FROM DonHang dh
    INNER JOIN inserted i ON dh.DonHangID = i.DonHangID
    INNER JOIN Users u ON i.UserID = u.UserID
    WHERE i.UserID IS NOT NULL
      AND (i.SoDienThoai IS NULL OR LTRIM(RTRIM(i.SoDienThoai)) = '');
END;
GO


-- SCRIPT: THÊM CỘT LƯU TRỮ THÔNG TIN KHI XÓA MÓN ĂN / BÀN
-- ============================================================

-- ============ PHẦN 1: THÊM CỘT CHO BẢNG ChiTietDonHang ============
-- Tại sao: Khi xóa món ăn, cột MonAnID sẽ không còn hợp lệ.
--          Cột TenMonSnapshot sẽ lưu lại tên món tại thời điểm đặt hàng.
ALTER TABLE ChiTietDonHang ADD TenMonSnapshot NVARCHAR(255) NULL;

-- Cho phép MonAnID = NULL (để sau khi xóa món, record vẫn tồn tại)
-- Tại sao: Mặc định FK không cho phép NULL, khi xóa sẽ báo lỗi.
--          Đổi thành NULL để có thể xóa món mà không ảnh hưởng record cũ.
ALTER TABLE ChiTietDonHang ALTER COLUMN MonAnID BIGINT NULL;

-- ============ PHẦN 2: THÊM CỘT CHO BẢNG DatBan ============
-- Tại sao: Khi xóa bàn, cột BanID sẽ không còn hợp lệ.
--          Cột TenBanSnapshot sẽ lưu lại tên bàn tại thời điểm đặt.
ALTER TABLE DatBan ADD TenBanSnapshot NVARCHAR(100) NULL;

-- BanID đã là nullable trong model hiện tại, không cần thay đổi.

-- ============ PHẦN 3: XÓA RÀNG BUỘC FOREIGN KEY (NẾU CẦN) ============
-- Tại sao: SQL Server có thể không cho xóa record nếu có FK constraint.
--          Cần xóa constraint cũ và tạo lại với ON DELETE SET NULL.

-- Tìm tên constraint FK của ChiTietDonHang -> MonAn (chạy query này để xem tên):
-- SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('ChiTietDonHang') AND referenced_object_id = OBJECT_ID('MonAn');

-- Sau đó xóa và tạo lại (thay 'FK_ChiTietDonHang_MonAn' bằng tên thực):
-- ALTER TABLE ChiTietDonHang DROP CONSTRAINT FK_ChiTietDonHang_MonAn;
-- ALTER TABLE ChiTietDonHang ADD CONSTRAINT FK_ChiTietDonHang_MonAn 
--     FOREIGN KEY (MonAnID) REFERENCES MonAn(MonAnID) ON DELETE SET NULL;

-- Tương tự cho DatBan -> BanAn:
-- SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('DatBan') AND referenced_object_id = OBJECT_ID('BanAn');
-- ALTER TABLE DatBan DROP CONSTRAINT FK_DatBan_BanAn;
-- ALTER TABLE DatBan ADD CONSTRAINT FK_DatBan_BanAn 
--     FOREIGN KEY (BanID) REFERENCES BanAn(BanID) ON DELETE SET NULL;