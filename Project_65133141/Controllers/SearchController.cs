using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Project_65133141.Models;

namespace Project_65133141.Controllers
{
    /// <summary>
    /// Controller xử lý tìm kiếm toàn trang web
    /// </summary>
    public class SearchController : Controller
    {
        private readonly QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        /// <summary>
        /// API trả về gợi ý tìm kiếm theo từ khóa
        /// </summary>
        public JsonResult Suggest(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            query = query.ToLower().Trim();

            // 1. Tìm kiếm Món ăn (Products)
            var products = db.MonAns
                .Where(m => m.TenMon.ToLower().Contains(query) ||
                           (m.MoTa != null && m.MoTa.ToLower().Contains(query)))
                .Take(5)
                .Select(m => new
                {
                    id = m.MonAnID,
                    name = m.TenMon,
                    image = m.HinhAnh,
                    price = m.Gia,
                    url = "/Menu#mon-" + m.MonAnID
                })
                .ToList();

            // 2. Tìm kiếm Tin tức
            var news = db.TinTucs
                .Where(t => t.IsHienThi == true &&
                           (t.TieuDe.ToLower().Contains(query) ||
                           (t.MoTaNgan != null && t.MoTaNgan.ToLower().Contains(query))))
                .Take(5)
                .Select(t => new
                {
                    id = t.TinTucID,
                    title = t.TieuDe,
                    image = t.HinhAnh,
                    slug = t.Slug,
                    url = "/User_65133141/TinTuc/Details/" + t.Slug
                })
                .ToList();

            // 3. Tìm kiếm Danh mục
            var categories = db.DanhMucs
                .Where(d => d.TenDanhMuc.ToLower().Contains(query))
                .Take(3)
                .Select(d => new
                {
                    id = d.DanhMucID,
                    name = d.TenDanhMuc,
                    url = "/Menu?category=" + d.DanhMucID
                })
                .ToList();

            // 4. LUÔN hiển thị tất cả trang gợi ý (Suggested)
            var suggestedPages = new List<object>
            {
                new { name = "Trang chủ", url = "/" },
                new { name = "Giới thiệu", url = "/Home/About" },
                new { name = "Liên hệ", url = "/Home/Contact" },
                new { name = "Thực đơn", url = "/Menu" },
                new { name = "Đặt bàn", url = "/User_65133141/DatBan/Create" },
                new { name = "Tin tức & Khuyến mãi", url = "/User_65133141/TinTuc" },
                new { name = "Món ăn nổi bật", url = "/#featured-dishes" },
                new { name = "Đăng nhập", url = "/Account/Login" }
            };

            return Json(new
            {
                success = true,
                products = products,
                news = news,
                pages = suggestedPages, // Luôn hiển thị tất cả
                categories = categories
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Trang kết quả tìm kiếm đầy đủ
        /// </summary>
        public ActionResult Results(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Query = q;
            q = q.ToLower().Trim();

            var products = db.MonAns
                .Where(m => m.TenMon.ToLower().Contains(q) ||
                           (m.MoTa != null && m.MoTa.ToLower().Contains(q)))
                .ToList();

            var news = db.TinTucs
                .Where(t => t.IsHienThi == true &&
                           (t.TieuDe.ToLower().Contains(q) ||
                           (t.MoTaNgan != null && t.MoTaNgan.ToLower().Contains(q))))
                .ToList();

            ViewBag.Products = products;
            ViewBag.News = news;
            ViewBag.TotalResults = products.Count + news.Count;

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
