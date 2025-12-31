using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Cryptography;
using System.Text;
using Project_65133141.Models;
using Project_65133141.Models.Form;

namespace Project_65133141.Controllers
{
    public class AccountController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            // If user is already authenticated, redirect to appropriate area based on role
            if (User.Identity.IsAuthenticated)
            {
                var userRole = Session["UserRole"] as string;
                return RedirectToRoleArea(userRole);
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        // [ValidateAntiForgeryToken]  // Disabled for AJAX modal login to avoid missing cookie issues
        public ActionResult Login(LoginForm model, string returnUrl)
        {
            // Check if it's an AJAX request (from modal)
            bool isAjaxRequest = Request.IsAjaxRequest();

            // Validate CAPTCHA
            if (!CaptchaController.VerifyCaptcha(Session, model.CaptchaCode))
            {
                ModelState.AddModelError("CaptchaCode", "Mã xác nhận không đúng. Vui lòng thử lại.");
                
                if (isAjaxRequest)
                {
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return Json(new { success = false, errors = errors });
                }
                
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Try to find customer in Users table first
                var customerUser = db.Users.FirstOrDefault(u => u.Email == model.Email);
                
                if (customerUser != null)
                {
                    // Verify password for customer
                    bool isPasswordValid = VerifyPassword(model.Password, customerUser.Password);

                    if (isPasswordValid)
                    {
                        // Check if account is disabled
                        if (customerUser.TrangThai == false)
                        {
                            ModelState.AddModelError("", "Tài khoản đã bị vô hiệu hoá");
                            
                            if (isAjaxRequest)
                            {
                                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                    .ToDictionary(
                                        kvp => kvp.Key,
                                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                    );
                                return Json(new { success = false, errors = errors });
                            }
                            
                            return View(model);
                        }

                        // Customer role
                        var userRole = "Khách hàng";

                        // Customer: Session cookie (no persistent cookie)
                        var ticket = new FormsAuthenticationTicket(
                            1,
                            customerUser.Email,
                            DateTime.Now,
                            DateTime.Now.AddMinutes(60), // 60 minutes timeout
                            false, // NOT persistent - session cookie
                            userRole,
                            FormsAuthentication.FormsCookiePath
                        );
                        var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                        {
                            HttpOnly = true,
                            Secure = false,
                            Path = FormsAuthentication.FormsCookiePath
                        };
                        Response.Cookies.Add(cookie);
                        
                        // Set session timeout to 60 minutes
                        Session.Timeout = 60;
                        Session["UserId"] = customerUser.UserID; // Use UserID from Users table
                        Session["UserName"] = customerUser.HoTen;
                        Session["UserEmail"] = customerUser.Email;
                        Session["UserRole"] = userRole;
                        Session["IsAdminOrEmployee"] = false;
                        Session["SessionStartTime"] = DateTime.Now;
                        Session["SessionExpiryTime"] = DateTime.Now.AddMinutes(60);

                        // Redirect to return URL or home page
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            if (isAjaxRequest)
                            {
                                return Json(new { success = true, redirectUrl = returnUrl });
                            }
                            return Redirect(returnUrl);
                        }
                        
                        // Customer always redirect to home page
                        var redirectUrl = Url.Action("Index", "Home");
                        if (isAjaxRequest)
                        {
                            return Json(new { success = true, redirectUrl = redirectUrl });
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    // Try to find admin/employee in nhan_vien table
                    var user = db.nhan_vien.FirstOrDefault(u => u.Email == model.Email);

                    if (user != null)
                    {
                        // Verify password - check both MatKhau and SDT for backward compatibility
                        bool isPasswordValid = false;
                        if (!string.IsNullOrEmpty(user.MatKhau))
                        {
                            isPasswordValid = VerifyPassword(model.Password, user.MatKhau);
                        }
                        else if (!string.IsNullOrEmpty(user.SDT))
                        {
                            // Backward compatibility: check if password was stored in SDT
                            isPasswordValid = VerifyPassword(model.Password, user.SDT);
                        }

                        if (isPasswordValid)
                        {
                            // Check if account is disabled
                            if (user.TrangThai == "Vô hiệu hóa")
                            {
                                ModelState.AddModelError("", "Tài khoản đã bị vô hiệu hoá");
                                
                                if (isAjaxRequest)
                                {
                                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                        .ToDictionary(
                                            kvp => kvp.Key,
                                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                        );
                                    return Json(new { success = false, errors = errors });
                                }
                                
                                return View(model);
                            }

                            // Determine user role
                            var userRole = user.VaiTro?.TenVaiTro;
                            var roleLower = userRole?.ToLower().Trim() ?? "";
                            bool isAdminOrEmployee = roleLower == "admin" || roleLower.Contains("admin") || 
                                                     roleLower == "administrator" ||
                                                     roleLower == "nhân viên" || roleLower == "nhan vien" || 
                                                     roleLower == "employee" || roleLower.Contains("nhân viên") || 
                                                     roleLower.Contains("nhan vien");

                            // Set authentication cookie with different settings for admin/employee vs user
                            if (isAdminOrEmployee)
                            {
                                // Admin/Employee: Session cookie (no persistent cookie, expires when browser closes)
                                // Set timeout to 1 minute
                                var ticket = new FormsAuthenticationTicket(
                                    1,
                                    user.Email,
                                    DateTime.Now,
                                    DateTime.Now.AddMinutes(60), // 60 minutes timeout
                                    false, // NOT persistent - session cookie
                                    userRole,
                                    FormsAuthentication.FormsCookiePath
                                );
                                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                                {
                                    HttpOnly = true,
                                    Secure = false,
                                    Path = FormsAuthentication.FormsCookiePath
                                };
                                Response.Cookies.Add(cookie);
                                
                                // Set session timeout to 60 minutes
                                Session.Timeout = 60;
                            }
                            else
                            {
                                // User/Customer: Session cookie (no persistent cookie, expires when browser closes)
                                // Set timeout to 1 minute
                                var ticket = new FormsAuthenticationTicket(
                                    1,
                                    user.Email,
                                    DateTime.Now,
                                    DateTime.Now.AddMinutes(60), // 60 minutes timeout
                                    false, // NOT persistent - session cookie
                                    userRole,
                                    FormsAuthentication.FormsCookiePath
                                );
                                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                                {
                                    HttpOnly = true,
                                    Secure = false,
                                    Path = FormsAuthentication.FormsCookiePath
                                };
                                Response.Cookies.Add(cookie);
                                
                                // Set session timeout to 60 minutes
                                Session.Timeout = 60;
                            }

                            // Store user info in session
                            Session["UserId"] = user.NhanVienID;
                            Session["UserName"] = user.HoTen;
                            Session["UserEmail"] = user.Email;
                            Session["UserRole"] = userRole;
                            Session["IsAdminOrEmployee"] = isAdminOrEmployee;
                            Session["SessionStartTime"] = DateTime.Now;
                            Session["SessionExpiryTime"] = DateTime.Now.AddMinutes(60);

                            // Redirect to return URL or based on role
                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            {
                                if (isAjaxRequest)
                                {
                                    return Json(new { success = true, redirectUrl = returnUrl });
                                }
                                return Redirect(returnUrl);
                            }
                            
                            // Redirect based on user role
                            var redirectUrl = GetRedirectUrlForRole(user.VaiTro?.TenVaiTro);
                            if (isAjaxRequest)
                            {
                                return Json(new { success = true, redirectUrl = redirectUrl });
                            }
                            return RedirectToRoleArea(user.VaiTro?.TenVaiTro);
                        }
                    }
                }

                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            }

            // If ModelState is invalid, return errors for AJAX
            if (isAjaxRequest)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, errors = errors });
            }

