using SHOP_IPHONE.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class AddressController : Controller
    {
        DBiphoneEntities1 db = new DBiphoneEntities1();

        public ActionResult Index()
        {
            int accountId = (int)Session["account_id"];
            var list = db.Addresses.Where(a => a.AccountId == accountId).ToList();
            return View(list);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Address model)
        {
            if (ModelState.IsValid)
            {
                model.AccountId = (int)Session["account_id"];
                db.Addresses.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var addr = db.Addresses.Find(id);
            return View(addr);
        }

        [HttpPost]
        public ActionResult Edit(Address model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Delete(int id)
        {
            var addr = db.Addresses.Find(id);
            db.Addresses.Remove(addr);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}