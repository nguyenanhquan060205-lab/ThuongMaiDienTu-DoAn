using System;
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

            // Danh sách người đã chat với mình
            var listNguoiDung = db.TINNHANs
                .Where(t => t.NguoiGui == currentUser.MaKH || t.NguoiNhan == currentUser.MaKH)
                .Select(t => t.NguoiGui == currentUser.MaKH ? t.NGUOIDUNG1 : t.NGUOIDUNG)
                .Distinct()
                .ToList();

            // User thường → ép admin vào đầu danh sách
            if (currentUser.VaiTro != "Admin" && admin != null)
            {
                if (!listNguoiDung.Any(u => u.MaKH == admin.MaKH))
                    listNguoiDung.Insert(0, admin);
            }

            // Admin → thấy toàn bộ user
            if (currentUser.VaiTro == "Admin")
            {
                listNguoiDung = db.NGUOIDUNGs
                    .Where(u => u.MaKH != currentUser.MaKH)
                    .ToList();
            }

            // Nếu chưa chọn người để nhắn
            if (idNguoiNhan == null)
            {
                ViewBag.NguoiNhanID = 0;
                ViewBag.NguoiNhanTen = "Chưa chọn người để trò chuyện";
                ViewBag.NguoiGuiID = currentUser.MaKH;
                return View(listNguoiDung);
            }

            var userNhan = db.NGUOIDUNGs.Find(idNguoiNhan);
            if (userNhan == null)
                return HttpNotFound();

            ViewBag.NguoiNhanID = userNhan.MaKH;
            ViewBag.NguoiNhanTen = userNhan.HoTen;
            ViewBag.NguoiGuiID = currentUser.MaKH;

            return View(listNguoiDung);
        }

        // ==========================
        // LOAD TIN NHẮN + UPDATE "ĐÃ XEM"
        // ==========================
        public ActionResult LoadTinNhan(int idNguoiGui, int idNguoiNhan)
        {
            // Lấy toàn bộ đoạn chat
            var list = db.TINNHANs
                .Where(t =>
                    (t.NguoiGui == idNguoiGui && t.NguoiNhan == idNguoiNhan) ||
                    (t.NguoiGui == idNguoiNhan && t.NguoiNhan == idNguoiGui))
                .OrderBy(t => t.NgayGui)
                .ToList()
                .Select(t =>
                {
                    // ==== FIX AVATAR TỰ ĐỘNG GIỐNG LAYOUT ADMIN ====
                    string avatar = string.IsNullOrEmpty(t.NGUOIDUNG.AnhDaiDien)
                        ? "Default.jpg"
                        : t.NGUOIDUNG.AnhDaiDien;

                    string avatarPath = Server.MapPath("~/Content/avatars/" + avatar);
                    if (!System.IO.File.Exists(avatarPath))
                    {
                        avatar = "Default.jpg";
                    }

                    return new
                    {
                        t.NguoiGui,
                        t.NguoiNhan,
                        NoiDung = t.NoiDung,
                        Gio = t.NgayGui.HasValue ? t.NgayGui.Value.ToString("HH:mm dd/MM") : "",
                        AvatarGui = avatar,     // <<< avatar đã kiểm tra tồn tại
                        t.DaDoc                  // giữ trạng thái đã đọc
                    };
                });


            return Json(list, JsonRequestBehavior.AllowGet);
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
                NgayGui = DateTime.Now,
                DaDoc = false
            };

            db.TINNHANs.Add(tin);
            db.SaveChanges();

            return new HttpStatusCodeResult(200);
        }
    }
}
