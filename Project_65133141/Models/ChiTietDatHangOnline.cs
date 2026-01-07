namespace Project_65133141.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ChiTietDatHangOnline
    {
        public long ChiTietID { get; set; }
        public long DonOnlineID { get; set; }
        public long MonAnID { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public Nullable<decimal> ThanhTien { get; set; }
    
        public virtual DatHangOnline DatHangOnline { get; set; }
        public virtual MonAn MonAn { get; set; }
    }
}
