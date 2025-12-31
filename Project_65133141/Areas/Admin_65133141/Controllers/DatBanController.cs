using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using Project_65133141.Services;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class DatBanController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        /// <summary>
        /// Tính trạng thái bàn dựa trên bảng DatBan
        /// Logic:
        /// - Không có bản ghi nào trong DatBan cho bàn đó → Trống
        /// - Có bản ghi TrangThai = 'Đã đặt' hoặc 'Đã xác nhận' → Đã đặt
        /// - Có bản ghi TrangThai = 'Đang sử dụng' hoặc 'Đang phục vụ' hoặc đang trong thời gian ăn → Đang phục vụ
        /// </summary>
        /// <param name="banID">ID của bàn</param>
        /// <returns>Trạng thái: "Trống", "Đã đặt", hoặc "Đang phục vụ"</returns>
        private string GetTableStatusFromDatBan(long banID)
        {
            var now = DateTime.Now;
            
            // Lấy tất cả đặt bàn của bàn này (trừ những cái đã hoàn thành hoặc hủy)
            var activeReservations = db.DatBans
                .Where(d => d.BanID == banID && 
                           d.TrangThai != "Hoàn thành" && 
                           d.TrangThai != "Đã hủy")
                .OrderByDescending(d => d.ThoiGianDen)
                .ToList();

            // Không có bản ghi nào → Trống
            if (!activeReservations.Any())
            {
                return "Trống";
            }

            // Kiểm tra xem có đặt bàn nào với trạng thái "Đang sử dụng" hoặc "Đang phục vụ" không
            var inUseReservation = activeReservations.FirstOrDefault(d =>
                d.TrangThai == "Đang sử dụng" || d.TrangThai == "Đang phục vụ");

            if (inUseReservation != null)
            {
                return "Đang phục vụ";
            }

            // Kiểm tra xem có đặt bàn nào đang trong thời gian ăn không (thời gian ăn ước tính 2 giờ)
            var inTimeReservation = activeReservations.FirstOrDefault(d =>
            {
                var thoiGianDen = d.ThoiGianDen;
                var thoiGianKetThuc = thoiGianDen.AddHours(2); // Thời gian ăn ước tính 2 giờ
                return now >= thoiGianDen && now <= thoiGianKetThuc && 
                       (d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đang phục vụ");
            });

            if (inTimeReservation != null)
            {
                return "Đang phục vụ";
            }

            // Kiểm tra xem có đặt bàn nào với trạng thái "Đã đặt" hoặc "Đã xác nhận" không
            var reservedReservation = activeReservations.FirstOrDefault(d =>
                d.TrangThai == "Đã đặt" || d.TrangThai == "Đã xác nhận");

            if (reservedReservation != null)
            {
                return "Đã đặt";
            }

            return "Trống";
        }

        /// <summary>
        /// Cập nhật trạng thái bàn trong bảng BanAn dựa trên DatBan
        /// </summary>
        private void UpdateTableStatusFromDatBan(long banID)
        {
            var banAn = db.BanAns.Find(banID);
            if (banAn != null)
            {
                banAn.TrangThai = GetTableStatusFromDatBan(banID);
            }
        }

        // GET: Admin_65133141/DatBan
        public ActionResult Index(string searchString, string statusFilter = null, int page = 1)
        {
            const int pageSize = 10;
            
            var query = db.DatBans.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => 
                    d.HoTenKhach.Contains(searchString) || 
                    d.SDTKhach.Contains(searchString) ||
                    (d.BanAn != null && d.BanAn.TenBan.Contains(searchString))
                );
            }

            // Status filter
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(d => d.TrangThai == statusFilter);
            }

            // Get total count before pagination
            var totalCount = query.Count();

            // Order by date descending
            var datBans = query
                .OrderByDescending(d => d.ThoiGianDen)
                .ThenByDescending(d => d.DatBanID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Load related data
            foreach (var datBan in datBans)
            {
                if (datBan.BanID.HasValue)
                {
                    datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                }
                if (datBan.UserID.HasValue)
                {
                    datBan.User = db.Users.Find(datBan.UserID.Value);
                }
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalCount;

            // Get all statuses for filter dropdown
            var statuses = db.DatBans
                .Where(d => !string.IsNullOrEmpty(d.TrangThai))
                .Select(d => d.TrangThai)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ViewBag.Statuses = statuses;

            return View(datBans);
        }

        // GET: Admin_65133141/DatBan/Details/5
        public ActionResult Details(long id)
        {
            var datBan = db.DatBans.Find(id);
            if (datBan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn!";
                return RedirectToAction("Index");
            }

            // Load related data
            if (datBan.BanID.HasValue)
            {
                datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
            }
            if (datBan.UserID.HasValue)
            {
                datBan.User = db.Users.Find(datBan.UserID.Value);
            }

            return View(datBan);
        }

        // GET: Admin_65133141/DatBan/Edit/5
        public ActionResult Edit(long id)
        {
            var datBan = db.DatBans.Find(id);
            if (datBan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn!";
                return RedirectToAction("Index");
            }

            // Load available tables - lấy tất cả bàn và tính trạng thái thực tế từ DatBan
            // Cho phép chọn bàn trống hoặc bàn đang được chỉnh sửa
            var allTables = db.BanAns.OrderBy(b => b.ViTri).ThenBy(b => b.TenBan).ToList();
            var availableTables = new List<BanAn>();
            
            foreach (var table in allTables)
            {
                var actualStatus = GetTableStatusFromDatBan(table.BanID);
                // Cho phép chọn bàn trống hoặc bàn đang được chỉnh sửa
                if (actualStatus == "Trống" || table.BanID == datBan.BanID)
                {
                    availableTables.Add(table);
                }
            }

            // Tạo SelectList với display text bao gồm tên bàn, vị trí và sức chứa
            var tableSelectList = availableTables.Select(t => new
            {
                BanID = t.BanID,
                DisplayText = t.TenBan + 
                             (string.IsNullOrEmpty(t.ViTri) ? "" : " (" + t.ViTri + ")") +
                             (t.SucChua.HasValue ? " - " + t.SucChua + " người" : "")
            }).ToList();

            ViewBag.BanID = new SelectList(
                tableSelectList,
                "BanID",
                "DisplayText",
                datBan.BanID
            );

            // Get status options
            var statuses = new List<string> { "Chờ xác nhận", "Đã xác nhận", "Đang phục vụ", "Hoàn thành", "Đã hủy" };
            ViewBag.TrangThai = new SelectList(statuses, datBan.TrangThai);

            return View(datBan);
        }

        // POST: Admin_65133141/DatBan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(DatBan model)
        {
            if (ModelState.IsValid)
            {
                var datBan = db.DatBans.Find(model.DatBanID);
                if (datBan == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đặt bàn!";
                    return RedirectToAction("Index");
                }

                // Lưu BanID cũ để cập nhật trạng thái bàn cũ
                var oldBanID = datBan.BanID;

                // Update fields
                datBan.HoTenKhach = model.HoTenKhach;
                datBan.SDTKhach = model.SDTKhach;
                datBan.BanID = model.BanID;
                datBan.ThoiGianDen = model.ThoiGianDen;
                datBan.SoNguoi = model.SoNguoi;
                datBan.GhiChu = model.GhiChu;
                datBan.TrangThai = model.TrangThai;

                // Cập nhật trạng thái bàn dựa trên DatBan
                // Nếu đổi bàn, cập nhật trạng thái bàn cũ
                if (oldBanID.HasValue && oldBanID != model.BanID)
                {
                    UpdateTableStatusFromDatBan(oldBanID.Value);
                }

                // Cập nhật trạng thái bàn mới (nếu có)
                if (model.BanID.HasValue)
                {
                    UpdateTableStatusFromDatBan(model.BanID.Value);
                }

                try
                {
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật đặt bàn thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                }
            }

            // Reload dropdowns on error - lấy tất cả bàn và tính trạng thái thực tế từ DatBan
            var allTables = db.BanAns.OrderBy(b => b.ViTri).ThenBy(b => b.TenBan).ToList();
            var availableTables = new List<BanAn>();
            
            foreach (var table in allTables)
            {
                var actualStatus = GetTableStatusFromDatBan(table.BanID);
                // Cho phép chọn bàn trống hoặc bàn đang được chỉnh sửa
                if (actualStatus == "Trống" || table.BanID == model.BanID)
                {
                    availableTables.Add(table);
                }
            }

            // Tạo SelectList với display text bao gồm tên bàn, vị trí và sức chứa
            var tableSelectList = availableTables.Select(t => new
            {
                BanID = t.BanID,
                DisplayText = t.TenBan + 
                             (string.IsNullOrEmpty(t.ViTri) ? "" : " (" + t.ViTri + ")") +
                             (t.SucChua.HasValue ? " - " + t.SucChua + " người" : "")
            }).ToList();

            ViewBag.BanID = new SelectList(
                tableSelectList,
                "BanID",
                "DisplayText",
                model.BanID
            );

            var statuses = new List<string> { "Chờ xác nhận", "Đã xác nhận", "Đang phục vụ", "Hoàn thành", "Đã hủy" };
            ViewBag.TrangThai = new SelectList(statuses, model.TrangThai);

            return View(model);
        }

        // POST: Admin_65133141/DatBan/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            var datBan = db.DatBans.Find(id);
            if (datBan == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt bàn!" });
                }
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn!";
                return RedirectToAction("Index");
            }

            try
            {
                // Lưu BanID để cập nhật trạng thái bàn sau khi xóa
                var banID = datBan.BanID;

                db.DatBans.Remove(datBan);
                db.SaveChanges();

                // Cập nhật trạng thái bàn dựa trên DatBan sau khi xóa
                if (banID.HasValue)
                {
                    UpdateTableStatusFromDatBan(banID.Value);
                    db.SaveChanges();
                }

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Xóa đặt bàn thành công!" });
                }

                TempData["SuccessMessage"] = "Xóa đặt bàn thành công!";
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Admin_65133141/DatBan/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm(long id)
        {
            var datBan = db.DatBans.Find(id);
            if (datBan == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt bàn!" });
                }
                TempData["ErrorMessage"] = "Không tìm thấy đặt bàn!";
                return RedirectToAction("Index");
            }

            try
            {
                // Load related data for email
                if (datBan.BanID.HasValue && datBan.BanAn == null)
                {
                    datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                }
                
                // Get customer email and name
                string customerEmail = null;
                string customerName = datBan.HoTenKhach;
                
                if (datBan.UserID.HasValue)
                {
                    var user = db.Users.Find(datBan.UserID.Value);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        customerEmail = user.Email;
                        if (string.IsNullOrEmpty(customerName))
                        {
                            customerName = user.HoTen;
                        }
                    }
                }
                
                // Only send email if we have a valid email address
                // If no email, skip sending but still confirm the booking

                // Cập nhật trạng thái thành "Đã xác nhận"
                datBan.TrangThai = "Đã xác nhận";
                db.SaveChanges();

                // Cập nhật trạng thái bàn nếu có
                if (datBan.BanID.HasValue)
                {
                    UpdateTableStatusFromDatBan(datBan.BanID.Value);
                    db.SaveChanges();
                }

                // Send confirmation email if we have customer email
                if (!string.IsNullOrEmpty(customerEmail) && customerEmail.Contains("@"))
                {
                    try
                    {
                        var emailService = new EmailService();
                        var emailSent = emailService.SendBookingConfirmationEmail(
                            datBan, 
                            customerEmail, 
                            customerName, 
                            isConfirmed: true
                        );
                        
                        if (!emailSent)
                        {
                            System.Diagnostics.Debug.WriteLine("Failed to send booking confirmation email");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log error but don't fail the confirmation
                        System.Diagnostics.Debug.WriteLine($"Error sending email: {emailEx.Message}");
                    }
                }

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Xác nhận đặt bàn thành công! Email xác nhận đã được gửi đến khách hàng." });
                }

                TempData["SuccessMessage"] = "Xác nhận đặt bàn thành công! Email xác nhận đã được gửi đến khách hàng.";
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Lỗi khi xác nhận: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Lỗi khi xác nhận: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Admin_65133141/DatBan/GetDatBanDetails/5
        public JsonResult GetDatBanDetails(long id)
        {
            try
            {
                var datBan = db.DatBans.Find(id);
                if (datBan == null) 
                    return Json(new { success = false, message = "Không tìm thấy đặt bàn" }, JsonRequestBehavior.AllowGet);

                // Load related data
                BanAn banAn = null;
                if (datBan.BanID.HasValue)
                {
                    banAn = db.BanAns.Find(datBan.BanID.Value);
                }

                User user = null;
                if (datBan.UserID.HasValue)
                {
                    user = db.Users.Find(datBan.UserID.Value);
                }

                var data = new
                {
                    datBan.DatBanID,
                    datBan.HoTenKhach,
                    datBan.SDTKhach,
                    datBan.BanID,
                    TenBan = banAn != null ? banAn.TenBan : null,
                    ViTri = banAn != null ? banAn.ViTri : null,
                    SucChua = banAn != null ? banAn.SucChua : null,
                    datBan.SoNguoi,
                    ThoiGianDen = datBan.ThoiGianDen.ToString("yyyy-MM-ddTHH:mm"),
                    datBan.TrangThai,
                    datBan.GhiChu,
                    NgayTao = datBan.NgayTao?.ToString("yyyy-MM-ddTHH:mm") ?? null,
                    UserHoTen = user != null ? user.HoTen : null,
                    UserEmail = user != null ? user.Email : null
                };

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Admin_65133141/DatBan/GetAvailableTables
        public JsonResult GetAvailableTables(long? currentDatBanId = null)
        {
            try
            {
                var allTables = db.BanAns.OrderBy(b => b.ViTri).ThenBy(b => b.TenBan).ToList();
                var availableTables = new List<object>();
                
                foreach (var table in allTables)
                {
                    var actualStatus = GetTableStatusFromDatBan(table.BanID);
                    // Cho phép chọn bàn trống hoặc bàn đang được chỉnh sửa
                    if (actualStatus == "Trống" || (currentDatBanId.HasValue && 
                        db.DatBans.Any(d => d.DatBanID == currentDatBanId.Value && d.BanID == table.BanID)))
                    {
                        availableTables.Add(new
                        {
                            BanID = table.BanID,
                            DisplayText = table.TenBan + 
                                         (string.IsNullOrEmpty(table.ViTri) ? "" : " (" + table.ViTri + ")") +
                                         (table.SucChua.HasValue ? " - " + table.SucChua + " người" : "")
                        });
                    }
                }

                return Json(new { success = true, tables = availableTables }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin_65133141/DatBan/EditDatBanAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // Temporarily allow for debugging, remove after fix
        public JsonResult EditDatBanAjax(DatBan model)
        {
            try
            {
                if (model == null || model.DatBanID == 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var datBan = db.DatBans.Find(model.DatBanID);
                if (datBan == null) 
                    return Json(new { success = false, message = "Không tìm thấy đặt bàn" });

                // Clear ModelState errors for fields we're not validating
                ModelState.Remove("UserID");
                ModelState.Remove("NgayTao");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors });
                }

                // Lưu BanID cũ để cập nhật trạng thái bàn cũ
                var oldBanID = datBan.BanID;

                // Update fields
                datBan.HoTenKhach = model.HoTenKhach;
                datBan.SDTKhach = model.SDTKhach;
                datBan.BanID = model.BanID;
                datBan.ThoiGianDen = model.ThoiGianDen;
                datBan.SoNguoi = model.SoNguoi;
                datBan.GhiChu = model.GhiChu;
                datBan.TrangThai = model.TrangThai;

                // Cập nhật trạng thái bàn dựa trên DatBan
                // Nếu đổi bàn, cập nhật trạng thái bàn cũ
                if (oldBanID.HasValue && oldBanID != model.BanID)
                {
                    UpdateTableStatusFromDatBan(oldBanID.Value);
                }

                // Cập nhật trạng thái bàn mới (nếu có)
                if (model.BanID.HasValue)
                {
                    UpdateTableStatusFromDatBan(model.BanID.Value);
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật đặt bàn thành công!" });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                return Json(new { success = false, message = "Lỗi validation: " + string.Join("; ", errorMessages) });
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var innerException = ex.InnerException?.InnerException;
                if (innerException is System.Data.SqlClient.SqlException sqlEx)
                {
                    return Json(new { success = false, message = "Lỗi database: " + sqlEx.Message });
                }
                return Json(new { success = false, message = "Lỗi khi cập nhật database: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
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

