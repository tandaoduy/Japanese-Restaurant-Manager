using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Models;

namespace Project_65133141.Controllers
{
    public class SeedDataController : Controller
    {
        private QuanLyNhaHangNhat_65133141Entities6 db = new QuanLyNhaHangNhat_65133141Entities6();

        // GET: SeedData/CreateTables
        // Action này sẽ tạo dữ liệu mẫu cho bàn ăn
        public ActionResult CreateTables()
        {
            try
            {
                // Xóa tất cả bàn cũ (nếu cần)
                // var existingTables = db.BanAns.ToList();
                // db.BanAns.RemoveRange(existingTables);
                // db.SaveChanges();

                var tables = new List<BanAn>();

                // Tạo 20 bàn thường
                for (int i = 1; i <= 20; i++)
                {
                    tables.Add(new BanAn
                    {
                        TenBan = $"Bàn {i}",
                        SucChua = 4, // Sức chứa 4 người
                        TrangThai = "Trống",
                        ViTri = "Tầng 1"
                    });
                }

                // Tạo 3 phòng VIP
                for (int i = 1; i <= 3; i++)
                {
                    tables.Add(new BanAn
                    {
                        TenBan = $"Phòng VIP {i}",
                        SucChua = 8, // Sức chứa 8 người
                        TrangThai = "Trống",
                        ViTri = "Tầng 2 - VIP"
                    });
                }

                // Tạo 5 phòng đôi
                for (int i = 1; i <= 5; i++)
                {
                    tables.Add(new BanAn
                    {
                        TenBan = $"Phòng đôi {i}",
                        SucChua = 2, // Sức chứa 2 người
                        TrangThai = "Trống",
                        ViTri = "Tầng 2 - Đôi"
                    });
                }

                // Thêm vào database
                db.BanAns.AddRange(tables);
                db.SaveChanges();

                TempData["SuccessMessage"] = $"Đã tạo thành công {tables.Count} bàn/phòng: 20 bàn thường, 3 phòng VIP, 5 phòng đôi";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tạo dữ liệu: " + ex.Message;
            }

            return RedirectToAction("Index", "Home");
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

