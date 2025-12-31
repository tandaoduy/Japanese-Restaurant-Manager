using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class DonHangController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/DonHang
        public ActionResult Index()
        {
            // Get all orders with related data
            var orders = db.DonHangs
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.NhanVien)
                .OrderByDescending(o => o.NgayDat)
                .ThenByDescending(o => o.DonHangID)
                .ToList();

            return View(orders);
        }

        // GET: Admin_65133141/DonHang/Details/5
        public ActionResult Details(long id)
        {
            var order = db.DonHangs
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.NhanVien)
                .Include(o => o.ChiTietDonHangs)
                .Include(o => o.ChiTietDonHangs.Select(c => c.MonAn))
                .FirstOrDefault(o => o.DonHangID == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Index");
            }

            return View(order);
        }
    }
}
