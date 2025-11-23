using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ThuongMaiDienTu_DoAn.Models
{
    public class AdminProductViewModel
    {
        // Chứa danh sách sản phẩm cho Tab 1
        public List<SANPHAM> SanPhams { get; set; }

        // Chứa danh sách loại cho Tab 2
        public List<LOAISANPHAM> LoaiSanPhams { get; set; }
    }
}