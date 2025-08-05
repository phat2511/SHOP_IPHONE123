using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class Homemol
    {
        public List<Product> ListProduct1 { get; set; } // Sản phẩm nổi bật
        public IPagedList<Product> ListProduct { get; set; }  // Tất cả sản phẩm (phân trang)
        public List<Voucher> ListVoucher { get; set; }
    }
}