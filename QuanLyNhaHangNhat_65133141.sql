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
    
    -- Đánh giá sao cho dịch vụ/website
    SoSao INT NOT NULL CHECK (SoSao BETWEEN 1 AND 5), 
    
    -- Nội dung góp ý về trải nghiệm
    NoiDung NVARCHAR(MAX), 
    
    -- Thời điểm đánh giá
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    
    -- Liên kết với bảng người dùng
    CONSTRAINT FK_DanhGia_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
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

--admin
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

-- Danh mục
INSERT INTO DanhMuc (TenDanhMuc) VALUES (N'SHUSHI'), (N'CƠM/MÌ'), (N'MÓN KHÁC'), (N'ĐỒ UỐNG'),(N'TRÁNG MIỆNG') ;

INSERT INTO DanhMuc (TenDanhMuc, MoTa)
SELECT N'Sashimi', N'Cá sống Nhật Bản'
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc WHERE TenDanhMuc = N'Sashimi');

INSERT INTO DanhMuc (TenDanhMuc, MoTa)
SELECT N'Teishoku', N'Set ăn Nhật Bản'
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc WHERE TenDanhMuc = N'Teishoku');
GO

-- Bàn ăn
INSERT INTO BanAn (TenBan, SucChua, ViTri) VALUES 
(N'Bàn 1', 4, N'Sảnh chính'),
(N'Phòng Tatami VIP', 10, N'Tầng 2');

PRINT N'Cài đặt cơ sở dữ liệu QuanLyNhaHangNhat_65133141 thành công với thông tin cá nhân bổ sung!';
GO

--Thông báo
INSERT INTO ThongBao (NguoiNhanID, LoaiNguoiNhan, TieuDe, NoiDung, LienKet, LoaiThongBao, DaDoc, NgayTao)
VALUES 
-- 1. Thông báo chào mừng thành viên mới
(1, N'KhachHang', N'Chào mừng bạn đến với Nhà hàng!', N'Cảm ơn bạn đã đăng ký thành viên. Tặng bạn mã giảm giá WELCOME10 giảm 10% cho lần đặt bàn đầu tiên.', N'/KhuyenMai/ChiTiet/WELCOME10', N'HeThong', 0, GETDATE()),

-- 2. Thông báo xác nhận đặt bàn (Loại thông báo: DonHang)
(1, N'KhachHang', N'Xác nhận đặt bàn thành công #db123', N'Bàn của bạn đã được xác nhận vào lúc 19:00 ngày hôm nay. Vui lòng đến đúng giờ nhé!', N'/DatBan/ChiTiet/123', N'DonHang', 0, DATEADD(MINUTE, -30, GETDATE())),

-- 3. Thông báo khuyến mãi (Loại thông báo: KhuyenMai)
(2, N'KhachHang', N'Ưu đãi "Thứ 6 Vui Vẻ" - Giảm 20%', N'Duy nhất thứ 6 tuần này, giảm ngay 20% cho tất cả các set Sashimi. Đặt bàn ngay để không bỏ lỡ!', N'/KhuyenMai/ChiTiet/KM-T6', N'KhuyenMai', 0, DATEADD(HOUR, -2, GETDATE())),

-- 4. Thông báo món mới (Loại thông báo: TinTuc)
(3, N'KhachHang', N'Ra mắt Thực đơn Mùa Xuân', N'Khám phá hương vị tươi mới với bộ sưu tập món ăn Mùa Xuân vừa ra mắt. Xem chi tiết tại đây.', N'/ThucDon/MuaXuan', N'TinTuc', 1, DATEADD(DAY, -1, GETDATE())),

-- 5. Thông báo chăm sóc khách hàng (Loại thông báo: CSKH)
(2, N'KhachHang', N'Chúc mừng sinh nhật Quý khách!', N'Nhà hàng xin gửi tặng bạn một phần bánh ngọt tráng miệng cho bữa tiệc sinh nhật trong tháng này.', N'/TaiKhoan/Voucher', N'CSKH', 0, DATEADD(DAY, -5, GETDATE()));
GO

