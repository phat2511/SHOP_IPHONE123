using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class CartItemModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public byte[] Images { get; set; }
        public string Variant { get; set; }

        public decimal Total => Quantity * (Price ?? 0);
    }
}