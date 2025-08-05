using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SHOP_IPHONE
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
               defaults: new { controller = "Product", action = "Index", id = UrlParameter.Optional }
            );
                routes.MapRoute(
                name: "DienThoai",
                url: "DIENTHOAI/{action}/{id}",
                defaults: new { controller = "DienThoai", action = "DIENTHOAI", slug = "dien-thoai" }
                );

            routes.MapRoute(
                name: "MayTinh",
                url: "may-tinh",
                defaults: new { controller = "Product", action = "CategoryBySlug", slug = "may-tinh" }
            );

            routes.MapRoute(
                name: "DongHo",
                url: "dong-ho",
                defaults: new { controller = "Product", action = "CategoryBySlug", slug = "dong-ho" }
            );


        }
    }
}
