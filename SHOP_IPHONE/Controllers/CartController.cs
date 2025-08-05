using SHOP_IPHONE.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class CartController : Controller
    {
        private DBiphoneEntities1 db = new DBiphoneEntities1();

        private List<CartItemModel> GetCart()
        {
            var cart = Session["Cart"] as List<CartItemModel>;
            if (cart == null)
            {
                cart = new List<CartItemModel>();
                Session["Cart"] = cart;
            }
            return cart;
        }

        public ActionResult AddToCart(int id, string variant = null)
        {
            var product = db.Products.FirstOrDefault(p => p.product_id == id);
            if (product == null) return HttpNotFound();

            // Kiểm tra tồn kho
            if (!string.IsNullOrEmpty(variant))
            {
                var variantObj = db.ProductVariants.FirstOrDefault(v => v.product_id == id && v.color == variant);
                if (variantObj == null || (variantObj.stock ?? 0) <= 0)
                {
                    TempData["CartMessage"] = "Biến thể này đã hết hàng!";
                    return RedirectToAction("Index", "Product", new { id = id });
                }
            }
            else
            {
                if (product.stock <= 0)
                {
                    TempData["CartMessage"] = "Sản phẩm này đã hết hàng!";
                    return RedirectToAction("Index", "Product", new { id = id });
                }
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductId == id && p.Variant == variant);

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                cart.Add(new CartItemModel
                {
                    ProductId = product.product_id,
                    ProductName = product.product_name,
                    Price = product.price,
                    Quantity = 1,
                    Images = product.images,
                    Variant = variant
                });
            }

            return RedirectToAction("Index");
        }

        public ActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(item => item.Total);
            
            // Debug: Kiểm tra giỏ hàng
            if (cart.Count == 0)
            {
                TempData["CartMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm vào giỏ hàng.";
            }
            
            return View(cart);
        }

        public ActionResult Remove(int id, string variant = null)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductId == id && p.Variant == variant);
            if (item != null)
            {
                cart.Remove(item);
            }
            return RedirectToAction("Index");
        }

        public ActionResult Clear()
        {
            Session["Cart"] = null;
            return RedirectToAction("Index");
        }
       

    }
}