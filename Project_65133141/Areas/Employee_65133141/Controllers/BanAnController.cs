using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee", "admin")]
    public class BanAnController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/BanAn
        public ActionResult Index(string statusFilter = null)
        {
            var query = db.BanAns.AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(b => b.TrangThai == statusFilter);
            }

            // Group by ViTri (location/type)
            var tables = query
                .OrderBy(b => b.ViTri)
                .ThenBy(b => b.TenBan)
                .ToList();

            // Group tables by type/location
            var groupedTables = tables
                .GroupBy(b => b.ViTri ?? "Khác")
                .OrderBy(g => g.Key)
                .ToList();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.GroupedTables = groupedTables;

            // Get status counts
            // Get status counts (Global stats, independent of filter)
            ViewBag.TrongCount = db.BanAns.Count(b => b.TrangThai == "Trống");
            ViewBag.DaDatCount = db.BanAns.Count(b => b.TrangThai == "Đã đặt");
            ViewBag.DangPhucVuCount = db.BanAns.Count(b => b.TrangThai == "Đang phục vụ");
            ViewBag.TotalCount = db.BanAns.Count();

            return View(tables);
        }

        [HttpGet]
        public ActionResult GetTable(long id)
        {
            var table = db.BanAns.Find(id);
            if (table == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bàn." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    table.BanID,
                    table.TenBan,
                    table.ViTri,
                    table.SucChua,
                    table.TrangThai
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(long id, string trangThai)
        {
            if (string.IsNullOrEmpty(trangThai))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            var allowedStatuses = new[] { "Trống", "Đã đặt", "Đang phục vụ" };
            if (!allowedStatuses.Contains(trangThai))
            {
                return Json(new { success = false, message = "Trạng thái không được hỗ trợ." });
            }

            var table = db.BanAns.Find(id);
            if (table == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bàn." });
            }

            table.TrangThai = trangThai;
            db.SaveChanges();

            return Json(new { success = true, message = "Cập nhật trạng thái bàn thành công.", status = table.TrangThai });
        }

        // GET: Employee_65133141/BanAn/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Employee_65133141/BanAn/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BanAn banAn)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default values
                    banAn.TrangThai = "Trống";
                    if (string.IsNullOrEmpty(banAn.ViTri))
                    {
                        banAn.ViTri = "Tầng trệt";
                    }
                    
                    db.BanAns.Add(banAn);
                    db.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Thêm bàn thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi thêm bàn: " + ex.Message);
                }
            }

            return View(banAn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            var table = db.BanAns.Find(id);
            if (table == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bàn." });
            }

            try
            {
                // Kiểm tra xem bàn có đang được sử dụng không
                var hasActiveReservations = db.DatBans.Any(d => d.BanID == id && 
                    (d.TrangThai == "Đang phục vụ" || d.TrangThai == "Đang sử dụng" || 
                     d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đã đặt"));
                
                if (hasActiveReservations)
                {
                    return Json(new { success = false, message = "Không thể xóa bàn đang được sử dụng hoặc đã được đặt." });
                }

                // Kiểm tra xem bàn có đơn hàng đang xử lý không
                var hasActiveOrders = db.DonHangs.Any(d => d.BanID == id && 
                    (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ"));
                
                if (hasActiveOrders)
                {
                    return Json(new { success = false, message = "Không thể xóa bàn có đơn hàng đang xử lý." });
                }

                db.BanAns.Remove(table);
                db.SaveChanges();

                return Json(new { success = true, message = "Xóa bàn thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa bàn: " + ex.Message });
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

