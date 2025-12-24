using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    [RoleAuthorize("admin")]
    public class DonHangController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities5 db = new QuanLyNhaHangNhat_65133141Entities5();

        // GET: Admin_65133141/DonHang
        public ActionResult Index()
        {
            var orders = db.DonHangs
                .OrderByDescending(o => o.NgayDat)
                .ThenByDescending(o => o.DonHangID)
                .ToList();

            return View(orders);
        }
    }
}
