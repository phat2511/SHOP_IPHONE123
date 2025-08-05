using SHOP_IPHONE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace SHOP_IPHONE.Controllers
{
    public class OrderController : Controller
    {
        private Models.DBiphoneEntities1 db = new Models.DBiphoneEntities1();

        // Danh sách tĩnh chứa chi tiết các phương thức thanh toán
        private static List<PaymentMethodDetail> _paymentMethodsDetails = new List<PaymentMethodDetail>
        {
            new PaymentMethodDetail
            {
                MethodName = "COD",
                DisplayName = "Thanh toán khi nhận hàng (COD)",
                Description = "Bạn sẽ thanh toán tiền mặt trực tiếp cho nhân viên giao hàng khi nhận được sản phẩm.",
                Instructions = "Vui lòng chuẩn bị sẵn tiền mặt đúng số tiền đơn hàng."
            },
            new PaymentMethodDetail
            {
               MethodName = "Chuyển khoản",
                DisplayName = "Thanh toán qua Ví điện tử/VietQR", // Có thể đổi tên hiển thị
                Description = "Quét mã QR để thanh toán qua tài khoản/số điện thoại VietQR.",
                Instructions = "Nội dung: [Mã đơn hàng của bạn]",
                QrCodeUrl = "/Content/Images/QR/vietqr_danghoang.png", // <-- Thay thế đường dẫn này
                EwalletNumber = "0775782445 (DANG HOANG)" // Hiển thị số điện thoại nếu muốn
            },
        };

        // Phương thức trợ giúp để lấy chi tiết phương thức thanh toán
        private PaymentMethodDetail GetPaymentMethodDetail(string methodName)
        {
            return _paymentMethodsDetails.FirstOrDefault(p => p.MethodName == methodName);
        }

        // Action hiển thị trang xác nhận đơn hàng
        public ActionResult Confirm()
        {
            var cart = Session["Cart"] as List<CartItemModel>;
            if (cart == null || !cart.Any())
            {
                TempData["Message"] = "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Debug: Log thông tin giỏ hàng
            System.Diagnostics.Debug.WriteLine($"Cart items count: {cart.Count}");
            foreach (var item in cart)
            {
                System.Diagnostics.Debug.WriteLine($"Product: {item.ProductName}, Quantity: {item.Quantity}, Price: {item.Price}, Variant: {item.Variant}");
            }
            if (Session["account_id"] != null && (int?)Session["role"] == (int)AccountType.Admin)
            {
                TempData["Message"] = "Tài khoản admin không thể mua hàng.";
                return RedirectToAction("Index", "Product");
            }

            // Kiểm tra số lượng hàng tồn kho trước khi cho phép đặt hàng
            foreach (var item in cart)
            {
                System.Diagnostics.Debug.WriteLine($"Checking item: {item.ProductName}, Variant: '{item.Variant}', Quantity: {item.Quantity}");
                
                // Luôn kiểm tra sản phẩm chính trước
                var productObj = db.Products.FirstOrDefault(p => p.product_id == item.ProductId);
                if (productObj == null)
                {
                    TempData["Message"] = $"Không tìm thấy sản phẩm {item.ProductName}!";
                    return RedirectToAction("Index", "Cart");
                }
                
                System.Diagnostics.Debug.WriteLine($"Product check: Stock={productObj.stock}, Need={item.Quantity}");
                
                // Nếu là biến thể thực sự (không phải "Mặc định"), kiểm tra biến thể
                if (!string.IsNullOrEmpty(item.Variant) && !item.Variant.Trim().Equals("Mặc định", StringComparison.OrdinalIgnoreCase))
                {
                    var variantObj = db.ProductVariants.FirstOrDefault(v => v.product_id == item.ProductId && v.color == item.Variant);
                    System.Diagnostics.Debug.WriteLine($"Variant check: Found={variantObj != null}, Stock={variantObj?.stock}");
                    if (variantObj == null)
                    {
                        TempData["Message"] = $"Không tìm thấy biến thể {item.Variant} cho sản phẩm {item.ProductName}!";
                        return RedirectToAction("Index", "Cart");
                    }
                    if ((variantObj.stock ?? 0) <= 0)
                    {
                        TempData["Message"] = $"Biến thể {item.Variant} đã hết hàng!";
                        return RedirectToAction("Index", "Cart");
                    }
                }
                else
                {
                    // Kiểm tra sản phẩm chính
                    if (productObj.stock <= 0)
                    {
                        TempData["Message"] = $"Sản phẩm {item.ProductName} đã hết hàng!";
                        return RedirectToAction("Index", "Cart");
                    }
                }
            }

            var confirmModel = new ConfirmOrderViewModel
            {
                CartItems = cart,
                ShippingFee = 30000,
                PaymentMethods = _paymentMethodsDetails.Select(p => p.MethodName).ToList(),
                AvailableVouchers = db.Vouchers
                   .Where(v => v.is_active == true && v.end_date >= DateTime.Now)
                   .Select(v => new VoucherViewModel
                   {
                       voucher_id = v.voucher_id,
                       code = v.code,
                       discount_type = v.discount_percent.HasValue ? "percent" : "amount",
                       discount_value = v.discount_percent.HasValue ? v.discount_percent.Value : (v.discount_amount ?? 0)
                   }).ToList()
            };

            // Nếu đã đăng nhập, lấy thông tin người dùng và địa chỉ
            if (Session["account_id"] != null)
            {
                int accountId = (int)Session["account_id"];
                var account = db.Accounts.Find(accountId);
                if (account != null)
                {
                    confirmModel.CustomerName = account.full_name;
                    confirmModel.Phone = account.phone;
                    confirmModel.Address = account.address;
                }

                confirmModel.Adresses = db.Addresses.Where(a => a.AccountId == accountId).ToList();
            }

            ViewBag.PaymentDetails = _paymentMethodsDetails;
            ViewBag.IsLoggedIn = Session["account_id"] != null;
            return View(confirmModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm(ConfirmOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Cần nạp lại danh sách voucher và địa chỉ nếu model lỗi
                model.CartItems = Session["Cart"] as List<CartItemModel>;
                model.PaymentMethods = _paymentMethodsDetails.Select(p => p.MethodName).ToList();
                model.AvailableVouchers = db.Vouchers
                    .Where(v => v.is_active == true && v.end_date >= DateTime.Now)
                    .Select(v => new VoucherViewModel
                    {
                        voucher_id = v.voucher_id,
                        code = v.code,
                        discount_type = v.discount_percent.HasValue ? "percent" : "amount",
                        discount_value = v.discount_percent.HasValue ? v.discount_percent.Value : (v.discount_amount ?? 0)
                    }).ToList();
                if (Session["account_id"] != null)
                {
                    int accountId = (int)Session["account_id"];
                    model.Adresses = db.Addresses.Where(a => a.AccountId == accountId).ToList();
                }

                ViewBag.PaymentDetails = _paymentMethodsDetails;
                ViewBag.IsLoggedIn = Session["account_id"] != null;
                return View(model);
            }
            // TODO: Lưu đơn hàng vào DB, lấy OrderId
            int orderId = 999; // Giả lập
            return RedirectToAction("OrderSuccess", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizeOrder(ConfirmOrderViewModel model)
        {
            int? userId = Session["account_id"] as int?;

            // Chỉ kiểm tra admin nếu đã đăng nhập
            if (userId != null && Session["role"]?.ToString() == "admin")
            {
                TempData["Message"] = "Tài khoản admin không được phép mua hàng.";
                return RedirectToAction("Index", "Home");
            }

            var cart = Session["Cart"] as List<CartItemModel>;
            if ((cart == null || !cart.Any()) && TempData["PendingOrder"] is ConfirmOrderViewModel pending)
            {
                cart = pending.CartItems;
                model.OrderId = pending.OrderId;
                model.ShippingFee = pending.ShippingFee;
            }

            if (model.ShippingFee <= 0)
            {
                model.ShippingFee = 30000; // Gán mặc định nếu bị null hoặc mất
            }

            if (cart == null || !cart.Any())
            {
                TempData["Message"] = "Không tìm thấy giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            Order order = null;

            if (model.OrderId > 0)
            {
                order = db.Orders
                          .Include(o => o.OrderItems.Select(oi => oi.Product))
                          .FirstOrDefault(o => o.order_id == model.OrderId);

                if (order != null)
                {
                    order.shipping_fee = model.ShippingFee;
                    order.voucher_id = model.SelectedVoucherId;
                    order.discount_amount = model.DiscountAmount;
                    foreach (var item in order.OrderItems)
                    {
                        if (item.price == 0)
                        {
                            var product = db.Products.Find(item.product_id);
                            if (product != null)
                            {
                                item.price = product.price;
                            }
                        }
                    }
                    decimal discountAmount = model.DiscountAmount;
                    if (discountAmount == 0 && model.SelectedVoucherId.HasValue)
                    {
                        var selectedVoucher = db.Vouchers.FirstOrDefault(v => v.voucher_id == model.SelectedVoucherId);
                        if (selectedVoucher != null)
                        {
                            var subtotal = order.OrderItems.Sum(i => i.price * i.quantity);
                            if (selectedVoucher.discount_percent.HasValue)
                            {
                                discountAmount = subtotal * selectedVoucher.discount_percent.Value / 100;
                            }
                            else if (selectedVoucher.discount_amount.HasValue)
                            {
                                discountAmount = selectedVoucher.discount_amount.Value;
                            }
                        }
                    }
                    order.Vat = 0.1m; // 10%
                    decimal vat = order.Vat ?? 0;
                    order.total_amount = CalculateOrderTotal(order.order_id, order.shipping_fee ?? 0, vat) - discountAmount;
                    db.SaveChanges();
                }
            }
            else
            {
                order = new Order
                {
                    account_id = userId, // Có thể null nếu chưa đăng nhập
                    customer_name = model.CustomerName,
                    address = model.Address,
                    phone = model.Phone,
                    payment_method = model.SelectedPaymentMethod,
                    shipping_fee = model.ShippingFee,
                    order_date = DateTime.Now,
                    status = "Chờ xử lý"
                };
                db.Orders.Add(order);
                order.voucher_id = model.SelectedVoucherId;
                order.discount_amount = model.DiscountAmount;
                db.SaveChanges();
                model.OrderId = order.order_id;

                foreach (var item in cart)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        order_id = order.order_id,
                        product_id = item.ProductId,
                        quantity = item.Quantity,
                        price = item.Price ?? 0
                    });

                    // Trừ số lượng tồn kho
                    if (!string.IsNullOrEmpty(item.Variant))
                    {
                        var variantObj = db.ProductVariants.FirstOrDefault(v => v.product_id == item.ProductId && v.color == item.Variant);
                        if (variantObj != null)
                        {
                            if (variantObj.stock < item.Quantity)
                            {
                                TempData["Message"] = $"Biến thể {item.Variant} đã hết hàng hoặc không đủ số lượng!";
                                return RedirectToAction("Index", "Cart");
                            }
                            variantObj.stock -= item.Quantity;
                        }
                    }
                    else
                    {
                        var productObj = db.Products.FirstOrDefault(p => p.product_id == item.ProductId);
                        if (productObj != null)
                        {
                            if (productObj.stock < item.Quantity)
                            {
                                TempData["Message"] = "Sản phẩm đã hết hàng hoặc không đủ số lượng!";
                                return RedirectToAction("Index", "Cart");
                            }
                            productObj.stock -= item.Quantity;
                        }
                    }
                }
                db.SaveChanges();

                // ✅ Sửa tại đây
                order.Vat = 0.1m;
                decimal vat = order.Vat ?? 0;
                order.total_amount = CalculateOrderTotal(order.order_id, order.shipping_fee ?? 0, vat);
                order.Vat = 0.1m;
                decimal discountAmount = model.DiscountAmount;

                // Nếu discountAmount chưa có, tính lại dựa trên voucher đã chọn
                if (discountAmount == 0 && model.SelectedVoucherId.HasValue)
                {
                    var selectedVoucher = db.Vouchers.FirstOrDefault(v => v.voucher_id == model.SelectedVoucherId);
                    if (selectedVoucher != null)
                    {
                        var subtotal = order.OrderItems.Sum(i => i.price * i.quantity);
                        if (selectedVoucher.discount_percent.HasValue)
                        {
                            discountAmount = subtotal * selectedVoucher.discount_percent.Value / 100;
                        }
                        else if (selectedVoucher.discount_amount.HasValue)
                        {
                            discountAmount = selectedVoucher.discount_amount.Value;
                        }
                    }
                }

                order.total_amount = CalculateOrderTotal(order.order_id, order.shipping_fee ?? 0, vat) - discountAmount;
                db.SaveChanges();
            }


            var paymentDetail = GetPaymentMethodDetail(order.payment_method);

            var orderModel = new OrderModel
            {
                OrderId = order.order_id,
                OrderDate = order.order_date ?? DateTime.Now,
                CustomerName = order.customer_name,
                Address = order.address,
                Phone = order.phone,
                ShippingFee = order.shipping_fee ?? 0,
                TotalAmount = order.total_amount,
                PaymentMethod = order.payment_method,
                SelectedPaymentDetail = paymentDetail,
                Items = db.OrderItems
                    .Where(oi => oi.order_id == order.order_id)
                    .Include(oi => oi.Product)
                    .ToList()
                    .Select(oi => new CartItemModel
                    {
                        ProductId = oi.product_id ?? 0,
                        ProductName = oi.Product?.product_name,
                        Price = oi.price,
                        Quantity = oi.quantity
                    }).ToList(),
                DiscountAmount = order.discount_amount ?? 0,
            };

            Session["Cart"] = null;
            return View("OrderSuccess", orderModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(ConfirmOrderViewModel model)
        {
            int? userId = Session["account_id"] as int?;

            // Chỉ kiểm tra admin nếu đã đăng nhập
            if (userId != null && Session["role"]?.ToString() == "admin")
            {
                TempData["Message"] = "Tài khoản admin không được phép mua hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["Debug"] = "ModelState Invalid";
                model.CartItems = Session["Cart"] as List<CartItemModel>;
                model.PaymentMethods = _paymentMethodsDetails.Select(p => p.MethodName).ToList();
                ViewBag.PaymentDetails = _paymentMethodsDetails;
                model.AvailableVouchers = db.Vouchers
                .Where(v => v.is_active == true && v.end_date >= DateTime.Now)
                .Select(v => new VoucherViewModel
                {
                    voucher_id = v.voucher_id,
                    code = v.code,
                    discount_type = v.discount_percent.HasValue ? "percent" : "amount",
                    discount_value = v.discount_percent.HasValue ? v.discount_percent.Value : (v.discount_amount ?? 0)
                }).ToList();

                // Chỉ lấy địa chỉ nếu đã đăng nhập
                if (userId != null)
                {
                    int accountId = userId.Value;
                    model.Adresses = db.Addresses.Where(a => a.AccountId == accountId).ToList();
                }

                ViewBag.IsLoggedIn = userId != null;
                return View("Confirm", model);
            }

            var cart = Session["Cart"] as List<CartItemModel>;
            if (cart == null || !cart.Any())
            {
                TempData["Message"] = "Giỏ hàng trống.";
                model.AvailableVouchers = db.Vouchers
                    .Where(v => v.is_active == true && v.end_date >= DateTime.Now)
                    .Select(v => new VoucherViewModel
                    {
                        voucher_id = v.voucher_id,
                        code = v.code,
                        discount_type = v.discount_percent.HasValue ? "percent" : "amount",
                        discount_value = v.discount_percent.HasValue ? v.discount_percent.Value : (v.discount_amount ?? 0)
                    }).ToList();
                return RedirectToAction("Confirm");
            }

            // Kiểm tra số lượng hàng tồn kho trước khi đặt hàng
            foreach (var item in cart)
            {
                if (!string.IsNullOrEmpty(item.Variant))
                {
                    var variantObj = db.ProductVariants.FirstOrDefault(v => v.product_id == item.ProductId && v.color == item.Variant);
                    if (variantObj == null || variantObj.stock < item.Quantity)
                    {
                        TempData["Message"] = $"Biến thể {item.Variant} đã hết hàng hoặc không đủ số lượng!";
                        return RedirectToAction("Index", "Cart");
                    }
                }
                else
                {
                    var productObj = db.Products.FirstOrDefault(p => p.product_id == item.ProductId);
                    if (productObj == null || productObj.stock < item.Quantity)
                    {
                        TempData["Message"] = "Sản phẩm đã hết hàng hoặc không đủ số lượng!";
                        return RedirectToAction("Index", "Cart");
                    }
                }
            }

            // Cập nhật lại giá sản phẩm nếu bị thiếu
            foreach (var item in cart)
            {
                if (item.Price == null || item.Price == 0)
                {
                    var product = db.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        item.Price = product.price;
                    }
                }
            }

            string finalAddress = !string.IsNullOrWhiteSpace(model.Address)
                ? model.Address
                : model.SelectedAddressId.HasValue
                    ? db.Addresses.Find(model.SelectedAddressId.Value)?.Detail
                    : null;

            if (string.IsNullOrWhiteSpace(finalAddress))
            {
                ModelState.AddModelError("Address", "Vui lòng chọn hoặc nhập địa chỉ giao hàng.");
                model.CartItems = cart;
                model.PaymentMethods = _paymentMethodsDetails.Select(p => p.MethodName).ToList();
                ViewBag.PaymentDetails = _paymentMethodsDetails;
                model.AvailableVouchers = db.Vouchers
            .Where(v => v.is_active == true && v.end_date >= DateTime.Now)
            .Select(v => new VoucherViewModel
            {
                voucher_id = v.voucher_id,
                code = v.code,
                discount_type = v.discount_percent.HasValue ? "percent" : "amount",
                discount_value = v.discount_percent.HasValue ? v.discount_percent.Value : (v.discount_amount ?? 0)
            }).ToList();
                return View("Confirm", model);
            }


            if (!model.HasTransferred && model.SelectedPaymentMethod?.Contains("Chuyển khoản") == true)
            {
                var order = new Order
                {
                    account_id = userId, // Có thể null nếu chưa đăng nhập
                    customer_name = model.CustomerName,
                    address = finalAddress,
                    phone = model.Phone,
                    payment_method = model.SelectedPaymentMethod,
                    shipping_fee = model.ShippingFee,
                    Vat = 0.1m,
                    order_date = DateTime.Now,
                    status = "Chờ xử lý"
                };
                db.Orders.Add(order);
                db.SaveChanges();

                foreach (var item in cart)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        order_id = order.order_id,
                        product_id = item.ProductId,
                        quantity = item.Quantity,
                        price = item.Price ?? 0
                    });

                    // Trừ số lượng tồn kho
                    if (!string.IsNullOrEmpty(item.Variant))
                    {
                        var variantObj = db.ProductVariants.FirstOrDefault(v => v.product_id == item.ProductId && v.color == item.Variant);
                        if (variantObj != null)
                        {
                            variantObj.stock -= item.Quantity;
                        }
                    }
                    else
                    {
                        var productObj = db.Products.FirstOrDefault(p => p.product_id == item.ProductId);
                        if (productObj != null)
                        {
                            productObj.stock -= item.Quantity;
                        }
                    }
                }
                db.SaveChanges();

                // Tính tổng phụ để áp dụng voucher
                decimal subtotal = cart.Sum(i => i.Price.GetValueOrDefault(0) * i.Quantity);
                decimal discountAmount = 0;
                order.voucher_id = model.SelectedVoucherId;
                order.discount_amount = discountAmount;

                var selectedVoucher = db.Vouchers.FirstOrDefault(v => v.voucher_id == model.SelectedVoucherId);
                if (selectedVoucher != null)
                {
                    if (selectedVoucher.discount_percent.HasValue)
                    {
                        discountAmount = subtotal * selectedVoucher.discount_percent.Value / 100;
                    }
                    else if (selectedVoucher.discount_amount.HasValue)
                    {
                        discountAmount = selectedVoucher.discount_amount.Value;
                    }
                }
                model.DiscountAmount = discountAmount;
                decimal vat = order.Vat ?? 0;
                order.total_amount = CalculateOrderTotal(order.order_id, order.shipping_fee ?? 0, vat) - discountAmount;
                db.SaveChanges();

                Session["Cart"] = null;

                model.OrderId = order.order_id;
                model.CartItems = cart;
                model.Address = finalAddress;
                model.ShippingFee = order.shipping_fee ?? 0;
                model.SelectedVoucherId = selectedVoucher?.voucher_id;
                TempData["PendingOrder"] = model;

                return RedirectToAction("TransferInstruction");
            }

            model.Address = finalAddress;
            return FinalizeOrder(model);

        }

        // Action hiển thị trang đặt hàng thành công
        public ActionResult Success(int orderId)
        {
            var order = db.Orders
                          .Include(o => o.OrderItems.Select(oi => oi.Product))
                          .FirstOrDefault(o => o.order_id == orderId);
            decimal discountAmount = 0;

            if (order == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            var selectedPaymentDetail = GetPaymentMethodDetail(order.payment_method);
            if (order != null && order.discount_amount != null)
            {
                discountAmount = order.discount_amount.Value;
            }
            // Nếu không có, tính lại dựa trên voucher đã dùng
            else if (order != null && order.voucher_id != null)
            {
                var voucher = db.Vouchers.FirstOrDefault(v => v.voucher_id == order.voucher_id);
                var subtotal = order.OrderItems.Sum(i => i.price * i.quantity);
                if (voucher != null)
                {
                    if (voucher.discount_percent.HasValue)
                        discountAmount = subtotal * voucher.discount_percent.Value / 100;
                    else if (voucher.discount_amount.HasValue)
                        discountAmount = voucher.discount_amount.Value;
                }
            }
            var successModel = new OrderModel
            {
                OrderId = order.order_id,
                OrderDate = order.order_date ?? DateTime.Now,
                TotalAmount = order.total_amount,
                ShippingFee = order.shipping_fee ?? 0,
                CustomerName = order.customer_name,
                Address = order.address,
                Phone = order.phone,
                PaymentMethod = order.payment_method,
                SelectedPaymentDetail = selectedPaymentDetail,
                Items = order.OrderItems.Select(oi => new CartItemModel
                {
                    ProductId = oi.product_id ?? 0,
                    ProductName = oi.Product?.product_name,
                    Price = oi.price,
                    Quantity = oi.quantity
                }).ToList(),
                DiscountAmount = discountAmount,
                VoucherId = order.voucher_id
            };
            return View("OrderSuccess", successModel);
        }
        public ActionResult CancelOrder(int id)
        {
            int? userId = Session["account_id"] as int?;
            if (userId == null)
            {
                TempData["Message"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders.FirstOrDefault(o => o.order_id == id && o.account_id == userId);
            if (order != null && order.status != "Đã hủy")
            {
                order.status = "Đã hủy";
                db.SaveChanges();
                TempData["Message"] = "Đơn hàng đã được hủy.";
            }
            else
            {
                TempData["Message"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền hủy.";
            }

            return RedirectToAction("MyOrders");
        }
        public ActionResult MyOrders()
        {
            int? userId = Session["account_id"] as int?;
            if (userId == null)
            {
                TempData["Message"] = "Vui lòng đăng nhập để xem lịch sử đơn hàng.";
                return RedirectToAction("Login", "Account"); // nếu bạn có trang Login
            }

            var orders = db.Orders
                           .Where(o => o.account_id == userId)
                           .OrderByDescending(o => o.order_date)
                           .ToList();
            ViewBag.PaymentDetails = _paymentMethodsDetails;
            return View(orders);
        }
        public ActionResult BuyNow(int id)
        {
            // Kiểm tra nếu là admin
            if (Session["account_id"] != null && Session["role"]?.ToString() == "admin")
            {
                TempData["Message"] = "Tài khoản admin không được phép mua hàng.";
                return RedirectToAction("Index", "Home");
            }

            var product = db.Products.Find(id);
            if (product == null)
            {
                TempData["Message"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", "Product");
            }

            // Kiểm tra số lượng hàng tồn kho
            if (product.stock <= 0)
            {
                TempData["Message"] = $"Sản phẩm {product.product_name} đã hết hàng.";
                return RedirectToAction("Index", "Product");
            }

            // Lấy giỏ hàng hiện tại nếu có
            var cart = Session["Cart"] as List<CartItemModel>;
            if (cart == null)
            {
                cart = new List<CartItemModel>();
            }

            // Kiểm tra nếu sản phẩm đã tồn tại trong giỏ thì tăng số lượng
            var existingItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (existingItem != null)
            {
                // Kiểm tra xem có vượt quá số lượng tồn kho không
                if (existingItem.Quantity + 1 > product.stock)
                {
                    TempData["Message"] = "Số lượng sản phẩm trong giỏ hàng đã đạt giới hạn tồn kho.";
                    return RedirectToAction("Index", "Product");
                }
                existingItem.Quantity += 1;
            }
            else
            {
                cart.Add(new CartItemModel
                {
                    ProductId = product.product_id,
                    ProductName = product.product_name,
                    Quantity = 1,
                    Price = (product.GiaKhuyenMai.HasValue && product.GiaKhuyenMai.Value > 0)
                        ? product.GiaKhuyenMai.Value
                        : product.price,
                    Images = product.images,
                    Variant = "Mặc định"
                });
            }

            Session["Cart"] = cart;
            return RedirectToAction("Confirm", "Order");
        }
        public ActionResult TransferInstruction()
        {
            var model = TempData["PendingOrder"] as ConfirmOrderViewModel;
            if (model == null)
            {
                return RedirectToAction("Confirm");
            }

            // Dự phòng gán giá nếu còn thiếu
            foreach (var item in model.CartItems)
            {
                if (item.Price == null || item.Price == 0)
                {
                    var product = db.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        item.Price = product.price;
                    }
                }
            }

            ViewBag.PaymentDetails = _paymentMethodsDetails;
            TempData.Keep("PendingOrder"); // giữ lại qua request kế tiếp

            return View(model);
        }
        [HttpPost]
        public ActionResult ConfirmTransfer()
        {
            var model = TempData["PendingOrder"] as ConfirmOrderViewModel;
            if (model == null)
                return RedirectToAction("Confirm");

            TempData.Remove("PendingOrder");
            model.HasTransferred = true;
            Session["Cart"] = model.CartItems;

            return FinalizeOrder(model);
        }
        private decimal CalculateOrderTotal(int orderId, decimal shippingFee, decimal vat)
        {
            var subtotal = db.OrderItems
                .Where(x => x.order_id == orderId)
                .ToList()
                .Sum(x => x.price * x.quantity);

            var vatAmount = subtotal * vat;
            return subtotal + vatAmount + shippingFee;
        }
        [HttpPost]
        public JsonResult CalculateDiscount(int? voucherId)
        {
            var cart = Session["Cart"] as List<CartItemModel>;
            decimal subtotal = cart?.Sum(i => i.Price.GetValueOrDefault(0) * i.Quantity) ?? 0;
            decimal shippingFee = 30000; // hoặc lấy từ session/model nếu có
            decimal vat = subtotal * 0.1m;
            decimal totalWithVat = subtotal + vat + shippingFee;
            decimal discount = 0;

            if (voucherId.HasValue)
            {
                var voucher = db.Vouchers.FirstOrDefault(v => v.voucher_id == voucherId.Value);
                if (voucher != null)
                {
                    if (voucher.discount_percent.HasValue)
                        discount = subtotal * voucher.discount_percent.Value / 100;
                    else if (voucher.discount_amount.HasValue)
                        discount = voucher.discount_amount.Value;
                }
            }

            decimal totalAfterDiscount = totalWithVat - discount;
            if (totalAfterDiscount < 0) totalAfterDiscount = 0;

            return Json(new
            {
                discount = discount.ToString("n0"),
                totalAfterDiscount = totalAfterDiscount.ToString("n0")
            });
        }
        [HttpPost]
        public JsonResult CalculateDiscountByCode(string code)
        {
            var cart = Session["Cart"] as List<CartItemModel>;
            decimal subtotal = cart?.Sum(i => i.Price.GetValueOrDefault(0) * i.Quantity) ?? 0;
            decimal shippingFee = 30000;
            decimal vat = subtotal * 0.1m;
            decimal totalWithVat = subtotal + vat + shippingFee;
            decimal discount = 0;
            int? voucherId = null;

            var voucher = db.Vouchers.FirstOrDefault(v => v.code == code && v.is_active == true && v.end_date >= DateTime.Now);
            if (voucher != null)
            {
                voucherId = voucher.voucher_id;
                if (voucher.discount_percent.HasValue)
                    discount = subtotal * voucher.discount_percent.Value / 100;
                else if (voucher.discount_amount.HasValue)
                    discount = voucher.discount_amount.Value;

                decimal totalAfterDiscount = totalWithVat - discount;
                if (totalAfterDiscount < 0) totalAfterDiscount = 0;

                return Json(new
                {
                    success = true,
                    discount = discount.ToString("n0"),
                    totalAfterDiscount = totalAfterDiscount.ToString("n0"),
                    voucherId = voucher.voucher_id
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Mã voucher không hợp lệ hoặc đã hết hạn.",
                    discount = "0",
                    totalAfterDiscount = totalWithVat.ToString("n0"),
                    voucherId = ""
                });
            }
        }
        // Nút quản lý đơn hàng
        public ActionResult Manage()
        {
            ViewBag.NewOrderCount = db.Orders.Count(o => o.status == "Chờ xử lý");
            ViewBag.ApprovedOrderCount = db.Orders.Count(o => o.status == "Đã xác nhận");
            ViewBag.ReturnOrderCount = db.Orders
                    .Where(o => o.status.Trim().ToLower() == "đã hủy")
                    .Count();
            return View();
        }

        // Nút xem đơn hàng mới
        public ActionResult NewOrders()
        {
            var newOrders = db.Orders
                .Where(o => o.status == "Chờ xử lý")
                .OrderByDescending(o => o.order_date)
                .ToList();

            return View(newOrders);
        }

        // Nút xem chi tiết đơn hàng
        public ActionResult AllNewOrderDetail()
        {
            var orders = db.Orders
                           .Where(o => o.status == "Chờ xử lý")
                           .OrderByDescending(o => o.order_date)
                           .ToList();

            var orderItems = db.OrderItems.Include("Product").ToList();

            ViewBag.Orders = orders;
            return View(orderItems); // View nằm ở Views/Order/AllNewOrderDetails.cshtml
        }

        // Nút duyệt đơn
        [HttpPost]
        public ActionResult ApproveOrder(int orderId)
        {
            var order = db.Orders.FirstOrDefault(o => o.order_id == orderId);
            if (order == null)
            {
                return HttpNotFound();
            }

            order.status = "Đã xác nhận";
            db.SaveChanges();

            return RedirectToAction("ApprovedOrders"); // Trang đơn đã duyệt
        }
        public ActionResult ApprovedOrders()
        {
            var approved = db.Orders
                             .Where(o => o.status == "Đã xác nhận")
                             .OrderByDescending(o => o.order_date)
                             .ToList();

            return View(approved); // View: Views/Order/ApprovedOrders.cshtml
        }

        // Nút xem đơn đã hoàn hàng
        public ActionResult Returns()
        {
            var returnOrders = db.Orders
                .Where(o => o.status.Trim().ToLower() == "đã hủy")
                .OrderByDescending(o => o.order_date)
                .ToList();

            return View(returnOrders);
        }

    }
}