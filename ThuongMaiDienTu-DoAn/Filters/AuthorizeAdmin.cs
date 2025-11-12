using System;
using System.Web;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;

namespace ThuongMaiDienTu_DoAn.Filters
{
    public class AuthorizeAdmin : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.Session["user"] as NGUOIDUNG;

            if (user == null || !string.Equals(user.VaiTro, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = new RedirectResult("~/TaiKhoan/DangNhap");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
