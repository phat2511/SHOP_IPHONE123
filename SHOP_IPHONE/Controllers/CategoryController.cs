using SHOP_IPHONE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class CategoryController : Controller
    {
        private DBiphoneEntities1 db = new DBiphoneEntities1();

        public PartialViewResult MenuDanhMuc()
        {
            var categories = db.Categories.ToList();
            return PartialView("_MenuDanhMuc", categories);
        }

        // GET: Category
        public ActionResult Index()
        {
            return View();

        }
       
    }
}