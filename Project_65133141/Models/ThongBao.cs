namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ThongBao
    {
        public long ThongBaoID { get; set; }
        public long NguoiNhanID { get; set; }
        public string LoaiNguoiNhan { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string LienKet { get; set; }
        public string LoaiThongBao { get; set; }
        public bool DaDoc { get; set; }
        public Nullable<System.DateTime> NgayTao { get; set; }
    }
}
