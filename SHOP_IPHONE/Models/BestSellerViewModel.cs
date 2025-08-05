using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class BestSellerViewModel
    {
        public string ProductName { get; set; }
        public int TotalSold { get; set; }
        public int CancelledSold { get; set; }
    }
}