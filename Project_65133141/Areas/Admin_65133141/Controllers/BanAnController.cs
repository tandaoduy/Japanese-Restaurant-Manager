using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class BanAnController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/BanAn
        public ActionResult Index(string statusFilter = null)
        {
            // Calculate GLOBAL statistics (from ALL tables, not filtered)
            ViewBag.TrongCount = db.BanAns.Count(b => b.TrangThai == "Trống");
            ViewBag.DaDatCount = db.BanAns.Count(b => b.TrangThai == "Đã đặt");
            ViewBag.DangPhucVuCount = db.BanAns.Count(b => b.TrangThai == "Đang phục vụ");
            ViewBag.TotalCount = db.BanAns.Count();

            var query = db.BanAns.AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(b => b.TrangThai == statusFilter);
            }

            var tables = query
                .OrderBy(b => b.ViTri)
                .ThenBy(b => b.TenBan)
                .ToList();

            var groupedTables = tables
                .GroupBy(b => b.ViTri ?? "Khác")
                .OrderBy(g => g.Key)
                .ToList();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.GroupedTables = groupedTables;

            return View(tables);
        }

        /// <summary>
        /// Lấy trạng thái bàn từ bảng DatBan
        /// </summary>
        private string GetTableStatusFromDatBan(long banID)
        {
            var now = DateTime.Now;
            
            // Kiểm tra các đặt bàn đang hoạt động
            var activeReservation = db.DatBans
                .Where(d => d.BanID == banID && 
                    (d.TrangThai == "Đang phục vụ" || 
                     d.TrangThai == "Đang sử dụng"))
                .OrderByDescending(d => d.ThoiGianDen)
                .FirstOrDefault();

            if (activeReservation != null)
            {
                return "Đang phục vụ";
            }

            // Kiểm tra các đặt bàn trong vòng 2 giờ tới
            // Tính toán giá trị trước để tránh lỗi LINQ to Entities
            var twoHoursLater = now.AddHours(2);
            var upcomingReservation = db.DatBans
                .Where(d => d.BanID == banID && 
                    d.ThoiGianDen <= twoHoursLater &&
                    d.ThoiGianDen >= now &&
                    (d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đã đặt"))
                .OrderByDescending(d => d.ThoiGianDen)
                .FirstOrDefault();

            if (upcomingReservation != null)
            {
                return "Đang phục vụ";
            }

            // Kiểm tra các đặt bàn đã xác nhận
            var reservedReservation = db.DatBans
                .Where(d => d.BanID == banID && 
                    (d.TrangThai == "Đã đặt" || d.TrangThai == "Đã xác nhận"))
                .OrderByDescending(d => d.ThoiGianDen)
                .FirstOrDefault();

            if (reservedReservation != null)
            {
                return "Đã đặt";
            }

            return "Trống";
        }

        // GET: Admin_65133141/BanAn/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin_65133141/BanAn/Create
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
                // Chỉ chặn xóa nếu bàn ĐANG CÓ KHÁCH (Đang phục vụ/Đang sử dụng)
                // Các trạng thái "Đã đặt", "Đã xác nhận" (tương lai) vẫn cho xóa -> sẽ bị xóa theo cascade bên dưới
                var hasActiveReservations = db.DatBans.Any(d => d.BanID == id && 
                    (d.TrangThai == "Đang phục vụ" || d.TrangThai == "Đang sử dụng"));
                
                if (hasActiveReservations)
                {
                    return Json(new { success = false, message = "Không thể xóa bàn đang có khách ngồi (Đang phục vụ)." });
                }

                // Kiểm tra xem bàn có đơn hàng đang xử lý không
                var hasActiveOrders = db.DonHangs.Any(d => d.BanID == id && 
                    (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ"));
                
                if (hasActiveOrders)
                {
                    return Json(new { success = false, message = "Không thể xóa bàn có đơn hàng đang xử lý." });
                }

                // Xóa tất cả lịch sử đặt bàn liên quan (cancelled/completed)
                var allReservations = db.DatBans.Where(d => d.BanID == id).ToList();
                if (allReservations.Any())
                {
                    db.DatBans.RemoveRange(allReservations);
                }

                // Xóa tất cả đơn hàng cũ và chi tiết đơn hàng liên quan
                var allOrders = db.DonHangs.Where(d => d.BanID == id).ToList();
                foreach (var order in allOrders)
                {
                    // Xóa chi tiết đơn hàng trước
                    var orderDetails = db.ChiTietDonHangs.Where(ct => ct.DonHangID == order.DonHangID).ToList();
                    if (orderDetails.Any())
                    {
                        db.ChiTietDonHangs.RemoveRange(orderDetails);
                    }
                }
                // Xóa đơn hàng
                if (allOrders.Any())
                {
                    db.DonHangs.RemoveRange(allOrders);
                }

                // Cuối cùng xóa bàn
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

