using SHOP_IPHONE.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class BannerController : Controller
    {
        private DBiphoneEntities1 db = new DBiphoneEntities1();

        // Quản lý danh sách banner
        public ActionResult Index()
        {
            var list = db.Banner.OrderByDescending(b => b.created_at).ToList();
            ViewBag.BannerCount = list.Count;
            return View(list);
        }

        // GET: Banner/Create
        // GET: Banner/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Banner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "title,image_path")] Banner banner)
        {
            if (ModelState.IsValid)
            {
                banner.created_at = DateTime.Now;
                banner.is_active = true; // tự động active
                db.Banner.Add(banner);
                db.SaveChanges();

                // Chuyển về Index của Product (hiển thị banner)
                return RedirectToAction("Index", "Product");
            }

            return View(banner);
        }






        // SỬA BANNER

        public ActionResult Edit(int id)
        {
            // Tìm banner theo ID
            Banner banner = db.Banner.Find(id);
            if (banner == null)
            {
                return HttpNotFound();
            }

            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Banner banner)
        {
            if (ModelState.IsValid)
            {
                // Cập nhật thời gian nếu muốn
                banner.created_at = DateTime.Now;

                // Đánh dấu là đã chỉnh sửa
                db.Entry(banner).State = System.Data.Entity.EntityState.Modified;

                // Lưu vào database
                db.SaveChanges();

                // Quay về danh sách
                return RedirectToAction("Index");
            }

            return View(banner);
        }



        // XOÁ BANNER

        public ActionResult Delete(int id)
        {
            // Tìm banner theo ID
            Banner banner = db.Banner.Find(id);
            if (banner == null)
            {
                return HttpNotFound();
            }

            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Banner banner = db.Banner.Find(id);
            if (banner != null)
            {
                db.Banner.Remove(banner);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

    }
}