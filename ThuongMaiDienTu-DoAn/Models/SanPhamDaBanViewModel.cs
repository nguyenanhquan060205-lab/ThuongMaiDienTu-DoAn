using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ThuongMaiDienTu_DoAn.Models
{
    public class SanPhamDaBanViewModel
    {
        public int MaHD { get; set; }
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongBan { get; set; }
        public decimal ThanhTien { get; set; } 
        public string NguoiMua { get; set; }
        public DateTime NgayMua { get; set; } 
        public string TrangThai { get; set; }
    }
}