using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

       // GET: Employee_65133141/Menu
        public ActionResult Index(long? categoryId, int page = 1)
        {
            // Recompile Trigger
            const int pageSize = 12; // 12 món ăn mỗi trang
            
            // Lấy danh sách danh mục với thứ tự tùy chỉnh (Giống Admin)
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
                    return 6; 
                })
                .ToList();

            // Lấy danh sách món ăn
            var query = db.MonAns.AsQueryable();

            // Lọc bỏ món "Ngừng phục vụ" theo yêu cầu mới
            query = query.Where(m => m.TrangThai != "Ngừng phục vụ"); 

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.DanhMucID == categoryId.Value);
            }

            // Đếm tổng số
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Đảm bảo page hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Lấy dữ liệu cho trang hiện tại (Sorting Logic)
            
            // 1. Fetch ALL relevant items first (filtered by Category/Searching)
            // Note: We fetch into memory to perform complex sorting by Category Name
            // 1. Map Category Dictionary for efficient lookup
            var categoryMap = categories.ToDictionary(c => c.DanhMucID, c => c.TenDanhMuc ?? "");

            // 2. Fetch ALL relevant items first
            var allFilteredItems = query.ToList(); 

            // 3. Sort in memory using the Dictionary
            var products = allFilteredItems
                .OrderBy(m => {
                    // Get Category Name safely from Map
                    string catName = "";
                    if (categoryMap.ContainsKey(m.DanhMucID))
                    {
                        catName = categoryMap[m.DanhMucID].ToUpperInvariant();
                    }
                    
                    // Assign Rank based on User Request
                    if (catName.Contains("SASHIMI")) return 1;
                    if (catName.Contains("SUSHI") || catName.Contains("SHUSHI")) return 2;
                    if (catName.Contains("CƠM") || catName.Contains("MÌ") || catName.Contains("LẨU")) return 3;
                    if (catName.Contains("TEISHOKU") || catName.Contains("SET")) return 4;
                    if (catName.Contains("UỐNG") || catName.Contains("NƯỚC") || catName.Contains("DRINK")) return 5;
                    if (catName.Contains("TRÁNG MIỆNG") || catName.Contains("DESSERT")) return 7;
                    
                    return 6; // Others
                })
                .ThenBy(m => m.TenMon) // Secondary Sort by Name
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
                    product.Gia,
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