            return View(model);
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            // If user is already authenticated, redirect to appropriate area based on role
            if (User.Identity.IsAuthenticated)
            {
                var userRole = Session["UserRole"] as string;
                return RedirectToRoleArea(userRole);
            }

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

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        // [ValidateAntiForgeryToken]  // Disabled for AJAX modal register; CAPTCHA is used for protection
        public ActionResult Register(RegisterForm model)
        {
            // Check if it's an AJAX request (from modal)
            bool isAjaxRequest = Request.IsAjaxRequest();

            // Validate CAPTCHA
            if (!CaptchaController.VerifyCaptcha(Session, model.CaptchaCode))
            {
                ModelState.AddModelError("CaptchaCode", "Mã xác nhận không đúng. Vui lòng thử lại.");
                
                if (isAjaxRequest)
                {
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return Json(new { success = false, errors = errors });
                }
                
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists in Users table (chỉ khách hàng)
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    
                    if (isAjaxRequest)
                    {
                        var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            );
                        return Json(new { success = false, errors = errors });
                    }
                    
                    return View(model);
                }

                // Check if phone number already exists in Users table
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.Users.Any(u => u.SDT == model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng");
                    
                    if (isAjaxRequest)
                    {
                        var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            );
                        return Json(new { success = false, errors = errors });
                    }
                    
