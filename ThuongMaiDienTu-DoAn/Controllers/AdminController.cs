using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Filters;
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    [AuthorizeAdmin]
    public class AdminController : Controller
    {
        private readonly TMDTEntities db = new TMDTEntities();

        // === DASHBOARD ===
        public ActionResult Index()
        {
            ViewBag.TongNguoiDung = db.NGUOIDUNGs.Count();
            ViewBag.TongSanPham = db.SANPHAMs.Count();
            ViewBag.TinChoDuyet = db.SANPHAMs.Count(sp => sp.TrangThai == "Chờ duyệt");
            ViewBag.TinDaDuyet = db.SANPHAMs.Count(sp => sp.TrangThai == "Đã duyệt");
            return View();
        }

        // === DUYỆT TIN / SẢN PHẨM ===
        public ActionResult DuyetTin()
        {
            var list = db.SANPHAMs
                .Include(s => s.NGUOIDUNG)
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

            TempData["Success"] = $"✅ Đã cập nhật trạng thái sản phẩm **{sp.TenSP}** thành '{tt}'";
            return RedirectToAction("DuyetTin");
        }

        // === QUẢN LÝ NGƯỜI DÙNG ===
        public ActionResult QuanLyNguoiDung()
        {
            var ds = db.NGUOIDUNGs.OrderByDescending(x => x.NgayTao).ToList();
            return View(ds);
        }

        // === QUẢN LÝ SẢN PHẨM ===
        public ActionResult QuanLySanPham()
        {
            var ds = db.SANPHAMs.Include(x => x.NGUOIDUNG)
                                 .Include(x => x.LOAISANPHAM)
                                 .OrderByDescending(x => x.NgayTao)
                                 .ToList();
            return View(ds);
        }

        // === QUẢN LÝ ĐƠN HÀNG ===
        public ActionResult QuanLyDonHang()
        {
            var donhangs = db.HOADONs
                .Select(hd => new DonHangC2CViewModel
                {
                    MaHD = hd.MaHD,
                    NguoiMua = hd.NGUOIDUNG.HoTen,
                    NguoiBan = hd.CT_HOADON
                                 .Select(ct => ct.SANPHAM.NGUOIDUNG.HoTen)
                                 .FirstOrDefault() ?? "(Chưa có sản phẩm)",
                    NgayDat = hd.NgayDat,
                    TongTien = (decimal)hd.TongTien,
                    TrangThai = hd.TrangThai
                })
                .ToList();

            return View(donhangs);
        }

        // === LOẠI SẢN PHẨM ===
        public ActionResult QuanLyLoaiSP()
        {
            var loais = db.LOAISANPHAMs.ToList();
            return View(loais);
        }

            [HttpPost]
        public ActionResult ThemLoaiSP(string tenLoai)
        {
            if (string.IsNullOrWhiteSpace(tenLoai))
            {
                TempData["Error"] = "Tên loại không được để trống!";
                return RedirectToAction("QuanLyLoaiSP");
            }

            var loai = new LOAISANPHAM { TenLoai = tenLoai };
            db.LOAISANPHAMs.Add(loai);
            db.SaveChanges();

            TempData["Success"] = "✅ Đã thêm loại sản phẩm mới!";
            return RedirectToAction("QuanLyLoaiSP");
        }


        // === QUẢN LÝ KHIẾU NẠI ===
        public ActionResult QuanLyKhieuNai()
        {
            var dsKhieuNai = db.KHIEUNAIs
                .Include("NGUOIDUNG")
                .Include("SANPHAM")
                .OrderByDescending(k => k.NgayGui)
                .ToList();

            return View(dsKhieuNai);
        }


        [HttpPost]
        public ActionResult CapNhatTrangThaiKN(int id, string trangThai)
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null || user.VaiTro != "Admin")
                return new HttpStatusCodeResult(403, "Không có quyền.");

            var kn = db.KHIEUNAIs.Find(id);
            if (kn == null)
                return HttpNotFound();

            kn.TrangThai = trangThai;
            db.SaveChanges();

            return Json(new { ok = true, status = kn.TrangThai });
        }


    }
}
