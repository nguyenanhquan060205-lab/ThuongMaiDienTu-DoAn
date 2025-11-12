using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ThuongMaiDienTu_DoAn.Models
{
    public class LichSuViewModel
    {
        public int MaHD { get; set; }
        public DateTime? NgayDat { get; set; }
        public DateTime? NgayTT { get; set; }
        public string TrangThai { get; set; }
        public string PhuongThucTT { get; set; }
        public bool DaDanhGia { get; set; }
    }
}