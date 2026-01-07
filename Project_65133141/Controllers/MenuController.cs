using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;

namespace Project_65133141.Controllers
{
    public class MenuController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Menu
        public ActionResult Index(long? categoryId, string searchString = "")
        {
            // Lấy danh sách danh mục với thứ tự tùy chỉnh
            var categories = db.DanhMucs.ToList()
                .OrderBy(c => {
                    var name = (c.TenDanhMuc ?? "").ToUpperInvariant();
                    // Thứ tự: Sashimi, Sushi, Cơm/Mì, Teishoku, Đồ uống, Món khác, Tráng miệng
                    if (name.Contains("SASHIMI")) return 1;
                    if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                    if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                    if (name.Contains("TEISHOKU")) return 4;
                    if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                    if (name.Contains("TRÁNG MIỆNG")) return 7;
                    return 6; // Món khác và các danh mục khác
                })
                .ToList();

            // Tạo dictionary lưu thứ tự của từng category
            var categoryOrder = categories.Select((c, idx) => new { c.DanhMucID, Order = idx }).ToDictionary(x => x.DanhMucID, x => x.Order);

            // Lấy danh sách món ăn
            var query = db.MonAns
                .Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ")
                .AsQueryable();

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(m => m.DanhMucID == categoryId.Value);
            }

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => 
                    m.TenMon.Contains(searchString) || 
                    (m.MoTa != null && m.MoTa.Contains(searchString))
                );
            }

            // Load products
            var products = query.ToList();
            
            // Sort products by category order, then by name
            products = products
                .OrderBy(m => categoryOrder.ContainsKey(m.DanhMucID) ? categoryOrder[m.DanhMucID] : 99)
                .ThenBy(m => m.TenMon)
                .ToList();

            // Create a dictionary to map DanhMucID to TenDanhMuc for easy lookup in view
            var categoryDict = db.DanhMucs.ToDictionary(d => d.DanhMucID, d => d.TenDanhMuc);
            ViewBag.Categories = categories;
            ViewBag.CategoryDict = categoryDict;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchString = searchString;

            return View(products);
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


