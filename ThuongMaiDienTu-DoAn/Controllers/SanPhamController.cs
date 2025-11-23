using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Filters; // Giữ nguyên nếu bạn có dùng Filter
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly TMDTEntities db = new TMDTEntities();

        // 1. TRANG CHỦ / DANH SÁCH
        public ActionResult Index(string q, int? maloai)
        {
            // CHỈ HIỆN TIN ĐÃ DUYỆT (Ẩn/Khóa thì không hiện)
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

        // 2. CHI TIẾT SẢN PHẨM
        public ActionResult ChiTiet(int id)
        {
            var sp = db.SANPHAMs.Find(id);

            // Nếu SP không tồn tại hoặc bị Ẩn -> Báo lỗi 404
            // (Trừ khi là chủ sở hữu hoặc Admin thì vẫn cho xem - Logic này bạn có thể mở rộng sau)
            if (sp == null || sp.TrangThai == "Ẩn")
            {
                // Mở rộng: Cho phép chủ sở hữu xem tin bị ẩn của chính mình
                var u = Session["user"] as NGUOIDUNG;
                if (u != null && sp != null && sp.MaKH == u.MaKH)
                {
                    // Cho qua (Không return 404)
                }
                else
                {
                    return HttpNotFound();
                }
            }

            var danhGia = db.DANHGIAs.Where(d => d.MaSP == id).ToList();
            ViewBag.TongDanhGia = danhGia.Count();
            ViewBag.TrungBinhDanhGia = danhGia.Any() ? danhGia.Average(d => d.SoSao) : 0;

            ViewBag.AnhChiTiet = db.HINHANHSPs
                .Where(a => a.Masp == id && a.AnhBia == false)
                .ToList();

            var spLienQuan = db.SANPHAMs
                .Where(x => x.MaLoai == sp.MaLoai && x.MaSP != sp.MaSP && x.TrangThai == "Đã duyệt")
                .OrderByDescending(x => x.NgayTao)
                .Take(4)
                .ToList();

            ViewBag.SPLienQuan = spLienQuan;

            // Kiểm tra quyền sở hữu
            var currentUser = Session["user"] as NGUOIDUNG;
            if (currentUser != null && currentUser.MaKH == sp.MaKH)
            {
                return View("ChiTietCuaNguoiBan", sp);
            }

            return View(sp);
        }

        // 3. TẠO MỚI (GET)
        [HttpGet]
        public ActionResult TaoMoi()
        {
            if (Session["user"] == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            ViewBag.MaLoai = new SelectList(db.LOAISANPHAMs, "MaLoai", "TenLoai");
            return View();
        }

        // 3. TẠO MỚI (POST) - SỬA LOGIC TRẠNG THÁI TẠI ĐÂY
        [HttpPost]
        [ValidateInput(false)] // Cho phép nhập HTML nếu cần
        public ActionResult TaoMoi(SANPHAM m, IEnumerable<HttpPostedFileBase> files)
        {
            var u = Session["user"] as NGUOIDUNG;
            if (u == null) return RedirectToAction("DangNhap", "TaiKhoan");

            // GÁN THÔNG TIN
            m.MaKH = u.MaKH;
            m.NgayTao = DateTime.Now;

            // [THAY ĐỔI]: Mặc định là "Đã duyệt" luôn (Bỏ qua bước chờ duyệt)
            m.TrangThai = "Đã duyệt";

            db.SANPHAMs.Add(m);
            db.SaveChanges(); // Lưu để lấy MaSP

            // XỬ LÝ ẢNH (Giữ nguyên logic cũ của bạn)
            if (files != null && files.Any(f => f != null && f.ContentLength > 0))
            {
                bool firstImage = true;
                foreach (var file in files)
                {
                    if (file == null || file.ContentLength == 0) continue;

                    string ext = Path.GetExtension(file.FileName).ToLower();
                    string[] allow = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!allow.Contains(ext)) continue;

                    string fileName = Guid.NewGuid().ToString() + ext;
                    string savePath = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                    file.SaveAs(savePath);

                    db.HINHANHSPs.Add(new HINHANHSP
                    {
                        Masp = m.MaSP,
                        URLAnh = fileName,
                        AnhBia = firstImage
                    });
                    firstImage = false;
                }
                db.SaveChanges();
            }
            else
            {
                // Ảnh mặc định
                db.HINHANHSPs.Add(new HINHANHSP { Masp = m.MaSP, URLAnh = "noimage.jpg", AnhBia = true });
                db.SaveChanges();
            }

            TempData["OK"] = "🎉 Đăng tin thành công! Sản phẩm đã được hiển thị.";
            return RedirectToAction("CuaToi");
        }

        // 4. TIN CỦA TÔI
        public ActionResult CuaToi()
        {
            var u = Session["user"] as NGUOIDUNG;
            if (u == null) return RedirectToAction("DangNhap", "TaiKhoan");

            var list = db.SANPHAMs
                .Where(x => x.MaKH == u.MaKH)
                .OrderByDescending(x => x.NgayTao)
                .ToList();

            return View(list);
        }

        // 5. SỬA TIN (GET)
        [HttpGet]
        public ActionResult Sua(int id)
        {
            var sanPham = db.SANPHAMs.Find(id);
            // Kiểm tra quyền sở hữu (bổ sung cho an toàn)
            var u = Session["user"] as NGUOIDUNG;
            if (u == null || sanPham == null || sanPham.MaKH != u.MaKH)
            {
                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.LOAISANPHAMs, "MaLoai", "TenLoai", sanPham.MaLoai);
            return View(sanPham);
        }

        // 5. SỬA TIN (POST) - SỬA LOGIC TRẠNG THÁI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Sua(SANPHAM model, IEnumerable<HttpPostedFileBase> files, int? id)
        {
            var sanPhamGoc = db.SANPHAMs.Find(id.Value);

            // Cập nhật thông tin
            sanPhamGoc.TenSP = model.TenSP;
            sanPhamGoc.Gia = model.Gia;
            sanPhamGoc.MoTa = model.MoTa;
            sanPhamGoc.SoLuong = model.SoLuong;
            sanPhamGoc.MaLoai = model.MaLoai;

            // [THAY ĐỔI]: Sửa xong vẫn cho hiện luôn, không cần chờ duyệt lại
            // Trừ khi sản phẩm đang bị Admin "Khóa/Ẩn" vì vi phạm thì giữ nguyên trạng thái "Ẩn"
            if (sanPhamGoc.TrangThai != "Ẩn")
            {
                sanPhamGoc.TrangThai = "Đã duyệt";
            }

            db.Entry(sanPhamGoc).State = EntityState.Modified;
            db.SaveChanges();

            // (Phần xử lý ảnh sửa đổi để sau, hoặc bạn có thể thêm logic xóa ảnh cũ thêm ảnh mới ở đây)

            TempData["OK"] = "✅ Cập nhật thành công!";
            return RedirectToAction("ChiTiet", new { id = sanPhamGoc.MaSP });
        }

        // 6. XÓA TIN
        [HttpGet]
        public ActionResult Xoa(int id)
        {
            var u = Session["user"] as NGUOIDUNG;
            var sanPham = db.SANPHAMs.Find(id);

            if (u == null || sanPham == null || sanPham.MaKH != u.MaKH)
            {
                TempData["Loi"] = "Bạn không có quyền xóa sản phẩm này.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Xóa ảnh server
                var hinhAnh = db.HINHANHSPs.Where(a => a.Masp == id).ToList();
                string path = Server.MapPath("~/Content/Images/");
                foreach (var anh in hinhAnh)
                {
                    string fullPath = Path.Combine(path, anh.URLAnh);
                    if (System.IO.File.Exists(fullPath) && anh.URLAnh != "noimage.jpg")
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Xóa ảnh DB
                db.HINHANHSPs.RemoveRange(hinhAnh);

                // Xóa sản phẩm
                db.SANPHAMs.Remove(sanPham);
                db.SaveChanges();

                TempData["OK"] = "🗑️ Sản phẩm đã được xóa thành công.";
            }
            catch (Exception)
            {
                // Nếu dính khóa ngoại (đã có đơn hàng) -> Chuyển sang trạng thái Ẩn thay vì Xóa
                // Đây là giải pháp an toàn (Soft Delete)
                sanPham.TrangThai = "Ẩn"; // Hoặc "Đã xóa"
                db.SaveChanges();
                TempData["OK"] = "Sản phẩm đã được ẩn (do đã có lịch sử giao dịch).";
            }

            return RedirectToAction("CuaToi");
        }

        // 7. CÁC HÀM KHÁC (SanPhamDaBan, HoanThanh...) - GIỮ NGUYÊN
        public ActionResult SanPhamDaBan()
        {
            var u = Session["user"] as NGUOIDUNG;
            if (u == null) return RedirectToAction("DangNhap", "TaiKhoan");

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