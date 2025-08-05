using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SHOP_IPHONE.Models;

namespace SHOP_IPHONE.Controllers
{
    public class ProductVariantController : Controller
    {
        private DBiphoneEntities1 db = new DBiphoneEntities1();

        // Kiểm tra quyền admin
        private bool IsAdmin()
        {
            return Session["role"] != null && Session["role"].ToString() == "2";
        }

        // GET: ProductVariant
        public ActionResult Index(int? productId)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể truy cập trang này.");
            }

            if (productId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products.Find(productId);
            if (product == null)
            {
                return HttpNotFound();
            }

            var variants = db.ProductVariants.Where(v => v.product_id == productId).ToList();
            
            ViewBag.Product = product;
            ViewBag.ProductId = productId;
            
            return View(variants);
        }

        // GET: ProductVariant/Create
        public ActionResult Create(int? productId)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể truy cập trang này.");
            }

            if (productId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products.Find(productId);
            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.Product = product;
            ViewBag.AvailableColors = GetAvailableColors();

            var variant = new ProductVariant
            {
                product_id = productId.Value
            };

            return View(variant);
        }

        // POST: ProductVariant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "product_id,color,price,stock")] ProductVariant variant, HttpPostedFileBase imageFile)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể thực hiện hành động này.");
            }
            if (ModelState.IsValid)
            {
                // Kiểm tra màu sắc đã tồn tại chưa
                var existingVariant = db.ProductVariants.FirstOrDefault(v => 
                    v.product_id == variant.product_id && v.color == variant.color);
                
                if (existingVariant != null)
                {
                    ModelState.AddModelError("color", "Màu sắc này đã tồn tại cho sản phẩm này.");
                    var product = db.Products.Find(variant.product_id);
                    ViewBag.Product = product;
                    ViewBag.AvailableColors = GetAvailableColors();
                    return View(variant);
                }

                // Xử lý ảnh
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        imageFile.InputStream.CopyTo(memoryStream);
                        variant.image = memoryStream.ToArray();
                    }
                }

                db.ProductVariants.Add(variant);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Thêm biến thể thành công!";
                return RedirectToAction("Index", new { productId = variant.product_id });
            }

            var productForView = db.Products.Find(variant.product_id);
            ViewBag.Product = productForView;
            ViewBag.AvailableColors = GetAvailableColors();
            return View(variant);
        }

        // GET: ProductVariant/Edit/5
        public ActionResult Edit(int? id)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể truy cập trang này.");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var variant = db.ProductVariants.Find(id);
            if (variant == null)
            {
                return HttpNotFound();
            }

            var product = db.Products.Find(variant.product_id);
            ViewBag.Product = product;
            ViewBag.AvailableColors = GetAvailableColors();

            return View(variant);
        }

        // POST: ProductVariant/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "variant_id,product_id,color,price,stock")] ProductVariant variant, HttpPostedFileBase imageFile)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể thực hiện hành động này.");
            }
            if (ModelState.IsValid)
            {
                var existingVariant = db.ProductVariants.Find(variant.variant_id);
                if (existingVariant == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra màu sắc đã tồn tại chưa (trừ chính nó)
                var duplicateVariant = db.ProductVariants.FirstOrDefault(v => 
                    v.product_id == variant.product_id && 
                    v.color == variant.color && 
                    v.variant_id != variant.variant_id);
                
                if (duplicateVariant != null)
                {
                    ModelState.AddModelError("color", "Màu sắc này đã tồn tại cho sản phẩm này.");
                    var product = db.Products.Find(variant.product_id);
                    ViewBag.Product = product;
                    ViewBag.AvailableColors = GetAvailableColors();
                    return View(variant);
                }

                // Xử lý ảnh
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        imageFile.InputStream.CopyTo(memoryStream);
                        variant.image = memoryStream.ToArray();
                    }
                }
                else
                {
                    // Giữ lại ảnh cũ
                    variant.image = existingVariant.image;
                }

                db.Entry(existingVariant).CurrentValues.SetValues(variant);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Cập nhật biến thể thành công!";
                return RedirectToAction("Index", new { productId = variant.product_id });
            }

            var productForView = db.Products.Find(variant.product_id);
            ViewBag.Product = productForView;
            ViewBag.AvailableColors = GetAvailableColors();
            return View(variant);
        }

        // GET: ProductVariant/Delete/5
        public ActionResult Delete(int? id)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể truy cập trang này.");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var variant = db.ProductVariants.Find(id);
            if (variant == null)
            {
                return HttpNotFound();
            }

            var product = db.Products.Find(variant.product_id);
            ViewBag.Product = product;

            return View(variant);
        }

        // POST: ProductVariant/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Kiểm tra quyền admin
            if (!IsAdmin())
            {
                return new HttpUnauthorizedResult("Chỉ admin mới có thể thực hiện hành động này.");
            }
            var variant = db.ProductVariants.Find(id);
            if (variant == null)
            {
                return HttpNotFound();
            }

            int productId = variant.product_id;
            db.ProductVariants.Remove(variant);
            db.SaveChanges();
            
            TempData["SuccessMessage"] = "Xóa biến thể thành công!";
            return RedirectToAction("Index", new { productId = productId });
        }

        // Helper method để lấy danh sách màu sắc có sẵn
        private List<string> GetAvailableColors()
        {
            return new List<string>
            {
                "Đen",
                "Trắng", 
                "Vàng",
                "Xanh dương",
                "Xanh lá",
                "Đỏ",
                "Hồng",
                "Tím",
                "Cam",
                "Xám",
                "Bạc",
                "Vàng gold",
                "Xanh navy",
                "Đỏ ruby",
                "Hồng rose gold"
            };
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