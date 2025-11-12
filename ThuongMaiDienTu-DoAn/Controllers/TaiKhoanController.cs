using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly TMDTEntities db = new TMDTEntities();

        // ========== [ĐĂNG NHẬP] ==========
        [HttpGet]
        public ActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(string taikhoan, string matkhau)
        {
            if (string.IsNullOrWhiteSpace(taikhoan) || string.IsNullOrWhiteSpace(matkhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin đăng nhập!";
                return View();
            }

            var user = db.NGUOIDUNGs
                .FirstOrDefault(u => u.TaiKhoan == taikhoan && u.MatKhau == matkhau);

            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
                return View();
            }

            Session["user"] = user;
            if (user.VaiTro == "Admin")
                return RedirectToAction("Index", "Admin");
            else
                return RedirectToAction("Index", "Home");
        }

        // ========== [ĐĂNG KÝ] ==========
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(NGUOIDUNG nd)
        {
            if (!ModelState.IsValid)
                return View(nd);

            if (db.NGUOIDUNGs.Any(x => x.TaiKhoan == nd.TaiKhoan || x.Email == nd.Email))
            {
                ViewBag.Error = "Tài khoản hoặc email đã tồn tại!";
                return View(nd);
            }

            nd.VaiTro = "User";
            nd.NgayTao = DateTime.Now;
            nd.AnhDaiDien = "default.jpg";

            db.NGUOIDUNGs.Add(nd);
            db.SaveChanges();

            Session["user"] = nd;
            return RedirectToAction("Index", "Home");
        }

        // ========== [THÔNG TIN CÁ NHÂN] ==========
        [HttpGet]
        public ActionResult ThongTin()
        {
            var user = Session["user"] as NGUOIDUNG;
            if (user == null)
                return RedirectToAction("DangNhap");

            var currentUser = db.NGUOIDUNGs.Find(user.MaKH);
            if (currentUser == null)
            {
                Session.Clear();
                return RedirectToAction("DangNhap");
            }

            return View(currentUser);
        }

        // ========== [CẬP NHẬT THÔNG TIN + ẢNH] ==========
        [HttpPost]

        public ActionResult CapNhatThongTin(NGUOIDUNG model, HttpPostedFileBase fileUpload)
        {
            try
            {
                var user = db.NGUOIDUNGs.Find(model.MaKH);
                if (user == null)
                    return RedirectToAction("DangNhap");

                // Kiểm tra Email trùng
                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    db.NGUOIDUNGs.Any(x => x.Email == model.Email && x.MaKH != model.MaKH))
                {
                    TempData["Error"] = "Email đã được sử dụng bởi tài khoản khác!";
                    return RedirectToAction("ThongTin");
                }

                // Kiểm tra SĐT trùng
                if (!string.IsNullOrWhiteSpace(model.SDT) &&
                    db.NGUOIDUNGs.Any(x => x.SDT == model.SDT && x.MaKH != model.MaKH))
                {
                    TempData["Error"] = "Số điện thoại đã được sử dụng bởi tài khoản khác!";
                    return RedirectToAction("ThongTin");
                }

                // ✅ Upload ảnh đại diện
                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    string[] allowedExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string ext = Path.GetExtension(fileUpload.FileName).ToLower();

                    if (!allowedExt.Contains(ext))
                    {
                        TempData["Error"] = "Định dạng ảnh không hợp lệ!";
                        return RedirectToAction("ThongTin");
                    }

                    // 🧩 Tạo thư mục nếu chưa có
                    string folder = Server.MapPath("~/Content/Avatars");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    // 🧩 Tạo tên file an toàn
                    string fileName = $"user_{user.MaKH}_{DateTime.Now.Ticks}{ext}";
                    string path = Path.Combine(folder, fileName);

                    // 🧩 Lưu file
                    fileUpload.SaveAs(path);

                    // 🧩 Xóa ảnh cũ nếu không phải default
                    if (!string.IsNullOrEmpty(user.AnhDaiDien) && user.AnhDaiDien != "default.jpg")
                    {
                        string oldPath = Path.Combine(folder, user.AnhDaiDien);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Cập nhật DB
                    user.AnhDaiDien = fileName;
                }

                // 🧩 Cập nhật thông tin cá nhân
                user.HoTen = model.HoTen;
                user.GioiTinh = model.GioiTinh;
                user.Email = model.Email;
                user.SDT = model.SDT;
                user.DiaChi = model.DiaChi;

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                Session["user"] = user;
                TempData["Success"] = "Cập nhật thành công!";
            }
            catch (Exception ex)
            {
                string logPath = Server.MapPath("~/error_log.txt");
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now}: {ex}\n");

                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật thông tin!";
            }

            return RedirectToAction("ThongTin");
        }

        // ========== [ĐĂNG XUẤT] ==========
        public ActionResult DangXuat()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ========== [Lịch sử] ==========
        public ActionResult LichSu()
        {
            var kh = Session["user"] as NGUOIDUNG;
            if (kh == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var dsDonHang = db.HOADONs
                .Where(d => d.MaKH == kh.MaKH)
                .OrderByDescending(d => d.NgayDat)
                .Select(d => new LichSuViewModel
                {
                    MaHD = d.MaHD,
                    NgayDat = d.NgayDat,
                    NgayTT = d.NgayTT,
                    TrangThai = d.TrangThai,
                    PhuongThucTT = d.PhuongThucTT,
                    DaDanhGia = db.CT_HOADON
                                    .Where(ct => ct.MaHD == d.MaHD)
                                    .All(ct => db.DANHGIAs.Any(dg => dg.MaKH == kh.MaKH && dg.MaSP == ct.MaSP))
                })
                .ToList();

            return View(dsDonHang);
        }

        public ActionResult CT_LichSu(int id)
        {
            var kh = Session["user"] as NGUOIDUNG;
            var hd = db.HOADONs.FirstOrDefault(d => d.MaHD == id && d.MaKH == kh.MaKH);
            if (hd == null)
            {
                return HttpNotFound();
            }
            var chiTiet = db.CT_HOADON
                    .Where(ct => ct.MaHD == id)
                    .Include(ct => ct.SANPHAM)
                    .Include(ct => ct.HOADON)
                    .ToList();
            ViewBag.ChiTiet = chiTiet;
            return View(hd);
        }
        [HttpGet]
        public ActionResult HuyDonHang(int id)
        {
            var kh = Session["user"] as NGUOIDUNG;
            if (kh == null)
            {
                TempData["ThongBao"] = "Vui lòng đăng nhập để thực hiện!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var hd = db.HOADONs.FirstOrDefault(d => d.MaHD == id && d.MaKH == kh.MaKH);
            if (hd == null)
            {
                return HttpNotFound();
            }

            if (hd.TrangThai == "Đang chờ xử lý")
            {
                // Hoàn lại số lượng sản phẩm
                var chiTiet = db.CT_HOADON.Where(ct => ct.MaHD == hd.MaHD).ToList();
                foreach (var item in chiTiet)
                {
                    var sp = db.SANPHAMs.Find(item.MaSP);
                    if (sp != null)
                    {
                        sp.SoLuong += item.SoLuong;
                        if (sp.TrangThai == "Đã bán" && sp.SoLuong > 0)
                            sp.TrangThai = "Đã duyệt";
                    }
                }

                hd.TrangThai = "Đã Huỷ";
                db.SaveChanges();
                TempData["ThongBao"] = "Đơn hàng đã được hủy thành công!";
            }

            else
            {
                TempData["ThongBao"] = "Đơn hàng không thể hủy vì đã giao hoặc hoàn tất!";
            }

            return RedirectToAction("LichSu");
        }
        [HttpGet]
        public ActionResult SuaDonHang(int id)
        {
            var kh = Session["user"] as NGUOIDUNG;
            if (kh == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var hd = db.HOADONs.FirstOrDefault(d => d.MaHD == id && d.MaKH == kh.MaKH);
            if (hd == null)
            {
                return HttpNotFound();
            }

            return View(hd); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaDonHang(HOADON model)
        {
            var kh = Session["user"] as NGUOIDUNG;
            if (kh == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var hd = db.HOADONs.Include("CT_HOADON")
                       .FirstOrDefault(d => d.MaHD == model.MaHD && d.MaKH == kh.MaKH);
            if (hd == null)
                return HttpNotFound();

            // Cập nhật số lượng từ model (nếu cần)
            foreach (var ctModel in model.CT_HOADON)
            {
                var ct = hd.CT_HOADON.FirstOrDefault(c => c.MaSP == ctModel.MaSP);
                if (ct != null)
                {
                    var sp = db.SANPHAMs.Find(ct.MaSP);
                    if (sp == null)
                    {
                        ModelState.AddModelError("", $"Sản phẩm {ct.MaSP} không tồn tại!");
                        return View(hd);
                    }

                    // Kiểm tra tồn kho
                    if (ctModel.SoLuong > sp.SoLuong + ct.SoLuong)
                    {
                        ModelState.AddModelError("",
                            $"Số lượng sản phẩm '{sp.TenSP}' không đủ. Tồn kho: {sp.SoLuong + ct.SoLuong}");
                        return View(hd);
                    }

                    ct.SoLuong = ctModel.SoLuong;
                    ct.ThanhTien = ct.SoLuong * sp.Gia;
                }
            }

            // Cập nhật thông tin HOADON
            hd.PhuongThucTT = model.PhuongThucTT;
            hd.DiaChiGiaoHang = model.DiaChiGiaoHang;

            db.SaveChanges();
            TempData["ThongBao"] = "Cập nhật đơn hàng thành công!";
            return RedirectToAction("LichSu");
        }
        // GET: Hiển thị form đánh giá
        public ActionResult DanhGia(int maHD)
        {
            var hoaDon = db.CT_HOADON
                           .Where(ct => ct.MaHD == maHD)
                           .Select(ct => new DanhGiaViewModel
                           {
                               MaSP = ct.MaSP,
                               TenSP = ct.SANPHAM.TenSP
                           }).ToList();

            ViewBag.MaHD = maHD;
            return View(hoaDon);
        }
        // POST: Lưu đánh giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DanhGia(int maHD, int maSP, int soSao, string noiDung)
        {
            var kh = Session["user"] as NGUOIDUNG;
            if (kh == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var danhGia = new DANHGIA
            {
                MaKH = kh.MaKH,
                MaSP = maSP,
                SoSao = soSao,
                NoiDung = noiDung,
                NgayDG = DateTime.Now
            };
            db.DANHGIAs.Add(danhGia);
            db.SaveChanges();

            TempData["ThongBao"] = "✅ Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("LichSu");
        }

    }
}