--danh gia
-- Thêm dữ liệu mẫu cho bảng DanhGia
INSERT INTO DanhGia (UserID, SoSao, NoiDung, NgayDanhGia)
VALUES 
(1, 5, N'Món Sashimi rất tươi, trình bày đẹp mắt như một tác phẩm nghệ thuật. Sẽ quay lại!', '2026-01-02 18:30:00'),
(2, 4, N'Không gian quán ấm cúng, đậm chất Nhật Bản. Tuy nhiên phục vụ hơi chậm một chút vào giờ cao điểm.', '2026-01-03 19:15:00'),
(3, 5, N'Sushi cá hồi béo ngậy, tan trong miệng. Rất đáng đồng tiền bát gạo.', '2026-01-03 20:00:00'),
(5, 5, N'Nhân viên nhiệt tình, dễ thương. Mình đi sinh nhật được tặng bánh rất ngon.', '2026-01-04 12:30:00'),
(6, 3, N'Món ăn ngon nhưng giá hơi cao so với mặt bằng chung.', '2026-01-04 13:45:00'),
(7, 5, N'Tuyệt vời! Đã ăn ở nhiều nơi nhưng bò Wagyu ở đây là đỉnh nhất.', '2026-01-05 18:00:00'),
(8, 4, N'Thích nhất là không gian riêng tư, nhạc nhẹ nhàng rất thư giãn.', '2026-01-05 19:30:00'),
(9, 5, N'10 điểm cho chất lượng. Sẽ giới thiệu cho bạn bè.', '2026-01-05 20:45:00'),
(10, 2, N'Hôm nay quán quá đông, mình phải đợi bàn hơi lâu dù đã đặt trước.', '2026-01-06 11:30:00'),
(11, 5, N'Mọi thứ đều hoàn hảo từ món khai vị đến tráng miệng.', '2026-01-06 12:15:00'),
(12, 4, N'Set cơm trưa rất đầy đặn và ngon miệng.', '2026-01-06 13:00:00'),
(13, 5, N'Rượu Sake rất ngon, hợp với đồ nhắm.', '2026-01-07 19:00:00'),
(14, 5, N'Nhà hàng view đẹp, thích hợp để hẹn hò.', '2026-01-07 20:30:00'),
(1, 3, N'Tempura hơi nhiều dầu một chút, cần khắc phục.', '2026-01-08 11:45:00'),
(2, 5, N'Ông chủ rất hiếu khách. Cảm giác như đang ăn ở Osaka.', '2026-01-08 12:30:00'),
(3, 4, N'Menu đa dạng, nhiều lựa chọn cho cả người ăn chay.', '2026-01-08 13:15:00'),
(5, 5, N'Đã trở thành quán ruột của gia đình mình vào cuối tuần.', '2026-01-08 18:45:00'),
(6, 5, N'Chất lượng phục vụ 5 sao chuẩn Nhật.', '2026-01-08 19:30:00'),
(7, 4, N'Mỳ Udon nước dùng thanh ngọt, sợi mỳ dai vừa đủ.', '2026-01-08 20:00:00'),
(8, 5, N'Không gian check-in sống ảo cực đẹp.', '2026-01-08 21:00:00'),
--(Nội dung ngắn gọn hoặc UserID đại diện cho việc ẩn danh)
(9, 5, N'Dịch vụ tốt.', '2026-01-01 10:00:00'),
(10, 4, N'Hài lòng.', '2026-01-01 11:00:00'),
(11, 5, N'Rất ngon.', '2026-01-02 12:00:00'),
(12, 3, N'Tạm ổn.', '2026-01-02 13:00:00'),
(13, 5, N'Xuất sắc.', '2026-01-03 14:00:00'),
(14, 5, N'Tuyệt.', '2026-01-03 15:00:00'),
(1, 4, N'Ok.', '2026-01-04 16:00:00'),
(2, 5, N'Good service.', '2026-01-05 17:00:00'),
(3, 5, N'Yummy.', '2026-01-06 18:00:00'),
(5, 4, N'Nice place.', '2026-01-07 19:00:00');
GO
ALTER TABLE DonHang
ADD SoDienThoai NVARCHAR(15) NULL;

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
ALTER TABLE ChiTietDonHang ADD TenMonSnapshot NVARCHAR(255) NULL;
ALTER TABLE ChiTietDonHang ALTER COLUMN MonAnID BIGINT NULL;

-- ============ PHẦN 2: THÊM CỘT CHO BẢNG DatBan ============
-- Tại sao: Khi xóa bàn, cột BanID sẽ không còn hợp lệ.
--          Cột TenBanSnapshot sẽ lưu lại tên bàn tại thời điểm đặt.
ALTER TABLE DatBan ADD TenBanSnapshot NVARCHAR(100) NULL;

SELECT VaiTroID, TenVaiTro
FROM VaiTro;
SELECT *FROM Users;
SELECT *FROM ThongBao;