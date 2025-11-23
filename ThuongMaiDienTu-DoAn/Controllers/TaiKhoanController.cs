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


        // GET: TaiKhoan/ThongTinKhachHang/5
        [HttpGet]
        public ActionResult ThongTinKhachHang(int? id)
        {
            var currentUser = Session["user"] as NGUOIDUNG;

            // 1. Chưa đăng nhập -> Đá về Login
            if (currentUser == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NGUOIDUNG targetUser;

            // 2. LOGIC QUAN TRỌNG:
            // Nếu có ID truyền vào VÀ người đang đăng nhập là Admin -> Xem thông tin người khác
            if (id.HasValue && currentUser.VaiTro == "Admin")
            {
                targetUser = db.NGUOIDUNGs.Find(id);
                if (targetUser == null) return HttpNotFound(); // Không tìm thấy user này
            }
            else
            {
                // Ngược lại (Khách xem mình hoặc Admin xem mình) -> Lấy từ Session
                targetUser = db.NGUOIDUNGs.Find(currentUser.MaKH);
            }

            return View(targetUser);
        }

        // GET: TaiKhoan/ThongTinAdmin
        public ActionResult ThongTinAdmin()
        {
            var user = Session["user"] as ThuongMaiDienTu_DoAn.Models.NGUOIDUNG;

            // Kiểm tra: Nếu chưa đăng nhập hoặc không phải Admin thì đuổi về trang đăng nhập
            if (user == null || user.VaiTro != "Admin")
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            return View(user);
        }


        [HttpPost]
        public ActionResult CapNhatThongTin(NGUOIDUNG model, HttpPostedFileBase fileUpload)
        {
            // 1. Tìm user trong DB trước để lấy VaiTro
            var user = db.NGUOIDUNGs.Find(model.MaKH);
            if (user == null) return RedirectToAction("DangNhap");

            // 2. XÁC ĐỊNH TRANG ĐÍCH (QUAN TRỌNG)
            // Nếu là Admin -> Về ThongTinAdmin
            // Nếu là Khách -> Về ThongTinKhachHang
            string actionName = (user.VaiTro == "Admin") ? "ThongTinAdmin" : "ThongTinKhachHang";

            try
            {
                // Kiểm tra Email trùng
                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    db.NGUOIDUNGs.Any(x => x.Email == model.Email && x.MaKH != model.MaKH))
                {
                    TempData["Error"] = "Email đã được sử dụng bởi tài khoản khác!";
                    return RedirectToAction(actionName); // Trả về đúng trang
                }

                // Kiểm tra SĐT trùng
                if (!string.IsNullOrWhiteSpace(model.SDT) &&
                    db.NGUOIDUNGs.Any(x => x.SDT == model.SDT && x.MaKH != model.MaKH))
                {
                    TempData["Error"] = "Số điện thoại đã được sử dụng bởi tài khoản khác!";
                    return RedirectToAction(actionName); // Trả về đúng trang
                }

                // Upload ảnh đại diện
                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    string[] allowedExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string ext = Path.GetExtension(fileUpload.FileName).ToLower();

                    if (!allowedExt.Contains(ext))
                    {
                        TempData["Error"] = "Định dạng ảnh không hợp lệ!";
                        return RedirectToAction(actionName); // Trả về đúng trang
                    }

                    // Tạo thư mục
                    string folder = Server.MapPath("~/Content/Avatars");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    // Tạo tên file
                    string fileName = $"user_{user.MaKH}_{DateTime.Now.Ticks}{ext}";
                    string path = Path.Combine(folder, fileName);

                    // Lưu file
                    fileUpload.SaveAs(path);

                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(user.AnhDaiDien) && user.AnhDaiDien != "default.jpg")
                    {
                        string oldPath = Path.Combine(folder, user.AnhDaiDien);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    user.AnhDaiDien = fileName;
                }

                // Cập nhật thông tin
                user.HoTen = model.HoTen;
                user.GioiTinh = model.GioiTinh;
                // user.Email = model.Email; // Thường Email là tên đăng nhập, hạn chế cho sửa, nhưng nếu bạn muốn sửa thì bỏ comment
                user.SDT = model.SDT;
                user.DiaChi = model.DiaChi;

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                // Cập nhật lại Session để hiển thị ngay lập tức trên Header
                Session["user"] = user;

                TempData["Success"] = "✅ Cập nhật thông tin thành công!";
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (Optional)
                TempData["Error"] = "Đã xảy ra lỗi hệ thống: " + ex.Message;
            }

            // Quay về đúng trang đích đã xác định ở trên
            return RedirectToAction(actionName);
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

        // GET: Chi tiết lịch sử đơn hàng (Admin xem được của tất cả mọi người)
        public ActionResult CT_LichSu(int id)
        {
            var kh = Session["user"] as NGUOIDUNG;

            // 1. Kiểm tra đăng nhập
            if (kh == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // 2. Tìm hóa đơn theo ID (Bỏ điều kiện MaKH ở đây để tìm được đơn của người khác)
            var hd = db.HOADONs.FirstOrDefault(d => d.MaHD == id);

            if (hd == null)
            {
                return HttpNotFound();
            }

            // 3. KIỂM TRA QUYỀN HẠN (QUAN TRỌNG)
            // Nếu người xem KHÔNG PHẢI chủ đơn hàng VÀ cũng KHÔNG PHẢI Admin -> Chặn
            if (hd.MaKH != kh.MaKH && kh.VaiTro != "Admin")
            {
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem đơn hàng này.");
            }

            // 4. Lấy chi tiết sản phẩm
            var chiTiet = db.CT_HOADON
                    .Where(ct => ct.MaHD == id)
                    .Include(ct => ct.SANPHAM)
                    .Include(ct => ct.HOADON)
                    .ToList();

            ViewBag.ChiTiet = chiTiet;

            // 5. Trả về View
            // Nếu bạn muốn Admin xem thì hiện Layout Admin, User xem hiện Layout User thì thêm đoạn này:
            if (kh.VaiTro == "Admin")
            {
                // Cách này giúp Admin không bị nhảy về giao diện người dùng
                // Yêu cầu: View CT_LichSu.cshtml phải hỗ trợ đổi Layout động (giống ThongTin.cshtml)
                // Hoặc đơn giản là return View(hd); nếu View đó dùng Layout = null hoặc logic động.
            }

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
