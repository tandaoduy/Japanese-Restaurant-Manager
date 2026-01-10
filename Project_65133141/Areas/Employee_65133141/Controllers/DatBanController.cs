using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using Project_65133141.Services;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee")]
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

        // POST: Employee_65133141/DatBan/Create (Offline Reservation)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string customerName, string customerPhone, string customerEmail, DatBan datBan)
        {
            if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(customerPhone))
            {
                TempData["ErrorMessage"] = "Tên khách hàng và số điện thoại là bắt buộc.";
                return RedirectToAction("Index");
            }

            try
            {
                // Validation: Check if table is busy
                if (datBan.BanID.HasValue)
                {
                    // Check logic status
                    var status = GetTableStatusFromDatBanExcluding(datBan.BanID.Value, null);
                    if (status == "Đang phục vụ" || status == "Đã đặt") 
                    {
                          var ban = db.BanAns.Find(datBan.BanID.Value);
                          TempData["ErrorMessage"] = $"Bàn {ban?.TenBan} đang ở trạng thái '{status}', không thể đặt!";
                          return RedirectToAction("Index");
                    }
                }

                // DatBan model stores customer info directly (no KhachHangID)
                datBan.HoTenKhach = customerName;
                datBan.SDTKhach = customerPhone;
                datBan.TrangThai = "Đã xác nhận"; // Auto-confirm for offline reservations
                datBan.NgayTao = DateTime.Now;
                datBan.UserID = null; // Offline reservations don't have UserID

                db.DatBans.Add(datBan);

                // Update table status if table was selected
                if (datBan.BanID.HasValue)
                {
                    UpdateTableStatusFromDatBan(datBan.BanID.Value);
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "Đặt bàn offline thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi đặt bàn: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // AJAX: Get available tables (only empty tables)
        [HttpGet]
        public JsonResult GetAvailableTables(DateTime arrivalTime, int? excludeReservationId = null)
        {
            try
            {
                var allTables = db.BanAns.OrderBy(b => b.TenBan).ToList();
                var availableTables = new List<object>();

                // Show ALL tables unconditionally
                foreach (var table in allTables)
                {
                    availableTables.Add(new { 
                        BanID = table.BanID, 
                        TenBan = table.TenBan,
                        ViTri = table.ViTri ?? "N/A",
                        SucChua = table.SucChua ?? 0,
                        Capacity = table.SucChua ?? 0
                    });
                }

                return Json(availableTables, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error and return empty array
                System.Diagnostics.Debug.WriteLine("GetAvailableTables Error: " + ex.Message);
                return Json(new object[] { }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Employee_65133141/DatBan
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

            if (Request.IsAjaxRequest())
            {
                return PartialView("_DatBanList", datBans);
            }

            return View(datBans);
        }

        // GET: Employee_65133141/DatBan/Details/5
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

        public ActionResult GetDatBanDetails(long id)
        {
            var datBan = db.DatBans.Find(id);
            if (datBan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đặt bàn!" }, JsonRequestBehavior.AllowGet);
            }

            string tenBan = null;
            int? sucChua = null;
            if (datBan.BanID.HasValue)
            {
                var ban = db.BanAns.Find(datBan.BanID.Value);
                if (ban != null)
                {
                    tenBan = ban.TenBan;
                    sucChua = ban.SucChua;
                }
            }

            string tenUser = null;
            string emailUser = null;
            if (datBan.UserID.HasValue)
            {
                var user = db.Users.Find(datBan.UserID.Value);
                if (user != null)
                {
                    tenUser = user.HoTen;
                    emailUser = user.Email;
                }
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    DatBanID = datBan.DatBanID,
                    TrangThai = datBan.TrangThai,
                    HoTenKhach = datBan.HoTenKhach,
                    SDTKhach = datBan.SDTKhach,
                    SoNguoi = datBan.SoNguoi,
                    ThoiGianDen = datBan.ThoiGianDen,
                    NgayTao = datBan.NgayTao,
                    GhiChu = datBan.GhiChu,
                    BanAn = tenBan,
                    SucChua = sucChua,
                    UserHoTen = tenUser,
                    UserEmail = emailUser
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var results = db.DatBans
                .Where(d => d.HoTenKhach.Contains(term) || d.SDTKhach.Contains(term))
                .OrderByDescending(d => d.ThoiGianDen)
                .Take(5)
                .Select(d => new
                {
                    d.HoTenKhach,
                    d.SDTKhach,
                    TenBan = d.BanAn != null ? d.BanAn.TenBan : ""
                })
                .ToList()
                .Select(x => new
                {
                    label = x.HoTenKhach, // Search result title
                    value = x.HoTenKhach, // Value to put in input
                    desc = (string.IsNullOrEmpty(x.TenBan) ? "" : "Bàn " + x.TenBan + " - ") + x.SDTKhach,
                    avatar = string.IsNullOrEmpty(x.HoTenKhach) ? "?" : x.HoTenKhach.Substring(0, 1).ToUpper()
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        // GET: Employee_65133141/DatBan/Edit/5
        public ActionResult Edit(long id)
        {
            // Block direct access to Edit page, force users to use modal
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult GetReservation(long id)
        {
            var datBan = db.DatBans.Find(id);
            if (datBan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đặt bàn!" }, JsonRequestBehavior.AllowGet);
            }

            // Get available tables
            var allTables = db.BanAns.OrderBy(b => b.TenBan).ToList();
            var availableTables = new List<object>();

            // Add "No Table" option or similar if allowed? usually required.
            // Logic similar to Edit GET
            // Show ALL tables unconditionally
            foreach (var table in allTables)
            {
                bool isCurrent = table.BanID == datBan.BanID;
                availableTables.Add(new { 
                    Value = table.BanID, 
                    Text = table.TenBan + " ( " + table.SucChua + " ghế )",
                    Capacity = table.SucChua ?? 0
                });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    datBan.DatBanID,
                    datBan.HoTenKhach,
                    datBan.SDTKhach,
                    BanID = datBan.BanID ?? 0,
                    ThoiGianDen = datBan.ThoiGianDen.ToString("yyyy-MM-ddTHH:mm"), // Format for datetime-local
                    SoNguoi = datBan.SoNguoi,
                    datBan.TrangThai, // For Select
                    datBan.GhiChu
                },
                tables = availableTables
            }, JsonRequestBehavior.AllowGet);
        }

        // POST: Employee_65133141/DatBan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(DatBan model)
        {
            if (ModelState.IsValid)
            {
                var datBan = db.DatBans.Find(model.DatBanID);
                if (datBan == null)
                {
                    if (Request.IsAjaxRequest())
                    {
                         return Json(new { success = false, message = "Không tìm thấy đặt bàn!" });
                    }
                    TempData["ErrorMessage"] = "Không tìm thấy đặt bàn!";
                    return RedirectToAction("Index");
                }

                // Lưu BanID cũ để cập nhật trạng thái bàn cũ
                var oldBanID = datBan.BanID;

                // Validation: Check if table is occupied by others
                // Validation: Check if table is occupied by others
                if (model.BanID.HasValue)
                {
                    var status = GetTableStatusFromDatBanExcluding(model.BanID.Value, model.DatBanID);
                    
                    if (status == "Đang phục vụ" || status == "Đã đặt")
                    {
                         var ban = db.BanAns.Find(model.BanID);
                         if (Request.IsAjaxRequest())
                         {
                              return Json(new { success = false, message = $"Bàn {ban?.TenBan} đang ở trạng thái '{status}', vui lòng chọn bàn khác!" });
                         }
                         TempData["ErrorMessage"] = $"Bàn {ban?.TenBan} đang ở trạng thái '{status}', vui lòng chọn bàn khác!";
                         return RedirectToAction("Index");
                    }
                }
                
                // Old logic kept commented out or removed for clarity


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

                    // Nếu trạng thái sau khi cập nhật là "Đã xác nhận" thì gửi email xác nhận cho khách (nếu có email)
                    if (datBan.TrangThai == "Đã xác nhận" && datBan.UserID.HasValue)
                    {
                        try
                        {
                            // Load thông tin user và bàn để email đầy đủ
                            var user = db.Users.Find(datBan.UserID.Value);
                            if (user != null && !string.IsNullOrEmpty(user.Email) && user.Email.Contains("@"))
                            {
                                if (datBan.BanID.HasValue && datBan.BanAn == null)
                                {
                                    datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                                }

                                var emailService = new EmailService();
                                var customerName = string.IsNullOrEmpty(datBan.HoTenKhach) ? user.HoTen : datBan.HoTenKhach;
                                var emailSent = emailService.SendBookingConfirmationEmail(
                                    datBan,
                                    user.Email,
                                    customerName,
                                    isConfirmed: true
                                );

                                if (!emailSent)
                                {
                                    System.Diagnostics.Debug.WriteLine("[Employee DatBanController] Không gửi được email xác nhận đặt bàn.");
                                }
                            }
                        }
                        catch (Exception emailEx)
                        {
                            // Log nhưng không làm fail việc cập nhật đặt bàn
                            System.Diagnostics.Debug.WriteLine("[Employee DatBanController] Lỗi khi gửi email xác nhận: " + emailEx.Message);
                        }
                    }

                    if (Request.IsAjaxRequest())
                    {
                         return Json(new { success = true, message = "Cập nhật đặt bàn thành công!" });
                    }

                    TempData["SuccessMessage"] = "Cập nhật đặt bàn thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    if (Request.IsAjaxRequest())
                    {
                         return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
                    }
                    ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                }
            }

            // Error case
             if (Request.IsAjaxRequest())
            {
                 var errors = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
                 return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            // Reload dropdowns on error - lấy tất cả bàn và tính trạng thái thực tế từ DatBan
            var allTables = db.BanAns.OrderBy(b => b.TenBan).ToList();
            var availableTablesNormal = new List<BanAn>();
            
            foreach (var table in allTables)
            {
                var actualStatus = GetTableStatusFromDatBan(table.BanID);
                // Cho phép chọn bàn trống hoặc bàn đang được chỉnh sửa
                if (actualStatus == "Trống" || table.BanID == model.BanID)
                {
                    availableTablesNormal.Add(table);
                }
            }

            ViewBag.BanID = new SelectList(
                availableTablesNormal,
                "BanID",
                "TenBan",
                model.BanID
            );

            var statuses = new List<string> { "Chờ xác nhận", "Đã xác nhận", "Đang phục vụ", "Hoàn thành", "Đã hủy" };
            ViewBag.TrangThai = new SelectList(statuses, model.TrangThai);

            return View(model);
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

