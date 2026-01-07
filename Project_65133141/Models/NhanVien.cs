namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class NhanVien
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public NhanVien()
        {
            this.DonHangs = new HashSet<DonHang>();
            this.HoaDons = new HashSet<HoaDon>();
            this.TinTucs = new HashSet<TinTuc>();
        }
    
        public long NhanVienID { get; set; }
        public long VaiTroID { get; set; }
        public string HoTen { get; set; }
        public Nullable<System.DateTime> NgaySinh { get; set; }
        public string Avatar { get; set; }
        public string TaiKhoan { get; set; }
        public string MatKhau { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public Nullable<System.DateTime> NgayVaoLam { get; set; }
        public string TrangThai { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DonHang> DonHangs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<HoaDon> HoaDons { get; set; }
        public virtual VaiTro VaiTro { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TinTuc> TinTucs { get; set; }
    }
}
