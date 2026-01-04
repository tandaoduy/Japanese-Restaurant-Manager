using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Project_65133141.Models;
using Project_65133141.Models.Form;

namespace Project_65133141.Controllers
{
    public class HomeController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        public ActionResult Index()
        {
            // Clear all cart sessions when accessing home page
            try
            {
                if (Session != null)
                {
                    var keysToRemove = new List<string>();
                    foreach (string key in Session.Keys)
                    {
                        if (key != null && (key.StartsWith("Cart_Pending_") || key.StartsWith("Cart_Confirmed_")))
                        {
                            keysToRemove.Add(key);
                        }
                    }
                    
                    foreach (var key in keysToRemove)
                    {
                        Session.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                System.Diagnostics.Debug.WriteLine("Error clearing cart sessions: " + ex.Message);
            }
            
            // Nếu còn đăng nhập, xử lý tuỳ theo vai trò
            // Skip redirect if user was just signed out (to prevent redirect loop)
            var signedOut = Request.QueryString["signedOut"];
            if (User.Identity.IsAuthenticated && string.IsNullOrEmpty(signedOut))
            {
                var role = Session["UserRole"] as string;
                if (!string.IsNullOrEmpty(role))
                {
                    var roleLower = role.ToLower().Trim();

                    // Nếu là khách hàng/user -> chuyển hẳn sang khu vực User
                    if (roleLower == "khách hàng" || roleLower == "khach hang" ||
                        roleLower == "user" || roleLower == "customer" ||
                        roleLower.Contains("khách hàng") || roleLower.Contains("khach hang"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "User_65133141" });
                    }

                    // Nếu là admin hoặc nhân viên -> redirect về area tương ứng của họ
                    if (roleLower == "admin" || roleLower.Contains("admin") || roleLower == "administrator")
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin_65133141" });
                    }
                    else if (roleLower == "nhân viên" || roleLower == "nhan vien" || roleLower == "employee" ||
                        roleLower.Contains("nhân viên") || roleLower.Contains("nhan vien"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Employee_65133141" });
                    }
                }
            }

            ViewBag.LoginForm = new LoginForm();
            ViewBag.RegisterForm = new RegisterForm();
            
            // Get default user role (customer/user) for registration
            var defaultUserRole = db.vai_tro
                .FirstOrDefault(r => r.TenVaiTro.ToLower() == "khách hàng" || 
                                    r.TenVaiTro.ToLower() == "khach hang" || 
                                    r.TenVaiTro.ToLower() == "user" || 
                                    r.TenVaiTro.ToLower() == "customer");
            
            if (defaultUserRole != null)
            {
                ViewBag.DefaultUserRoleId = defaultUserRole.VaiTroID;
            }
            
            // Check for TempData message for unauthorized access
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"].ToString();
            }
            
            // Get all active products for menu display with category filter
            var allProducts = db.MonAns
                .Where(m => m.TrangThai == "Hoạt động" || m.TrangThai == "Đang phục vụ")
                .OrderByDescending(m => m.NgayTao)
                .ToList();
            ViewBag.FeaturedProducts = allProducts;
            
            // Get categories for category filter
            var categories = db.DanhMucs.ToDictionary(d => d.DanhMucID, d => d.TenDanhMuc);
            ViewBag.Categories = categories;
            
            // Get all categories for filter buttons
            var allCategories = db.DanhMucs.ToList();
            ViewBag.AllCategories = allCategories;
            
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        // Action để hiển thị thông tin user profile
        public new ActionResult Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = Session["UserId"] as int?;
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = db.nhan_vien.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Map nhan_vien to RegisterForm để sử dụng DisplayName
            var model = new RegisterForm
            {
                FullName = user.ho_ten,
                Email = user.email,
                PhoneNumber = user.so_dien_thoai,
                RoleId = user.vai_tro_id
            };

            return View(model);
        }
    }
}