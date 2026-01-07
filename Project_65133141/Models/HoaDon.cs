namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class HoaDon
    {
        public long HoaDonID { get; set; }
        public long DonHangID { get; set; }
        public Nullable<long> NhanVienThuNganID { get; set; }
        public Nullable<System.DateTime> NgayLap { get; set; }
        public decimal TongTienHang { get; set; }
        public Nullable<decimal> GiamGia { get; set; }
        public Nullable<decimal> ThueVAT { get; set; }
        public Nullable<decimal> PhiPhucVu { get; set; }
        public decimal TongThanhToan { get; set; }
        public string PhuongThucTT { get; set; }
    
        public virtual DonHang DonHang { get; set; }
        public virtual NhanVien NhanVien { get; set; }
    }
}
