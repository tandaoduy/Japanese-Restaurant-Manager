using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;
using Project_65133141.Models;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class HomeController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/Home
        public ActionResult Index()
        {
            // Get customer role IDs
            var customerRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "khách hàng" || 
                            r.TenVaiTro.ToLower().Trim() == "khach hang" || 
                            r.TenVaiTro.ToLower().Trim() == "user" || 
                            r.TenVaiTro.ToLower().Trim() == "customer")
                .Select(r => r.VaiTroID)
                .ToList();
            
            // Get admin role IDs
            var adminRoleIds = db.vai_tro
                .Where(r => r.TenVaiTro.ToLower().Trim() == "admin" || 
                            r.TenVaiTro.ToLower().Trim() == "administrator" ||
                            r.TenVaiTro.ToLower().Trim().Contains("admin"))
                .Select(r => r.VaiTroID)
                .ToList();
            
            // Calculate date 7 days ago
            var sevenDaysAgo = DateTime.Now.AddDays(-7).Date;
            
            // Get all accounts
            var allAccounts = db.nhan_vien.ToList();
            
            // Calculate TOTAL customers (all customers)
            var totalCustomers = allAccounts.Count(x => 
                customerRoleIds.Contains(x.vai_tro_id));
            
            // Calculate new customers in the last 7 days (using NgayVaoLam)
            var newCustomers = allAccounts.Count(x => 
                customerRoleIds.Contains(x.vai_tro_id) &&
                x.NgayVaoLam.HasValue &&
                x.NgayVaoLam.Value.Date >= sevenDaysAgo);
            
            // Calculate TOTAL employees (exclude customers and admins)
            var totalEmployees = allAccounts.Count(x => 
            {
                if (customerRoleIds.Contains(x.vai_tro_id)) return false;
                if (adminRoleIds.Contains(x.vai_tro_id)) return false;
                // Also check by role name directly to be safe
                var roleName = x.VaiTro?.TenVaiTro?.ToLower().Trim() ?? "";
                if (roleName == "admin" || roleName == "administrator" || roleName.Contains("admin")) return false;
                return true;
            });
            
            // Calculate new employees in the last 7 days (exclude customers and admins, using NgayVaoLam)
            var newEmployees = allAccounts.Count(x => 
            {
                if (customerRoleIds.Contains(x.vai_tro_id)) return false;
                if (adminRoleIds.Contains(x.vai_tro_id)) return false;
                // Also check by role name directly to be safe
                var roleName = x.VaiTro?.TenVaiTro?.ToLower().Trim() ?? "";
                if (roleName == "admin" || roleName == "administrator" || roleName.Contains("admin")) return false;
                // Check if NgayVaoLam is within last 7 days
                return x.NgayVaoLam.HasValue && x.NgayVaoLam.Value.Date >= sevenDaysAgo;
            });
            
            // Calculate total active dishes (mon an)
            var totalActiveDishes = db.MonAns
                .Count(m => m.TrangThai == "Hoạt động");
            
            // Calculate new dishes in the last 7 days
            var newDishesCount = db.MonAns
                .Count(m => m.NgayTao >= sevenDaysAgo);
            
            // Pass statistics to view
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.NewCustomers = newCustomers;
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.NewEmployees = newEmployees;
            ViewBag.TotalActiveDishes = totalActiveDishes;
            ViewBag.NewDishesCount = newDishesCount;
            
            return View();
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