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

            var accounts = db.nhan_vien.AsQueryable();

            // Filter ONLY employees (exclude customers and admins)
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
            
            // Get only employee roles
            var employeeRoles = db.vai_tro
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
                if (db.nhan_vien.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                }

                // Check if phone number already exists
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.nhan_vien.Any(u => u.SDT == model.PhoneNumber))
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
                        MatKhau = hashedPassword,
                        VaiTroID = model.RoleId,
                        TaiKhoan = model.Email, // Use email as username
                        NgayVaoLam = model.StartDate ?? DateTime.Now,
                        TrangThai = model.Status ?? "Hoạt động",
                        DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null
                    };

                    try
                    {
                        db.nhan_vien.Add(newUser);
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
            var employeeRoles = db.vai_tro
                .Where(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID))
                .ToList();
            
            ViewBag.Roles = new SelectList(employeeRoles, "id", "ten_vai_tro", model.RoleId);
            // Note: chi_nhanh is not in the database model, so Branches dropdown is disabled
            // ViewBag.Branches = new SelectList(db.chi_nhanh.ToList(), "id", "ten_chi_nhanh", model.BranchId);
            return View(model);
        }


        // GET: Admin_65133141/Employee/Details/5
        public ActionResult Details(long id, string viewType = null, int page = 1, string searchString = null, string statusFilter = null)
        {
            var account = db.nhan_vien
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
                StartDate = account.ngay_vao_lam,
                Status = account.trang_thai,
                Password = account.mat_khau,
                Address = account.DiaChi
            };

            ViewBag.RoleName = account.vai_tro?.ten_vai_tro ?? "-";
            ViewBag.Address = account.DiaChi ?? "-";
            ViewBag.ViewType = viewType ?? "table";
            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;

            return View(model);
        }

        // GET: Admin_65133141/Employee/Edit/5
        public ActionResult Edit(long id)
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
                var account = db.nhan_vien.Find(model.Id);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction("Index");
                }

                // Check if email already exists (excluding current user)
                if (db.nhan_vien.Any(u => u.Email == model.Email && u.NhanVienID != model.Id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                }

                // Check if phone number already exists (excluding current user)
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.nhan_vien.Any(u => u.SDT == model.PhoneNumber && u.NhanVienID != model.Id))
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
            var account = db.nhan_vien.Find(id);
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
                db.nhan_vien.Remove(account);
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

        // GET: Admin_65133141/Employee/GetEmployeeDetails/5
        public JsonResult GetEmployeeDetails(long id)
        {
            try
            {
                var employee = db.nhan_vien.Find(id);
                if (employee == null) 
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" }, JsonRequestBehavior.AllowGet);

                var data = new
                {
                    Id = employee.id,
                    employee.ho_ten,
                    FullName = employee.ho_ten,
                    employee.email,
                    Email = employee.email,
                    PhoneNumber = employee.so_dien_thoai,
                    Address = employee.DiaChi,
                    DateOfBirth = employee.ngay_sinh.HasValue ? employee.ngay_sinh.Value.ToString("yyyy-MM-dd") : (string)null,
                    StartDate = employee.ngay_vao_lam.HasValue ? employee.ngay_vao_lam.Value.ToString("yyyy-MM-dd") : (string)null,
                    RoleId = employee.vai_tro_id,
                    RoleName = employee.vai_tro?.ten_vai_tro ?? null,
                    Status = employee.trang_thai,
                    Password = employee.mat_khau
                };

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Admin_65133141/Employee/GetEmployeeRoles
        public JsonResult GetEmployeeRoles()
        {
            try
            {
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
                    .Select(r => new { id = r.id, name = r.ten_vai_tro })
                    .ToList();

                return Json(new { success = true, roles = employeeRoles }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Admin_65133141/Employee/EditEmployeeAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditEmployeeAjax(AccountForm model)
        {
            try
            {
                if (model == null || model.Id == 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var employee = db.nhan_vien.Find(model.Id);
                if (employee == null) 
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" });

                // Clear ModelState errors for fields we're not validating
                ModelState.Remove("ConfirmPassword");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors });
                }

                // Check if email already exists (excluding current user)
                if (db.nhan_vien.Any(u => u.Email == model.Email && u.NhanVienID != model.Id))
                {
                    return Json(new { success = false, message = "Email này đã được sử dụng" });
                }

                // Check if phone number already exists (excluding current user)
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.nhan_vien.Any(u => u.SDT == model.PhoneNumber && u.NhanVienID != model.Id))
                {
                    return Json(new { success = false, message = "Số điện thoại này đã được sử dụng" });
                }

                // Update fields
                employee.ho_ten = model.FullName;
                employee.email = model.Email;
                employee.so_dien_thoai = model.PhoneNumber;
                employee.vai_tro_id = model.RoleId;
                employee.ngay_vao_lam = model.StartDate;
                employee.trang_thai = model.Status;
                employee.DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null;
                employee.ngay_sinh = model.DateOfBirth;

                // Update password if provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    if (model.Password != model.ConfirmPassword)
                    {
                        return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
                    }

                    using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Password));
                        System.Text.StringBuilder builder = new System.Text.StringBuilder();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            builder.Append(bytes[i].ToString("x2"));
                        }
                        employee.mat_khau = builder.ToString();
                    }
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật nhân viên thành công!" });
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

        // POST: Admin_65133141/Employee/CreateEmployeeAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateEmployeeAjax(AccountForm model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Clear ModelState errors for fields we're not validating
                ModelState.Remove("ConfirmPassword");
                ModelState.Remove("Id");

                // Basic validation
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ModelState.AddModelError("FullName", "Họ tên là bắt buộc");
                }
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Email là bắt buộc");
                }
                if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại là bắt buộc");
                }
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError("Password", "Mật khẩu là bắt buộc");
                }

                // Get default employee role (exclude customer/admin roles)
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
                
                var defaultEmployeeRole = db.vai_tro
                    .FirstOrDefault(r => !customerRoleIds.Contains(r.VaiTroID) && !adminRoleIds.Contains(r.VaiTroID));
                
                if (defaultEmployeeRole == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vai trò nhân viên mặc định trong hệ thống." });
                }
                
                // Set default role
                model.RoleId = defaultEmployeeRole.VaiTroID;

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }

                // Check if email already exists
                if (db.nhan_vien.Any(u => u.Email == model.Email))
                {
                    return Json(new { success = false, message = "Email này đã được sử dụng" });
                }

                // Check if phone number already exists
                if (!string.IsNullOrEmpty(model.PhoneNumber) && db.nhan_vien.Any(u => u.SDT == model.PhoneNumber))
                {
                    return Json(new { success = false, message = "Số điện thoại này đã được sử dụng" });
                }

                // Validate password
                if (string.IsNullOrEmpty(model.Password))
                {
                    return Json(new { success = false, message = "Mật khẩu là bắt buộc khi tạo tài khoản mới" });
                }

                if (model.Password != model.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
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

                var newUser = new NhanVien
                {
                    HoTen = model.FullName,
                    Email = model.Email,
                    SDT = model.PhoneNumber,
                    MatKhau = hashedPassword,
                    VaiTroID = defaultEmployeeRole.VaiTroID,
                    TaiKhoan = model.Email,
                    NgayVaoLam = model.StartDate ?? DateTime.Now,
                    TrangThai = "Hoạt động",
                    DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address : null,
                    NgaySinh = model.DateOfBirth
                };

                try
                {
                    db.nhan_vien.Add(newUser);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Tạo nhân viên thành công!" });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    var errorMessages = new List<string>();
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            errorMessages.Add($"{validationError.PropertyName}: {validationError.ErrorMessage}");
                        }
                    }
                    return Json(new { success = false, message = "Lỗi validation: " + string.Join("; ", errorMessages) });
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    var innerException = ex.InnerException?.InnerException;
                    if (innerException is System.Data.SqlClient.SqlException sqlEx)
                    {
                        return Json(new { success = false, message = "Lỗi database: " + sqlEx.Message + (sqlEx.Number > 0 ? " (Error " + sqlEx.Number + ")" : "") });
                    }
                    var innerMsg = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, message = "Lỗi khi cập nhật database: " + innerMsg });
                }
                catch (Exception ex)
                {
                    var innerMsg = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, message = "Lỗi khi tạo nhân viên: " + innerMsg });
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Lỗi server: " + innerMsg });
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
