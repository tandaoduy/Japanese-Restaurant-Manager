using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Areas.Admin_65133141.Data.Form;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class EmployeeController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/Employee
        public ActionResult Index(string searchString, string viewType = null, int page = 1, string sortBy = "code")
        {
            // pageSize: 5 for table view, 12 for card view (giống Customer)
            var effectiveViewType = string.IsNullOrEmpty(viewType) ? "table" : viewType;
            var pageSize = effectiveViewType == "card" ? 12 : 5;

            var accounts = db.NhanViens.AsQueryable();

            // Filter ONLY employees (exclude customers and admins)
            var customerRoleIds = db.VaiTroes
                .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" || 
                            r.TenVaiTro.ToLower().Trim() == "khach hang" || 
                            r.TenVaiTro.ToLower().Trim() == "user" || 
                            r.TenVaiTro.ToLower().Trim() == "customer")
                .Select(r => r.VaiTroID)
                .ToList();
            
            var adminRoleIds = db.VaiTroes
                .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" || 
                            r.TenVaiTro.ToLower().Trim() == "administrator" ||
                            r.TenVaiTro.ToLower().Trim().Contains("admin"))
                .Select(r => r.VaiTroID)
                .ToList();
            
            // Filter: not customer and not admin
            accounts = accounts.Where(a => 
                !customerRoleIds.Contains(a.VaiTroID) &&
                !adminRoleIds.Contains(a.VaiTroID) &&
                a.VaiTro != null &&
                !(a.VaiTro.TenVaiTro.ToLower().Trim() == "admin" || 
                  a.VaiTro.TenVaiTro.ToLower().Trim() == "administrator" ||
                  a.VaiTro.TenVaiTro.ToLower().Trim().Contains("admin")));

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                accounts = accounts.Where(a => 
                    a.HoTen.Contains(searchString) || 
                    a.Email.Contains(searchString) || 
                    a.SDT.Contains(searchString)
                );
            }

            // Get total count before pagination
            var totalCount = accounts.Count();

            // Sorting
            switch ((sortBy ?? "code").ToLower())
            {
                case "name":
                    accounts = accounts.OrderBy(a => a.HoTen);
                    break;
                case "code":
                default:
                    accounts = accounts.OrderBy(a => a.NhanVienID);
                    break;
            }

            // Pagination
            var accountList = accounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            ViewBag.TotalAccounts = totalCount;
            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalCount;
            ViewBag.ViewType = effectiveViewType;
            ViewBag.SortBy = sortBy;

            return View(accountList);
        }

        // GET: Admin_65133141/Employee/Create
        public ActionResult Create()
        {
            // Only show employee roles (exclude customer and admin roles)
            var customerRoleIds = db.VaiTroes
                .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" || 
                            r.TenVaiTro.ToLower().Trim() == "khach hang" || 
                            r.TenVaiTro.ToLower().Trim() == "user" || 
                            r.TenVaiTro.ToLower().Trim() == "customer")
                .Select(r => r.VaiTroID)
                .ToList();
            
            var adminRoleIds = db.VaiTroes
                .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" || 
                            r.TenVaiTro.ToLower().Trim() == "administrator" ||
                            r.TenVaiTro.ToLower().Trim().Contains("admin"))
                .Select(r => r.VaiTroID)
                .ToList();
            
            // Get only employee roles
            var employeeRoles = db.VaiTroes
                .Where(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID))
                .ToList();
            
            ViewBag.Roles = new SelectList(employeeRoles, "id", "ten_vai_tro");
            // Note: chi_nhanh is not in the database model, so Branches dropdown is disabled
            // ViewBag.Branches = new SelectList(db.chi_nhanh.ToList(), "id", "ten_chi_nhanh");
            return View(new AccountForm());
        }

        // POST: Admin_65133141/Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AccountForm model)
        {
            // Validate role - only allow employee roles
            var customerRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" || 
                            r.TenVaiTro.ToLower().Trim() == "khach hang" || 
                            r.TenVaiTro.ToLower().Trim() == "user" || 
                            r.TenVaiTro.ToLower().Trim() == "customer")
                .Select(r => r.VaiTroID)
                .ToList();
            
            var adminRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" || 
                            r.TenVaiTro.ToLower().Trim() == "administrator" ||
                            r.TenVaiTro.ToLower().Trim().Contains("admin"))
                .Select(r => r.VaiTroID)
                .ToList();
            
            // Lấy danh sách vai trò nhân viên (không bao gồm khách hàng và admin)
            var employeeRoles = db.VaiTroes
                .Where(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID))
                .ToList();

            // Nếu form không gửi RoleId (do đã ẩn dropdown) thì gán mặc định vai trò nhân viên đầu tiên
            if (model.RoleId == 0 && employeeRoles.Any())
            {
                model.RoleId = employeeRoles.First().VaiTroID;
            }

            // Nếu không gửi trạng thái thì mặc định "Hoạt động"
            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "Hoạt động";
            }
            
            if (customerRoleIds.Contains(model.RoleId))
            {
                ModelState.AddModelError("RoleId", "Không thể tạo nhân viên với vai trò khách hàng");
            }
            else if (adminRoleIds.Contains(model.RoleId))
            {
                ModelState.AddModelError("RoleId", "Không thể tạo nhân viên với vai trò admin");
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (db.NhanViens.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                }

                // Check if phone number already exists
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.NhanViens.Any(u => u.SDT == model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng");
                }

                // Validate password is required for new account
                if (string.IsNullOrEmpty(model.Password))
                {
                    ModelState.AddModelError("Password", "Mật khẩu là bắt buộc khi tạo tài khoản mới");
                }

                if (ModelState.IsValid)
                {
                    // Hash password
                    string hashedPassword = null;
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
                        {
                            byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password));
                            System.Text.StringBuilder builder = new System.Text.StringBuilder();
                            for (int i = 0; i < bytes.Length; i++)
                            {
                                builder.Append(bytes[i].ToString("x2"));
                            }
                            hashedPassword = builder.ToString();
                        }
                    }

                    var newUser = new NhanVien
                    {
                        HoTen = model.FullName,
                        Email = model.Email,
                        SDT = model.PhoneNumber,
                        // Lưu ngày sinh nếu có
                        NgaySinh = model.DateOfBirth,
                        MatKhau = hashedPassword,
                        VaiTroID = model.RoleId,
                        TaiKhoan = model.Email, // Use email as username
                        NgayVaoLam = model.StartDate ?? DateTime.Now,
                        TrangThai = model.Status ?? "Hoạt động",
                        DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null
                    };

                    try
                    {
                        db.NhanViens.Add(newUser);
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Tạo nhân viên thành công!";
                        return RedirectToAction("Index");
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        var errorMessages = new List<string>();
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                errorMessages.Add($"Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");
                            }
                        }
                        ModelState.AddModelError("", "Lỗi khi tạo nhân viên: " + string.Join("; ", errorMessages));
                    }
                    catch (Exception ex)
                    {
                        var innerException = ex.InnerException?.Message ?? ex.Message;
                        ModelState.AddModelError("", "Lỗi khi tạo nhân viên: " + innerException);
                    }
                }
            }

            // Reload employee roles only
            ViewBag.Roles = new SelectList(employeeRoles, "id", "ten_vai_tro", model.RoleId);
            // Note: chi_nhanh is not in the database model, so Branches dropdown is disabled
            // ViewBag.Branches = new SelectList(db.chi_nhanh.ToList(), "id", "ten_chi_nhanh", model.BranchId);
            return View(model);
        }


        // GET: Admin_65133141/Employee/Details/5
        public ActionResult Details(long id, string viewType = null, int page = 1, string searchString = null, string statusFilter = null)
        {
            var account = db.NhanViens
                .AsNoTracking()
                .FirstOrDefault(a => a.NhanVienID == id);
            
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Index", new { viewType = viewType, page = page, searchString = searchString, statusFilter = statusFilter });
            }

            var model = new AccountForm
            {
                Id = account.id,
                FullName = account.ho_ten,
                Email = account.email,
                PhoneNumber = account.so_dien_thoai,
                RoleId = account.vai_tro_id,
                // Map ngày sinh từ entity sang form
                DateOfBirth = account.NgaySinh,
                StartDate = account.ngay_vao_lam,
                Status = account.trang_thai,
                Password = account.mat_khau,
                Address = account.DiaChi
            };

            ViewBag.Address = account.DiaChi ?? "-";
            // Ngày sinh lấy trực tiếp từ model (đã map từ account.NgaySinh)
            ViewBag.DateOfBirth = model.DateOfBirth;
            ViewBag.PlaceOfBirth = null; // Có thể lấy từ database nếu có trường NoiSinh
            ViewBag.ViewType = viewType ?? "table";
            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;

            return View(model);
        }

        // GET: Admin_65133141/Employee/Edit/5
        public ActionResult Edit(long id)
        {
            var account = db.NhanViens.Find(id);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Index");
            }

            var model = new AccountForm
            {
                Id = account.id,
                FullName = account.ho_ten,
                Email = account.email,
                PhoneNumber = account.so_dien_thoai,
                RoleId = account.vai_tro_id,
                // Map ngày sinh để có thể chỉnh sửa
                DateOfBirth = account.NgaySinh,
                StartDate = account.ngay_vao_lam,
                Status = account.trang_thai,
                Address = account.DiaChi
            };

            // Only show employee roles
            var customerRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" || 
                            r.TenVaiTro.ToLower().Trim() == "khach hang" || 
                            r.TenVaiTro.ToLower().Trim() == "user" || 
                            r.TenVaiTro.ToLower().Trim() == "customer")
                .Select(r => r.VaiTroID)
                .ToList();
            
            var adminRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" || 
                            r.TenVaiTro.ToLower().Trim() == "administrator" ||
                            r.TenVaiTro.ToLower().Trim().Contains("admin"))
                .Select(r => r.VaiTroID)
                .ToList();
            
            var employeeRoles = db.vai_tro
                .Where(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID))
                .ToList();

            ViewBag.Roles = new SelectList(employeeRoles, "id", "ten_vai_tro", model.RoleId);
            // Note: chi_nhanh is not in the database model, so Branches dropdown is disabled
            // ViewBag.Branches = new SelectList(db.chi_nhanh.ToList(), "id", "ten_chi_nhanh", model.BranchId);
            return View(model);
        }

        // POST: Admin_65133141/Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AccountForm model)
        {
            if (ModelState.IsValid)
            {
                var account = db.NhanViens.Find(model.Id);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction("Index");
                }

                // Check if email already exists (excluding current user)
                if (db.NhanViens.Any(u => u.Email == model.Email && u.NhanVienID != model.Id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                }

                // Check if phone number already exists (excluding current user)
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.NhanViens.Any(u => u.SDT == model.PhoneNumber && u.NhanVienID != model.Id))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng");
                }

                if (ModelState.IsValid)
                {
                    account.ho_ten = model.FullName;
                    account.email = model.Email;
                    account.so_dien_thoai = model.PhoneNumber;
                    account.vai_tro_id = model.RoleId;
                    account.ngay_vao_lam = model.StartDate;
                    account.trang_thai = model.Status;
                    account.DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null;

                    // Update password if provided
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
                        {
                            byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password));
                            System.Text.StringBuilder builder = new System.Text.StringBuilder();
                            for (int i = 0; i < bytes.Length; i++)
                            {
                                builder.Append(bytes[i].ToString("x2"));
                            }
                            account.mat_khau = builder.ToString();
                        }
                    }

                    try
                    {
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi khi cập nhật nhân viên: " + ex.Message);
                    }
                }
            }

            // Reload employee roles
            var customerRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" || 
                            r.TenVaiTro.ToLower().Trim() == "khach hang" || 
                            r.TenVaiTro.ToLower().Trim() == "user" || 
                            r.TenVaiTro.ToLower().Trim() == "customer")
                .Select(r => r.VaiTroID)
                .ToList();
            
            var adminRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" || 
                            r.TenVaiTro.ToLower().Trim() == "administrator" ||
                            r.TenVaiTro.ToLower().Trim().Contains("admin"))
                .Select(r => r.VaiTroID)
                .ToList();
            
            var employeeRoles = db.vai_tro
                .Where(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID))
                .ToList();

            ViewBag.Roles = new SelectList(employeeRoles, "id", "ten_vai_tro", model.RoleId);
            // Note: chi_nhanh is not in the database model, so Branches dropdown is disabled
            // ViewBag.Branches = new SelectList(db.chi_nhanh.ToList(), "id", "ten_chi_nhanh", model.BranchId);
            return View(model);
        }

        // GET: Admin_65133141/Employee/DisableUser/5 (handle GET requests)
        [HttpGet]
        [ActionName("DisableUser")]
        public ActionResult DisableUserGet(long id)
        {
            // If accessed via GET, redirect to Index with message
            TempData["ErrorMessage"] = "Vui lòng sử dụng nút vô hiệu hóa/kích hoạt trên trang danh sách.";
            return RedirectToAction("Index");
        }

        // POST: Admin_65133141/Employee/DisableUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DisableUser")]
        public ActionResult DisableUserPost(long id)
        {
            var account = db.NhanViens.Find(id);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Index");
            }

            // Toggle status
            account.TrangThai = account.TrangThai == "Hoạt động" ? "Vô hiệu hóa" : "Hoạt động";
            
            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Đã {(account.TrangThai == "Hoạt động" ? "kích hoạt" : "vô hiệu hóa")} nhân viên thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật trạng thái: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Admin_65133141/Employee/Delete/5
        public ActionResult Delete(long id)
        {
            var account = db.nhan_vien.Find(id);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Index");
            }

            var model = new AccountForm
            {
                Id = account.id,
                FullName = account.ho_ten,
                Email = account.email,
                PhoneNumber = account.so_dien_thoai,
                RoleId = account.vai_tro_id,
                BranchId = account.chi_nhanh_id,
                Salary = account.luong,
                StartDate = account.ngay_vao_lam,
                Status = account.trang_thai
            };

            ViewBag.RoleName = account.vai_tro?.ten_vai_tro ?? "-";
            // Note: chi_nhanh is not in the database model
            ViewBag.BranchName = "-";

            return View(model);
        }

        // POST: Admin_65133141/Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id, string viewType = null, int page = 1, string searchString = null, string statusFilter = null)
        {
            var account = db.nhan_vien.Find(id);
            if (account == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên!" });
                }
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Index", new { viewType = viewType, page = page, searchString = searchString, statusFilter = statusFilter });
            }

            try
            {
                db.NhanViens.Remove(account);
                db.SaveChanges();
                
                if (Request.IsAjaxRequest())
                {
                    return Json(new { 
                        success = true, 
                        message = "Xóa nhân viên thành công!" 
                    });
                }
                
                TempData["SuccessMessage"] = "Xóa nhân viên thành công!";
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Lỗi khi xóa nhân viên: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Lỗi khi xóa nhân viên: " + ex.Message;
            }

            return RedirectToAction("Index", new { viewType = viewType ?? "table", page = page, searchString = searchString, statusFilter = statusFilter });
        }

        // GET: Admin_65133141/Employee/GetEmployeeDetails/5 (AJAX)
        [HttpGet]
        public JsonResult GetEmployeeDetails(long id)
        {
            try
            {
                var employee = db.NhanViens
                    .AsNoTracking()
                    .FirstOrDefault(e => e.NhanVienID == id);

                if (employee == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên!" }, JsonRequestBehavior.AllowGet);
                }

                var employeeCode = "NV" + employee.NhanVienID.ToString("D5");

                var employeeData = new
                {
                    employeeCode = employeeCode,
                    fullName = employee.HoTen ?? "-",
                    email = employee.Email ?? "-",
                    phoneNumber = employee.SDT ?? "-",
                    dateOfBirth = employee.NgaySinh.HasValue ? employee.NgaySinh.Value.ToString("dd/MM/yyyy") : "-",
                    startDate = employee.NgayVaoLam.HasValue ? employee.NgayVaoLam.Value.ToString("dd/MM/yyyy") : "-",
                    address = employee.DiaChi ?? "-",
                    passwordHash = employee.MatKhau ?? ""
                };

                return Json(new { success = true, employee = employeeData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải thông tin nhân viên: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin_65133141/Employee/CreateEmployeeAjax (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateEmployeeAjax(AccountForm model)
        {
            try
            {
                // Validate role - only allow employee roles
                var customerRoleIds = db.VaiTroes
                    .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" ||
                                r.TenVaiTro.ToLower().Trim() == "khach hang" ||
                                r.TenVaiTro.ToLower().Trim() == "user" ||
                                r.TenVaiTro.ToLower().Trim() == "customer")
                    .Select(r => r.VaiTroID)
                    .ToList();

                var adminRoleIds = db.VaiTroes
                    .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" ||
                                r.TenVaiTro.ToLower().Trim() == "administrator" ||
                                r.TenVaiTro.ToLower().Trim().Contains("admin"))
                    .Select(r => r.VaiTroID)
                    .ToList();

                // Get employee roles (not customer or admin)
                var employeeRoles = db.VaiTroes
                    .Where(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID))
                    .ToList();

                // If no RoleId provided, assign first employee role
                if (model.RoleId == 0 && employeeRoles.Any())
                {
                    model.RoleId = employeeRoles.First().VaiTroID;
                }

                // Default status if not provided
                if (string.IsNullOrEmpty(model.Status))
                {
                    model.Status = "Hoạt động";
                }

                // Validation errors dictionary
                var errors = new Dictionary<string, string>();

                // Basic validation
                if (string.IsNullOrEmpty(model.FullName))
                {
                    errors["FullName"] = "Họ tên là bắt buộc";
                }

                if (string.IsNullOrEmpty(model.Email))
                {
                    errors["Email"] = "Email là bắt buộc";
                }
                else if (db.NhanViens.Any(u => u.Email == model.Email))
                {
                    errors["Email"] = "Email này đã được sử dụng";
                }

                if (string.IsNullOrEmpty(model.PhoneNumber))
                {
                    errors["PhoneNumber"] = "Số điện thoại là bắt buộc";
                }
                else if (db.NhanViens.Any(u => u.SDT == model.PhoneNumber))
                {
                    errors["PhoneNumber"] = "Số điện thoại này đã được sử dụng";
                }

                if (!model.DateOfBirth.HasValue)
                {
                    errors["DateOfBirth"] = "Ngày sinh là bắt buộc";
                }

                if (!model.StartDate.HasValue)
                {
                    errors["StartDate"] = "Ngày vào làm là bắt buộc";
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    errors["Password"] = "Mật khẩu là bắt buộc";
                }
                else if (model.Password.Length < 8)
                {
                    errors["Password"] = "Mật khẩu phải có ít nhất 8 ký tự";
                }

                if (string.IsNullOrEmpty(model.ConfirmPassword))
                {
                    errors["ConfirmPassword"] = "Xác nhận mật khẩu là bắt buộc";
                }
                else if (model.Password != model.ConfirmPassword)
                {
                    errors["ConfirmPassword"] = "Mật khẩu xác nhận không khớp";
                }

                if (errors.Any())
                {
                    return Json(new { success = false, message = "Vui lòng kiểm tra lại thông tin", errors = errors });
                }

                // Hash password
                string hashedPassword = null;
                if (!string.IsNullOrEmpty(model.Password))
                {
                    using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password));
                        System.Text.StringBuilder builder = new System.Text.StringBuilder();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            builder.Append(bytes[i].ToString("x2"));
                        }
                        hashedPassword = builder.ToString();
                    }
                }

                var newEmployee = new NhanVien
                {
                    HoTen = model.FullName,
                    Email = model.Email,
                    SDT = model.PhoneNumber,
                    NgaySinh = model.DateOfBirth,
                    MatKhau = hashedPassword,
                    VaiTroID = model.RoleId,
                    TaiKhoan = model.Email, // Use email as username
                    NgayVaoLam = model.StartDate ?? DateTime.Now,
                    TrangThai = model.Status ?? "Hoạt động",
                    DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null
                };

                db.NhanViens.Add(newEmployee);
                db.SaveChanges();

                return Json(new { success = true, message = "Tạo nhân viên thành công!" });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = new List<string>();
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");
                    }
                }
                return Json(new { success = false, message = "Lỗi khi tạo nhân viên: " + string.Join("; ", errorMessages) });
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi khi tạo nhân viên: " + innerException });
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
