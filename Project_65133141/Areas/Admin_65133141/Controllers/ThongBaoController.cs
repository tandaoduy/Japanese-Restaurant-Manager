using System;
using System.Linq;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    /// <summary>
    /// Controller quản lý thông báo (CRUD đầy đủ)
    /// </summary>
    [RoleAuthorize("admin")]
    public class ThongBaoController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/ThongBao
        public ActionResult Index(string search = "", string loai = "", int page = 1)
        {
            const int pageSize = 15;

            var query = db.ThongBaos.AsQueryable();

            // Filter by type
            if (!string.IsNullOrWhiteSpace(loai))
            {
                query = query.Where(t => t.LoaiThongBao == loai);
            }

            // Search by title or content
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(t => 
                    t.TieuDe.ToLower().Contains(search) || 
                    t.NoiDung.ToLower().Contains(search));
            }

            // Order by newest first
            query = query.OrderByDescending(t => t.NgayTao);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var thongBaos = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.Loai = loai;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(thongBaos);
        }

        // GET: Admin_65133141/ThongBao/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin_65133141/ThongBao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ThongBao model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.TieuDe))
                {
                    ModelState.AddModelError("TieuDe", "Vui lòng nhập tiêu đề thông báo");
                    return View(model);
                }

                model.NgayTao = DateTime.Now;
                model.DaDoc = false;
                
                // Default values if not set
                if (string.IsNullOrEmpty(model.LoaiNguoiNhan))
                    model.LoaiNguoiNhan = "all";
                if (string.IsNullOrEmpty(model.LoaiThongBao))
                    model.LoaiThongBao = "thongbao";

                db.ThongBaos.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã tạo thông báo \"" + model.TieuDe + "\" thành công!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: Admin_65133141/ThongBao/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var thongBao = db.ThongBaos.Find(id);
            if (thongBao == null)
            {
                return HttpNotFound();
            }

            return View(thongBao);
        }

        // POST: Admin_65133141/ThongBao/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ThongBao model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var thongBao = db.ThongBaos.Find(model.ThongBaoID);
            if (thongBao == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TieuDe))
            {
                ModelState.AddModelError("TieuDe", "Vui lòng nhập tiêu đề thông báo");
                return View(model);
            }

            thongBao.TieuDe = model.TieuDe;
            thongBao.NoiDung = model.NoiDung;
            thongBao.LienKet = model.LienKet;
            thongBao.LoaiThongBao = model.LoaiThongBao ?? "thongbao";
            thongBao.LoaiNguoiNhan = model.LoaiNguoiNhan ?? "all";
            thongBao.NguoiNhanID = model.NguoiNhanID;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã cập nhật thông báo \"" + thongBao.TieuDe + "\" thành công!";
            return RedirectToAction("Index");
        }

        // POST: Admin_65133141/ThongBao/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long ThongBaoID)
        {
            var thongBao = db.ThongBaos.Find(ThongBaoID);
            if (thongBao == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Không tìm thấy thông báo cần xóa." });
                }
                return HttpNotFound();
            }

            var tieuDe = thongBao.TieuDe;
            db.ThongBaos.Remove(thongBao);
            db.SaveChanges();

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, message = "Đã xóa thông báo \"" + tieuDe + "\" thành công!" });
            }

            TempData["SuccessMessage"] = "Đã xóa thông báo \"" + tieuDe + "\" thành công!";
            return RedirectToAction("Index");
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
