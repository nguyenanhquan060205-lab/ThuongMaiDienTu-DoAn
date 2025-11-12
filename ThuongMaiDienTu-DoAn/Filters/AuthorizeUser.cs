using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ThuongMaiDienTu_DoAn.Models;
namespace ThuongMaiDienTu_DoAn.Filters
{
    public class AuthorizeUser : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var user = HttpContext.Current.Session["user"] as NGUOIDUNG;

            // Nếu chưa đăng nhập => chuyển hướng đến trang đăng nhập
            if (user == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "TaiKhoan", action = "DangNhap" }
                    )
                );
                return;
            }


            base.OnAuthorization(filterContext);
        }
    }
}