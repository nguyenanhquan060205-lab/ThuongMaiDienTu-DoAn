using System;
using System.Linq;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class LoaiSanPhamController : Controller
    {
        private TMDTEntities db = new TMDTEntities();

        // 1. THÊM
        [HttpPost]
        public ActionResult Them(string TenLoai)
        {
            // Gửi tín hiệu để View biết là phải mở Tab Danh mục
            TempData["ActiveTab"] = "category";

            if (string.IsNullOrWhiteSpace(TenLoai))
            {
                TempData["Error"] = "Tên loại không được để trống!";
                return RedirectToAction("QuanLySanPham", "Admin");
            }

            if (db.LOAISANPHAMs.Any(x => x.TenLoai == TenLoai))
            {
                TempData["Error"] = "Tên danh mục này đã tồn tại!";
                return RedirectToAction("QuanLySanPham", "Admin");
            }

            try
            {
                db.LOAISANPHAMs.Add(new LOAISANPHAM { TenLoai = TenLoai });
                db.SaveChanges();
                TempData["Success"] = "✅ Thêm thành công!";
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống!";
            }

            // Load lại trang Dashboard (QuanLySanPham)
            return RedirectToAction("QuanLySanPham", "Admin");
        }

        // 2. SỬA
        [HttpPost]
        public ActionResult Sua(int id, string TenLoaiMoi)
        {
            TempData["ActiveTab"] = "category"; // Giữ tab

            var loai = db.LOAISANPHAMs.Find(id);
            if (loai != null && !string.IsNullOrWhiteSpace(TenLoaiMoi))
            {
                loai.TenLoai = TenLoaiMoi;
                db.SaveChanges();
                TempData["Success"] = "✅ Cập nhật thành công!";
            }
            else
            {
                TempData["Error"] = "Lỗi cập nhật!";
            }

            return RedirectToAction("QuanLySanPham", "Admin");
        }

        // 3. XÓA
        [HttpPost]
        public ActionResult Xoa(int id)
        {
            TempData["ActiveTab"] = "category"; // Giữ tab

            var loai = db.LOAISANPHAMs.Find(id);
            bool coSP = db.SANPHAMs.Any(x => x.MaLoai == id);

            if (loai != null && !coSP)
            {
                db.LOAISANPHAMs.Remove(loai);
                db.SaveChanges();
                TempData["Success"] = "🗑️ Đã xóa!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa (đang có SP hoặc lỗi)!";
            }

            return RedirectToAction("QuanLySanPham", "Admin");
        }
    }
}