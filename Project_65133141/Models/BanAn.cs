namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class BanAn
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BanAn()
        {
            this.DatBans = new HashSet<DatBan>();
            this.DonHangs = new HashSet<DonHang>();
        }
    
        public long BanID { get; set; }
        public string TenBan { get; set; }
        public Nullable<int> SucChua { get; set; }
        public string TrangThai { get; set; }
        public string ViTri { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DatBan> DatBans { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DonHang> DonHangs { get; set; }
    }
}
