using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class SearchViewModel
    {
        public string Keyword { get; set; }
        public List<Product> Results { get; set; }
    }
}