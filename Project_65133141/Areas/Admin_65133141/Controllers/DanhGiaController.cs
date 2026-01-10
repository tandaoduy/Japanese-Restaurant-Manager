using System;
using System.Linq;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    /// <summary>
    /// Controller quản lý đánh giá khách hàng (chỉ xem và xóa)
    /// </summary>
    [RoleAuthorize("admin")]
    public class DanhGiaController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/DanhGia
        public ActionResult Index(string search = "", int? filterStar = null, int page = 1)
        {
            const int pageSize = 12;

            var query = db.DanhGias.AsQueryable();

            // Filter by star rating
            if (filterStar.HasValue && filterStar >= 1 && filterStar <= 5)
            {
                query = query.Where(d => d.SoSao == filterStar.Value);
            }

            // Search by content
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(d => d.NoiDung.ToLower().Contains(search));
            }

            // Order by newest first
            query = query.OrderByDescending(d => d.NgayDanhGia);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var danhGias = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Manually populate User info since navigation property is not in EDMX
            foreach (var item in danhGias)
            {
                item.User = db.Users.Find(item.UserID);
            }

            // Calculate average rating
            var allRatings = db.DanhGias.ToList();
            double avgRating = allRatings.Any() ? allRatings.Average(d => d.SoSao) : 0;

            ViewBag.Search = search;
            ViewBag.FilterStar = filterStar;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.AverageRating = Math.Round(avgRating, 1);

            return View(danhGias);
        }

        // GET: Admin_65133141/DanhGia/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var danhGia = db.DanhGias.Find(id);
            if (danhGia == null)
            {
                return HttpNotFound();
            }

            return View(danhGia);
        }

        // POST: Admin_65133141/DanhGia/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long DanhGiaID)
        {
            var danhGia = db.DanhGias.Find(DanhGiaID);
            if (danhGia == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá cần xóa." });
                }
                return HttpNotFound();
            }

            db.DanhGias.Remove(danhGia);
            db.SaveChanges();

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, message = "Đã xóa đánh giá thành công!" });
            }

            TempData["SuccessMessage"] = "Đã xóa đánh giá thành công!";
            return RedirectToAction("Index");
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
