using ThuongMaiDienTu_DoAn.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class HoaDonController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        //Khi khách đặt hàng  
        public ActionResult DatHang()
        {
            var kh = Session["user"] as NGUOIDUNG;
            if (kh == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var gio = db.GIOHANGs.Include("CT_GIOHANG").FirstOrDefault(g => g.MaKH == kh.MaKH);
            if (gio == null || !gio.CT_GIOHANG.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "GioHang");
            }

            // Tạo hóa đơn
            var hd = new HOADON
            {
                MaKH = kh.MaKH,
                NgayDat = DateTime.Now,
                PhuongThucTT = "Thanh toán khi nhận hàng",
                TrangThai = "Đang chờ xử lý",
                TongTien = 0
            };
            db.HOADONs.Add(hd);
            db.SaveChanges();

            foreach (var item in gio.CT_GIOHANG)
            {
                var sp = db.SANPHAMs.Find(item.MaSP);
                if (sp == null) continue;

                db.CT_HOADON.Add(new CT_HOADON
                {
                    MaHD = hd.MaHD,
                    MaSP = item.MaSP,
                    SoLuong = item.SoLuong,
                    ThanhTien = item.ThanhTien
                });

                hd.TongTien += item.ThanhTien;
            }

            db.CT_GIOHANG.RemoveRange(gio.CT_GIOHANG);
            db.SaveChanges();

            TempData["Success"] = "Đặt hàng thành công! Đơn hàng đang chờ xử lý.";
            return RedirectToAction("ChiTiet", new { id = hd.MaHD });
        }

        // Khi Admin xác nhận đã giao hàng thành công (COD)
        [HttpPost]
        public ActionResult XacNhanThanhToan(int id)
        {
            var hd = db.HOADONs.Include("CT_HOADON").FirstOrDefault(h => h.MaHD == id);
            if (hd == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction("DanhSachDonHang");
            }

            if (hd.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] = "Đơn hàng này đã được thanh toán rồi.";
                return RedirectToAction("DanhSachDonHang");
            }

            // Trừ hàng thật
            foreach (var item in hd.CT_HOADON)
            {
                var sp = db.SANPHAMs.Find(item.MaSP);
                if (sp != null)
                {
                    sp.SoLuong -= item.SoLuong;
                    if (sp.SoLuong <= 0)
                        sp.TrangThai = "Đã bán";
                }
            }

            // Cập nhật trạng thái hóa đơn
            hd.TrangThai = "Đã thanh toán";
            hd.NgayTT = DateTime.Now;
            db.SaveChanges();

            TempData["Success"] = $"Đơn hàng #{hd.MaHD} đã được thanh toán và trừ hàng.";
            return RedirectToAction("DanhSachDonHang");
        }

        //   Danh sách đơn hàng cho admin xem
        public ActionResult DanhSachDonHang()
        {
            var list = db.HOADONs.Include("NGUOIDUNG").OrderByDescending(h => h.NgayDat).ToList();
            return View(list);
        }

        // Chi tiết đơn hàng
        public ActionResult ChiTiet(int id)
        {
            var hd = db.HOADONs.Include("CT_HOADON.SANPHAM").FirstOrDefault(h => h.MaHD == id);
            if (hd == null) return RedirectToAction("DanhSachDonHang");
            return View(hd);
        }
    }
}
