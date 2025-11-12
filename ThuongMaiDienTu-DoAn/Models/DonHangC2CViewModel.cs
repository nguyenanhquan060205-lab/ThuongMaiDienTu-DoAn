using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ThuongMaiDienTu_DoAn.Models
{
    public class DonHangC2CViewModel
    {
        public int MaHD { get; set; }
        public string NguoiMua { get; set; }
        public string NguoiBan { get; set; }
        public DateTime? NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
    }
}