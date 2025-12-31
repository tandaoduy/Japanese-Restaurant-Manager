using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class StatisticsController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Statistics
        public ActionResult Index(DateTime? startDate, DateTime? endDate)
        {
            // Get current employee ID
            var nhanVienId = Session["UserId"] as long?;
            if (!nhanVienId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên";
                return RedirectToAction("Index", "Home");
            }

            // Default to current month if no dates provided
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!endDate.HasValue)
            {
                endDate = DateTime.Now;
            }

            // Get orders for this employee in date range
            var orders = db.DonHangs
                .Where(o => o.NhanVienID == nhanVienId.Value && 
                           o.NgayDat >= startDate.Value && 
                           o.NgayDat <= endDate.Value)
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.ChiTietDonHangs)
                .Include(o => o.ChiTietDonHangs.Select(c => c.MonAn))
                .ToList();

            // Calculate statistics
            var totalOrders = orders.Count;
            var completedOrders = orders.Count(o => o.TrangThai == "Đã thanh toán");
            var totalRevenue = orders.Where(o => o.TrangThai == "Đã thanh toán").Sum(o => o.TongTien);
            var averageOrderValue = completedOrders > 0 ? totalRevenue / completedOrders : 0;

            // Get orders by status
            var ordersByStatus = orders
                .GroupBy(o => o.TrangThai ?? "Chưa xác định")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            // Get top selling items
            var topItems = orders
                .Where(o => o.TrangThai == "Đã thanh toán")
                .SelectMany(o => o.ChiTietDonHangs)
                .GroupBy(d => new { d.MonAnID, d.MonAn.TenMon })
                .Select(g => new
                {
                    MonAnID = g.Key.MonAnID,
                    TenMon = g.Key.TenMon,
                    SoLuong = g.Sum(d => d.SoLuong),
                    DoanhThu = g.Sum(d => d.ThanhTien ?? (d.DonGia * d.SoLuong))
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToList();

            // Get daily revenue (last 30 days)
            var dailyRevenue = orders
                .Where(o => o.TrangThai == "Đã thanh toán" && 
                           o.NgayDat >= DateTime.Now.AddDays(-30))
                .GroupBy(o => o.NgayDat.HasValue ? o.NgayDat.Value.Date : (DateTime?)null)
                .Where(g => g.Key.HasValue)
                .Select(g => new
                {
                    Date = g.Key.Value,
                    Revenue = g.Sum(o => o.TongTien),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.StartDate = startDate.Value;
            ViewBag.EndDate = endDate.Value;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AverageOrderValue = averageOrderValue;
            ViewBag.OrdersByStatus = ordersByStatus;
            ViewBag.TopItems = topItems;
            ViewBag.DailyRevenue = dailyRevenue;
            ViewBag.Orders = orders.OrderByDescending(o => o.NgayDat).Take(50).ToList();

            return View();
        }
    }
}

