using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class VoucherViewModel
    {
        public int voucher_id { get; set; }
        public string code { get; set; }
        public string discount_type { get; set; } // "percent" hoặc "amount"
        public decimal discount_value { get; set; }
    }

}