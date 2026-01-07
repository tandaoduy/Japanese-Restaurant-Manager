using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Admin_65133141.Controllers
{
    /// <summary>
    /// Controller quản lý tin tức và ưu đãi (CRUD trang riêng)
    /// </summary>
    [RoleAuthorize("admin")]
    public class TinTucController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: Admin_65133141/TinTuc
        public ActionResult Index(string search = "", int page = 1)
        {
            const int pageSize = 10;

            var query = db.TinTucs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(t =>
                    (t.TieuDe ?? "").ToLower().Contains(search) ||
                    (t.MoTaNgan ?? "").ToLower().Contains(search));
            }

            query = query.OrderByDescending(t => t.NgayDang);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var tinTucs = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(tinTucs);
        }

        // GET: Admin_65133141/TinTuc/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var tinTuc = db.TinTucs.Find(id);
            if (tinTuc == null)
            {
                return HttpNotFound();
            }

            return View(tinTuc);
        }

        // GET: Admin_65133141/TinTuc/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin_65133141/TinTuc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Cho phép nhập HTML trong NoiDung
        public ActionResult Create(TinTuc model, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.TieuDe))
                {
                    ModelState.AddModelError("TieuDe", "Vui lòng nhập tiêu đề");
                    return View(model);
                }

                model.Slug = GenerateSlug(model.TieuDe);
                model.NgayDang = DateTime.Now;

                var nhanVienId = Session["NhanVienID"];
                if (nhanVienId != null)
                {
                    model.NguoiDangID = Convert.ToInt64(nhanVienId);
                }

                if (!model.IsHienThi.HasValue) model.IsHienThi = true;
                if (!model.IsNoiBat.HasValue) model.IsNoiBat = false;

                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    var uploadFolder = Server.MapPath("~/Images/TinTuc");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                    var fullPath = Path.Combine(uploadFolder, fileName);
                    imageFile.SaveAs(fullPath);
                    model.HinhAnh = fileName;
                }

                db.TinTucs.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: Admin_65133141/TinTuc/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var tinTuc = db.TinTucs.Find(id);
            if (tinTuc == null)
            {
                return HttpNotFound();
            }

            return View(tinTuc);
        }

        // POST: Admin_65133141/TinTuc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Cho phép nhập HTML trong NoiDung
        public ActionResult Edit(TinTuc model, HttpPostedFileBase imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tinTuc = db.TinTucs.Find(model.TinTucID);
            if (tinTuc == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TieuDe))
            {
                ModelState.AddModelError("TieuDe", "Vui lòng nhập tiêu đề");
                return View(model);
            }

            tinTuc.TieuDe = model.TieuDe;
            tinTuc.MoTaNgan = model.MoTaNgan;
            tinTuc.NoiDung = model.NoiDung;
            tinTuc.Slug = GenerateSlug(model.TieuDe);
            tinTuc.IsHienThi = model.IsHienThi ?? true;
            tinTuc.IsNoiBat = model.IsNoiBat ?? false;

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                var uploadFolder = Server.MapPath("~/Images/TinTuc");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                var fullPath = Path.Combine(uploadFolder, fileName);
                imageFile.SaveAs(fullPath);
                tinTuc.HinhAnh = fileName;
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Admin_65133141/TinTuc/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var tinTuc = db.TinTucs.Find(id);
            if (tinTuc == null)
            {
                return HttpNotFound();
            }

            return View(tinTuc);
        }

        // POST: Admin_65133141/TinTuc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long TinTucID)
        {
            var tinTuc = db.TinTucs.Find(TinTucID);
            if (tinTuc == null)
            {
                return HttpNotFound();
            }

            db.TinTucs.Remove(tinTuc);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Tạo slug từ tiêu đề
        /// </summary>
        private string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            
            // Chuyển thành chữ thường
            text = text.ToLower().Trim();
            
            // Bỏ dấu tiếng Việt
            text = RemoveVietnameseDiacritics(text);
            
            // Thay khoảng trắng bằng dấu gạch ngang
            text = Regex.Replace(text, @"\s+", "-");
            
            // Xóa các ký tự không hợp lệ
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            
            // Xóa các dấu gạch ngang liên tiếp
            text = Regex.Replace(text, @"-+", "-");
            
            // Trim dấu gạch ngang ở đầu và cuối
            text = text.Trim('-');
            
            return text;
        }

        /// <summary>
        /// Bỏ dấu tiếng Việt
        /// </summary>
        private string RemoveVietnameseDiacritics(string text)
        {
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }

            return text;
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
