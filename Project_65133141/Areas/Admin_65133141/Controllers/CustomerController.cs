using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class CustomerController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();
        
        // POST: Admin_65133141/Customer/ResetDemoCustomers
        // Chỉ dùng cho môi trường demo: xoá toàn bộ khách hàng và tạo lại 50 khách hàng mẫu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetDemoCustomers()
        {
            try
            {
                // Xoá toàn bộ tài khoản khách hàng hiện tại trong bảng Users
                var customersToRemove = db.Users.ToList();

                if (customersToRemove.Any())
                {
                    db.Users.RemoveRange(customersToRemove);
                    db.SaveChanges();

                    // Reseed lại identity để mã khách hàng hiển thị từ 1
                    db.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Users', RESEED, 0)");
                }

                // Dữ liệu mẫu
                string[] firstNames = {
                    "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
                    "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Đào", "Mai", "Tạ"
                };

                string[] middleNames = {
                    "Văn", "Thị", "Đức", "Minh", "Thanh", "Hữu", "Công", "Quang", "Đình", "Xuân",
                    "Hồng", "Thu", "Lan", "Hương", "Phương", "Anh", "Bảo", "Gia", "Tuấn", "Duy"
                };

                string[] lastNames = {
                    "An", "Bình", "Chi", "Dũng", "Giang", "Hoa", "Hùng", "Khang", "Linh", "Mai",
                    "Nam", "Oanh", "Phúc", "Quân", "Sơn", "Thảo", "Uyên", "Việt", "Yến", "Anh",
                    "Bảo", "Cường", "Đức", "Gia", "Hạnh", "Khoa", "Long", "Minh", "Nga", "Phong",
                    "Đài", "Hải", "Khanh", "Lâm", "My", "Như", "Oanh", "Phương", "Quyên", "Sang"
                };

                // Địa chỉ từ Nha Trang và các tỉnh thành khác
                string[] cities = {
                    "Nha Trang, Khánh Hòa", "Cam Ranh, Khánh Hòa", "Ninh Hòa, Khánh Hòa", "TP. Hồ Chí Minh", "Hà Nội", "Đà Nẵng"
                };

                string[] streets = {
                    "Nguyễn Huệ", "Lê Lợi", "Trần Hưng Đạo", "Hai Bà Trưng", "Lý Thường Kiệt",
                    "Nguyễn Trãi", "Phạm Ngũ Lão", "Trần Phú", "Thống Nhất", "Yersin", "Hùng Vương"
                };

                var random = new Random();
                string rawPassword = "Duytan0712@";
                string hashedPassword = HashPassword(rawPassword);

                // Danh sách email đã tạo để tránh trùng lặp
                var createdEmails = new HashSet<string>();

                for (int i = 1; i <= 50; i++)
                {
                    string first = firstNames[random.Next(firstNames.Length)];
                    string middle = middleNames[random.Next(middleNames.Length)];
                    string last = lastNames[random.Next(lastNames.Length)];
                    string fullName = string.Format("{0} {1} {2}", first, middle, last);

                    // Tạo SĐT 10 số bắt đầu bằng 0
                    string phone = "0" + random.Next(100000000, 999999999).ToString();

                    // Địa chỉ
                    string street = streets[random.Next(streets.Length)];
                    int number = random.Next(1, 500);
                    string city = cities[random.Next(cities.Length)];
                    string address = string.Format("{0} {1}, {2}", number, street, city);

                    // Email dựa trên tên khách hàng
                    string emailBase = RemoveDiacritics(fullName).ToLower();
                    emailBase = new string(emailBase.Where(c => !char.IsWhiteSpace(c) && char.IsLetterOrDigit(c)).ToArray());
                    if (string.IsNullOrEmpty(emailBase))
                    {
                        emailBase = "khachhang" + i;
                    }

                    string email = emailBase + "@gmail.com";
                    int suffix = 1;
                    // Kiểm tra trong danh sách đã tạo và trong database
                    while (createdEmails.Contains(email) || db.Users.Any(u => u.Email == email))
                    {
                        email = emailBase + suffix + "@gmail.com";
                        suffix++;
                    }
                    createdEmails.Add(email);

                    var customer = new User
                    {
                        Username = email, // Username là email
                        HoTen = fullName,
                        Email = email,
                        SDT = phone,
                        Password = hashedPassword,
                        TrangThai = true, // Default active
                        DiaChi = address,
                        NgayTao = DateTime.Now,
                        DiemTichLuy = 0
                    };

                    db.Users.Add(customer);
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã reset và tạo lại 50 khách hàng demo với mật khẩu chung Duytan0712@.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi reset dữ liệu khách hàng: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

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

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Chuẩn hoá riêng ký tự đ/Đ trước (quan trọng: phải làm trước khi normalize)
            text = text.Replace('đ', 'd').Replace('Đ', 'D');

            // Bảng chuyển đổi các ký tự có dấu sang không dấu
            var vietnameseMap = new Dictionary<char, char>
            {
                {'à', 'a'}, {'á', 'a'}, {'ạ', 'a'}, {'ả', 'a'}, {'ã', 'a'}, {'â', 'a'}, {'ầ', 'a'}, {'ấ', 'a'}, {'ậ', 'a'}, {'ẩ', 'a'}, {'ẫ', 'a'}, {'ă', 'a'}, {'ằ', 'a'}, {'ắ', 'a'}, {'ặ', 'a'}, {'ẳ', 'a'}, {'ẵ', 'a'},
                {'è', 'e'}, {'é', 'e'}, {'ẹ', 'e'}, {'ẻ', 'e'}, {'ẽ', 'e'}, {'ê', 'e'}, {'ề', 'e'}, {'ế', 'e'}, {'ệ', 'e'}, {'ể', 'e'}, {'ễ', 'e'},
                {'ì', 'i'}, {'í', 'i'}, {'ị', 'i'}, {'ỉ', 'i'}, {'ĩ', 'i'},
                {'ò', 'o'}, {'ó', 'o'}, {'ọ', 'o'}, {'ỏ', 'o'}, {'õ', 'o'}, {'ô', 'o'}, {'ồ', 'o'}, {'ố', 'o'}, {'ộ', 'o'}, {'ổ', 'o'}, {'ỗ', 'o'}, {'ơ', 'o'}, {'ờ', 'o'}, {'ớ', 'o'}, {'ợ', 'o'}, {'ở', 'o'}, {'ỡ', 'o'},
                {'ù', 'u'}, {'ú', 'u'}, {'ụ', 'u'}, {'ủ', 'u'}, {'ũ', 'u'}, {'ư', 'u'}, {'ừ', 'u'}, {'ứ', 'u'}, {'ự', 'u'}, {'ử', 'u'}, {'ữ', 'u'},
                {'ỳ', 'y'}, {'ý', 'y'}, {'ỵ', 'y'}, {'ỷ', 'y'}, {'ỹ', 'y'},
                {'À', 'A'}, {'Á', 'A'}, {'Ạ', 'A'}, {'Ả', 'A'}, {'Ã', 'A'}, {'Â', 'A'}, {'Ầ', 'A'}, {'Ấ', 'A'}, {'Ậ', 'A'}, {'Ẩ', 'A'}, {'Ẫ', 'A'}, {'Ă', 'A'}, {'Ằ', 'A'}, {'Ắ', 'A'}, {'Ặ', 'A'}, {'Ẳ', 'A'}, {'Ẵ', 'A'},
                {'È', 'E'}, {'É', 'E'}, {'Ẹ', 'E'}, {'Ẻ', 'E'}, {'Ẽ', 'E'}, {'Ê', 'E'}, {'Ề', 'E'}, {'Ế', 'E'}, {'Ệ', 'E'}, {'Ể', 'E'}, {'Ễ', 'E'},
                {'Ì', 'I'}, {'Í', 'I'}, {'Ị', 'I'}, {'Ỉ', 'I'}, {'Ĩ', 'I'},
                {'Ò', 'O'}, {'Ó', 'O'}, {'Ọ', 'O'}, {'Ỏ', 'O'}, {'Õ', 'O'}, {'Ô', 'O'}, {'Ồ', 'O'}, {'Ố', 'O'}, {'Ộ', 'O'}, {'Ổ', 'O'}, {'Ỗ', 'O'}, {'Ơ', 'O'}, {'Ờ', 'O'}, {'Ớ', 'O'}, {'Ợ', 'O'}, {'Ở', 'O'}, {'Ỡ', 'O'},
                {'Ù', 'U'}, {'Ú', 'U'}, {'Ụ', 'U'}, {'Ủ', 'U'}, {'Ũ', 'U'}, {'Ư', 'U'}, {'Ừ', 'U'}, {'Ứ', 'U'}, {'ự', 'U'}, {'ử', 'U'}, {'ữ', 'U'},
                {'Ỳ', 'Y'}, {'Ý', 'Y'}, {'Ỵ', 'Y'}, {'Ỷ', 'Y'}, {'Ỹ', 'Y'}
            };

            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (vietnameseMap.ContainsKey(c))
                {
                    sb.Append(vietnameseMap[c]);
                }
                else if (c == 'đ' || c == 'Đ')
                {
                    sb.Append(c == 'đ' ? 'd' : 'D');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public ActionResult Index(string searchString, string statusFilter, string sortBy, string viewType = "table", int page = 1)
        {
            int pageSize = (viewType == "card") ? 12 : 5;
            
            // Query Users table instead of NhanViens
            var accounts = db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string searchLower = searchString.ToLower();
                accounts = accounts.Where(a =>
                    (a.HoTen != null && a.HoTen.ToLower().Contains(searchLower)) || 
                    (a.Email != null && a.Email.ToLower().Contains(searchLower)) || 
                    (a.SDT != null && a.SDT.Contains(searchString))
                );
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                // Convert string filter to boolean
                bool isActive = statusFilter == "Hoạt động";
                bool isInactive = statusFilter == "Vô hiệu hóa";
                
                if (isActive)
                    accounts = accounts.Where(a => a.TrangThai == true);
                else if (isInactive)
                    accounts = accounts.Where(a => a.TrangThai == false);
            }

            // Sorting
            var effectiveSort = string.IsNullOrEmpty(sortBy) ? "code" : sortBy.ToLower();
            switch (effectiveSort)
            {
                case "name":
                    accounts = accounts
                        .OrderBy(a => a.HoTen)
                        .ThenBy(a => a.UserID);
                    break;
                default:
                    accounts = accounts.OrderBy(a => a.UserID);
                    effectiveSort = "code";
                    break;
            }

            var totalCount = accounts.Count();
            
            var accountList = accounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            ViewBag.TotalAccounts = totalCount;
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortBy = effectiveSort;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalCount;
            ViewBag.ViewType = viewType;

            if (Request.IsAjaxRequest() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CustomerList", accountList);
            }

            return View(accountList);
        }

        public JsonResult GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var termLower = term.ToLower();
            var suggestions = db.Users
                .AsNoTracking()
                .Where(u => 
                    (u.HoTen != null && u.HoTen.ToLower().Contains(termLower)) || 
                    (u.Email != null && u.Email.ToLower().Contains(termLower)) || 
                    (u.SDT != null && u.SDT.Contains(term))
                )
                .Select(u => new { 
                    label = u.HoTen, 
                    value = u.HoTen, 
                    desc = u.Email,
                    avatar = u.HoTen.Substring(0, 1).ToUpper()
                })
                .Take(5)
                .ToList();

            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Details(long id, string viewType = null, int page = 1, string searchString = null, string statusFilter = null)
        {
            var account = db.Users
                .AsNoTracking()
                .FirstOrDefault(a => a.UserID == id);
            
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng!";
                // Make sure to pass parameters back to keep state
                return RedirectToAction("Index", new { viewType = viewType, page = page, searchString = searchString, statusFilter = statusFilter });
            }

            string address = account.DiaChi ?? "-";

            ViewBag.CustomerCode = "KH" + account.UserID.ToString("D5");
            ViewBag.FullName = account.HoTen ?? "-";
            ViewBag.Email = account.Email ?? "-";
            ViewBag.PhoneNumber = account.SDT ?? "-";
            
            // Map boolean status to string
            bool isActive = account.TrangThai ?? true; // Default to true if null
            ViewBag.Status = isActive ? "Hoạt động" : "Vô hiệu hóa";
            
            ViewBag.Address = address;
            ViewBag.PasswordHash = account.Password ?? string.Empty; // User model uses Password, not MatKhau
            
            ViewBag.ViewType = viewType ?? "table";
            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;

            return View();
        }

        // GET: Admin_65133141/Customer/GetCustomerDetails/5
        // AJAX endpoint to get customer details as JSON for modal display
        public ActionResult GetCustomerDetails(long id)
        {
            try
            {
                var account = db.Users
                    .AsNoTracking()
                    .FirstOrDefault(a => a.UserID == id);
                
                if (account == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng!" }, JsonRequestBehavior.AllowGet);
                }

                bool isActive = account.TrangThai ?? true;

                var customerData = new
                {
                    customerCode = "KH" + account.UserID.ToString("D5"),
                    fullName = account.HoTen ?? "-",
                    email = account.Email ?? "-",
                    phoneNumber = account.SDT ?? "-",
                    address = account.DiaChi ?? "-",
                    status = isActive ? "Hoạt động" : "Vô hiệu hóa",
                    passwordHash = account.Password ?? string.Empty
                };

                return Json(new { success = true, customer = customerData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải thông tin khách hàng: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DisableUser(long id, string viewType = null, int page = 1, string searchString = null, string statusFilter = null)
        {
            var account = db.Users.Find(id);
            if (account == null)
            {
                if (Request.IsAjaxRequest() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
                }
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng!";
                return RedirectToAction("Index", new { viewType = viewType, page = page, searchString = searchString, statusFilter = statusFilter });
            }

            // Toggle status boolean
            bool currentStatus = account.TrangThai ?? true;
            account.TrangThai = !currentStatus;
            
            try
            {
                db.SaveChanges();
                string newStatusStr = (account.TrangThai == true) ? "Hoạt động" : "Vô hiệu hóa";
                var successMsg = $"Đã {(account.TrangThai == true ? "kích hoạt" : "vô hiệu hóa")} khách hàng thành công!";
                
                if (Request.IsAjaxRequest() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = successMsg, newStatus = newStatusStr });
                }
                
                TempData["SuccessMessage"] = successMsg;
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Lỗi khi cập nhật trạng thái: " + ex.Message;
            }

            return RedirectToAction("Index", new { viewType = viewType ?? "table", page = page, searchString = searchString, statusFilter = statusFilter });
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
