using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.User_65133141.Controllers
{
    // TEMPORARILY DISABLED FOR TESTING - TODO: re-enable after fixing redirect loop
    // [RoleAuthorize("Khách hàng", "khach hang", "user", "customer")]
    public class HomeController : BaseAreaController
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: User_65133141/Home
        public ActionResult Index()
        {
            // Get featured dishes for user menu display (top 6 active dishes)
            var featuredProducts = db.MonAns
                .Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ")
                .OrderByDescending(m => m.NgayTao)
                .Take(6)
                .ToList();
            ViewBag.FeaturedProducts = featuredProducts;

            // Create a dictionary to map DanhMucID to TenDanhMuc for easy lookup in view
            var categories = db.DanhMucs.ToDictionary(d => d.DanhMucID, d => d.TenDanhMuc);
            ViewBag.Categories = categories;

            // Get featured news for home page display (matching root controller)
            var featuredNews = db.TinTucs
                .Where(t => t.IsHienThi == true)
                .OrderByDescending(t => t.IsNoiBat)
                .ThenByDescending(t => t.NgayDang)
                .Take(6)
                .ToList();
            ViewBag.FeaturedNews = featuredNews;

            return View();
        }

        // GET: User_65133141/Home/About
        public ActionResult About()
        {
            return View();
        }

        // GET: User_65133141/Home/Contact
        public ActionResult Contact()
        {
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