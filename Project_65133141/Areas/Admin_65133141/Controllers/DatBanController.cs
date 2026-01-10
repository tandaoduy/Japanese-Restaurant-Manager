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
        /// Tính trạng thái bàn, nhưng bỏ qua một đặt bàn cụ thể (dùng khi chỉnh sửa đặt bàn)
        /// </summary>
        private string GetTableStatusFromDatBanExcluding(long banID, long? excludeDatBanId)
        {
            var now = DateTime.Now;
            
            // Lấy tất cả đặt bàn của bàn này (trừ những cái đã hoàn thành, hủy, hoặc đang chỉnh sửa)
            var activeReservations = db.DatBans
                .Where(d => d.BanID == banID && 
                           d.TrangThai != "Hoàn thành" && 
                           d.TrangThai != "Đã hủy" &&
                           (!excludeDatBanId.HasValue || d.DatBanID != excludeDatBanId.Value))
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
                var thoiGianKetThuc = thoiGianDen.AddHours(2);
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
            const int pageSize = 5;
            
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

            // Order by DatBanID descending (newest first)
            var datBans = query
                .OrderByDescending(d => d.DatBanID)
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

            ViewBag.BaseUrl = Url.Action("Index", "DatBan", new
            {
                area = "Admin_65133141",
                searchString = searchString,
                statusFilter = statusFilter
            });

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

                    // --- FORCE SEND EMAIL ---
                    if (datBan.TrangThai == "Đã xác nhận" && datBan.UserID.HasValue)
                    {
                        try
                        {
                            var user = db.Users.Find(datBan.UserID.Value);
                            if (user != null && !string.IsNullOrEmpty(user.Email) && user.Email.Contains("@"))
                            {
                                // Load table info for email
                                if (datBan.BanID.HasValue && datBan.BanAn == null)
                                {
                                    datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                                }

                                var emailService = new EmailService();
                                var customerName = string.IsNullOrEmpty(datBan.HoTenKhach) ? user.HoTen : datBan.HoTenKhach;
                                emailService.SendBookingConfirmationEmail(
                                    datBan,
                                    user.Email,
                                    customerName,
                                    isConfirmed: true
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Email error: " + ex.Message);
                        }
                    }
                    // -------------------------

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
                
                System.Diagnostics.Debug.WriteLine($"[Confirm] DatBanID: {id}, UserID: {datBan.UserID}");
                
                if (datBan.UserID.HasValue)
                {
                    var user = db.Users.Find(datBan.UserID.Value);
                    System.Diagnostics.Debug.WriteLine($"[Confirm] Found User: {user != null}, Email: {user?.Email}");
                    
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        customerEmail = user.Email;
                        if (string.IsNullOrEmpty(customerName))
                        {
                            customerName = user.HoTen;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[Confirm] Customer Email: {customerEmail}, Name: {customerName}");

                // Validation: Check if table is occupied by others
                if (datBan.BanID.HasValue)
                {
                    var banID = datBan.BanID.Value;
                    var conflict = db.DatBans.Any(d => 
                        d.BanID == banID && 
                        d.DatBanID != datBan.DatBanID &&
                        d.TrangThai != "Hoàn thành" && d.TrangThai != "Đã hủy" && // Only check active bookings
                        (d.TrangThai == "Đã đặt" || d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đang phục vụ" || d.TrangThai == "Đang sử dụng"));
                    
                    if (conflict)
                    {
                        var ban = db.BanAns.Find(banID);
                        var status = ban?.TrangThai ?? "Đã đặt";
                        if (Request.IsAjaxRequest()) 
                        {
                            return Json(new { success = false, message = $"Bàn {ban?.TenBan} đang ở trạng thái '{status}', không thể xác nhận!" });
                        }
                        TempData["ErrorMessage"] = $"Bàn {ban?.TenBan} đang ở trạng thái '{status}', không thể xác nhận!";
                        return RedirectToAction("Index");
                    }
                }

                // Cập nhật trạng thái thành "Đã xác nhận"
                datBan.TrangThai = "Đã xác nhận";
                db.SaveChanges();

                // Cập nhật trạng thái bàn nếu có
                if (datBan.BanID.HasValue)
                {
                    UpdateTableStatusFromDatBan(datBan.BanID.Value);
                    db.SaveChanges();
                }

                // --- FORCE SEND EMAIL (Confirm) ---
                bool emailSent = false;
                string emailStatus = "";
                
                if (!string.IsNullOrEmpty(customerEmail) && customerEmail.Contains("@"))
                {
                    try
                    {
                        // Enable Debug Logging
                        System.Diagnostics.Debug.WriteLine($"[Confirm] Attempting to send email to: {customerEmail}");
                        
                        // Ensure Table is loaded for email template
                        if (datBan.BanID.HasValue && datBan.BanAn == null)
                        {
                            datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                        }

                        var emailService = new EmailService();
                        emailSent = emailService.SendBookingConfirmationEmail(
                            datBan, 
                            customerEmail, 
                            customerName, 
                            isConfirmed: true
                        );
                        
                        if (emailSent)
                        {
                            emailStatus = $" Email xác nhận đã được gửi đến {customerEmail}.";
                        }
                        else
                        {
                            emailStatus = " (Email không gửi được - kiểm tra cấu hình SMTP)";
                        }
                    }
                    catch (Exception emailEx)
                    {
                        emailStatus = " (Lỗi gửi email: " + emailEx.Message + ")";
                        System.Diagnostics.Debug.WriteLine($"[Confirm] Error sending email: {emailEx.Message}");
                    }
                }
                else
                {
                    // Log why we didn't send
                    System.Diagnostics.Debug.WriteLine("[Confirm] No email to send to.");
                }
                // ---------------------------------

                string successMessage = "Xác nhận đặt bàn thành công!" + emailStatus;

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = successMessage, emailSent = emailSent });
                }

                TempData["SuccessMessage"] = successMessage;
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
                
                // Hiển thị tất cả bàn bình thường, không lọc, không thêm status
                foreach (var table in allTables)
                {
                    availableTables.Add(new
                    {
                        BanID = table.BanID,
                        DisplayText = table.TenBan + 
                                     (string.IsNullOrEmpty(table.ViTri) ? "" : " (" + table.ViTri + ")") +
                                     (table.SucChua.HasValue ? " - " + table.SucChua + " người" : ""),
                        SucChua = table.SucChua ?? 0
                    });
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

                // Validation: Kiểm tra bàn có đang bị chiếm bởi đặt bàn khác không
                if (model.BanID.HasValue)
                {
                    // Lấy trạng thái thực tế của bàn, bỏ qua đặt bàn hiện tại
                    var targetTableStatus = GetTableStatusFromDatBanExcluding(model.BanID.Value, model.DatBanID);
                    
                    if (targetTableStatus == "Đang phục vụ" || targetTableStatus == "Đã đặt")
                    {
                        var ban = db.BanAns.Find(model.BanID);
                        return Json(new { 
                            success = false, 
                            message = $"Bàn '{ban?.TenBan ?? "đã chọn"}' đang ở trạng thái [{targetTableStatus}]. Vui lòng chọn bàn khác!" 
                        });
                    }
                }

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

                // --- FORCE SEND EMAIL (Ajax) ---
                bool emailSent = false;
                string emailStatus = " (Chưa gửi)";
                
                if (datBan.TrangThai == "Đã xác nhận" && datBan.UserID.HasValue)
                {
                    try
                    {
                        var user = db.Users.Find(datBan.UserID.Value);
                        if (user != null && !string.IsNullOrEmpty(user.Email) && user.Email.Contains("@"))
                        {
                            // Explicitly load BanAn to ensure we have Table Name
                            if (datBan.BanID.HasValue)
                            {
                                datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                            }

                            var emailService = new EmailService();
                            var customerName = string.IsNullOrEmpty(datBan.HoTenKhach) ? user.HoTen : datBan.HoTenKhach;
                            emailSent = emailService.SendBookingConfirmationEmail(
                                datBan,
                                user.Email,
                                customerName,
                                isConfirmed: true
                            );

                            emailStatus = emailSent ? " (Đã gửi email)" : " (Không gửi được email)";
                            System.Diagnostics.Debug.WriteLine($"[Ajax Info] Email sent: {emailSent} to {user.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        emailStatus = " (Lỗi email: " + emailEx.Message + ")";
                        System.Diagnostics.Debug.WriteLine($"[Ajax Error] Email failed: {emailEx.Message}");
                    }
                }
                // ------------------------------

                return Json(new { success = true, message = "Cập nhật đặt bàn thành công!" + emailStatus, emailSent = emailSent });
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

