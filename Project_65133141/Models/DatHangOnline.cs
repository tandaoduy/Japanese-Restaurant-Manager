namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DatHangOnline
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DatHangOnline()
        {
            this.ChiTietDatHangOnlines = new HashSet<ChiTietDatHangOnline>();
            this.ThanhToans = new HashSet<ThanhToan>();
        }
    
        public long DonOnlineID { get; set; }
        public Nullable<long> KhachHangID { get; set; }
        public string HoTenNguoiNhan { get; set; }
        public string SDTNguoiNhan { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public Nullable<System.DateTime> NgayDat { get; set; }
        public decimal TongTienHang { get; set; }
        public Nullable<decimal> PhiShip { get; set; }
        public Nullable<decimal> GiamGia { get; set; }
        public decimal TongThanhToan { get; set; }
        public string TrangThai { get; set; }
        public string GhiChu { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChiTietDatHangOnline> ChiTietDatHangOnlines { get; set; }
        public virtual User User { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ThanhToan> ThanhToans { get; set; }
    }
}
