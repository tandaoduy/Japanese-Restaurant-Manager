using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee")]
    public class BanAnController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/BanAn
        public ActionResult Index(string statusFilter = null)
        {
            // Update orders for Empty tables (Auto-close orders if table is Empty)
            // Note: We do this BEFORE calculating stats to ensure consistency, 
            // OR AFTER if we want stats to reflect DB state *before* this auto-fix.
            // User request: "khi bàn hiện đang trống thì các đơn hàng sẽ reset về đã xác nhận hoặc hoàn thành"
            // Let's do it first to keep everything clean.
            
            var emptyTables = db.BanAns.Where(b => b.TrangThai == "Trống").Select(b => b.BanID).ToList();
            if (emptyTables.Any())
            {
                // Tìm các đơn hàng chưa hoàn thành của các bàn đang Trống
                var hangOrders = db.DonHangs.Where(d => emptyTables.Contains(d.BanID.Value) && 
                    (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ" || d.TrangThai == "Chờ thanh toán" || d.TrangThai == "Chờ xác nhận")).ToList();
                
                bool anyChanged = false;
                foreach (var order in hangOrders)
                {
                    // User yêu cầu: "reset về đã xác nhận hoặc hoàn thành" và "xoá hết"
                    // Set về "Hoàn thành" để coi như đã đóng đơn/đã trả tiền, hoặc "Đã hủy" nếu muốn xóa bỏ.
                    // Tạm thời set "Hoàn thành" để lưu doanh thu, nhưng đảm bảo nó biến mất khỏi Active Orders.
                    order.TrangThai = "Hoàn thành"; 
                    anyChanged = true;
                }
                
                if (anyChanged)
                {
                    db.SaveChanges();
                }
            }

            // Calculate GLOBAL statistics (from ALL tables)
            ViewBag.TrongCount = db.BanAns.Count(b => b.TrangThai == "Trống");
            ViewBag.DaDatCount = db.BanAns.Count(b => b.TrangThai == "Đã đặt");
            ViewBag.DangPhucVuCount = db.BanAns.Count(b => b.TrangThai == "Đang phục vụ");
            ViewBag.TotalCount = db.BanAns.Count();

            var query = db.BanAns.AsQueryable();

            // Apply filter if specified
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

            // Khi chuyển bàn về trạng thái "Trống" thì xoá toàn bộ giỏ hàng (món đang gọi và đã gọi) của bàn đó
            if (trangThai == "Trống")
            {
                var pendingKey = $"Cart_Pending_{id}";
                var confirmedKey = $"Cart_Confirmed_{id}";

                // Xoá session hiện tại
                Session.Remove(pendingKey);
                Session.Remove(confirmedKey);

                // Khởi tạo lại giỏ hàng rỗng để UI luôn hiển thị số lượng/tổng tiền = 0
                Session[pendingKey] = new List<CartItem>();
                Session[confirmedKey] = new List<CartItem>();
            }

            // Đồng bộ trạng thái đơn hàng và đặt bàn theo trạng thái bàn
            // Khi bàn chuyển sang "Đang phục vụ" thì:
            //  - Các đơn đang "Đang chuẩn bị" của bàn đó cũng được cập nhật sang "Đang phục vụ".
            //  - Các đặt bàn trong khoảng thời gian ăn (ví dụ 2 giờ) sẽ chuyển sang "Đang phục vụ".
            if (trangThai == "Đang phục vụ")
            {
                // Cập nhật đơn hàng
                var relatedOrders = db.DonHangs
                    .Where(d => d.BanID == id && d.TrangThai == "Đang chuẩn bị")
                    .ToList();

                foreach (var order in relatedOrders)
                {
                    order.TrangThai = "Đang phục vụ";
                }

                // Cập nhật đặt bàn
                var now = DateTime.Now;
                var relatedReservations = db.DatBans
                    .Where(d => d.BanID == id &&
                                d.TrangThai != "Hoàn thành" &&
                                d.TrangThai != "Đã hủy")
                    .ToList();

                foreach (var reservation in relatedReservations)
                {
                    var start = reservation.ThoiGianDen;
                    var end = reservation.ThoiGianDen.AddHours(2); // thời gian ăn ước tính 2 giờ

                    if (now >= start && now <= end &&
                        (reservation.TrangThai == "Đã xác nhận" ||
                         reservation.TrangThai == "Đã đặt" ||
                         reservation.TrangThai == "Đang phục vụ"))
                    {
                        reservation.TrangThai = "Đang phục vụ";
                    }
                }
            }

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

                // Kiểm tra lịch sử liên quan (đơn hàng / đặt bàn / hóa đơn) để tránh lỗi khóa ngoại
                // Nếu bàn đã từng được dùng cho đơn hàng hoặc đặt bàn (kể cả đã hoàn thành),
                // không cho xóa để bảo toàn dữ liệu báo cáo, chỉ cho chuyển trạng thái về "Trống".
                var hasAnyReservations = db.DatBans.Any(d => d.BanID == id);
                var hasAnyOrders = db.DonHangs.Any(d => d.BanID == id);

                // Hóa đơn gắn với đơn hàng của bàn này
                bool hasAnyInvoices = false;
                if (hasAnyOrders)
                {
                    var relatedOrderIds = db.DonHangs
                        .Where(d => d.BanID == id)
                        .Select(d => d.DonHangID)
                        .ToList();

                    if (relatedOrderIds.Any())
                    {
                        hasAnyInvoices = db.HoaDons.Any(h => relatedOrderIds.Contains(h.DonHangID));
                    }
                }

                if (hasAnyReservations || hasAnyOrders || hasAnyInvoices)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bàn này đã có lịch sử đơn hàng/đặt bàn/hóa đơn nên không được xóa. Hãy chuyển trạng thái về 'Trống' thay vì xóa."
                    });
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

