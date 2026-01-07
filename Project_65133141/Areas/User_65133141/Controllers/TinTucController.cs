using System;
using System.Linq;
using System.Web.Mvc;
using Project_65133141.Models;

namespace Project_65133141.Areas.User_65133141.Controllers
{
    /// <summary>
    /// Controller hiển thị tin tức và ưu đãi cho khách hàng
    /// </summary>
    public class TinTucController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        /// <summary>
        /// Hiển thị danh sách tin tức công khai
        /// </summary>
        public ActionResult Index(int page = 1)
        {
            const int pageSize = 9;
            
            var query = db.TinTucs
                .Where(t => t.IsHienThi == true)
                .OrderByDescending(t => t.IsNoiBat)
                .ThenByDescending(t => t.NgayDang);
            
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            var tinTucs = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            
            return View(tinTucs);
        }

        /// <summary>
        /// Trường hợp truy cập /TinTuc/Details không có slug -> chuyển về danh sách
        /// </summary>
        public ActionResult Details()
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị chi tiết tin tức theo slug
        /// </summary>
        public ActionResult Details(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return RedirectToAction("Index");
            
            var tinTuc = db.TinTucs
                .FirstOrDefault(t => t.Slug == slug && t.IsHienThi == true);
            
            if (tinTuc == null)
                return HttpNotFound();
            
            // Lấy tin tức liên quan (cùng thể loại hoặc mới nhất)
            var relatedNews = db.TinTucs
                .Where(t => t.IsHienThi == true && t.TinTucID != tinTuc.TinTucID)
                .OrderByDescending(t => t.NgayDang)
                .Take(3)
                .ToList();
            
            ViewBag.RelatedNews = relatedNews;
            
            return View(tinTuc);
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
