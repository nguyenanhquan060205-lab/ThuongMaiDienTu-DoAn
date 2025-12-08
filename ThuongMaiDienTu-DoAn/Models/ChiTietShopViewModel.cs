using System.Collections.Generic;
using ThuongMaiDienTu_DoAn.Models; 

namespace ThuongMaiDienTu_DoAn.Models
{
    public class ChiTietShopViewModel
    {
        public NGUOIDUNG Shop { get; set; }
        public List<SANPHAM> SanPhams { get; set; }
    }
}