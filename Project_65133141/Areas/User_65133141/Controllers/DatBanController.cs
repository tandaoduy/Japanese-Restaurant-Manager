using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Services;

namespace Project_65133141.Areas.User_65133141.Controllers
{
    [Authorize]
    public class DatBanController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: User_65133141/DatBan/Create
        public ActionResult Create()
        {
            // Debug: Check authentication and session
            var isAuthenticated = User.Identity.IsAuthenticated;
            var userEmail = Session["UserEmail"] as string;
            
            // If authenticated but no email in session, try to get from User.Identity.Name
            if (isAuthenticated && string.IsNullOrEmpty(userEmail))
            {
                userEmail = User.Identity.Name; // Email is stored in Identity.Name
                if (!string.IsNullOrEmpty(userEmail))
                {
                    Session["UserEmail"] = userEmail;
                }
            }
            
            // Always return view - let the view handle authentication check
            // This ensures the form is always displayed
            return View();
        }

        // POST: User_65133141/DatBan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DatBan model)
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, requiresLogin = true, message = "Vui lòng đăng nhập để đặt bàn" });
            }

            // Get user email from session
            var userEmail = Session["UserEmail"] as string;
            
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại." });
            }

            // Find User in Users table (chỉ khách hàng)
            var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
            
            if (user == null)
            {
                return Json(new { success = false, message = "Tài khoản không hợp lệ. Vui lòng đăng ký tài khoản khách hàng để đặt bàn." });
            }

            // Handle date and time slot from form
            var ngayDat = Request.Form["ngayDat"];
            var khungGio = Request.Form["khungGio"];
            
            if (string.IsNullOrEmpty(ngayDat) || string.IsNullOrEmpty(khungGio))
            {
                ModelState.AddModelError("ThoiGianDen", "Vui lòng chọn ngày đặt và khung giờ");
            }
            else
            {
                try
                {
                    var date = DateTime.Parse(ngayDat);
                    var timeParts = khungGio.Split(':');
                    if (timeParts.Length > 0)
                    {
                        date = date.AddHours(int.Parse(timeParts[0]));
                        if (timeParts.Length > 1)
                        {
                            date = date.AddMinutes(int.Parse(timeParts[1]));
                        }
                        model.ThoiGianDen = date;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ThoiGianDen", "Ngày và giờ không hợp lệ: " + ex.Message);
                }
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(model.HoTenKhach))
            {
                ModelState.AddModelError("HoTenKhach", "Vui lòng nhập họ tên");
            }
            if (string.IsNullOrWhiteSpace(model.SDTKhach))
            {
                ModelState.AddModelError("SDTKhach", "Vui lòng nhập số điện thoại");
            }
            if (model.SoNguoi == null || model.SoNguoi <= 0)
            {
                ModelState.AddModelError("SoNguoi", "Vui lòng nhập số người hợp lệ");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure ThoiGianDen is set
                    if (model.ThoiGianDen == default(DateTime))
                    {
                        return Json(new { success = false, message = "Thời gian đến không được để trống" });
                    }

                    // Set user ID and default values
                    // Khách hàng KHÔNG chọn bàn - để BanID = null, hệ thống sẽ tự động sắp xếp sau
                    model.UserID = user.UserID;
                    model.BanID = null; // Không cho khách chọn bàn
                    model.TrangThai = "Chờ xác nhận";
                    model.NgayTao = DateTime.Now;

                    // Save reservation
                    db.DatBans.Add(model);
                    db.SaveChanges();
                    
                    // DatBanID is automatically assigned after SaveChanges
                    // NOTE: Email will NOT be sent here - it will be sent when Admin/Employee confirms the reservation

                    // Success message - no email mention since email is sent on confirmation
                    string successMessage = "Đặt bàn thành công! Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận và sắp xếp bàn phù hợp.";

                    // Always return JSON for AJAX handling - show alert and stay on page
                    return Json(new { success = true, message = successMessage });
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
                    return Json(new { success = false, message = fullErrorMessage });
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpdateEx)
                {
                    // Handle database update errors
                    var errorMessage = "Lỗi cập nhật database: ";
                    var innerEx = dbUpdateEx.InnerException;
                    
                    if (innerEx != null)
                    {
                        errorMessage += innerEx.Message;
                        
                        if (innerEx is System.Data.SqlClient.SqlException sqlEx)
                        {
                            switch (sqlEx.Number)
                            {
                                case 547:
                                    errorMessage = "Lỗi: Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin.";
                                    break;
                                case 2627:
                                    errorMessage = "Lỗi: Dữ liệu đã tồn tại trong hệ thống.";
                                    break;
                                case 515:
                                    errorMessage = "Lỗi: Thiếu thông tin bắt buộc.";
                                    break;
                            }
                        }
                    }

                    return Json(new { success = false, message = errorMessage });
                }
                catch (Exception ex)
                {
                    var errorMessage = "Lỗi: " + ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += " - Chi tiết: " + ex.InnerException.Message;
                    }

                    return Json(new { success = false, message = errorMessage });
                }
            }

            // Return validation errors
            var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return Json(new { success = false, errors = errors });
        }

        // GET: User_65133141/DatBan/MyReservations
        [Authorize]
        public ActionResult MyReservations()
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Find User in Users table (chỉ khách hàng)
            var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Tài khoản không hợp lệ. Vui lòng đăng ký tài khoản khách hàng.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var reservations = db.DatBans
                .Where(d => d.UserID == user.UserID)
                .OrderByDescending(d => d.ThoiGianDen)
                .ThenByDescending(d => d.DatBanID)
                .ToList();

            // Load related data
            foreach (var reservation in reservations)
            {
                if (reservation.BanID.HasValue)
                {
                    reservation.BanAn = db.BanAns.Find(reservation.BanID.Value);
                }
            }

            return View(reservations);
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
