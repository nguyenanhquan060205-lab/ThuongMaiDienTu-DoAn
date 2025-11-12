using ThuongMaiDienTu_DoAn.Filters;
using ThuongMaiDienTu_DoAn.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly TMDTEntities db = new TMDTEntities();

        public ActionResult Index(string q, int? maloai)
        {
            var sp = db.SANPHAMs.Where(s => s.TrangThai == "Đã duyệt");

            if (!string.IsNullOrWhiteSpace(q))
                sp = sp.Where(s => s.TenSP.Contains(q));

            if (maloai.HasValue)
                sp = sp.Where(s => s.MaLoai == maloai.Value);

            ViewBag.LoaiSP = db.LOAISANPHAMs.ToList();
            return View(sp.OrderByDescending(x => x.NgayTao).ToList());
        }

        public ActionResult ChiTiet(int id)
        {
            var sp = db.SANPHAMs.Find(id);
            if (sp == null || sp.TrangThai == "Ẩn")
                return HttpNotFound();

            return View(sp);
        }

        [HttpGet]
        public ActionResult TaoMoi()
        {
            if (Session["user"] == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            ViewBag.MaLoai = new SelectList(db.LOAISANPHAMs, "MaLoai", "TenLoai");
            return View();
        }
        [HttpPost]
        public ActionResult TaoMoi(SANPHAM m, IEnumerable<HttpPostedFileBase> files)
        {
            var u = Session["user"] as NGUOIDUNG;
            if (u == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            m.MaKH = u.MaKH;
            m.NgayTao = DateTime.Now;
            m.TrangThai = "Chờ duyệt";

            db.SANPHAMs.Add(m);
            db.SaveChanges();
 
            if (files != null)
            {
                int i = 0;
                foreach (var f in files)
                {
                    if (f != null && f.ContentLength > 0)
                    {
                        var name = Guid.NewGuid() + System.IO.Path.GetExtension(f.FileName);
                        var path = Server.MapPath("~/Content/Images/" + name);
                        f.SaveAs(path);

                        db.HINHANHSPs.Add(new HINHANHSP
                        {
                            Masp = m.MaSP,
                            URLAnh = name,
                            AnhBia = (i++ == 0)
                        });
                    }
                }
                db.SaveChanges();
            }

            TempData["OK"] = "Đăng tin thành công! Chờ admin duyệt.";
            return RedirectToAction("CuaToi");
        }
        public ActionResult CuaToi()
        {
            var u = Session["user"] as NGUOIDUNG;
            if (u == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var list = db.SANPHAMs
                .Where(x => x.MaKH == u.MaKH)
                .OrderByDescending(x => x.NgayTao)
                .ToList();

            return View(list);
        }
    }
}
