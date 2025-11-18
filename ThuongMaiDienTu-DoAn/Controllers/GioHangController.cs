using ThuongMaiDienTu_DoAn.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class GioHangController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        // ------------------- 🛒 ICON GIỎ HÀNG -------------------
        [ChildActionOnly]
        public ActionResult CartIcon()
        {
            var user = Session["user"] as NGUOIDUNG;
            int tong = 0;

            if (user != null)
            {
                var gio = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
                if (gio != null)
                    tong = gio.TongSoLuong ?? 0;
            }

            ViewBag.TongSoLuong = tong;
            return PartialView("CartIcon");
        }

        // ------------------- 🧾 TRANG GIỎ HÀNG -------------------
        public ActionResult Index()
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var gio = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
            if (gio == null)
            {
                gio = new GIOHANG { MaKH = user.MaKH };
                db.GIOHANGs.Add(gio);
                db.SaveChanges();
            }

            var ds = db.CT_GIOHANG
                       .Where(c => c.MaGH == gio.MaGH)
                       .Include(c => c.SANPHAM)
                       .Include(c => c.SANPHAM.HINHANHSPs)
                       .ToList();

            return View(ds);
        }

        //  THÊM SẢN PHẨM  
        public ActionResult Them(int id)
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var sp = db.SANPHAMs.Find(id);
            if (sp == null || sp.TrangThai != "Đã duyệt")
                return RedirectToAction("Index", "SanPham");

            //  Không cho người bán tự mua hàng của chính mình
            if (sp.MaKH == user.MaKH)
            {
                TempData["CartError"] = "Bạn không thể mua sản phẩm của chính mình!";
                return RedirectToAction("ChiTiet", "SanPham", new { id });
            }

            //  Kiểm tra xem có ai khác đã mua (đơn thật)
            bool daBan = db.CT_HOADON.Any(x => x.MaSP == id &&
                (x.HOADON.TrangThai == "Đã thanh toán" || x.HOADON.TrangThai == "Đang vận chuyển"));
            if (daBan || sp.SoLuong <= 0)
            {
                TempData["CartError"] = "Sản phẩm này đã được đặt mua bởi người khác!";
                return RedirectToAction("Index", "SanPham");
            }

            //  Lấy giỏ hàng người dùng
            var gio = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
            if (gio == null)
            {
                gio = new GIOHANG { MaKH = user.MaKH };
                db.GIOHANGs.Add(gio);
                db.SaveChanges();
            }

            //   Thêm sản phẩm vào chi tiết giỏ hàng
            var ct = db.CT_GIOHANG.FirstOrDefault(c => c.MaGH == gio.MaGH && c.MaSP == id);
            if (ct == null)
                db.CT_GIOHANG.Add(new CT_GIOHANG { MaGH = gio.MaGH, MaSP = id, SoLuong = 1, ThanhTien = sp.Gia });
            else
                ct.SoLuong = 1; // Sản phẩm chỉ được 1 cái duy nhất (vì hàng cũ)

            db.SaveChanges();

            //  Cập nhật lại số lượng giỏ hàng trong session
            var gioUpdate = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
            Session["CartCount"] = gioUpdate?.TongSoLuong ?? 0;

            TempData["CartOK"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        // TĂNG SỐ LƯỢNG 
        public ActionResult Tang(int id)
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var gio = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
            if (gio == null) return RedirectToAction("Index");

            var ct = db.CT_GIOHANG.FirstOrDefault(c => c.MaGH == gio.MaGH && c.MaSP == id);
            var sp = db.SANPHAMs.Find(id);

            if (ct != null && sp != null)
            {
                if (ct.SoLuong < sp.SoLuong)
                {
                    ct.SoLuong++;
                    ct.ThanhTien = ct.SoLuong * sp.Gia;
                    db.SaveChanges();
                    Session["CartCount"] = gio.TongSoLuong ?? 0;
                }
                else
                {
                    TempData["CartWarning"] = $"⚠️ Sản phẩm '{sp.TenSP}' còn {sp.SoLuong} sản phẩm!";
                }
            }

            return RedirectToAction("Index");
        }

        //   GIẢM SỐ LƯỢNG 
        public ActionResult Giam(int id)
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var gio = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
            if (gio == null) return RedirectToAction("Index");

            var ct = db.CT_GIOHANG.FirstOrDefault(c => c.MaGH == gio.MaGH && c.MaSP == id);
            if (ct != null)
            {
                var sp = db.SANPHAMs.Find(id);
                if (ct.SoLuong > 1)
                {
                    ct.SoLuong--;
                    ct.ThanhTien = ct.SoLuong * sp.Gia;
                }
                else
                {
                    db.CT_GIOHANG.Remove(ct);
                }
                db.SaveChanges();
                Session["CartCount"] = gio.TongSoLuong ?? 0;
            }

            return RedirectToAction("Index");
        }

        // XOÁ SẢN PHẨM 
        public ActionResult Xoa(int id)
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var gio = db.GIOHANGs.FirstOrDefault(g => g.MaKH == user.MaKH);
            if (gio == null) return RedirectToAction("Index");

            var ct = db.CT_GIOHANG.FirstOrDefault(c => c.MaGH == gio.MaGH && c.MaSP == id);
            if (ct != null)
            {
                db.CT_GIOHANG.Remove(ct);
                db.SaveChanges();
                Session["CartCount"] = gio.TongSoLuong ?? 0;
            }

            TempData["CartOK"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }
    }
}
