using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;
using Newtonsoft.Json;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee", "admin")]
    public class OrderController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Order - Hiển thị danh sách bàn đang phục vụ
        public ActionResult Index()
        {
            // Get tables that are in use (from BanAn with status "Đang sử dụng" hoặc "Đang phục vụ")
            var activeTables = db.BanAns
                .Where(b => b.TrangThai == "Đang sử dụng" || 
                           b.TrangThai == "Đang phục vụ" ||
                           db.DatBans.Any(d => d.BanID == b.BanID && 
                                             (d.TrangThai == "Đang phục vụ" || d.TrangThai == "Đang sử dụng")))
                .OrderBy(b => b.TenBan)
                .ToList();
            
            ViewBag.ActiveTables = activeTables;
            return View();
        }

        // GET: Employee_65133141/Order/SelectTable - Trang chọn món cho bàn
        public ActionResult SelectTable(long banId)
        {
            // Validate table exists and is active
            var table = db.BanAns.Find(banId);
            if (table == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bàn";
                return RedirectToAction("Index");
            }

            if (table.TrangThai != "Đang sử dụng" && table.TrangThai != "Đang phục vụ")
            {
                TempData["ErrorMessage"] = "Bàn này không đang phục vụ";
                return RedirectToAction("Index");
            }

            // Get all categories
            var categories = db.DanhMucs
                .Where(d => d.IsHienThi == true || d.IsHienThi == null)
                .OrderBy(d => d.TenDanhMuc)
                .ToList();

            // Get all active menu items
            var menuItems = db.MonAns
                .Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ")
                .OrderBy(m => m.DanhMucID)
                .ThenBy(m => m.TenMon)
                .ToList();

            // Group by category
            var groupedMenu = menuItems
                .GroupBy(m => m.DanhMucID)
                .Select(g => new System.Tuple<DanhMuc, List<MonAn>>(
                    categories.FirstOrDefault(c => c.DanhMucID == g.Key),
                    g.ToList()
                ))
                .Where(g => g.Item1 != null)
                .OrderBy(g => g.Item1.TenDanhMuc)
                .ToList();

            ViewBag.Table = table;
            ViewBag.Categories = categories;
            ViewBag.GroupedMenu = groupedMenu;

            // Initialize cart in session if not exists
            // Pending cart: món đang gọi (có thể thêm/sửa/xóa)
            if (Session[$"Cart_Pending_{banId}"] == null)
            {
                Session[$"Cart_Pending_{banId}"] = new List<CartItem>();
            }
            // Confirmed cart: món đã xác nhận (chỉ xem, không thể sửa)
            if (Session[$"Cart_Confirmed_{banId}"] == null)
            {
                Session[$"Cart_Confirmed_{banId}"] = new List<CartItem>();
            }
            
            // Load confirmed items from database if session is empty but there are orders
            // CHỈ load nếu bàn đang ở trạng thái "Đang sử dụng" hoặc "Đang phục vụ"
            // KHÔNG load nếu bàn đã "Trống" (đã thanh toán)
            var confirmedCart = Session[$"Cart_Confirmed_{banId}"] as List<CartItem>;
            if ((confirmedCart == null || !confirmedCart.Any()) && 
                (table.TrangThai == "Đang sử dụng" || table.TrangThai == "Đang phục vụ"))
            {
                // Load from recent orders for this table
                var recentOrders = db.DonHangs
                    .Where(d => d.BanID == banId && 
                               (d.TrangThai == "Đang chuẩn bị" || d.TrangThai == "Đang phục vụ"))
                    .OrderByDescending(d => d.NgayDat)
                    .Take(5) // Load last 5 orders
                    .ToList();
                
                confirmedCart = new List<CartItem>();
                foreach (var order in recentOrders)
                {
                    var orderDetails = db.ChiTietDonHangs
                        .Where(c => c.DonHangID == order.DonHangID)
                        .Include(c => c.MonAn)
                        .ToList();
                    
                    foreach (var detail in orderDetails)
                    {
                        // Check if item already exists in confirmed cart (merge by MonAnID)
                        var existingItem = confirmedCart.FirstOrDefault(c => c.MonAnID == detail.MonAnID);
                        if (existingItem != null)
                        {
                            // Merge: increase quantity and use the latest price
                            existingItem.SoLuong += detail.SoLuong;
                            existingItem.Gia = detail.DonGia; // Use latest price
                        }
                        else
                        {
                            // Add new item
                            confirmedCart.Add(new CartItem
                            {
                                MonAnID = detail.MonAnID,
                                TenMon = detail.MonAn?.TenMon ?? "Món không xác định",
                                Gia = detail.DonGia,
                                SoLuong = detail.SoLuong,
                                HinhAnh = detail.MonAn?.HinhAnh,
                                DonViTinh = detail.MonAn?.DonViTinh
                            });
                        }
                    }
                }
                
                Session[$"Cart_Confirmed_{banId}"] = confirmedCart;
            }
            else if (table.TrangThai == "Trống")
            {
                // Nếu bàn đã trống, đảm bảo cart là rỗng
                Session[$"Cart_Confirmed_{banId}"] = new List<CartItem>();
                Session[$"Cart_Pending_{banId}"] = new List<CartItem>();
            }

            return View();
        }

        // GET: Employee_65133141/Order/Menu
        public ActionResult Menu()
        {
            // Get all active products for menu display
            var allProducts = db.MonAns
                .Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ")
                .OrderByDescending(m => m.NgayTao)
                .ToList();
            ViewBag.FeaturedProducts = allProducts;

            // Get categories for category mapping
            var categories = db.DanhMucs.ToDictionary(d => d.DanhMucID, d => d.TenDanhMuc);
            ViewBag.Categories = categories;

            // Get all categories for filter buttons
            var allCategories = db.DanhMucs.ToList();
            ViewBag.AllCategories = allCategories;

            return View();
        }

        // GET: Employee_65133141/Order/GetMenuItems (AJAX)
        [HttpGet]
        public JsonResult GetMenuItems(string searchTerm = "", long? categoryId = null)
        {
            try
            {
                var query = db.MonAns.AsQueryable();

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(m => m.DanhMucID == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(m => m.TenMon.Contains(searchTerm));
                }

                query = query.Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ");

                var items = query
                    .ToList()
                    .Select(m => {
                        var category = db.DanhMucs.FirstOrDefault(d => d.DanhMucID == m.DanhMucID);
                        return new
                        {
                            MonAnID = m.MonAnID,
                            TenMon = m.TenMon,
                            DanhMucID = m.DanhMucID,
                            TenDanhMuc = category != null ? category.TenDanhMuc : "Khác",
                            Gia = m.Gia,
                            GiaGoc = m.GiaGoc,
                            GiaGiam = m.GiaGiam,
                            MoTa = m.MoTa,
                            HinhAnh = m.HinhAnh,
                            TrangThai = m.TrangThai,
                            DonViTinh = m.DonViTinh,
                            IsAvailable = m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ"
                        };
                    })
                    .OrderBy(m => m.DanhMucID)
                    .ThenBy(m => m.TenMon)
                    .ToList();

                return Json(new { success = true, data = items }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Employee_65133141/Order/GetActiveTables (AJAX)
        [HttpGet]
        public JsonResult GetActiveTables()
        {
            try
            {
                var tables = db.BanAns
                    .Where(b => b.TrangThai == "Đang sử dụng" || 
                               b.TrangThai == "Đang phục vụ" ||
                               db.DatBans.Any(d => d.BanID == b.BanID && 
                                                 (d.TrangThai == "Đang phục vụ" || d.TrangThai == "Đang sử dụng")))
                    .Select(b => new
                    {
                        BanID = b.BanID,
                        TenBan = b.TenBan,
                        SucChua = b.SucChua,
                        TrangThai = b.TrangThai ?? "Trống",
                        ViTri = b.ViTri
                    })
                    .OrderBy(b => b.TenBan)
                    .ToList();

                return Json(new { success = true, data = tables }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Employee_65133141/Order/AddToCart - Thêm món vào giỏ hàng (món đang gọi)
        [HttpPost]
        public JsonResult AddToCart(long banId, long monAnId, int soLuong = 1)
        {
            try
            {
                var menuItem = db.MonAns.Find(monAnId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy món ăn" });
                }

                if (menuItem.TrangThai != "Hoạt động" && menuItem.TrangThai != "Đang phục vụ")
                {
                    return Json(new { success = false, message = "Món này đã hết" });
                }

                var cartKey = $"Cart_Pending_{banId}";
                var cart = Session[cartKey] as List<CartItem>;
                
                // Ensure we have a valid cart list
                if (cart == null)
                {
                    cart = new List<CartItem>();
                }

                // Check if item already exists in cart (by MonAnID)
                var existingItem = cart.FirstOrDefault(c => c.MonAnID == monAnId);
                if (existingItem != null)
                {
                    // Item exists, increase quantity
                    existingItem.SoLuong += soLuong;
                }
                else
                {
                    // Item doesn't exist, add new item
                    cart.Add(new CartItem
                    {
                        MonAnID = monAnId,
                        TenMon = menuItem.TenMon,
                        Gia = menuItem.Gia,
                        SoLuong = soLuong,
                        HinhAnh = menuItem.HinhAnh,
                        DonViTinh = menuItem.DonViTinh
                    });
                }

                // Always save back to session to ensure persistence
                Session[cartKey] = cart;

                return Json(new { 
                    success = true, 
                    message = "Đã thêm vào giỏ hàng",
                    cartCount = cart.Sum(c => c.SoLuong),
                    totalAmount = cart.Sum(c => c.Gia * c.SoLuong)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Employee_65133141/Order/UpdateCartItem - Cập nhật số lượng món trong giỏ
        [HttpPost]
        public JsonResult UpdateCartItem(long banId, long monAnId, int soLuong)
        {
            try
            {
                var cartKey = $"Cart_Pending_{banId}";
                var cart = Session[cartKey] as List<CartItem> ?? new List<CartItem>();

                var item = cart.FirstOrDefault(c => c.MonAnID == monAnId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy món trong giỏ hàng" });
                }

                if (soLuong <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.SoLuong = soLuong;
                }

                Session[cartKey] = cart;

                return Json(new { 
                    success = true, 
                    cartCount = cart.Sum(c => c.SoLuong),
                    totalAmount = cart.Sum(c => c.Gia * c.SoLuong)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Employee_65133141/Order/RemoveFromCart - Xóa món khỏi giỏ hàng
        [HttpPost]
        public JsonResult RemoveFromCart(long banId, long monAnId, string cartType = "pending")
        {
            try
            {
                string cartKey;
                if (cartType == "confirmed")
                {
                    cartKey = $"Cart_Confirmed_{banId}";
                }
                else
                {
                    cartKey = $"Cart_Pending_{banId}";
                }
                
                var cart = Session[cartKey] as List<CartItem> ?? new List<CartItem>();

                var item = cart.FirstOrDefault(c => c.MonAnID == monAnId);
                if (item != null)
                {
                    cart.Remove(item);
                }

                Session[cartKey] = cart;

                // Get updated totals
                var pendingCart = Session[$"Cart_Pending_{banId}"] as List<CartItem> ?? new List<CartItem>();
                var confirmedCart = Session[$"Cart_Confirmed_{banId}"] as List<CartItem> ?? new List<CartItem>();

                return Json(new { 
                    success = true,
                    message = "Đã xóa món khỏi giỏ hàng",
                    pendingCount = pendingCart.Sum(c => c.SoLuong),
                    confirmedCount = confirmedCart.Sum(c => c.SoLuong),
                    pendingTotal = pendingCart.Sum(c => c.Gia * c.SoLuong),
                    confirmedTotal = confirmedCart.Sum(c => c.Gia * c.SoLuong)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Employee_65133141/Order/GetCart - Lấy thông tin giỏ hàng
        [HttpGet]
        public JsonResult GetCart(long banId)
        {
            try
            {
                var pendingCart = Session[$"Cart_Pending_{banId}"] as List<CartItem> ?? new List<CartItem>();
                var confirmedCart = Session[$"Cart_Confirmed_{banId}"] as List<CartItem> ?? new List<CartItem>();

                return Json(new
                {
                    success = true,
                    pending = pendingCart.Select(c => new
                    {
                        c.MonAnID,
                        c.TenMon,
                        c.Gia,
                        c.SoLuong,
                        c.HinhAnh,
                        c.DonViTinh,
                        Total = c.Gia * c.SoLuong
                    }),
                    confirmed = confirmedCart.Select(c => new
                    {
                        c.MonAnID,
                        c.TenMon,
                        c.Gia,
                        c.SoLuong,
                        c.HinhAnh,
                        c.DonViTinh,
                        Total = c.Gia * c.SoLuong
                    }),
                    pendingCount = pendingCart.Sum(c => c.SoLuong),
                    confirmedCount = confirmedCart.Sum(c => c.SoLuong),
                    pendingTotal = pendingCart.Sum(c => c.Gia * c.SoLuong),
                    confirmedTotal = confirmedCart.Sum(c => c.Gia * c.SoLuong)
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Employee_65133141/Order/ConfirmOrder - Xác nhận đơn (chuyển từ món đang gọi sang món đã gọi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ConfirmOrder(long banId)
        {
            try
            {
                var pendingCartKey = $"Cart_Pending_{banId}";
                var confirmedCartKey = $"Cart_Confirmed_{banId}";

                var pendingCart = Session[pendingCartKey] as List<CartItem> ?? new List<CartItem>();
                var confirmedCart = Session[confirmedCartKey] as List<CartItem> ?? new List<CartItem>();

                if (!pendingCart.Any())
                {
                    return Json(new { success = false, message = "Không có món nào để xác nhận" });
                }

                // Get employee ID from session
                var employeeId = Session["UserId"] as long?;
                if (!employeeId.HasValue)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });
                }

                // Validate table exists
                var table = db.BanAns.Find(banId);
                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn" });
                }

                // Calculate total
                decimal totalAmount = pendingCart.Sum(c => c.Gia * c.SoLuong);

                // Create order
                var order = new DonHang
                {
                    BanID = banId,
                    NhanVienID = employeeId.Value,
                    NgayDat = DateTime.Now,
                    TongTien = totalAmount,
                    TrangThai = "Đang chuẩn bị",
                    GhiChu = ""
                };

                db.DonHangs.Add(order);
                db.SaveChanges();

                // Create order details
                foreach (var item in pendingCart)
                {
                    var orderDetail = new ChiTietDonHang
                    {
                        DonHangID = order.DonHangID,
                        MonAnID = item.MonAnID,
                        SoLuong = item.SoLuong,
                        DonGia = item.Gia,
                        GhiChuMon = ""
                    };

                    db.ChiTietDonHangs.Add(orderDetail);
                }

                db.SaveChanges();

                // Move items from pending to confirmed (merge by MonAnID)
                foreach (var item in pendingCart)
                {
                    // Check if item already exists in confirmed cart
                    var existingItem = confirmedCart.FirstOrDefault(c => c.MonAnID == item.MonAnID);
                    if (existingItem != null)
                    {
                        // Merge: increase quantity and use the latest price
                        existingItem.SoLuong += item.SoLuong;
                        existingItem.Gia = item.Gia; // Use latest price
                    }
                    else
                    {
                        // Add new item
                        confirmedCart.Add(new CartItem
                        {
                            MonAnID = item.MonAnID,
                            TenMon = item.TenMon,
                            Gia = item.Gia,
                            SoLuong = item.SoLuong,
                            HinhAnh = item.HinhAnh,
                            DonViTinh = item.DonViTinh
                        });
                    }
                }
                
                // Clear pending cart
                pendingCart.Clear();

                Session[pendingCartKey] = pendingCart;
                Session[confirmedCartKey] = confirmedCart;

                return Json(new
                {
                    success = true,
                    message = "Xác nhận đơn hàng thành công",
                    orderId = order.DonHangID,
                    pendingCount = 0,
                    confirmedCount = confirmedCart.Sum(c => c.SoLuong),
                    confirmedTotal = confirmedCart.Sum(c => c.Gia * c.SoLuong)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Employee_65133141/Order/CreateOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateOrder()
        {
            try
            {
                // Read JSON from form data
                string jsonData = Request.Form["data"];
                if (string.IsNullOrEmpty(jsonData))
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var model = JsonConvert.DeserializeObject<CreateOrderModel>(jsonData);

                if (model == null || model.BanID <= 0 || model.OrderItems == null || !model.OrderItems.Any())
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Get employee ID from session
                var employeeId = Session["UserId"] as long?;
                if (!employeeId.HasValue)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });
                }

                // Validate table exists and is active
                var table = db.BanAns.Find(model.BanID);
                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn" });
                }

                // Calculate total
                decimal totalAmount = 0;
                foreach (var item in model.OrderItems)
                {
                    var menuItem = db.MonAns.Find(item.MonAnID);
                    if (menuItem == null)
                    {
                        return Json(new { success = false, message = $"Không tìm thấy món ăn ID: {item.MonAnID}" });
                    }

                    if (menuItem.TrangThai != "Hoạt động" && menuItem.TrangThai != "Đang phục vụ")
                    {
                        return Json(new { success = false, message = $"Món '{menuItem.TenMon}' đã hết" });
                    }

                    totalAmount += menuItem.Gia * item.SoLuong;
                }

                // Create order
                var order = new DonHang
                {
                    BanID = model.BanID,
                    NhanVienID = employeeId.Value,
                    NgayDat = DateTime.Now,
                    TongTien = totalAmount,
                    TrangThai = "Đang chuẩn bị",
                    GhiChu = model.GhiChu
                };

                db.DonHangs.Add(order);
                db.SaveChanges();

                // Create order details
                foreach (var item in model.OrderItems)
                {
                    var menuItem = db.MonAns.Find(item.MonAnID);
                    var orderDetail = new ChiTietDonHang
                    {
                        DonHangID = order.DonHangID,
                        MonAnID = item.MonAnID,
                        SoLuong = item.SoLuong,
                        DonGia = menuItem.Gia,
                        GhiChuMon = item.GhiChu
                    };

                    db.ChiTietDonHangs.Add(orderDetail);
                }

                db.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = "Tạo đơn hàng thành công",
                    orderId = order.DonHangID,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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

    // Model for creating order
    public class CreateOrderModel
    {
        public long BanID { get; set; }
        public string GhiChu { get; set; }
        public List<OrderItemModel> OrderItems { get; set; }
    }

    public class OrderItemModel
    {
        public long MonAnID { get; set; }
        public int SoLuong { get; set; }
        public string GhiChu { get; set; }
    }

}

// Model for cart items - moved outside controller for better accessibility
namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    public class CartItem
    {
        public long MonAnID { get; set; }
        public string TenMon { get; set; }
        public decimal Gia { get; set; }
        public int SoLuong { get; set; }
        public string HinhAnh { get; set; }
        public string DonViTinh { get; set; }
    }
}

