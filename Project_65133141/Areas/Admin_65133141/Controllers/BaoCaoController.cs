using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Project_65133141.Filters;
using Project_65133141.Models;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class BaoCaoController : Controller
    {
        private readonly QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/BaoCao
        public ActionResult Index(DateTime? fromDate = null, DateTime? toDate = null, bool groupByMonth = false)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                ViewBag.ErrorMessage = "Ngày bắt đầu không được lớn hơn ngày kết thúc!";
                var emptyModel = new AdminReportViewModel 
                { 
                    FromDate = fromDate, 
                    ToDate = toDate, 
                    GroupByMonth = groupByMonth 
                };
                return View(emptyModel);
            }

            var model = BuildReport(fromDate, toDate, groupByMonth);
            return View(model);
        }

        private AdminReportViewModel BuildReport(DateTime? fromDate, DateTime? toDate, bool groupByMonth)
        {
            var model = new AdminReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                GroupByMonth = groupByMonth
            };

            var hoaDonQuery = db.HoaDons.AsQueryable();

            if (fromDate.HasValue)
            {
                hoaDonQuery = hoaDonQuery.Where(h => h.NgayLap >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                // +1 ngày để bao trọn ngày cuối
                var end = toDate.Value.Date.AddDays(1);
                hoaDonQuery = hoaDonQuery.Where(h => h.NgayLap < end);
            }

            // Doanh thu
            if (groupByMonth)
            {
                var revenueByMonth = hoaDonQuery
                    .Where(h => h.NgayLap.HasValue)
                    .GroupBy(h => new { h.NgayLap.Value.Year, h.NgayLap.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Total = g.Sum(x => x.TongThanhToan)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                foreach (var item in revenueByMonth)
                {
                    model.RevenuePoints.Add(new RevenuePoint
                    {
                        Label = item.Month.ToString("D2") + "/" + item.Year,
                        Total = item.Total
                    });
                    model.TotalRevenue += item.Total;
                }
            }
            else
            {
                var revenueByDay = hoaDonQuery
                    .Where(h => h.NgayLap.HasValue)
                    .GroupBy(h => DbFunctions.TruncateTime(h.NgayLap))
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(x => x.TongThanhToan)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                foreach (var item in revenueByDay)
                {
                    var dateLabel = item.Date.HasValue ? item.Date.Value.ToString("dd/MM/yyyy") : "N/A";
                    model.RevenuePoints.Add(new RevenuePoint
                    {
                        Label = dateLabel,
                        Total = item.Total
                    });
                    model.TotalRevenue += item.Total;
                }
            }

            // Món bán chạy
            var chiTietQuery = db.ChiTietDonHangs.Include(c => c.MonAn).AsQueryable();
            if (fromDate.HasValue || toDate.HasValue)
            {
                var donHangQuery = db.DonHangs.AsQueryable();
                if (fromDate.HasValue)
                {
                    donHangQuery = donHangQuery.Where(d => d.NgayDat >= fromDate.Value);
                }
                if (toDate.HasValue)
                {
                    var end = toDate.Value.Date.AddDays(1);
                    donHangQuery = donHangQuery.Where(d => d.NgayDat < end);
                }
                var donHangIds = donHangQuery.Select(d => d.DonHangID).ToList();
                chiTietQuery = chiTietQuery.Where(c => donHangIds.Contains(c.DonHangID));
            }

            var topDishes = chiTietQuery
                .GroupBy(c => new { c.MonAnID, TenMon = c.MonAn.TenMon })
                .Select(g => new TopDishReport
                {
                    MonAnID = g.Key.MonAnID,
                    TenMon = g.Key.TenMon,
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.ThanhTien ?? (x.DonGia * x.SoLuong))
                })
                .OrderByDescending(x => x.SoLuong)
                .ThenByDescending(x => x.DoanhThu)
                .Take(10)
                .ToList();

            model.TopDishes = topDishes;

            // Hiệu suất nhân viên (theo hóa đơn)
            var nhanVienStats = hoaDonQuery
                .Include(h => h.NhanVien)
                .GroupBy(h => new { h.NhanVienThuNganID, TenNV = h.NhanVien.HoTen })
                .Select(g => new EmployeePerformanceReport
                {
                    NhanVienID = g.Key.NhanVienThuNganID,
                    TenNhanVien = g.Key.TenNV,
                    SoHoaDon = g.Count(),
                    TongDoanhThu = g.Sum(x => x.TongThanhToan)
                })
                .OrderByDescending(x => x.TongDoanhThu)
                .ThenByDescending(x => x.SoHoaDon)
                .ToList();

            model.EmployeeStats = nhanVienStats;

            return model;
        }

        // GET: Admin_65133141/BaoCao/ExportExcel
        public ActionResult ExportExcel(DateTime? fromDate = null, DateTime? toDate = null, bool groupByMonth = false)
        {
            var model = BuildReport(fromDate, toDate, groupByMonth);

            var xml = new StringBuilder();
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
            xml.AppendLine("  </Style>");
            xml.AppendLine("</Styles>");

            // Sheet 1: Doanh thu
            xml.AppendLine("<Worksheet ss:Name=\"Doanh thu\">");
            xml.AppendLine("<Table>");
            xml.AppendLine("<Row ss:StyleID=\"Header\"><Cell><Data ss:Type=\"String\">Kỳ</Data></Cell><Cell><Data ss:Type=\"String\">Doanh thu</Data></Cell></Row>");
            foreach (var r in model.RevenuePoints)
            {
                xml.AppendLine("<Row>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(r.Label) + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + r.Total.ToString("F0") + "</Data></Cell>");
                xml.AppendLine("</Row>");
            }
            xml.AppendLine("<Row><Cell><Data ss:Type=\"String\">Tổng</Data></Cell><Cell><Data ss:Type=\"Number\">" + model.TotalRevenue.ToString("F0") + "</Data></Cell></Row>");
            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");

            // Sheet 2: Món bán chạy
            xml.AppendLine("<Worksheet ss:Name=\"Mon ban chay\">");
            xml.AppendLine("<Table>");
            xml.AppendLine("<Row ss:StyleID=\"Header\"><Cell><Data ss:Type=\"String\">Món</Data></Cell><Cell><Data ss:Type=\"String\">Số lượng</Data></Cell><Cell><Data ss:Type=\"String\">Doanh thu</Data></Cell></Row>");
            foreach (var d in model.TopDishes)
            {
                xml.AppendLine("<Row>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(d.TenMon ?? "") + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + d.SoLuong + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + d.DoanhThu.ToString("F0") + "</Data></Cell>");
                xml.AppendLine("</Row>");
            }
            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");

            // Sheet 3: Hiệu suất nhân viên
            xml.AppendLine("<Worksheet ss:Name=\"Nhan vien\">");
            xml.AppendLine("<Table>");
            xml.AppendLine("<Row ss:StyleID=\"Header\"><Cell><Data ss:Type=\"String\">Nhân viên</Data></Cell><Cell><Data ss:Type=\"String\">Số hóa đơn</Data></Cell><Cell><Data ss:Type=\"String\">Doanh thu</Data></Cell></Row>");
            foreach (var e in model.EmployeeStats)
            {
                xml.AppendLine("<Row>");
                xml.AppendLine("<Cell><Data ss:Type=\"String\">" + System.Security.SecurityElement.Escape(e.TenNhanVien ?? "Không rõ") + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + e.SoHoaDon + "</Data></Cell>");
                xml.AppendLine("<Cell><Data ss:Type=\"Number\">" + e.TongDoanhThu.ToString("F0") + "</Data></Cell>");
                xml.AppendLine("</Row>");
            }
            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");

            xml.AppendLine("</Workbook>");

            var bytes = new UTF8Encoding(false).GetBytes(xml.ToString());
            var fileName = "BaoCao_Admin_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls";
            return File(bytes, "application/vnd.ms-excel", fileName);
        }

        // GET: Admin_65133141/BaoCao/ExportPDF
        public ActionResult ExportPDF(DateTime? fromDate = null, DateTime? toDate = null, bool groupByMonth = false)
        {
            var model = BuildReport(fromDate, toDate, groupByMonth);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset='utf-8'><style>");
            html.AppendLine("body{font-family:Arial,sans-serif;margin:20px;}h1{color:#0d9488;}h2{color:#b91c1c;margin-top:24px;}table{width:100%;border-collapse:collapse;margin-top:10px;}th{background:#f0fdfa;padding:8px;text-align:left;border:1px solid #ddd;}td{padding:6px;border:1px solid #ddd;}tr:nth-child(even){background:#f9fafb;}");
            html.AppendLine("</style></head><body>");
            html.AppendLine("<h1>Báo cáo thống kê</h1>");

            if (model.FromDate.HasValue || model.ToDate.HasValue)
            {
                html.AppendLine("<p><strong>Khoảng thời gian:</strong> " +
                    (model.FromDate.HasValue ? model.FromDate.Value.ToString("dd/MM/yyyy") : "...") +
                    " - " +
                    (model.ToDate.HasValue ? model.ToDate.Value.ToString("dd/MM/yyyy") : "...") +
                    "</p>");
            }

            // Doanh thu
            html.AppendLine("<h2>Doanh thu " + (model.GroupByMonth ? "theo tháng" : "theo ngày") + "</h2>");
            html.AppendLine("<table><thead><tr><th>Kỳ</th><th>Doanh thu</th></tr></thead><tbody>");
            foreach (var r in model.RevenuePoints)
            {
                html.AppendLine("<tr><td>" + System.Web.HttpUtility.HtmlEncode(r.Label) + "</td><td>" + r.Total.ToString("N0") + " ₫</td></tr>");
            }
            html.AppendLine("</tbody><tfoot><tr><th>Tổng</th><th>" + model.TotalRevenue.ToString("N0") + " ₫</th></tr></tfoot></table>");

            // Món bán chạy
            html.AppendLine("<h2>Món bán chạy</h2>");
            html.AppendLine("<table><thead><tr><th>Món</th><th>Số lượng</th><th>Doanh thu</th></tr></thead><tbody>");
            foreach (var d in model.TopDishes)
            {
                html.AppendLine("<tr><td>" + System.Web.HttpUtility.HtmlEncode(d.TenMon ?? "") + "</td><td>" + d.SoLuong + "</td><td>" + d.DoanhThu.ToString("N0") + " ₫</td></tr>");
            }
            html.AppendLine("</tbody></table>");

            // Hiệu suất nhân viên
            html.AppendLine("<h2>Hiệu suất nhân viên</h2>");
            html.AppendLine("<table><thead><tr><th>Nhân viên</th><th>Số hóa đơn</th><th>Doanh thu</th></tr></thead><tbody>");
            foreach (var e in model.EmployeeStats)
            {
                var name = string.IsNullOrEmpty(e.TenNhanVien) ? "Không rõ" : e.TenNhanVien;
                html.AppendLine("<tr><td>" + System.Web.HttpUtility.HtmlEncode(name) + "</td><td>" + e.SoHoaDon + "</td><td>" + e.TongDoanhThu.ToString("N0") + " ₫</td></tr>");
            }
            html.AppendLine("</tbody></table>");

            html.AppendLine("</body></html>");

            ViewBag.HTMLContent = html.ToString();
            // Tái sử dụng PDFView của đơn hàng (in HTML)
            return View("~/Areas/Admin_65133141/Views/DonHang/PDFView.cshtml");
        }
    }
}
