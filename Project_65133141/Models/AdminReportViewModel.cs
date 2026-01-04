using System;
using System.Collections.Generic;

namespace Project_65133141.Models
{
    public class RevenuePoint
    {
        public string Label { get; set; }
        public decimal Total { get; set; }
    }

    public class TopDishReport
    {
        public long MonAnID { get; set; }
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class EmployeePerformanceReport
    {
        public long? NhanVienID { get; set; }
        public string TenNhanVien { get; set; }
        public int SoHoaDon { get; set; }
        public decimal TongDoanhThu { get; set; }
    }

    public class AdminReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool GroupByMonth { get; set; }

        public List<RevenuePoint> RevenuePoints { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<TopDishReport> TopDishes { get; set; }
        public List<EmployeePerformanceReport> EmployeeStats { get; set; }

        public AdminReportViewModel()
        {
            RevenuePoints = new List<RevenuePoint>();
            TopDishes = new List<TopDishReport>();
            EmployeeStats = new List<EmployeePerformanceReport>();
        }
    }
}
