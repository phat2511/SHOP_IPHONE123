using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class OrderModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public List<CartItemModel> Items { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string PaymentMethod { get; set; }
        public PaymentMethodDetail SelectedPaymentDetail { get; set; }
        public decimal SubTotal => Items.Sum(i => (i.Price ?? 0) * i.Quantity);
        public decimal VAT => SubTotal * 0.1m;
        public decimal Total => SubTotal + VAT + ShippingFee;
        public int? VoucherId { get; set; }
        public string VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }

    }
}