namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class VaiTro
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public VaiTro()
        {
            this.NhanViens = new HashSet<NhanVien>();
        }
    
        public long VaiTroID { get; set; }
        public string TenVaiTro { get; set; }
        public string MoTa { get; set; }
        public Nullable<bool> IsActive { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NhanVien> NhanViens { get; set; }
    }
}
