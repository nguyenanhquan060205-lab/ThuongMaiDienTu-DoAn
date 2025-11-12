using ThuongMaiDienTu_DoAn.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class DanhGiaController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        [HttpPost]
        public ActionResult Them(int maSP, int soSao, string noiDung)
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var dg = new DANHGIA
            {
                MaSP = maSP,
                MaKH = user.MaKH,
                SoSao = soSao,
                NoiDung = noiDung,
                NgayDG = DateTime.Now
            };

            db.DANHGIAs.Add(dg);
            db.SaveChanges();
            return RedirectToAction("ChiTiet", "Home", new { id = maSP });
        }
    }
}