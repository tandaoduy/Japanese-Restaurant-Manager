namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ThanhToan
    {
        public long ThanhToanID { get; set; }
        public long DonOnlineID { get; set; }
        public string PhuongThuc { get; set; }
        public decimal SoTien { get; set; }
        public string MaGiaoDich { get; set; }
        public string TrangThai { get; set; }
        public Nullable<System.DateTime> NgayThanhToan { get; set; }
        public string GhiChu { get; set; }
    
        public virtual DatHangOnline DatHangOnline { get; set; }
    }
}
