using SHOP_IPHONE.Helpers;
using SHOP_IPHONE.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class AccountAdminController : Controller
    {
        private DBiphoneEntities1 db = new DBiphoneEntities1();
        public ActionResult Index()
        {
            if (Session["role"] == null)
                return RedirectToAction("Login", "Account");

            if ((int)Session["role"] != (int)AccountType.Admin)
                return new HttpUnauthorizedResult();

            if (Session["role"] == null || (int)Session["role"] != (int)AccountType.Admin)
                return RedirectToAction("Login", "Account");

            var accounts = db.Accounts.ToList();

            return View(accounts);
        }
        public ActionResult ToggleLockByAdmin(int id)
        {
            var acc = db.Accounts.Find(id);
            if (acc == null || acc.idtype != 1)
                return HttpNotFound();
            acc.isLockedByAdmin = !acc.isLockedByAdmin;

            if (acc.isLockedByAdmin)
            {
                acc.lockDate = DateTime.Now;
                MailHelper.SendEmail(acc.email, "Tài khoản bị khóa", "Tài khoản của bạn đã bị khóa bởi Admin và không thể đăng nhập.");
                TempData["Success"] = "Đã khóa tài khoản người dùng.";
            }
            else
            {
                acc.lockDate = null;
                MailHelper.SendEmail(acc.email, "Tài khoản được mở lại", "Tài khoản của bạn đã được mở lại bởi Admin. Bạn có thể đăng nhập bình thường.");
                TempData["Success"] = "Đã mở tài khoản người dùng.";
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            db.Accounts.Remove(account);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Account account)
        {
            if (ModelState.IsValid)
            {
                account.idtype = 2;
                db.Accounts.Add(account);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(account);
        }
        public ActionResult Edit(int id)
        {
            var account = db.Accounts.Find(id);
            return View(account);
        }
        public ActionResult Details(int id)
        {
            var account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            return View(account);
        }

        [HttpPost]
        public ActionResult Edit(Account account)
        {
            if (ModelState.IsValid)
            {
                db.Entry(account).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(account);
        }
    }
}
