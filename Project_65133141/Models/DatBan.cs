namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DatBan
    {
        public long DatBanID { get; set; }
        public Nullable<long> UserID { get; set; }
        public string HoTenKhach { get; set; }
        public string SDTKhach { get; set; }
        public Nullable<long> BanID { get; set; }
        public System.DateTime ThoiGianDen { get; set; }
        public Nullable<int> SoNguoi { get; set; }
        public string GhiChu { get; set; }
        public string TrangThai { get; set; }
        public Nullable<System.DateTime> NgayTao { get; set; }
        public string TenBanSnapshot { get; set; }
    
        public virtual BanAn BanAn { get; set; }
        public virtual User User { get; set; }
    }
}
