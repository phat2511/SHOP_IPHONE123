using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SHOP_IPHONE.Models;

namespace SHOP_IPHONE.Controllers
{
    public class VoucherController : Controller
    {
        private DBiphoneEntities1 db = new DBiphoneEntities1();

        // GET: Voucher
        public ActionResult Index()
        {
            return View(db.Vouchers.ToList());
        }

        // GET: Voucher/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Voucher voucher = db.Vouchers.Find(id);
            if (voucher == null)
                return HttpNotFound();

            return View(voucher);
        }

        // GET: Voucher/Create
        public ActionResult Create()
        {
            if (Session["role"] == null || Session["role"].ToString() != "2")
            {
                TempData["Message"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Product");
            }
            return View(new Voucher());
        }

        // POST: Voucher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "voucher_id,code,discount_amount,discount_percent,start_date,end_date,min_order_value,is_active")] Voucher voucher)
        {
            if ((voucher.discount_amount.HasValue && voucher.discount_amount.Value > 0) &&
                (voucher.discount_percent.HasValue && voucher.discount_percent.Value > 0))
            {
                ModelState.AddModelError("", "Chỉ được nhập một trong hai: giảm tiền hoặc giảm phần trăm.");
                return View(voucher);
            }

            if (!voucher.discount_amount.HasValue && !voucher.discount_percent.HasValue)
            {
                ModelState.AddModelError("", "Bạn phải nhập giảm tiền hoặc giảm phần trăm.");
                return View(voucher);
            }

            if (ModelState.IsValid)
            {
                db.Vouchers.Add(voucher);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(voucher);
        }

        // GET: Voucher/Edit/5
        public ActionResult Edit(int? id)
        {
            if (Session["role"] == null || Session["role"].ToString() != "2")
            {
                TempData["Message"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Product");
            }
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Voucher voucher = db.Vouchers.Find(id);
            if (voucher == null)
                return HttpNotFound();

            return View(voucher);
        }

        // POST: Voucher/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "voucher_id,code,discount_amount,discount_percent,start_date,end_date,min_order_value,is_active")] Voucher voucher)
        {
            if (Session["role"] == null || Session["role"].ToString() != "2")
            {
                TempData["Message"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Product");
            }

            // Validation cho code (không được null hoặc empty)
            if (string.IsNullOrWhiteSpace(voucher.code))
            {
                ModelState.AddModelError("code", "Mã voucher không được để trống.");
                return View(voucher);
            }

            // Validation cho discount
            if ((voucher.discount_amount.HasValue && voucher.discount_amount.Value > 0) &&
                (voucher.discount_percent.HasValue && voucher.discount_percent.Value > 0))
            {
                ModelState.AddModelError("", "Chỉ được nhập một trong hai: giảm tiền hoặc giảm phần trăm.");
                return View(voucher);
            }

            if (!voucher.discount_amount.HasValue && !voucher.discount_percent.HasValue)
            {
                ModelState.AddModelError("", "Bạn phải nhập giảm tiền hoặc giảm phần trăm.");
                return View(voucher);
            }

            // Validation cho ngày
            if (voucher.start_date.HasValue && voucher.end_date.HasValue && voucher.start_date > voucher.end_date)
            {
                ModelState.AddModelError("", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return View(voucher);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(voucher).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật voucher: " + ex.Message);
                    return View(voucher);
                }
            }

            return View(voucher);
        }

        // GET: Voucher/Delete/5
        public ActionResult Delete(int? id)
        {
            if (Session["role"] == null || Session["role"].ToString() != "2")
            {
                TempData["Message"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Product");
            }
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Voucher voucher = db.Vouchers.Find(id);
            System.Diagnostics.Debug.WriteLine("Voucher found: " + (voucher != null));

            if (voucher == null)
                return HttpNotFound();

            return View(voucher);
        }

        // POST: Voucher/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (Session["role"] == null || Session["role"].ToString() != "2")
            {
                TempData["Message"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Product");
            }

            try
            {
                Voucher voucher = db.Vouchers.Find(id);

                if (voucher == null)
                {
                    TempData["ErrorMessage"] = "Voucher không tồn tại hoặc đã bị xóa.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem voucher có đang được sử dụng không
                if (voucher.Orders != null && voucher.Orders.Count > 0)
                {
                    TempData["ErrorMessage"] = "Không thể xóa voucher này vì đang được sử dụng trong đơn hàng.";
                    return RedirectToAction("Index");
                }

                // Xóa voucher
                db.Vouchers.Remove(voucher);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Xóa voucher thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa voucher: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
