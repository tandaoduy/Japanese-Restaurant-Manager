using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Menu
        public ActionResult Index(string searchTerm = "", long? categoryId = null)
        {
            try
            {
                // Get all categories
                var categories = db.DanhMucs
                    .Where(d => d.IsHienThi == true || d.IsHienThi == null)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToList();
                ViewBag.Categories = categories ?? new List<DanhMuc>();

                // Get menu items
                var query = db.MonAns.AsQueryable();

                // Filter by category
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(m => m.DanhMucID == categoryId.Value);
                }

                // Search by name
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(m => m.TenMon.Contains(searchTerm));
                }

                // Only show active items
                query = query.Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ");

                var menuItems = query
                    .OrderBy(m => m.DanhMucID)
                    .ThenBy(m => m.TenMon)
                    .ToList();

                // Group by category using Tuple for better compatibility
                var groupedMenu = menuItems
                    .GroupBy(m => m.DanhMucID)
                    .Select(g => new System.Tuple<DanhMuc, List<MonAn>>(
                        categories.FirstOrDefault(c => c.DanhMucID == g.Key),
                        g.ToList()
                    ))
                    .Where(g => g.Item1 != null)
                    .OrderBy(g => g.Item1.TenDanhMuc)
                    .ToList();

                ViewBag.GroupedMenu = groupedMenu;
                ViewBag.SearchTerm = searchTerm ?? "";
                ViewBag.CategoryId = categoryId;

                return View();
            }
            catch (Exception ex)
            {
                // Log error and show message
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải menu: " + ex.Message;
                ViewBag.Categories = new List<DanhMuc>();
                ViewBag.GroupedMenu = null;
                ViewBag.SearchTerm = searchTerm ?? "";
                ViewBag.CategoryId = categoryId;
                return View();
            }
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

