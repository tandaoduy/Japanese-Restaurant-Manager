using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee", "admin")]
    public class PaymentController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Payment/Index?banId=xxx
        public ActionResult Index(long banId)
        {
            var table = db.BanAns.Find(banId);
            if (table == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bàn";
                return RedirectToAction("Index", "Order");
            }

            // Get confirmed items from session
            var confirmedCart = Session[$"Cart_Confirmed_{banId}"] as List<CartItem>;
            if (confirmedCart == null || !confirmedCart.Any())
            {
                TempData["ErrorMessage"] = "Không có món nào đã xác nhận để thanh toán";
                return RedirectToAction("SelectTable", "Order", new { banId = banId });
            }

            // Calculate total
            decimal total = confirmedCart.Sum(item => item.Gia * item.SoLuong);

            // Get employee info
            var nhanVienId = Session["UserId"] as long?;
            var nhanVien = nhanVienId.HasValue ? db.NhanViens.Find(nhanVienId.Value) : null;

            // Get all users for customer selection
            var users = db.Users.Where(u => u.TrangThai == true).OrderBy(u => u.HoTen).ToList();

            ViewBag.Table = table;
            ViewBag.ConfirmedCart = confirmedCart;
            ViewBag.Total = total;
            ViewBag.NhanVien = nhanVien;
            ViewBag.Users = users;

            return View();
        }

        // GET: Employee_65133141/Payment/Preview - Preview bill before saving
        public ActionResult Preview(long banId, string paymentMethod, decimal? cashAmount, string customerType, long? userId, string customerName, string customerPhone)
        {
            var table = db.BanAns.Find(banId);
            if (table == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bàn";
                return RedirectToAction("Index", "Order");
            }

            // Get confirmed items from session
            var confirmedCart = Session[$"Cart_Confirmed_{banId}"] as List<CartItem>;
            if (confirmedCart == null || !confirmedCart.Any())
            {
                TempData["ErrorMessage"] = "Không có món nào để thanh toán";
                return RedirectToAction("SelectTable", "Order", new { banId = banId });
            }

            // Get employee info
            var nhanVienId = Session["UserId"] as long?;
            var nhanVien = nhanVienId.HasValue ? db.NhanViens.Find(nhanVienId.Value) : null;

            // Calculate total
            decimal total = confirmedCart.Sum(item => item.Gia * item.SoLuong);

            // Determine customer
            long? customerUserId = null;
            string finalCustomerName = null;
            string finalCustomerPhone = null;

            if (customerType == "system" && userId.HasValue)
            {
                customerUserId = userId.Value;
                var user = db.Users.Find(userId.Value);
                finalCustomerName = user?.HoTen;
                finalCustomerPhone = user?.SDT;
            }
            else if (customerType == "external")
            {
                finalCustomerName = string.IsNullOrWhiteSpace(customerName) ? "Khách vãng lai" : customerName.Trim();
                finalCustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim();
            }

            // Calculate change for cash payment
            decimal change = 0;
            if (paymentMethod == "cash" && cashAmount.HasValue)
            {
                change = cashAmount.Value - total;
            }

            // Get existing order to get NgayDat (giờ vào)
            var existingOrder = db.DonHangs
                .Where(d => d.BanID == banId && 
                           (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ"))
                .OrderByDescending(d => d.NgayDat)
                .FirstOrDefault();

            // Get next invoice number for preview (temporary)
            var nextHoaDonId = (db.HoaDons.Any() ? db.HoaDons.Max(h => h.HoaDonID) : 0) + 1;

            ViewBag.Table = table;
            ViewBag.ConfirmedCart = confirmedCart;
            ViewBag.Total = total;
            ViewBag.NhanVien = nhanVien;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.CashAmount = cashAmount;
            ViewBag.Change = change;
            ViewBag.CustomerName = finalCustomerName;
            ViewBag.CustomerPhone = finalCustomerPhone;
            ViewBag.CustomerUserId = customerUserId;
            ViewBag.CustomerType = customerType;
            ViewBag.CustomerNameInput = customerName;
            ViewBag.CustomerPhoneInput = customerPhone;
            ViewBag.NgayDat = existingOrder?.NgayDat; // Giờ vào
            ViewBag.PreviewHoaDonId = nextHoaDonId; // Mã hóa đơn tạm thời cho preview

            return View();
        }

        // POST: Employee_65133141/Payment/ConfirmPayment - Save to DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(long banId, string paymentMethod, decimal? cashAmount, string customerType, long? userId, string customerName, string customerPhone)
        {
            try
            {
                var table = db.BanAns.Find(banId);
                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn" });
                }

                // Get confirmed items from session
                var confirmedCart = Session[$"Cart_Confirmed_{banId}"] as List<CartItem>;
                if (confirmedCart == null || !confirmedCart.Any())
                {
                    return Json(new { success = false, message = "Không có món nào để thanh toán" });
                }

                // Get employee info
                var nhanVienId = Session["UserId"] as long?;
                if (!nhanVienId.HasValue)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });
                }

                // Calculate total
                decimal total = confirmedCart.Sum(item => item.Gia * item.SoLuong);

                // Determine customer (for all payment methods)
                long? customerUserId = null;
                string finalCustomerName = null;
                string finalCustomerPhone = null;

                    if (customerType == "system" && userId.HasValue)
                    {
                        customerUserId = userId.Value;
                        var user = db.Users.Find(userId.Value);
                        finalCustomerName = user?.HoTen;
                    finalCustomerPhone = user?.SDT;
                    }
                    else if (customerType == "external")
                    {
                    finalCustomerName = string.IsNullOrWhiteSpace(customerName) ? "Khách vãng lai" : customerName.Trim();
                    finalCustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim();
                }

                // Create or update order
                var existingOrder = db.DonHangs
                    .Where(d => d.BanID == banId && 
                               (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ"))
                    .OrderByDescending(d => d.NgayDat)
                    .FirstOrDefault();

                DonHang order;
                if (existingOrder != null)
                {
                    order = existingOrder;
                    // Update existing order
                    order.TongTien = total;
                    order.UserID = customerUserId;
                    order.NgayDat = DateTime.Now;
                    order.SoDienThoai = finalCustomerPhone;
                    if (!string.IsNullOrWhiteSpace(finalCustomerName))
                    {
                        order.GhiChu = finalCustomerName;
                    }
                    
                    // Remove old order details and add new ones
                    var oldDetails = db.ChiTietDonHangs.Where(c => c.DonHangID == order.DonHangID).ToList();
                    db.ChiTietDonHangs.RemoveRange(oldDetails);
                    
                    // Add new order details
                    foreach (var item in confirmedCart)
                    {
                        var orderDetail = new ChiTietDonHang
                        {
                            DonHangID = order.DonHangID,
                            MonAnID = item.MonAnID,
                            SoLuong = item.SoLuong,
                            DonGia = item.Gia,
                            ThanhTien = item.Gia * item.SoLuong
                        };
                        db.ChiTietDonHangs.Add(orderDetail);
                    }
                }
                else
                {
                    // Create new order
                    order = new DonHang
                    {
                        BanID = banId,
                        NhanVienID = nhanVienId.Value,
                        UserID = customerUserId,
                        NgayDat = DateTime.Now,
                        TongTien = total,
                        TrangThai = "Đã thanh toán",
                        SoDienThoai = finalCustomerPhone,
                        GhiChu = finalCustomerName
                    };
                    db.DonHangs.Add(order);
                    db.SaveChanges(); // Save to get DonHangID

                    // Add order details
                    foreach (var item in confirmedCart)
                    {
                        var orderDetail = new ChiTietDonHang
                        {
                            DonHangID = order.DonHangID,
                            MonAnID = item.MonAnID,
                            SoLuong = item.SoLuong,
                            DonGia = item.Gia,
                            ThanhTien = item.Gia * item.SoLuong
                        };
                        db.ChiTietDonHangs.Add(orderDetail);
                    }
                }

                // Create invoice
                decimal change = 0;
                if (paymentMethod == "cash" && cashAmount.HasValue)
                {
                    change = cashAmount.Value - total;
                }

                var hoaDon = new HoaDon
                {
                    DonHangID = order.DonHangID,
                    NhanVienThuNganID = nhanVienId.Value,
                    NgayLap = DateTime.Now,
                    TongTienHang = total,
                    GiamGia = 0,
                    ThueVAT = 0,
                    PhiPhucVu = 0,
                    TongThanhToan = total,
                    PhuongThucTT = paymentMethod == "cash" ? "Tiền mặt" : "Chuyển khoản"
                };
                db.HoaDons.Add(hoaDon);

                // Update order status
                order.TrangThai = "Đã thanh toán";
                db.SaveChanges();

                // Clear cart session - đảm bảo xóa hoàn toàn
                Session.Remove($"Cart_Confirmed_{banId}");
                Session.Remove($"Cart_Pending_{banId}");
                
                // Khởi tạo lại session với danh sách rỗng để đảm bảo không load từ DB
                Session[$"Cart_Confirmed_{banId}"] = new List<CartItem>();
                Session[$"Cart_Pending_{banId}"] = new List<CartItem>();

                // Update table status
                table.TrangThai = "Trống";
                db.SaveChanges();
                
                // Đảm bảo tất cả đơn hàng của bàn này đã được thanh toán
                var allOrdersForTable = db.DonHangs
                    .Where(d => d.BanID == banId && 
                               (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ"))
                    .ToList();
                foreach (var ord in allOrdersForTable)
                {
                    ord.TrangThai = "Đã thanh toán";
                }
                db.SaveChanges();

                // Set success message for redirect
                TempData["SuccessMessage"] = "Thanh toán thành công!";

                return Json(new { 
                    success = true, 
                    hoaDonId = hoaDon.HoaDonID,
                    donHangId = order.DonHangID,
                    change = change,
                    customerName = finalCustomerName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Employee_65133141/Payment/Invoice?hoaDonId=xxx&cashAmount=...&change=...
        public ActionResult Invoice(long hoaDonId, decimal? cashAmount, decimal? change)
        {
            var hoaDon = db.HoaDons
                .Include(h => h.DonHang)
                .Include(h => h.DonHang.BanAn)
                .Include(h => h.DonHang.NhanVien)
                .Include(h => h.DonHang.User)
                .Include(h => h.NhanVien)
                .Include(h => h.DonHang.ChiTietDonHangs)
                .Include(h => h.DonHang.ChiTietDonHangs.Select(c => c.MonAn))
                .FirstOrDefault(h => h.HoaDonID == hoaDonId);

            if (hoaDon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn";
                return RedirectToAction("Index", "Order");
            }

            ViewBag.CashAmount = cashAmount;
            ViewBag.Change = change;

            ViewBag.HoaDon = hoaDon;
            return View();
        }
    }
}

