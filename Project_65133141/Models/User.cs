namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            this.DatBans = new HashSet<DatBan>();
            this.DatHangOnlines = new HashSet<DatHangOnline>();
            this.DonHangs = new HashSet<DonHang>();
            this.DanhGias = new HashSet<DanhGia>();
        }
    
        public long UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string HoTen { get; set; }
        public Nullable<System.DateTime> NgaySinh { get; set; }
        public string Avatar { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public Nullable<int> DiemTichLuy { get; set; }
        public Nullable<System.DateTime> NgayTao { get; set; }
        public Nullable<bool> TrangThai { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DatBan> DatBans { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DatHangOnline> DatHangOnlines { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DonHang> DonHangs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DanhGia> DanhGias { get; set; }
    }
}
