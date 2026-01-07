namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DanhGia
    {
        public long DanhGiaID { get; set; }
        public long UserID { get; set; }
        public int SoSao { get; set; }
        public string NoiDung { get; set; }
        public Nullable<System.DateTime> NgayDanhGia { get; set; }

        public virtual User User { get; set; }
    }
}
