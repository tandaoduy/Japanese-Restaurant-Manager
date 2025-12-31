using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee", "admin")]
    public class HomeController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Employee_65133141/Home
        public ActionResult Index(string to)
        {
            if (!string.IsNullOrEmpty(to) && to.ToLower().Trim() == "account")
            {
                return RedirectToAction("Index", "Account", new { area = "Employee_65133141" });
            }
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