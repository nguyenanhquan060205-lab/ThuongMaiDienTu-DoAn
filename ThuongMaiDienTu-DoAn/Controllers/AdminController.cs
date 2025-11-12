using ThuongMaiDienTu_DoAn.Filters;
using ThuongMaiDienTu_DoAn.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    [AuthorizeAdmin]
    public class AdminController : Controller
    {
        private readonly TMDTEntities db = new TMDTEntities();
        public ActionResult Index()
        {
            ViewBag.TongNguoiDung = db.NGUOIDUNGs.Count();
            ViewBag.TongSanPham = db.SANPHAMs.Count();
            ViewBag.TinChoDuyet = db.SANPHAMs.Count(sp => sp.TrangThai == "Chờ duyệt");
            ViewBag.TinDaDuyet = db.SANPHAMs.Count(sp => sp.TrangThai == "Đã duyệt");
            return View();
        }
        public ActionResult DuyetTin()
        {
            var list = db.SANPHAMs
                .OrderByDescending(x => x.NgayTao)
                .ToList();

            return View(list);
        }

        [HttpPost]
        public ActionResult DoiTrangThai(int id, string tt)
        {
            var sp = db.SANPHAMs.Find(id);
            if (sp == null)
                return HttpNotFound();

            sp.TrangThai = tt;
            db.SaveChanges();

            TempData["Success"] = $"✅ Đã cập nhật trạng thái sản phẩm **{sp.TenSP}** thành '{tt}'.";
            return RedirectToAction("DuyetTin");
        }
        public ActionResult QuanLyNguoiDung()
        {
            var ds = db.NGUOIDUNGs.ToList();
            return View(ds);
        }

        public ActionResult DanhSachSanPham()
        {
            var ds = db.SANPHAMs.Include(x => x.NGUOIDUNG).ToList();
            return View(ds);
        }

        public ActionResult QuanLyDonHang()
        {
            var ds = db.HOADONs.Include(x => x.NGUOIDUNG).ToList();
            return View(ds);
        }

        public ActionResult QuanLyKhieuNai()
        {
            var ds = db.KHIEUNAIs.Include(k => k.NGUOIDUNG)
                                .Include(k => k.SANPHAM)
                                .OrderByDescending(k => k.MaKN)
                                .ToList();
            return View(ds);
        }

        [HttpPost]
        public ActionResult CapNhatKhieuNai(int id, string tt, string ph)
        {
            var kn = db.KHIEUNAIs.Find(id);
            if (kn == null) return HttpNotFound();

            kn.TrangThai = tt;
            kn.PhanHoi = ph;
            db.SaveChanges();

            TempData["Success"] = $"✅ Khiếu nại #{id} đã được cập nhật!";
            return RedirectToAction("QuanLyKhieuNai");
        }






    }
}
