using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class ProductController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/Product
        // Hiển thị danh sách món ăn, có thể lọc theo danh mục và phân trang
        public ActionResult Index(long? categoryId, int page = 1)
        {
            const int pageSize = 12; // 12 món ăn mỗi trang
            
            // Lấy danh sách danh mục với thứ tự tùy chỉnh
            var categories = db.DanhMucs.ToList()
                .OrderBy(c => {
                    var name = (c.TenDanhMuc ?? "").ToUpperInvariant();
                    // Thứ tự: Sashimi, Sushi, Cơm/Mì, Teishoku, Đồ uống, Món khác, Tráng miệng
                    if (name.Contains("SASHIMI")) return 1;
                    if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                    if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                    if (name.Contains("TEISHOKU")) return 4;
                    if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                    if (name.Contains("TRÁNG MIỆNG")) return 7;
                    return 6; // Món khác và các danh mục khác
                })
                .ToList();

            // Lấy danh sách món ăn (mặc định loại bỏ món đã ngừng phục vụ)
            var query = db.MonAns.AsQueryable();

            // Loại bỏ các món có trạng thái "Ngừng phục vụ" - đây là các món đã bị "xóa" (soft delete)
            query = query.Where(m => m.TrangThai != "Ngừng phục vụ");

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.DanhMucID == categoryId.Value);
            }

            // Lấy tất cả dữ liệu ra RAM
            var allResult = query.ToList();
            
            // Lấy danh sách danh mục để tra cứu (do không có properties navigation)
            var catLookup = db.DanhMucs.ToDictionary(k => k.DanhMucID, v => (v.TenDanhMuc ?? "").ToUpperInvariant());

            // Sắp xếp theo thứ tự ưu tiên của Danh Mục
            allResult = allResult.OrderBy(p => {
                string name = "";
                if (catLookup.ContainsKey(p.DanhMucID)) // Assuming DanhMucID is not nullable or handled
                {
                    name = catLookup[p.DanhMucID];
                }
                
                if (name.Contains("SASHIMI")) return 1;
                if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                if (name.Contains("TEISHOKU")) return 4;
                if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                if (name.Contains("TRÁNG MIỆNG")) return 7;
                return 6; // Món khác
            }).ThenBy(m => m.TenMon).ToList();

            // Phân trang trên danh sách đã sắp xếp
            int totalItems = allResult.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Đảm bảo page hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = allResult
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền dữ liệu cho view
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(products);
        }
        public ActionResult Create()
        {
            // Load categories với thứ tự tùy chỉnh
            var categories = db.DanhMucs.ToList()
                .OrderBy(c => {
                    var name = (c.TenDanhMuc ?? "").ToUpperInvariant();
                    if (name.Contains("SASHIMI")) return 1;
                    if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                    if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                    if (name.Contains("TEISHOKU")) return 4;
                    if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                    if (name.Contains("TRÁNG MIỆNG")) return 7;
                    return 6;
                })
                .ToList();

            ViewBag.Categories = categories;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MonAn model, HttpPostedFileBase imageFile)
        {
            // Always reload categories với thứ tự tùy chỉnh
            var categories = db.DanhMucs.ToList()
                .OrderBy(c => {
                    var name = (c.TenDanhMuc ?? "").ToUpperInvariant();
                    if (name.Contains("SASHIMI")) return 1;
                    if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                    if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                    if (name.Contains("TEISHOKU")) return 4;
                    if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                    if (name.Contains("TRÁNG MIỆNG")) return 7;
                    return 6;
                })
                .ToList();
            ViewBag.Categories = categories;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Đơn vị tính mặc định nếu người dùng không nhập (vì bạn muốn bỏ trường này trên form)
            if (string.IsNullOrWhiteSpace(model.DonViTinh))
            {
                model.DonViTinh = "Phần";
            }

            // Thiết lập ngày tạo
            model.NgayTao = DateTime.Now;

            // Set default status to active if not specified
            if (string.IsNullOrWhiteSpace(model.TrangThai))
            {
                model.TrangThai = "Đang phục vụ";
            }

            // Lưu giá gốc vào GiaGoc để tham chiếu (Giá trên form là giá gốc)
            model.GiaGoc = model.Gia;

            // Chuẩn hóa giá giảm: chỉ chấp nhận nếu >0 và < giá gốc, ngược lại bỏ qua
            if (!model.GiaGiam.HasValue || model.GiaGiam.Value <= 0 || model.GiaGiam.Value >= model.Gia)
            {
                model.GiaGiam = null;
            }

            // Xử lý upload hình ảnh nếu có
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                var uploadFolder = "~/Images/Products";
                var physicalFolder = Server.MapPath(uploadFolder);

                if (!Directory.Exists(physicalFolder))
                {
                    Directory.CreateDirectory(physicalFolder);
                }

                var fileExtension = Path.GetExtension(imageFile.FileName);
                var fileName = Guid.NewGuid().ToString("N") + fileExtension;
                var fullPath = Path.Combine(physicalFolder, fileName);

                imageFile.SaveAs(fullPath);

                // Chỉ lưu tên file, các view sẽ tự ghép với thư mục ~/Images/Products khi hiển thị
                model.HinhAnh = fileName;
            }

            db.MonAns.Add(model);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Tạo món ăn thành công.";

            return RedirectToAction("Index");
        }

        // GET: Xem chi tiết món ăn
        public ActionResult Details(long id)
        {
            var monAn = db.MonAns.Find(id);
            if (monAn == null) return HttpNotFound();
            
            // Load danh mục để hiển thị tên
            var danhMuc = db.DanhMucs.Find(monAn.DanhMucID);
            ViewBag.TenDanhMuc = danhMuc != null ? danhMuc.TenDanhMuc : "N/A";
            
            return View(monAn);
        }

        // GET: Trang sửa món ăn
        public ActionResult Edit(long id)
        {
            var monAn = db.MonAns.Find(id);
            if (monAn == null) return HttpNotFound();

            // Gán giá gốc vào trường Gia để form hiển thị đúng
            monAn.Gia = monAn.GiaGoc;

            var categories = db.DanhMucs.ToList()
                .OrderBy(c => {
                    var name = (c.TenDanhMuc ?? "").ToUpperInvariant();
                    if (name.Contains("SASHIMI")) return 1;
                    if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                    if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                    if (name.Contains("TEISHOKU")) return 4;
                    if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                    if (name.Contains("TRÁNG MIỆNG")) return 7;
                    return 6;
                })
                .ToList();
            ViewBag.Categories = categories;
            return View(monAn);
        }

        // POST: Xử lý cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MonAn model, HttpPostedFileBase imageFile)
        {
            var categories = db.DanhMucs.ToList()
                .OrderBy(c => {
                    var name = (c.TenDanhMuc ?? "").ToUpperInvariant();
                    if (name.Contains("SASHIMI")) return 1;
                    if (name.Contains("SUSHI") || name.Contains("SHUSHI")) return 2;
                    if (name.Contains("CƠM") || name.Contains("MÌ")) return 3;
                    if (name.Contains("TEISHOKU")) return 4;
                    if (name.Contains("UỐNG") || name.Contains("NƯỚC")) return 5;
                    if (name.Contains("TRÁNG MIỆNG")) return 7;
                    return 6;
                })
                .ToList();
            ViewBag.Categories = categories;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var dbEntry = db.MonAns.Find(model.MonAnID);
                if (dbEntry == null) return HttpNotFound();

                // Cập nhật các trường thông tin
                dbEntry.TenMon = model.TenMon;
                dbEntry.GiaGoc = model.Gia; // Giá nhập vào form là giá gốc
                dbEntry.DanhMucID = model.DanhMucID;
                dbEntry.TrangThai = model.TrangThai;
                dbEntry.MoTa = model.MoTa;
                dbEntry.DonViTinh = model.DonViTinh;
                dbEntry.IsNoiBat = model.IsNoiBat;

                // Chuẩn hóa giá giảm: chỉ chấp nhận nếu >0 và < giá gốc
                if (model.GiaGiam.HasValue && model.GiaGiam.Value > 0 && model.GiaGiam.Value < model.Gia)
                {
                    dbEntry.GiaGiam = model.GiaGiam;
                }
                else
                {
                    dbEntry.GiaGiam = null;
                }

                // Xử lý upload hình ảnh nếu có
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var uploadFolder = "~/Images/Products";
                    var physicalFolder = Server.MapPath(uploadFolder);

                    if (!Directory.Exists(physicalFolder))
                    {
                        Directory.CreateDirectory(physicalFolder);
                    }

                    var fileExtension = Path.GetExtension(imageFile.FileName);
                    var fileName = Guid.NewGuid().ToString("N") + fileExtension;
                    var fullPath = Path.Combine(physicalFolder, fileName);

                    imageFile.SaveAs(fullPath);
                    dbEntry.HinhAnh = fileName;
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật món ăn thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View(model);
            }
        }

        // POST: Xóa qua AJAX
        // POST: Xóa qua AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(long id)
        {
            try
            {
                var monAn = db.MonAns.Find(id);
                if (monAn == null) return Json(new { success = false, message = "Không tìm thấy món ăn." });

                // Kiểm tra ràng buộc khóa ngoại (Dependencies check)
                bool hasDependencies =  monAn.ChiTietDonHangs.Any() || 
                                        monAn.ChiTietDatHangOnlines.Any();

                if (hasDependencies)
                {
                    // Soft Delete (Chuyển trạng thái)
                    monAn.TrangThai = "Ngừng phục vụ";
                    db.SaveChanges();
                    return Json(new { success = true, message = "Món ăn đã có dữ liệu liên quan. Đã chuyển trạng thái sang 'Ngừng phục vụ' thay vì xóa vĩnh viễn." });
                }
                else
                {
                    // Hard Delete (Xóa thật nếu chưa có dữ liệu)
                    db.MonAns.Remove(monAn);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Xóa món ăn thành công." });
                }
            }
            catch (Exception ex)
            {
                // Log error here if cleaner logging existed
                 var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi khi xóa: " + innerMessage });
            }
        }
        // POST: AJAX Create Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateProductAjax(MonAn model, HttpPostedFileBase imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Keys.Where(k => ModelState[k].Errors.Count > 0)
                        .ToDictionary(k => k, k => ModelState[k].Errors[0].ErrorMessage);
                    return Json(new { success = false, message = "Vui lòng kiểm tra lại thông tin.", errors = errors });
                }

                // Defaults
                if (string.IsNullOrWhiteSpace(model.DonViTinh)) model.DonViTinh = "Phần";
                if (string.IsNullOrWhiteSpace(model.TrangThai)) model.TrangThai = "Đang phục vụ";
                model.NgayTao = DateTime.Now;
                model.GiaGoc = model.Gia;

                // Validate Discount
                if (!model.GiaGiam.HasValue || model.GiaGiam.Value <= 0 || model.GiaGiam.Value >= model.Gia)
                {
                    model.GiaGiam = null;
                }

                // Handle Image
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var uploadFolder = "~/Images/Products";
                    var physicalFolder = Server.MapPath(uploadFolder);
                    if (!Directory.Exists(physicalFolder)) Directory.CreateDirectory(physicalFolder);

                    var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                    imageFile.SaveAs(Path.Combine(physicalFolder, fileName));
                    model.HinhAnh = fileName;
                }

                db.MonAns.Add(model);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm món ăn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // GET: AJAX Get Product Details
        [HttpGet]
        public JsonResult GetProductDetails(long id)
        {
            try
            {
                var product = db.MonAns.Find(id);
                if (product == null) return Json(new { success = false, message = "Không tìm thấy món ăn" }, JsonRequestBehavior.AllowGet);

                var category = db.DanhMucs.Find(product.DanhMucID);

                var data = new
                {
                    product.MonAnID,
                    product.TenMon,
                    product.DanhMucID,
                    TenDanhMuc = category != null ? category.TenDanhMuc : "",
                    product.Gia,      // Current price logic might be needed but model has Gia/GiaGoc/GiaGiam
                    product.GiaGoc,
                    product.GiaGiam,
                    product.DonViTinh,
                    product.MoTa,
                    product.HinhAnh,
                    product.TrangThai,
                    product.IsNoiBat,
                    NgayTao = product.NgayTao.ToString("yyyy-MM-dd")
                };

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: AJAX Edit Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditProductAjax(MonAn model, HttpPostedFileBase imageFile)
        {
            try
            {
                var dbEntry = db.MonAns.Find(model.MonAnID);
                if (dbEntry == null) return Json(new { success = false, message = "Không tìm thấy món ăn" });

                if (!ModelState.IsValid)
                {
                     // Simplified error return for common fields
                     return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                dbEntry.TenMon = model.TenMon;
                dbEntry.DanhMucID = model.DanhMucID;
                dbEntry.GiaGoc = model.Gia; // Update base price
                dbEntry.Gia = model.Gia;    // Ensure consistent logic
                dbEntry.DonViTinh = model.DonViTinh;
                dbEntry.TrangThai = model.TrangThai;
                dbEntry.MoTa = model.MoTa;
                dbEntry.IsNoiBat = model.IsNoiBat;

                // Validate Discount
                if (model.GiaGiam.HasValue && model.GiaGiam.Value > 0 && model.GiaGiam.Value < model.Gia)
                {
                    dbEntry.GiaGiam = model.GiaGiam;
                }
                else
                {
                    dbEntry.GiaGiam = null;
                }

                // Handle Image
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var uploadFolder = "~/Images/Products";
                    var physicalFolder = Server.MapPath(uploadFolder);
                    if (!Directory.Exists(physicalFolder)) Directory.CreateDirectory(physicalFolder);

                    // Optional: Delete old image if needed, but skipping for simplicity
                    var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                    imageFile.SaveAs(Path.Combine(physicalFolder, fileName));
                    dbEntry.HinhAnh = fileName;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
    }
}
        