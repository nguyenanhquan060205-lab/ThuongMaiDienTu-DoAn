using System;
using System.Linq;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class KhieuNaiController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        // GET: /KhieuNai/TaoKhieuNai?idSanPham=...
        public ActionResult TaoKhieuNai(int idSanPham)
        {
            var currentUser = Session["user"] as NGUOIDUNG;
            if (currentUser == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var sanPham = db.SANPHAMs.Find(idSanPham);
            if (sanPham == null)
                return HttpNotFound();

            ViewBag.SanPham = sanPham;
            return View();
        }

        [HttpPost]
        public ActionResult TaoKhieuNai(int idSanPham, string MoTa)
        {
            var currentUser = Session["user"] as NGUOIDUNG;
            if (currentUser == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            if (string.IsNullOrWhiteSpace(MoTa))
            {
                TempData["Loi"] = "Vui lòng nhập nội dung khiếu nại!";
                return RedirectToAction("TaoKhieuNai", new { idSanPham });
            }

            var kn = new KHIEUNAI
            {
                MaKH = currentUser.MaKH,
                MaSP = idSanPham,
                MoTa = MoTa,
                NgayGui = DateTime.Now,
                TrangThai = "Chưa xử lý"
            };

            db.KHIEUNAIs.Add(kn);
            db.SaveChanges();

            TempData["ThongBao"] = "✅ Khiếu nại của bạn đã được gửi, vui lòng chờ phản hồi từ Admin.";
            return RedirectToAction("ChiTiet", "SanPham", new { id = idSanPham });
        }
    }
}
