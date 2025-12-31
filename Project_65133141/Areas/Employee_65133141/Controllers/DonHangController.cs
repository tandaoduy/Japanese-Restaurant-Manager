using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee", "admin")]
    public class DonHangController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/DonHang
        public ActionResult Index(string statusFilter = null, string searchTerm = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1)
        {
            // Get current employee ID
            var nhanVienId = Session["UserId"] as long?;
            if (!nhanVienId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên";
                return RedirectToAction("Index", "Home");
            }

            // Get all orders for this employee
            var query = db.DonHangs
                .Where(o => o.NhanVienID == nhanVienId.Value)
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.NhanVien)
                .Include(o => o.HoaDons)
                .AsQueryable();

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(o => o.NgayDat >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.NgayDat <= endDate.Value.AddDays(1));
            }

            // Search by order ID / invoice code (HD00011) / customer name / table name / walk-in customer / phone
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var trimmed = searchTerm.Trim();
                var searchLower = trimmed.ToLower();

                // Try parse invoice code format: HD00011
                long invoiceId = 0;
                bool isInvoiceSearch = false;
                if (searchLower.StartsWith("hd"))
                {
                    var digits = new string(trimmed.Skip(2).Where(char.IsDigit).ToArray());
                    isInvoiceSearch = (!string.IsNullOrEmpty(digits) && long.TryParse(digits, out invoiceId));
                }

                query = query.Where(o =>
                    o.DonHangID.ToString().Contains(trimmed) ||
                    (isInvoiceSearch && o.HoaDons.Any(h => h.HoaDonID == invoiceId)) ||
                    (o.User != null && o.User.HoTen != null && o.User.HoTen.ToLower().Contains(searchLower)) ||
                    (o.BanAn != null && o.BanAn.TenBan != null && o.BanAn.TenBan.ToLower().Contains(searchLower)) ||
                    (o.GhiChu != null && o.GhiChu.ToLower().Contains(searchLower)) ||
                    (o.SoDienThoai != null && o.SoDienThoai.Contains(trimmed))
                );
            }

            // Pagination
            const int pageSize = 5;
            if (page < 1) page = 1;

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var orders = query
                .OrderByDescending(o => o.NgayDat)
                .ThenByDescending(o => o.DonHangID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            // BaseUrl for pagination component (without page param)
            ViewBag.BaseUrl = Url.Action("Index", "DonHang", new
            {
                area = "Employee_65133141",
                statusFilter = statusFilter,
                searchTerm = searchTerm,
                startDate = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : null,
                endDate = endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : null
            });

            return View(orders);
        }

        // GET: Employee_65133141/DonHang/GetSearchSuggestions (AJAX)
        [HttpGet]
        public JsonResult GetSearchSuggestions(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return Json(new { success = true, suggestions = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            var nhanVienId = Session["UserId"] as long?;
            if (!nhanVienId.HasValue)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" }, JsonRequestBehavior.AllowGet);
            }

            var queryLower = query.ToLower();
            var suggestions = new List<object>();

            // Tìm theo mã đơn (HoaDon)
            // EF không hỗ trợ ToString("D5") trong LINQ to Entities => lấy ID trước rồi format sau
            var hoaDonIds = db.HoaDons
                .Where(h => h.DonHang.NhanVienID == nhanVienId.Value)
                .OrderByDescending(h => h.HoaDonID)
                .Select(h => h.HoaDonID)
                .Take(200)
                .ToList();

            var hoaDonSuggestions = hoaDonIds
                .Select(id => "HD" + id.ToString("D5"))
                .Where(code => code.ToLower().Contains(queryLower))
                .Take(5)
                .Select(code => new { text = code, type = "Mã đơn" })
                .ToList();

            suggestions.AddRange(hoaDonSuggestions);

            // Tìm theo tên khách hàng
            var customers = db.DonHangs
                .Where(o => o.NhanVienID == nhanVienId.Value && 
                           ((o.User != null && o.User.HoTen != null && o.User.HoTen.ToLower().Contains(queryLower)) ||
                            (o.GhiChu != null && o.GhiChu.ToLower().Contains(queryLower))))
                .Select(o => o.User != null ? o.User.HoTen : o.GhiChu)
                .Distinct()
                .Take(5)
                .ToList();
            foreach (var customer in customers)
            {
                if (!string.IsNullOrEmpty(customer))
                {
                    suggestions.Add(new { text = customer, type = "Khách hàng" });
                }
            }

            // Tìm theo tên bàn
            var tables = db.DonHangs
                .Where(o => o.NhanVienID == nhanVienId.Value && 
                           o.BanAn != null && 
                           o.BanAn.TenBan != null && 
                           o.BanAn.TenBan.ToLower().Contains(queryLower))
                .Select(o => o.BanAn.TenBan)
                .Distinct()
                .Take(5)
                .ToList();
            foreach (var table in tables)
            {
                suggestions.Add(new { text = table, type = "Bàn" });
            }

            return Json(new { success = true, suggestions = suggestions.Take(10).ToList() }, JsonRequestBehavior.AllowGet);
        }

        // GET: Employee_65133141/DonHang/ExportExcel
        public ActionResult ExportExcel(string searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var nhanVienId = Session["UserId"] as long?;
            if (!nhanVienId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên";
                return RedirectToAction("Index");
            }

            var query = db.DonHangs
                .Where(o => o.NhanVienID == nhanVienId.Value)
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.HoaDons)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.NgayDat >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.NgayDat <= endDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(o =>
                    o.DonHangID.ToString().Contains(searchTerm) ||
                    (o.User != null && o.User.HoTen != null && o.User.HoTen.ToLower().Contains(searchLower)) ||
                    (o.BanAn != null && o.BanAn.TenBan != null && o.BanAn.TenBan.ToLower().Contains(searchLower)) ||
                    (o.GhiChu != null && o.GhiChu.ToLower().Contains(searchLower)) ||
                    (o.SoDienThoai != null && o.SoDienThoai.Contains(searchTerm))
                );
            }

            var orders = query.OrderByDescending(o => o.NgayDat).ToList();

            // GenerateExcelFile tạo SpreadsheetML (XML Excel 2003). Vì vậy cần xuất .xls để Excel mở đúng.
            var excelContent = GenerateExcelFile(orders);
            var fileName = "DonHang_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls";
            return File(excelContent, "application/vnd.ms-excel", fileName);
        }

        private byte[] GenerateExcelFile(List<DonHang> orders)
        {
            // Tạo Excel file bằng Office Open XML format (SpreadsheetML)
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xml.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            xml.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            xml.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xml.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");

            // Styles (vì có sử dụng ss:StyleID="Header")
            xml.AppendLine("<Styles>");
            xml.AppendLine("  <Style ss:ID=\"Header\">");
            xml.AppendLine("    <Font ss:Bold=\"1\"/>");
            xml.AppendLine("    <Interior ss:Color=\"#F0F0F0\" ss:Pattern=\"Solid\"/>");
            xml.AppendLine("    <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\"/>");
            xml.AppendLine("    <Borders>");
            xml.AppendLine("      <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xml.AppendLine("      <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xml.AppendLine("      <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xml.AppendLine("      <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xml.AppendLine("    </Borders>");
            xml.AppendLine("  </Style>");
            xml.AppendLine("</Styles>");

            xml.AppendLine("<Worksheet ss:Name=\"Danh sách đơn hàng\">");
            xml.AppendLine("<Table>");

            // Header row với style
            xml.AppendLine("<Row ss:StyleID=\"Header\">");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Mã đơn</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Bàn</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Tên khách hàng</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">SĐT</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Ngày đặt</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Tổng tiền</Data></Cell>");
            xml.AppendLine("</Row>");

            // Data rows
            foreach (var item in orders)
            {
                var hoaDon = item.HoaDons?.FirstOrDefault();
                var maDon = hoaDon != null ? "HD" + hoaDon.HoaDonID.ToString("D5") : "DH" + item.DonHangID.ToString("D4");
                
                string customerName = "";
                string customerPhone = "";
                if (item.User != null)
                {
                    customerName = item.User.HoTen ?? "";
                    customerPhone = item.User.SDT ?? "";
                }
                else
                {
                    customerName = !string.IsNullOrEmpty(item.GhiChu) ? item.GhiChu : "";
                    customerPhone = item.SoDienThoai ?? "";
                }

                xml.AppendLine("<Row>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(maDon) + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(item.BanAn?.TenBan ?? "") + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(customerName) + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(customerPhone) + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(item.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "") + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + item.TongTien.ToString("F0") + "</Data></Cell>");
                xml.AppendLine("</Row>");
            }

            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");
            xml.AppendLine("</Workbook>");

            // UTF-8 without BOM (Excel vẫn đọc tốt khi khai báo encoding="utf-8")
            return new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(xml.ToString());
        }

        // GET: Employee_65133141/DonHang/ExportPDF
        public ActionResult ExportPDF(string searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var nhanVienId = Session["UserId"] as long?;
            if (!nhanVienId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên";
                return RedirectToAction("Index");
            }

            var query = db.DonHangs
                .Where(o => o.NhanVienID == nhanVienId.Value)
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.HoaDons)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.NgayDat >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.NgayDat <= endDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(o =>
                    o.DonHangID.ToString().Contains(searchTerm) ||
                    (o.User != null && o.User.HoTen != null && o.User.HoTen.ToLower().Contains(searchLower)) ||
                    (o.BanAn != null && o.BanAn.TenBan != null && o.BanAn.TenBan.ToLower().Contains(searchLower)) ||
                    (o.GhiChu != null && o.GhiChu.ToLower().Contains(searchLower)) ||
                    (o.SoDienThoai != null && o.SoDienThoai.Contains(searchTerm))
                );
            }

            var orders = query.OrderByDescending(o => o.NgayDat).ToList();

            // Tạo HTML cho PDF
            var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        h1 { color: #0d9488; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th { background: #f0fdfa; padding: 10px; text-align: left; border: 1px solid #ddd; }
        td { padding: 8px; border: 1px solid #ddd; }
        tr:nth-child(even) { background: #f9fafb; }
    </style>
</head>
<body>
    <h1>Danh sách đơn hàng</h1>
    <table>
        <thead>
            <tr>
                <th>Mã đơn</th>
                <th>Bàn</th>
                <th>Tên khách hàng</th>
                <th>SĐT</th>
                <th>Ngày đặt</th>
                <th>Tổng tiền</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in orders)
            {
                var hoaDon = item.HoaDons?.FirstOrDefault();
                var maDon = hoaDon != null ? "HD" + hoaDon.HoaDonID.ToString("D5") : "DH" + item.DonHangID.ToString("D4");
                
                string customerName = "";
                string customerPhone = "";
                if (item.User != null)
                {
                    customerName = item.User.HoTen ?? "";
                    customerPhone = item.User.SDT ?? "";
                }
                else
                {
                    customerName = !string.IsNullOrEmpty(item.GhiChu) ? item.GhiChu : "";
                    customerPhone = item.SoDienThoai ?? "";
                }

                html += string.Format(@"
            <tr>
                <td>{0}</td>
                <td>{1}</td>
                <td>{2}</td>
                <td>{3}</td>
                <td>{4}</td>
                <td>{5:N0} ₫</td>
            </tr>",
                    maDon,
                    System.Web.HttpUtility.HtmlEncode(item.BanAn?.TenBan ?? ""),
                    System.Web.HttpUtility.HtmlEncode(customerName),
                    System.Web.HttpUtility.HtmlEncode(customerPhone),
                    item.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    item.TongTien
                );
            }

            html += @"
        </tbody>
    </table>
</body>
</html>";

            ViewBag.HTMLContent = html;
            return View("PDFView");
        }

        // GET: Employee_65133141/DonHang/Details/5
        public ActionResult Details(long id)
        {
            var order = db.DonHangs
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.NhanVien)
                .Include(o => o.ChiTietDonHangs)
                .Include(o => o.ChiTietDonHangs.Select(c => c.MonAn))
                .FirstOrDefault(o => o.DonHangID == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Index");
            }

            // Check if order belongs to current employee
            var nhanVienId = Session["UserId"] as long?;
            if (order.NhanVienID != nhanVienId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // GET: Employee_65133141/DonHang/GetOrderDetails/5 (AJAX)
        [HttpGet]
        public JsonResult GetOrderDetails(long id)
        {
            var order = db.DonHangs
                .Include(o => o.BanAn)
                .Include(o => o.User)
                .Include(o => o.NhanVien)
                .Include(o => o.ChiTietDonHangs)
                .Include(o => o.ChiTietDonHangs.Select(c => c.MonAn))
                .Include(o => o.HoaDons)
                .FirstOrDefault(o => o.DonHangID == id);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" }, JsonRequestBehavior.AllowGet);
            }

            // Check if order belongs to current employee
            var nhanVienId = Session["UserId"] as long?;
            if (!nhanVienId.HasValue || order.NhanVienID != nhanVienId.Value)
            {
                return Json(new { success = false, message = "Bạn không có quyền xem đơn hàng này" }, JsonRequestBehavior.AllowGet);
            }

            // Lấy mã hóa đơn
            var hoaDon = order.HoaDons?.FirstOrDefault();
            var maDon = hoaDon != null ? "HD" + hoaDon.HoaDonID.ToString("D5") : "DH" + order.DonHangID.ToString("D4");

            // Lấy thông tin khách hàng
            string customerName = "";
            string customerPhone = "";
            if (order.User != null)
            {
                customerName = order.User.HoTen ?? "";
                customerPhone = order.User.SDT ?? "";
            }
            else
            {
                customerName = !string.IsNullOrEmpty(order.GhiChu) ? order.GhiChu : "Khách vãng lai";
                customerPhone = order.SoDienThoai ?? "";
            }

            var orderDetails = order.ChiTietDonHangs.Select(d => new
            {
                TenMon = d.MonAn?.TenMon ?? "N/A",
                SoLuong = d.SoLuong,
                DonGia = d.DonGia,
                ThanhTien = d.ThanhTien ?? (d.DonGia * d.SoLuong)
            }).ToList();

            return Json(new
            {
                success = true,
                data = new
                {
                    DonHangID = order.DonHangID,
                    MaDon = maDon,
                    BanAn = order.BanAn != null ? order.BanAn.TenBan + " - " + (order.BanAn.ViTri ?? "") : "N/A",
                    NgayDat = order.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                    TrangThai = order.TrangThai ?? "N/A",
                    TongTien = order.TongTien,
                    CustomerName = customerName,
                    CustomerPhone = customerPhone,
                    NhanVien = order.NhanVien?.HoTen ?? "N/A",
                    OrderDetails = orderDetails
                }
            }, JsonRequestBehavior.AllowGet);
        }
    }
}


