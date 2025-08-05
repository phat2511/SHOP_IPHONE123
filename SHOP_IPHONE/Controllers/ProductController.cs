    using System.Collections.Generic;
    using System.Web.Mvc;
    using SHOP_IPHONE.Models;
    using System.Linq; // Để dùng được ToList()
    using System.Web;
    using System.IO;
    using PagedList;
    using System.Net;
    using System.Data.Entity;
    using System;

    namespace SHOP_IPHONE.Controllers
    {
        public class ProductController : Controller
        {
            private DBiphoneEntities1 db = new DBiphoneEntities1();

        public ActionResult Index(int? page)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var model = new Homemol();
            model.ListVoucher = db.Vouchers.ToList();

            // Sản phẩm nổi bật - hiển thị tất cả sản phẩm nổi bật
            model.ListProduct1 = db.Products
                                  .Where(p => p.IsHot == true)
                                  .OrderByDescending(p => p.product_id)
                                  .Take(5)
                                  .ToList();

            // Tất cả sản phẩm (trừ sản phẩm nổi bật) - hiển thị tất cả
            model.ListProduct = db.Products
                                 .Where(p => p.IsHot == false || p.IsHot == null)
                                 .OrderBy(p => p.product_id)
                                 .ToPagedList(pageNumber, pageSize);



            // ✅ Lấy danh sách banner đang hoạt động từ bảng Banner
            ViewBag.Banners = db.Banner
                          .Where(b => b.is_active == true)
                          .OrderByDescending(b => b.created_at)
                          .ToList();

            // Thêm header để tránh cache
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();

            return View(model);
        }



        public ActionResult Index1()
            {
                var products = db.Products.ToList();
                return View(products);
            }

            private static List<Product> products = new List<Product>();

            public ActionResult Create()
            {
                ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name");
                ViewBag.company_id = new SelectList(db.Category1, "category1_id", "company_name");
                return View(new Product { IsHot = false });
            }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public ActionResult Create([Bind(Include = "product_id,product_name,price,stock,description,company_id,category_id,GiaKhuyenMai,IsHot")] Product product, HttpPostedFileBase imageFile)
            {
                if (product.price < 0)
                {
                    ModelState.AddModelError("price", "Giá không hợp lệ. Giá phải lớn hơn hoặc bằng 0.");
                }
                if (ModelState.IsValid)
                {
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        using (var binaryReader = new BinaryReader(imageFile.InputStream))
                        {
                            product.images = binaryReader.ReadBytes(imageFile.ContentLength);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("image", "Ảnh sản phẩm là bắt buộc.");
                        ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);
                        ViewBag.company_id = new SelectList(db.Category1, "company_id", "company_name", product.company_id);
                        return View(product);
                    }

                    db.Products.Add(product);
                    db.SaveChanges();
                    return RedirectToAction("Index1");
                }

                ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);
                ViewBag.company_id = new SelectList(db.Category1, "company_id", "company_name", product.company_id);
                return View(product);
            }

        public ActionResult Details(int? id, int? star = null, bool? hasImage = null, bool? hasComment = null)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.Products.Find(id);
            if (product == null)
                return HttpNotFound();

            // Lấy danh sách biến thể
            var variants = db.ProductVariants.Where(v => v.product_id == id).ToList();
            ViewBag.Variants = variants;

            var reviewsQuery = db.Reviews
                .Where(r => r.product_id == id)
                .Include("ReviewImages");

            // Lọc theo số sao
            if (star.HasValue)
                reviewsQuery = reviewsQuery.Where(r => r.rating == star.Value);

            // Lọc theo có ảnh
            if (hasImage == true)
                reviewsQuery = reviewsQuery.Where(r => r.ReviewImages.Any());

            // Lọc theo có bình luận
            if (hasComment == true)
                reviewsQuery = reviewsQuery.Where(r => !string.IsNullOrEmpty(r.comment));

            var reviews = reviewsQuery
                .OrderByDescending(r => r.created_date)
                .ToList();

            ViewBag.Reviews = reviews;

            // (Các ViewBag thống kê như hướng dẫn trước vẫn giữ nguyên)
            var allReviews = db.Reviews.Where(r => r.product_id == id).Include("ReviewImages").ToList();
            ViewBag.AvgRating = allReviews.Any() ? allReviews.Average(r => r.rating) : 0;
            ViewBag.Count5Star = allReviews.Count(r => r.rating == 5);
            ViewBag.Count4Star = allReviews.Count(r => r.rating == 4);
            ViewBag.Count3Star = allReviews.Count(r => r.rating == 3);
            ViewBag.Count2Star = allReviews.Count(r => r.rating == 2);
            ViewBag.Count1Star = allReviews.Count(r => r.rating == 1);
            ViewBag.CountWithImage = allReviews.Count(r => r.ReviewImages.Any());
            ViewBag.CountWithComment = allReviews.Count(r => !string.IsNullOrEmpty(r.comment));
            ViewBag.TotalReview = allReviews.Count;

            return View(product);
        }


        public ActionResult Delete(int? id)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                Product product = db.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound();
                }

                return View(product);
            }

            [HttpPost, ActionName("Delete")]
            [ValidateAntiForgeryToken]
            public ActionResult DeleteConfirmed(int id)
            {
                Product product = db.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra xem sản phẩm có đơn hàng nào không
                var hasOrders = db.OrderItems.Any(oi => oi.product_id == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm này vì đã có đơn hàng mua sản phẩm này. Vui lòng kiểm tra lại!";
                    return RedirectToAction("Index1");
                }

                // Kiểm tra xem sản phẩm có đánh giá nào không
                var hasReviews = db.Reviews.Any(r => r.product_id == id);
                if (hasReviews)
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm này vì đã có đánh giá. Vui lòng kiểm tra lại!";
                    return RedirectToAction("Index1");
                }

                // Xóa các biến thể của sản phẩm trước
                var variants = db.ProductVariants.Where(v => v.product_id == id).ToList();
                foreach (var variant in variants)
                {
                    db.ProductVariants.Remove(variant);
                }

                // Xóa sản phẩm
                db.Products.Remove(product);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công!";
                return RedirectToAction("Index1");
            }

            public ActionResult Edit(int? id)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Product product = db.Products.Find(id);
                Session["img"] = product.images;
                if (product == null)
                {
                    return HttpNotFound();
                }
                if (product.IsHot == null)
                {
                    product.IsHot = false;
                }
                ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);
                ViewBag.company_id = new SelectList(db.Category1, "category1_id", "company_name", product.company_id);
                return View(product);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public ActionResult Edit([Bind(Include = "product_id,product_name,price,stock,description,company_id,category_id,GiaKhuyenMai,images,IsHot")] Product product, HttpPostedFileBase imageFile)
            {
                if (product.price < 0)
                {
                    ModelState.AddModelError("price", "Giá không hợp lệ. Giá phải lớn hơn hoặc bằng 0.");
                }
                if (ModelState.IsValid)
                {
                    var existingProduct = db.Products.Find(product.product_id);

                    if (existingProduct == null)
                    {
                        ModelState.AddModelError("", "Sản phẩm không tồn tại.");
                        return View(product);
                    }

                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            imageFile.InputStream.CopyTo(memoryStream);
                            product.images = memoryStream.ToArray();
                        }
                    }
                    else
                    {
                        product.images = (byte[])Session["img"];
                    }

                    db.Entry(existingProduct).CurrentValues.SetValues(product);
                    db.Entry(existingProduct).State = EntityState.Modified;

                    try
                    {
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    catch
                    {
                        ModelState.AddModelError("", "Có lỗi khi lưu dữ liệu.");
                        return View(product);
                    }
                }

                ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);
                ViewBag.company_id = new SelectList(db.Category1, "category1_id", "company_name", product.company_id);
                return View(product);
            }

            public ActionResult ByCategory(int id)
            {
                var products = db.Products.Where(p => p.category_id == id).ToList();
                return View(products);
            }
            public ActionResult Category(int id)
            {
                var category = db.Categories.Find(id);
                if (category == null) return HttpNotFound();

                ViewBag.CategoryName = category.category_name;

                var products = db.Products.Where(p => p.category_id == id).ToList();
                return View(products);
            }
            /*tim kiem */
            public ActionResult Search(string keyword)
            {

                if (string.IsNullOrEmpty(keyword))
                {
                    return View(new SearchViewModel
                    {
                        Keyword = "",
                        Results = new List<Product>()
                    });
                }

                var product = db.Products
                                .Where(p => p.product_name.ToLower().Contains(keyword.ToLower()))
                                .ToList();

                var model = new SearchViewModel
                {
                    Keyword = keyword,
                    Results = product
                };

                return View(model);
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Review(int product_id, int rating, string comment, IEnumerable<HttpPostedFileBase> images)
        {
            if (Session["account_id"] == null)
                return RedirectToAction("Login", "Account");
            int account_id = Convert.ToInt32(Session["account_id"]);

            // Kiểm tra đã đánh giá chưa
            var existed = db.Reviews.FirstOrDefault(r => r.product_id == product_id && r.account_id == account_id);
            if (existed != null)
            {
                TempData["ReviewMessage"] = "Bạn đã đánh giá sản phẩm này rồi!";
                return RedirectToAction("Details", new { id = product_id });
            }

            var review = new Review
            {
                product_id = product_id,
                account_id = account_id,
                rating = rating,
                comment = comment,
                created_date = DateTime.Now,
                Likes = 0
            };
            db.Reviews.Add(review);
            db.SaveChanges();

            // Lưu nhiều ảnh
            if (images != null)
            {
                foreach (var file in images)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var path = Path.Combine(Server.MapPath("~/Content/ReviewImages"), fileName);
                        file.SaveAs(path);

                        var reviewImage = new ReviewImage
                        {
                            ReviewId = review.review_id, // hoặc review.ReviewId tùy tên trường
                            ImagePath = "/Content/ReviewImages/" + fileName
                        };
                        db.ReviewImages.Add(reviewImage);
                    }
                }
                db.SaveChanges();
            }

            TempData["ReviewMessage"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Details", new { id = product_id });
        }
        public ActionResult Category(int id, string keyword = "", decimal? minPrice = null, decimal? maxPrice = null)
        {
            var category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();

            ViewBag.CategoryName = category.category_name;

            var products = db.Products.Where(p => p.category_id == id);

            if (!string.IsNullOrEmpty(keyword))
            {
                products = products.Where(p => p.product_name.Contains(keyword));
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.price >= minPrice);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.price <= maxPrice);
            }

            return View(products.ToList());

        }
        [HttpPost]
        public ActionResult LikeReview(int reviewId)
        {
            var review = db.Reviews.Find(reviewId);
            if (review != null)
            {
                review.Likes += 1;
                db.SaveChanges();
                return Json(new { success = true, likes = review.Likes });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteReview(int reviewId, int productId)
        {
            var review = db.Reviews.Include("ReviewImages").FirstOrDefault(r => r.review_id == reviewId);
            if (review != null)
            {
                // Xóa ảnh liên quan
                foreach (var img in review.ReviewImages.ToList())
                {
                    // Xóa file vật lý nếu cần
                    var filePath = Server.MapPath(img.ImagePath);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    db.ReviewImages.Remove(img);
                }
                db.Reviews.Remove(review);
                db.SaveChanges();
                TempData["ReviewMessage"] = "Đã xóa đánh giá!";
            }
            return RedirectToAction("Details", new { id = productId });
        }
        public ActionResult EditReview(int reviewId)
        {
            var review = db.Reviews.Include("ReviewImages").FirstOrDefault(r => r.review_id == reviewId);
            if (review == null) return HttpNotFound();
            return View(review);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditReview(int review_id, int rating, string comment, IEnumerable<HttpPostedFileBase> images)
        {
            var review = db.Reviews.Include("ReviewImages").FirstOrDefault(r => r.review_id == review_id);
            if (review == null) return HttpNotFound();

            review.rating = rating;
            review.comment = comment;
            review.created_date = DateTime.Now;

            // Xử lý ảnh mới (nếu có)
            if (images != null)
            {
                foreach (var file in images)
                {
                    // Đảm bảo thư mục tồn tại trước khi lưu file
                    var dir = Server.MapPath("~/Content/ReviewImages");
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var path = Path.Combine(dir, fileName);
                    file.SaveAs(path);

                    var reviewImage = new ReviewImage
                    {
                        ReviewId = review.review_id,
                        ImagePath = "/Content/ReviewImages/" + fileName
                    };
                    db.ReviewImages.Add(reviewImage);
                }
            }
            db.SaveChanges();
            TempData["ReviewMessage"] = "Đã cập nhật đánh giá!";
            return RedirectToAction("Details", new { id = review.product_id });
        }
    }
}
