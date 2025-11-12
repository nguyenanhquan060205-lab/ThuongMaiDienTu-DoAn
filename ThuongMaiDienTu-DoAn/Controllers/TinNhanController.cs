using ThuongMaiDienTu_DoAn.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class TinNhanController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        public ActionResult HopThu(int? withUserId, int? maSp)
        {
            var me = Session["user"] as NGUOIDUNG;
            if (me == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var list = db.TINNHANs
                .Where(t => (t.NguoiGui == me.MaKH || t.NguoiNhan == me.MaKH)
                         && (!withUserId.HasValue || t.NguoiGui == withUserId || t.NguoiNhan == withUserId)
                         && (!maSp.HasValue || t.MaSP == maSp))
                .OrderBy(t => t.NgayGui).ToList();
            ViewBag.With = withUserId; ViewBag.MaSP = maSp;
            return View(list);
        }

        [HttpPost]
        public ActionResult Gui(int nguoiNhan, int maSp, string noiDung)
        {
            var me = Session["user"] as NGUOIDUNG;
            if (me == null) return new HttpStatusCodeResult(401);
            db.TINNHANs.Add(new TINNHAN
            {
                NguoiGui = me.MaKH,
                NguoiNhan = nguoiNhan,
                MaSP = maSp,
                NoiDung = noiDung,
                NgayGui = DateTime.Now
            });
            db.SaveChanges();
            return RedirectToAction("HopThu", new { withUserId = nguoiNhan, maSp });
        }
    }
}