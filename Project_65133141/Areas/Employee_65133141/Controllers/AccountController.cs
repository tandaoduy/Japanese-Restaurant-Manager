using System;
using System.Linq;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee")]
    public class AccountController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Account
        public ActionResult Index()
        {
            // Hiển thị trang thông tin cá nhân (dùng cùng view với Profile)
            return RedirectToAction("Profile");
        }

        // GET: Employee_65133141/Account/Profile
        public new ActionResult Profile()
        {
            var userId = Session["UserId"] as long?;
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var nhanVien = db.NhanViens.Find(userId.Value);
            if (nhanVien == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Dùng view Index.cshtml để hiển thị thông tin cá nhân
            return View("Index", nhanVien);
        }

        // GET: Employee_65133141/Account/Edit - Thay đổi thông tin cá nhân
        public ActionResult Edit()
        {
            var userId = Session["UserId"] as long?;
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var nhanVien = db.NhanViens.Find(userId.Value);
            if (nhanVien == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View(nhanVien);
        }

        // POST: Employee_65133141/Account/Edit - Cập nhật thông tin cá nhân cơ bản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string HoTen, string SDT, string DiaChi)
        {
            var userId = Session["UserId"] as long?;
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var nhanVien = db.NhanViens.Find(userId.Value);
            if (nhanVien == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Cập nhật các trường cơ bản
            nhanVien.HoTen = HoTen;
            nhanVien.SDT = SDT;
            nhanVien.DiaChi = DiaChi;

            db.Entry(nhanVien).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction("Profile");
        }

        // GET: Employee_65133141/Account/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            var userId = Session["UserId"] as long?;
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var nhanVien = db.NhanViens.Find(userId.Value);
            if (nhanVien == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Verify old password
            if (!VerifyPassword(CurrentPassword, nhanVien.MatKhau))
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                return View();
            }

            // Update new password
            nhanVien.MatKhau = HashPassword(NewPassword);
            db.Entry(nhanVien).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
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