                    return View(model);
                }

                // Create new customer user in Users table
                var newUser = new User
                {
                    Username = model.Email, // Use email as username
                    Password = HashPassword(model.Password),
                    Email = model.Email,
                    HoTen = model.FullName,
                    SDT = model.PhoneNumber,
                    DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null,
                    NgayTao = DateTime.Now,
                    TrangThai = true,
                    DiemTichLuy = 0
                };

                try
                {
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    // Auto login after registration (Customer only)
                    var userRole = "Khách hàng"; // Default role for customers
                    
                    // Customer: Session cookie (no persistent cookie)
                    var ticket = new FormsAuthenticationTicket(
                        1,
                        newUser.Email,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(60), // 60 minutes timeout
                        false, // NOT persistent - session cookie
                        userRole,
                        FormsAuthentication.FormsCookiePath
                    );
                    var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                    {
                        HttpOnly = true,
                        Secure = false,
                        Path = FormsAuthentication.FormsCookiePath
                    };
                    Response.Cookies.Add(cookie);
                    
                    // Set session timeout to 1 minute
                    Session.Timeout = 1;
                    Session["UserId"] = newUser.UserID; // Use UserID from Users table
                    Session["UserName"] = newUser.HoTen;
                    Session["UserEmail"] = newUser.Email;
                    Session["UserRole"] = userRole;
                    Session["IsAdminOrEmployee"] = false;
                    Session["SessionStartTime"] = DateTime.Now;
                    Session["SessionExpiryTime"] = DateTime.Now.AddMinutes(1);

                    if (isAjaxRequest)
                    {
                        // Customer always redirect to home page
                        var redirectUrl = Url.Action("Index", "Home");
                        return Json(new { success = true, redirectUrl = redirectUrl });
                    }

                    TempData["SuccessMessage"] = "Đăng ký thành công!";
                    
                    // Customer always redirect to home page
                    return RedirectToAction("Index", "Home");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    // Handle Entity Framework validation errors
                    var errorMessages = new List<string>();
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            errorMessages.Add($"{validationError.PropertyName}: {validationError.ErrorMessage}");
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                    var fullErrorMessage = "Lỗi xác thực dữ liệu: " + string.Join("; ", errorMessages);
                    ModelState.AddModelError("", fullErrorMessage);
                    
                    if (isAjaxRequest)
                    {
                        var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            );
                        return Json(new { success = false, errors = errors });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi đăng ký: " + ex.Message);
                    
                    if (isAjaxRequest)
                    {
                        var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            );
                        return Json(new { success = false, errors = errors });
                    }
                }
            }

            // If ModelState is invalid, return errors
            if (isAjaxRequest)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, errors = errors });
            }

            // Get default user role for hidden field
            var defaultUserRole = db.vai_tro
                .FirstOrDefault(r => r.TenVaiTro.ToLower() == "khách hàng" || 
                                    r.TenVaiTro.ToLower() == "khach hang" || 
                                    r.TenVaiTro.ToLower() == "user" || 
                                    r.TenVaiTro.ToLower() == "customer");
            
            if (defaultUserRole != null)
            {
                ViewBag.DefaultUserRoleId = defaultUserRole.VaiTroID;
            }
            
            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            try
            {
                // Clear all cart sessions (for all possible tables)
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
                // Log error but continue with logout
                System.Diagnostics.Debug.WriteLine("Error clearing cart sessions: " + ex.Message);
            }
            
            // Clear authentication
            FormsAuthentication.SignOut();
            
            // Clear all session data
            if (Session != null)
            {
                Session.Clear();
                Session.Abandon();
            }
            
            // Clear all cookies
            if (Request.Cookies != null)
            {
                foreach (string cookieName in Request.Cookies.AllKeys)
                {
                    HttpCookie cookie = new HttpCookie(cookieName);
                    cookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(cookie);
                }
            }
            
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword - Disabled
        public ActionResult ForgotPassword()
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string Email, string NewPassword, string ConfirmPassword, string CaptchaCode)
        {
            bool isAjaxRequest = Request.IsAjaxRequest();

            // Validate CAPTCHA
            if (!CaptchaController.VerifyCaptcha(Session, CaptchaCode))
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, errorMessage = "Mã xác nhận không đúng. Vui lòng thử lại." });
                }

                TempData["ErrorMessage"] = "Mã xác nhận không đúng. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, errorMessage = "Vui lòng nhập đầy đủ thông tin." });
                }

                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("ForgotPassword");
            }

            // Chuẩn hóa email để tránh lỗi do khoảng trắng hoặc chữ hoa/thường
            var normalizedEmail = Email.Trim().ToLower();

            if (NewPassword != ConfirmPassword)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, errorMessage = "Mật khẩu xác nhận không khớp." });
                }

                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("ForgotPassword");
            }

            // Tìm người dùng theo email (không phân biệt hoa thường)
            User user = null;
            try
            {
                user = db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);
            }
            catch (Exception ex)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, errorMessage = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message });
                }

                TempData["ErrorMessage"] = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message;
                return RedirectToAction("ForgotPassword");
            }
            if (user == null)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, errorMessage = "Không tìm thấy tài khoản với email này." });
                }

                TempData["ErrorMessage"] = "Không tìm thấy tài khoản với email này.";
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                user.Password = HashPassword(NewPassword);
                db.SaveChanges();
                if (isAjaxRequest)
                {
                    return Json(new { success = true, message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
                }

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, errorMessage = "Lỗi khi cập nhật mật khẩu: " + ex.Message });
                }

                TempData["ErrorMessage"] = "Lỗi khi cập nhật mật khẩu: " + ex.Message;
                return RedirectToAction("ForgotPassword");
            }

        }

        // Helper method to hash password using SHA256
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return null;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Helper method to verify password
        private bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            string hashedInput = HashPassword(password);
            return hashedInput.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        // Helper method to get redirect URL for role (for AJAX responses)
        private string GetRedirectUrlForRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return Url.Action("Index", "Home");
            }

            string roleLower = roleName.ToLower().Trim();
            
            // Check for admin role (case-insensitive, with variations)
            if (roleLower == "admin" || roleLower.Contains("admin") || roleLower == "administrator")
            {
                return Url.Action("Index", "Home", new { area = "Admin_65133141" });
            }
            // Check for employee role
            else if (roleLower == "nhân viên" || roleLower == "nhan vien" || roleLower == "employee" || roleLower.Contains("nhân viên") || roleLower.Contains("nhan vien"))
            {
                return Url.Action("Index", "Home", new { area = "Employee_65133141" });
            }
            // Check for customer/user role
            else if (roleLower == "khách hàng" || roleLower == "khach hang" || roleLower == "user" || roleLower == "customer" || roleLower.Contains("khách hàng") || roleLower.Contains("khach hang"))
            {
                return Url.Action("Index", "Home", new { area = "User_65133141" });
            }
            
            // Default: redirect to home page
            return Url.Action("Index", "Home");
        }

        // Helper method to redirect user to appropriate area based on role
        private ActionResult RedirectToRoleArea(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return RedirectToAction("Index", "Home");
            }

            string roleLower = roleName.ToLower().Trim();
            
            // Check for admin role (case-insensitive, with variations)
            if (roleLower == "admin" || roleLower.Contains("admin") || roleLower == "administrator")
            {
                return RedirectToAction("Index", "Home", new { area = "Admin_65133141" });
            }
            // Check for employee role
            else if (roleLower == "nhân viên" || roleLower == "nhan vien" || roleLower == "employee" || roleLower.Contains("nhân viên") || roleLower.Contains("nhan vien"))
            {
                return RedirectToAction("Index", "Home", new { area = "Employee_65133141" });
            }
            // Check for customer/user role
            else if (roleLower == "khách hàng" || roleLower == "khach hang" || roleLower == "user" || roleLower == "customer" || roleLower.Contains("khách hàng") || roleLower.Contains("khach hang"))
            {
                return RedirectToAction("Index", "Home", new { area = "User_65133141" });
            }
            
            // Default: redirect to home page
            return RedirectToAction("Index", "Home");
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