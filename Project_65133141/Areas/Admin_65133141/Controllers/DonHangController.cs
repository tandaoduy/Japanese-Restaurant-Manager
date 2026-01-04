using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;
using System.Data.Entity;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class DonHangController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/DonHang
        public ActionResult Index(string statusFilter = null, string searchTerm = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1)
        {
            var query = db.DonHangs
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

            // Search by order ID / invoice code (HD00011) / customer name / table name / note / phone
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var trimmed = searchTerm.Trim();
                var searchLower = trimmed.ToLower();

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

            ViewBag.BaseUrl = Url.Action("Index", "DonHang", new
            {
                area = "Admin_65133141",
                statusFilter = statusFilter,
                searchTerm = searchTerm,
                startDate = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : null,
                endDate = endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : null
            });

            return View(orders);
        }

        // GET: Admin_65133141/DonHang/Details/5
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

            return View(order);
        }

        // GET: Admin_65133141/DonHang/GetSearchSuggestions (AJAX)
        [HttpGet]
        public JsonResult GetSearchSuggestions(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return Json(new { success = true, suggestions = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            var queryLower = query.ToLower();
            var suggestions = new List<object>();

            // Lấy danh sách mã hoá đơn
            var hoaDonIds = db.HoaDons
                .OrderByDescending(h => h.HoaDonID)
                .Select(h => h.HoaDonID)
                .Take(300)
                .ToList();

            var hoaDonSuggestions = hoaDonIds
                .Select(id => "HD" + id.ToString("D5"))
                .Where(code => code.ToLower().Contains(queryLower))
                .Take(5)
                .Select(code => new { text = code, type = "Mã đơn" })
                .ToList();

            suggestions.AddRange(hoaDonSuggestions);

            // Tên khách hàng (bao gồm khách vãng lai)
            var customers = db.DonHangs
                .Where(o =>
                    (o.User != null && o.User.HoTen != null && o.User.HoTen.ToLower().Contains(queryLower)) ||
                    (o.GhiChu != null && o.GhiChu.ToLower().Contains(queryLower)))
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

            // Tên bàn
            var tables = db.DonHangs
                .Where(o => o.BanAn != null && o.BanAn.TenBan != null && o.BanAn.TenBan.ToLower().Contains(queryLower))
                .Select(o => o.BanAn.TenBan)
                .Distinct()
                .Take(5)
                .ToList();

            foreach (var table in tables)
            {
                suggestions.Add(new { text = table, type = "Bàn" });
            }

            // Số điện thoại
            var phones = db.DonHangs
                .Where(o => o.SoDienThoai != null && o.SoDienThoai.Contains(query))
                .Select(o => o.SoDienThoai)
                .Distinct()
                .Take(5)
                .ToList();

            foreach (var phone in phones)
            {
                suggestions.Add(new { text = phone, type = "SĐT" });
            }

            return Json(new { success = true, suggestions = suggestions.Take(15).ToList() }, JsonRequestBehavior.AllowGet);
        }

        // GET: Admin_65133141/DonHang/ExportExcel
        public ActionResult ExportExcel(string searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = db.DonHangs
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

            var excelContent = GenerateExcelFile(orders);
            var fileName = "DonHang_Admin_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls";
            return File(excelContent, "application/vnd.ms-excel", fileName);
        }

        private byte[] GenerateExcelFile(List<DonHang> orders)
        {
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xml.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            xml.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            xml.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xml.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");

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

            xml.AppendLine("<Worksheet ss:Name=\"Danh sach don hang\">");
            xml.AppendLine("<Table>");

            xml.AppendLine("<Row ss:StyleID=\"Header\">");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Mã đơn</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Bàn</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Nhân viên</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Tên khách hàng</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">SĐT</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Ngày đặt</Data></Cell>");
            xml.AppendLine("<Cell><Data ss:Type=\"String\">Tổng tiền</Data></Cell>");
            xml.AppendLine("</Row>");

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
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(item.NhanVien?.HoTen ?? "") + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(customerName) + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(customerPhone) + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(item.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "") + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + item.TongTien.ToString("F0") + "</Data></Cell>");
                xml.AppendLine("</Row>");
            }

            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");
            xml.AppendLine("</Workbook>");

            return new System.Text.UTF8Encoding(false).GetBytes(xml.ToString());
        }

        // GET: Admin_65133141/DonHang/ExportPDF
        public ActionResult ExportPDF(string searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = db.DonHangs
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
                <th>Nhân viên</th>
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

                html += string.Format(@"\n            <tr>
                <td>{0}</td>
                <td>{1}</td>
                <td>{2}</td>
                <td>{3}</td>
                <td>{4}</td>
                <td>{5}</td>
                <td>{6:N0} ₫</td>
            </tr>",
                    maDon,
                    System.Web.HttpUtility.HtmlEncode(item.BanAn?.TenBan ?? ""),
                    System.Web.HttpUtility.HtmlEncode(item.NhanVien?.HoTen ?? ""),
                    System.Web.HttpUtility.HtmlEncode(customerName),
                    System.Web.HttpUtility.HtmlEncode(customerPhone),
                    item.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    item.TongTien
                );
            }

            html += @"\n        </tbody>
    </table>
</body>
</html>";

            ViewBag.HTMLContent = html;
            return View("PDFView");
        }

        // GET: Admin_65133141/DonHang/GetOrderDetails/5 (AJAX)
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

            var hoaDon = order.HoaDons?.FirstOrDefault();
            var maDon = hoaDon != null ? "HD" + hoaDon.HoaDonID.ToString("D5") : "DH" + order.DonHangID.ToString("D4");

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
