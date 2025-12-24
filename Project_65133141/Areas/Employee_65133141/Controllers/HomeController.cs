using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    public class HomeController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Home
        public ActionResult Index()
        {
            // Get all active products for menu display
            var allProducts = db.MonAns
                .Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ")
                .OrderByDescending(m => m.NgayTao)
                .ToList();
            ViewBag.FeaturedProducts = allProducts;

            // Get categories for category mapping
            var categories = db.DanhMucs.ToDictionary(d => d.DanhMucID, d => d.TenDanhMuc);
            ViewBag.Categories = categories;

            // Get all categories for filter buttons
            var allCategories = db.DanhMucs.ToList();
            ViewBag.AllCategories = allCategories;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}