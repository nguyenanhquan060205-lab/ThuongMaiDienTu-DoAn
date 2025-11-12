using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;
namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class HomeController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        public ActionResult Index()
        {
            // Lấy 6 sản phẩm mới nhất
            var sanPhamMoi = db.SANPHAMs
                .Where(s => s.TrangThai == "Đã duyệt")
                .OrderByDescending(s => s.NgayTao)
                .Take(3)
                .ToList();

            ViewBag.TongSP = db.SANPHAMs.Count();
            ViewBag.TongUser = db.NGUOIDUNGs.Count();
            ViewBag.TyLeThanhCong = "99%";

            return View(sanPhamMoi);
        }


    }
}