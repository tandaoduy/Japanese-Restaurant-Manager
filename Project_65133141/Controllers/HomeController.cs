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

        /// <summary>
        /// Strict Area Isolation: Force logout when navigating from authenticated areas to root
        /// </summary>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Only check if user is authenticated
            // Skip check for Shared/Public APIs like SubmitRating, Contact, About if they are shared
            string actionName = filterContext.ActionDescriptor.ActionName;
            if (actionName == "SubmitRating") 
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            if (User.Identity.IsAuthenticated)
            {
                // Check if user is coming from restricted areas (User/Admin/Employee) via Referrer
                // This detects links like "Trang chủ" in user menu
                var referrer = Request.UrlReferrer?.AbsolutePath?.ToLower();
                
                if (!string.IsNullOrEmpty(referrer) &&
                    (referrer.Contains("/user_65133141/") || 
                     referrer.Contains("/admin_65133141/") || 
                     referrer.Contains("/employee_65133141/")))
                {
                    // Force complete logout
                    FormsAuthentication.SignOut();
                    Session.Clear();
                    Session.Abandon();
                    
                    // Clear auth cookie explicitly
                    if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                    {
                        var c = new HttpCookie(FormsAuthentication.FormsCookieName)
                        {
                            Expires = DateTime.Now.AddDays(-1)
                        };
                        Response.Cookies.Add(c);
                    }
                    
                    // Redirect to Home/Index to refresh state completely (with clean session)
                    // Add signedOut param to avoid redirect loops if any
                    filterContext.Result = RedirectToAction("Index", "Home", new { signedOut = "true", area = "" });
                    return;
                }
            }
            
            base.OnActionExecuting(filterContext);
        }

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
            
            // Get featured news for home page display
            var featuredNews = db.TinTucs
                .Where(t => t.IsHienThi == true)
                .OrderByDescending(t => t.IsNoiBat)
                .ThenByDescending(t => t.NgayDang)
                .Take(6)
                .ToList();
            ViewBag.FeaturedNews = featuredNews;
            
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

        /// <summary>
        /// Xử lý gửi đánh giá từ footer
        /// </summary>
        [HttpPost]
        public JsonResult SubmitRating()
        {
            try
            {
                // Read JSON from request body
                var reader = new System.IO.StreamReader(Request.InputStream);
                reader.BaseStream.Position = 0;
                var json = reader.ReadToEnd();
                
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                dynamic data = serializer.Deserialize<dynamic>(json);
                
                int rating = Convert.ToInt32(data["rating"]);
                string comment = data["comment"]?.ToString() ?? "";
                
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Đánh giá không hợp lệ" });
                }
                
                // Get user info - UserID can be long or int depending on session storage
                long userIdValue = 0;
                if (Session["UserId"] != null)
                {
                    // Try to get as long first, then int
                    if (Session["UserId"] is long)
                    {
                        userIdValue = (long)Session["UserId"];
                    }
                    else if (Session["UserId"] is int)
                    {
                        userIdValue = (int)Session["UserId"];
                    }
                    else
                    {
                        long.TryParse(Session["UserId"].ToString(), out userIdValue);
                    }
                }

                // If anonymous (userIdValue == 0), use or create a Guest user
                if (userIdValue == 0)
                {
                    var guestEmail = "guest@system.com";
                    var guestUser = db.Users.FirstOrDefault(u => u.Email == guestEmail);
                    
                    if (guestUser == null)
                    {
                        // Create guest user
                        guestUser = new User
                        {
                            Username = "guest",
                            Password = "NoLoginNeeded_" + Guid.NewGuid().ToString(),
                            HoTen = "Ẩn danh",
                            Email = guestEmail,
                            SDT = "0000000000",
                            DiaChi = "System",
                            NgayTao = DateTime.Now,
                            TrangThai = true,
                            DiemTichLuy = 0,
                            Avatar = "default.png"
                        };
                        db.Users.Add(guestUser);
                        db.SaveChanges();
                    }
                    userIdValue = guestUser.UserID;
                }
                
                // Save to DanhGia table
                var danhGia = new DanhGia
                {
                    SoSao = rating,
                    NoiDung = comment,
                    NgayDanhGia = DateTime.Now,
                    UserID = userIdValue
                };
                
                db.DanhGias.Add(danhGia);
                db.SaveChanges();
                
                return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SubmitRating error: " + ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại" });
            }
        }
    }
}