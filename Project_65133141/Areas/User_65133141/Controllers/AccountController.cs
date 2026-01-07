using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;

namespace Project_65133141.Areas.User_65133141.Controllers
{
    public class AccountController : BaseAreaController
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: User_65133141/Account/Profile
        [HttpGet]
        public new ActionResult Profile()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            long userId = (long)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return View(user);
        }

        // POST: User_65133141/Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateProfile(string tenKhachHang, string sdt, string diaChi, HttpPostedFileBase avatar)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });
                }

                long userId = (long)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Validate
                if (string.IsNullOrWhiteSpace(tenKhachHang))
                {
                    return Json(new { success = false, message = "Tên không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(sdt) || sdt.Length != 10 || !sdt.All(char.IsDigit))
                {
                    return Json(new { success = false, message = "Số điện thoại phải có 10 chữ số" });
                }

                // Handle Avatar Upload if provided
                if (avatar != null && avatar.ContentLength > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(avatar.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPG, PNG, GIF)" });
                    }

                    // Validate file size (2MB)
                    if (avatar.ContentLength > 2 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Ảnh không được vượt quá 2MB" });
                    }

                    // Save file
                    string fileName = user.UserID + "_" + DateTime.Now.Ticks + extension;
                    string path = Path.Combine(Server.MapPath("~/Images/Avatars/"), fileName);
                    
                    // Create directory if not exists
                    Directory.CreateDirectory(Server.MapPath("~/Images/Avatars/"));

                    // Delete old avatar if exists (optional)
                    if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("~/Images/Avatars/"))
                    {
                        string oldPath = Server.MapPath(user.Avatar);
                        if (System.IO.File.Exists(oldPath))
                        {
                            try { System.IO.File.Delete(oldPath); } catch { }
                        }
                    }

                    avatar.SaveAs(path);
                    user.Avatar = "~/Images/Avatars/" + fileName;
                    Session["UserAvatar"] = user.Avatar;
                }

                // Update Info
                user.HoTen = tenKhachHang.Trim();
                user.SDT = sdt.Trim();
                user.DiaChi = diaChi?.Trim();

                db.SaveChanges();

                // Update session
                Session["UserName"] = user.HoTen;

                return Json(new { success = true, message = "Cập nhật hồ sơ thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateProfile error: " + ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: User_65133141/Account/UpdateAvatar
        [HttpPost]
        public JsonResult UpdateAvatar(HttpPostedFileBase avatar)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });
                }

                long userId = (long)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                if (avatar == null || avatar.ContentLength == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ảnh đại diện" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(avatar.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, png, gif)" });
                }

                // Validate file size (2MB)
                if (avatar.ContentLength > 2 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 2MB" });
                }

                // Save file
                var fileName = "avatar_" + userId + "_" + DateTime.Now.Ticks + extension;
                var uploadPath = Server.MapPath("~/Images/Avatars/");
                
                // Create directory if not exists
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);
                avatar.SaveAs(filePath);

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    var oldAvatarPath = Server.MapPath(user.Avatar);
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                // Update database
                user.Avatar = "~/Images/Avatars/" + fileName;
                db.SaveChanges();
                
                // Update session
                Session["UserAvatar"] = user.Avatar;

                return Json(new { 
                    success = true, 
                    message = "Cập nhật ảnh đại diện thành công",
                    avatarUrl = Url.Content(user.Avatar)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateAvatar error: " + ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: User_65133141/Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });
                }

                long userId = (long)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại" });
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mật khẩu mới" });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
                }

                // Verify current password
                string hashedCurrentPassword = HashPassword(currentPassword);
                if (user.Password != hashedCurrentPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
                }

                // Update password
                user.Password = HashPassword(newPassword);
                db.SaveChanges();

                return Json(new { success = true, message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ChangePassword error: " + ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Helper method to hash password using SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
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
