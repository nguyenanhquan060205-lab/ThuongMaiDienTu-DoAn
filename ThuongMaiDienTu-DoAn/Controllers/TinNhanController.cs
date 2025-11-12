using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Controllers
{
    public class TinNhanController : Controller
    {
        TMDTEntities db = new TMDTEntities();

        // ==========================
        // HIỂN THỊ GIAO DIỆN CHAT
        // ==========================
        public ActionResult Chat(int? idNguoiNhan)
        {
            var currentUser = Session["user"] as NGUOIDUNG;
            if (currentUser == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var admin = db.NGUOIDUNGs.FirstOrDefault(u => u.VaiTro == "Admin");
            if (admin == null)
                ViewBag.ThongBao = "⚠️ Hiện chưa có tài khoản Admin trong hệ thống!";

            // === Lấy danh sách người đã chat (gửi hoặc nhận) ===
            var listNguoiDung = db.TINNHANs
                .Where(t => t.NguoiGui == currentUser.MaKH || t.NguoiNhan == currentUser.MaKH)
                .Select(t => t.NguoiGui == currentUser.MaKH ? t.NGUOIDUNG1 : t.NGUOIDUNG)
                .Distinct()
                .ToList();

            // === Nếu là người dùng bình thường -> thêm admin mặc định ===
            if (currentUser.VaiTro != "Admin" && admin != null && admin.MaKH != currentUser.MaKH)
            {
                bool adminDaGui = db.TINNHANs.Any(t => t.NguoiGui == admin.MaKH && t.NguoiNhan == currentUser.MaKH);
                if (adminDaGui || !listNguoiDung.Any())
                {
                    if (!listNguoiDung.Any(u => u.MaKH == admin.MaKH))
                        listNguoiDung.Insert(0, admin);
                }
            }

            // === Nếu là Admin -> xem tất cả user ===
            if (currentUser.VaiTro == "Admin")
            {
                listNguoiDung = db.NGUOIDUNGs
                    .Where(u => u.MaKH != currentUser.MaKH)
                    .ToList();
            }

            // === Nếu chưa chọn người nhận ===
            if (idNguoiNhan == null)
            {
                ViewBag.NguoiNhanTen = "Chưa chọn người để trò chuyện";
                ViewBag.NguoiNhanID = 0;
                ViewBag.NguoiGuiID = currentUser.MaKH;
                ViewBag.NguoiGuiTen = currentUser.HoTen;
                return View("Chat", listNguoiDung);
            }

            // === Nếu có người nhận cụ thể ===
            var userNhan = db.NGUOIDUNGs.Find(idNguoiNhan);
            if (userNhan == null)
                return HttpNotFound();

            if (!listNguoiDung.Any(u => u.MaKH == userNhan.MaKH))
                listNguoiDung.Add(userNhan);

            ViewBag.NguoiNhanID = userNhan.MaKH;
            ViewBag.NguoiNhanTen = userNhan.HoTen;
            ViewBag.NguoiGuiID = currentUser.MaKH;
            ViewBag.NguoiGuiTen = currentUser.HoTen;

            return View("Chat", listNguoiDung);
        }

        // ==========================
        // LOAD TOÀN BỘ TIN NHẮN
        // ==========================
        public ActionResult LoadTinNhan(int idNguoiGui, int idNguoiNhan)
        {
            var tinNhan = db.TINNHANs
                .Where(t => (t.NguoiGui == idNguoiGui && t.NguoiNhan == idNguoiNhan)
                         || (t.NguoiGui == idNguoiNhan && t.NguoiNhan == idNguoiGui))
                .OrderBy(t => t.NgayGui)
                .Select(t => new
                {
                    t.NguoiGui,
                    t.NguoiNhan,
                    t.NoiDung,
                    Ngay = SqlFunctions.DatePart("hour", t.NgayGui) + ":" +
                           SqlFunctions.DatePart("minute", t.NgayGui) + " " +
                           SqlFunctions.DatePart("day", t.NgayGui) + "/" +
                           SqlFunctions.DatePart("month", t.NgayGui)
                })
                .ToList();

            return Json(tinNhan, JsonRequestBehavior.AllowGet);
        }

        // ==========================
        // GỬI TIN NHẮN
        // ==========================
        [HttpPost]
        public ActionResult GuiTinNhan(int nguoiGui, int nguoiNhan, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung))
                return new HttpStatusCodeResult(400, "Nội dung trống");

            var tin = new TINNHAN
            {
                NguoiGui = nguoiGui,
                NguoiNhan = nguoiNhan,
                NoiDung = noiDung.Trim(),
                NgayGui = DateTime.Now
            };

            db.TINNHANs.Add(tin);
            db.SaveChanges();

            return new HttpStatusCodeResult(200);
        }
    }
}
