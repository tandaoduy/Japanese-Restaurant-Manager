using System;
using System.Linq;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using Project_65133141.Models;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    public class AccountController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/Account
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "" }); // Redirect to main login
            }

            long userId = Convert.ToInt64(Session["UserId"]);
            var nhanVien = db.NhanViens.Find(userId);

            if (nhanVien == null)
            {
                Session.Abandon();
                return RedirectToAction("Index", "Login", new { area = "" });
            }

            return View(nhanVien);
        }

        [HttpPost]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Phiên làm việc hết hạn. Vui lòng đăng nhập lại." });
            }

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });
            }

            long userId = Convert.ToInt64(Session["UserId"]);
            var nhanVien = db.NhanViens.Find(userId);

            if (nhanVien == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            // Verify old password
            string oldPasswordHash = HashPassword(oldPassword);
            if (nhanVien.MatKhau != oldPasswordHash)
            {
                return Json(new { success = false, message = "Mật khẩu cũ không chính xác." });
            }

            // Update password
            nhanVien.MatKhau = HashPassword(newPassword);
            db.SaveChanges();

            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
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
    }
}
