using System;
using System.Linq;
using System.Web.Mvc;
using SHOP_IPHONE.Models;

namespace SHOP_IPHONE.Controllers
{
    public class OrderReviewController : Controller
    {
        private DBiphoneEntities db = new DBiphoneEntities();

        // POST: OrderReview/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int order_id, int rating, string comment)
        {
            // Lấy account_id từ session (giả sử bạn lưu account_id khi đăng nhập)
            if (Session["account_id"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int account_id = Convert.ToInt32(Session["account_id"]);

            // Kiểm tra đã đánh giá chưa (mỗi đơn hàng chỉ được đánh giá 1 lần)
            var existed = db.OrderReviews.FirstOrDefault(r => r.order_id == order_id && r.account_id == account_id);
            if (existed != null)
            {
                TempData["ReviewMessage"] = "Bạn đã đánh giá đơn hàng này rồi!";
                return RedirectToAction("Index", "Order");
            }

            var review = new OrderReview
            {
                order_id = order_id,
                account_id = account_id,
                rating = rating,
                comment = comment,
                created_date = DateTime.Now
            };

            db.OrderReviews.Add(review);
            db.SaveChanges();

            TempData["ReviewMessage"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Index", "Order");
        }
    }
} 