using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Filters;
using ThuongMaiDienTu_DoAn.Models;

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
            ViewBag.TuKhoa = q;
            ViewBag.LoaiDangChon = maloai;

            return View(sp.OrderByDescending(x => x.NgayTao).ToList());
        }


        public ActionResult ChiTiet(int id)
        {
            var sp = db.SANPHAMs.Find(id);
            if (sp == null || sp.TrangThai == "Ẩn")
                return HttpNotFound();

            var danhGia = db.DANHGIAs
                            .Where(d => d.MaSP == id)
                            .ToList();

            ViewBag.TongDanhGia = danhGia.Count();
            ViewBag.TrungBinhDanhGia = danhGia.Any() ? danhGia.Average(d => d.SoSao) : 0;
            ViewBag.AnhChiTiet = db.HINHANHSPs
                                   .Where(a => a.Masp == id && a.AnhBia == false)
                                   .ToList();

            var spLienQuan = db.SANPHAMs
                                .Where(x => x.MaLoai == sp.MaLoai
                                         && x.MaSP != sp.MaSP
                                         && x.TrangThai == "Đã duyệt")
                                .OrderByDescending(x => x.NgayTao)
                                .Take(4)
                                .ToList();

            ViewBag.SPLienQuan = spLienQuan;

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

            // GÁN THÔNG TIN SẢN PHẨM
            m.MaKH = u.MaKH;
            m.NgayTao = DateTime.Now;
            m.TrangThai = "Chờ duyệt";

            // LƯU SẢN PHẨM TRƯỚC
            db.SANPHAMs.Add(m);
            db.SaveChanges();

            // XỬ LÝ ẢNH
            if (files != null && files.Any(f => f != null && f.ContentLength > 0))
            {
                bool firstImage = true;

                foreach (var file in files)
                {
                    if (file == null || file.ContentLength == 0)
                        continue;

                    // Kiểm tra định dạng file ảnh
                    string ext = Path.GetExtension(file.FileName).ToLower();
                    string[] allow = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                    if (!allow.Contains(ext))
                        continue;

                    // Tạo tên file random
                    string fileName = Guid.NewGuid().ToString() + ext;
                    string savePath = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);

                    file.SaveAs(savePath);

                    // Thêm record vào DB
                    db.HINHANHSPs.Add(new HINHANHSP
                    {
                        Masp = m.MaSP,
                        URLAnh = fileName,
                        AnhBia = firstImage // ảnh đầu tiên là ảnh bìa
                    });

                    firstImage = false;
                }

                db.SaveChanges();
            }
            else
            {
                // KHÔNG CÓ ẢNH NÀO → TẠO ẢNH MẶC ĐỊNH
                db.HINHANHSPs.Add(new HINHANHSP
                {
                    Masp = m.MaSP,
                    URLAnh = "noimage.jpg",
                    AnhBia = true
                });
                db.SaveChanges();
            }

            TempData["OK"] = "🎉 Đăng tin thành công! Tin của bạn đang chờ admin duyệt.";
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

        public ActionResult SanPhamDaBan()
        {
            var u = Session["user"] as NGUOIDUNG;
            if (u == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var sanPhamDaBan = db.CT_HOADON
                .Where(ct => ct.SANPHAM.MaKH == u.MaKH && ct.HOADON.MaKH != u.MaKH)
                .Select(ct => new SanPhamDaBanViewModel
                {
                    MaHD = ct.MaHD, 
                    MaSP = ct.MaSP,
                    TenSP = ct.SANPHAM.TenSP,
                    GiaBan = (decimal)ct.SANPHAM.Gia,
                    SoLuongBan = (int)ct.SoLuong,
                    ThanhTien = ct.ThanhTien ?? 0,
                    NguoiMua = ct.HOADON.NGUOIDUNG.HoTen ?? "Không rõ",
                    NgayMua = ct.HOADON.NgayDat ?? DateTime.Now,
                    TrangThai = ct.HOADON.TrangThai
                })
                .OrderByDescending(x => x.NgayMua)
                .ToList();


            return View(sanPhamDaBan);
        }
        // GET method
        public ActionResult HoanThanh(int MaHD)
        {
            var hoaDon = db.HOADONs.FirstOrDefault(h => h.MaHD == MaHD);
            if (hoaDon != null && hoaDon.TrangThai == "Đang chờ xử lý")
            {
                hoaDon.TrangThai = "Đã thanh toán";
                hoaDon.NgayTT = DateTime.Now;
                db.SaveChanges();
                TempData["ThongBao"] = "✅ Hóa đơn đã được đánh dấu là 'Đã thanh toán'.";
            }
            else
            {
                TempData["ThongBao"] = "⚠️ Không thể cập nhật hóa đơn này.";
            }

            return RedirectToAction("SanPhamDaBan");
        }



    }
}
