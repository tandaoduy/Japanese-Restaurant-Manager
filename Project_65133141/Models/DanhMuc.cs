namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DanhMuc
    {
        public long DanhMucID { get; set; }
        public string TenDanhMuc { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public Nullable<bool> IsHienThi { get; set; }
    }
}
