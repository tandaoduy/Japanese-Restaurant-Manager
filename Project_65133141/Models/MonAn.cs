namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class MonAn
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MonAn()
        {
            this.ChiTietDatHangOnlines = new HashSet<ChiTietDatHangOnline>();
            this.ChiTietDonHangs = new HashSet<ChiTietDonHang>();
        }
    
        public long MonAnID { get; set; }
        public string TenMon { get; set; }
        public long DanhMucID { get; set; }
        public decimal GiaGoc { get; set; }
        public Nullable<decimal> GiaGiam { get; set; }
        public decimal Gia { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public string DonViTinh { get; set; }
        public string TrangThai { get; set; }
        public bool IsNoiBat { get; set; }
        public System.DateTime NgayTao { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChiTietDatHangOnline> ChiTietDatHangOnlines { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
    }
}
