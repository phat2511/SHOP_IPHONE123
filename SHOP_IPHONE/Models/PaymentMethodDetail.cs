using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class PaymentMethodDetail
    {
        public string MethodName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Instructions { get; set; }
        public string QrCodeUrl { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string AccountHolder { get; set; }
        public string EwalletNumber { get; set; }
    }
}