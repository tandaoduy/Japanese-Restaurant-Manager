namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TinTuc
    {
        public long TinTucID { get; set; }
        public string TieuDe { get; set; }
        public string MoTaNgan { get; set; }
        public string NoiDung { get; set; }
        public string HinhAnh { get; set; }
        public string Slug { get; set; }
        public Nullable<long> NguoiDangID { get; set; }
        public Nullable<System.DateTime> NgayDang { get; set; }
        public Nullable<bool> IsHienThi { get; set; }
        public Nullable<bool> IsNoiBat { get; set; }
    
        public virtual NhanVien NhanVien { get; set; }
    }
}
