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
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginForm model, string returnUrl)
        {
            // Check if it's an AJAX request (from modal)
            bool isAjaxRequest = Request.IsAjaxRequest();

            if (ModelState.IsValid)
            {
                // Find user by email
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

                        // Create authentication ticket
                        FormsAuthentication.SetAuthCookie(user.Email, model.RememberMe);

                        // Store user info in session
                        Session["UserId"] = user.NhanVienID;
                        Session["UserName"] = user.HoTen;
                        Session["UserEmail"] = user.Email;
                        Session["UserRole"] = user.VaiTro?.TenVaiTro;

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
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterForm model)
        {
            // Check if it's an AJAX request (from modal)
            bool isAjaxRequest = Request.IsAjaxRequest();

            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (db.nhan_vien.Any(u => u.Email == model.Email))
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
                    
                    // Get default user role for hidden field
                    var defaultUserRoleForError = db.vai_tro
                        .FirstOrDefault(r => r.TenVaiTro.ToLower() == "khách hàng" || 
                                            r.TenVaiTro.ToLower() == "khach hang" || 
                                            r.TenVaiTro.ToLower() == "user" || 
                                            r.TenVaiTro.ToLower() == "customer");
                    
                    if (defaultUserRoleForError != null)
                    {
                        ViewBag.DefaultUserRoleId = defaultUserRoleForError.VaiTroID;
                    }
                    
                    return View(model);
                }

                // Check if phone number already exists
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.nhan_vien.Any(u => u.SDT == model.PhoneNumber))
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
                    
                    // Get default user role for hidden field
                    var defaultUserRoleForError = db.vai_tro
                        .FirstOrDefault(r => r.TenVaiTro.ToLower() == "khách hàng" || 
                                            r.TenVaiTro.ToLower() == "khach hang" || 
                                            r.TenVaiTro.ToLower() == "user" || 
                                            r.TenVaiTro.ToLower() == "customer");
                    
                    if (defaultUserRoleForError != null)
                    {
                        ViewBag.DefaultUserRoleId = defaultUserRoleForError.VaiTroID;
                    }
                    
                    return View(model);
                }

                // Get default user role (customer/user) if RoleId is not provided or is 0
                long roleId = model.RoleId;
                if (roleId == 0)
                {
                    var defaultUserRoleForRegistration = db.vai_tro
                        .FirstOrDefault(r => r.TenVaiTro.ToLower() == "khách hàng" || 
                                            r.TenVaiTro.ToLower() == "khach hang" || 
                                            r.TenVaiTro.ToLower() == "user" || 
                                            r.TenVaiTro.ToLower() == "customer");
                    
                    if (defaultUserRoleForRegistration == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy vai trò mặc định (Khách hàng)!");
                        
                        if (isAjaxRequest)
                        {
                            var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                );
                            return Json(new { success = false, errors = errors });
                        }
                        
                        ViewBag.Roles = new SelectList(db.vai_tro.ToList(), "VaiTroID", "TenVaiTro");
                        return View(model);
                    }
                    
                    roleId = defaultUserRoleForRegistration.VaiTroID;
                }

                // Create new user
                var newUser = new NhanVien
                {
                    HoTen = model.FullName,
                    Email = model.Email,
                    SDT = model.PhoneNumber,
                    MatKhau = HashPassword(model.Password),
                    VaiTroID = roleId,
                    TaiKhoan = model.Email, // Use email as username
                    NgayVaoLam = null, // Customers don't have start date
                    TrangThai = "Hoạt động",
                    DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null // Save address if provided
                };

                try
                {
                    db.nhan_vien.Add(newUser);
                    db.SaveChanges();

                    // Auto login after registration
                    FormsAuthentication.SetAuthCookie(newUser.Email, false);
                    Session["UserId"] = newUser.NhanVienID;
                    Session["UserName"] = newUser.HoTen;
                    Session["UserEmail"] = newUser.Email;
                    var userRole = db.vai_tro.Find(newUser.VaiTroID)?.TenVaiTro;
                    Session["UserRole"] = userRole;

                    if (isAjaxRequest)
                    {
                        // Customer/User always redirect to home page
                        var redirectUrl = Url.Action("Index", "Home");
                        return Json(new { success = true, redirectUrl = redirectUrl });
                    }

                    TempData["SuccessMessage"] = "Đăng ký thành công!";
                    
                    // Customer/User always redirect to home page
                    return RedirectToAction("Index", "Home");
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
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
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