using SHOP_IPHONE.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class AdminController : Controller
    {
        public ActionResult Index()
        {
            if (Session["role"] == null || Session["role"].ToString() != ((int)AccountType.Admin).ToString())
            { return RedirectToAction("Login", "Account"); }


            using (DBiphoneEntities1 db = new DBiphoneEntities1())
            {
                ViewBag.UserCount = db.Accounts.Count();
                ViewBag.ProductCount = db.Products.Count();
                ViewBag.OrderCount = db.Orders.Count();
                ViewBag.BannerCount = db.Banner.Count();
            }

            return View();
        }
        [HttpGet]
        public ActionResult CreateAccount()
        {
            ViewBag.IdTypes = new SelectList(new[] {
        new { Id = (int)AccountType.User, Name = "Người dùng" },
        new { Id = (int)AccountType.Admin, Name = "Admin" }
    }, "Id", "Name");

            return View();
        }

        [HttpPost]
        public ActionResult CreateAccount(string fullName, string username, string email, string password, string confirmPassword, string phone, string address, int idtype)
        {
            using (DBiphoneEntities1 db = new DBiphoneEntities1())
            {
                if (password != confirmPassword)
                {
                    ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp.";
                    return View();
                }
                var existing = db.Accounts.FirstOrDefault(a => a.email == email || a.username == username);
                if (existing != null)
                { 
                    ViewBag.ErrorMessage = "Email hoặc tên đăng nhập đã tồn tại.";
                    return View();
                }

                Account newAccount = new Account
                {
                    full_name = fullName,
                    username = username,
                    email = email,
                    password = password,
                    phone = phone,
                    address = address,
                    idtype = idtype
                };

                db.Accounts.Add(newAccount);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        // Thống kê báo cáo
        public ActionResult Reports(string fromDate, string toDate)
        {
            using (var db = new DBiphoneEntities1())
            {
                DateTime now = DateTime.Now;
                DateTime from = string.IsNullOrEmpty(fromDate)
                    ? now.AddDays(-6).Date
                    : DateTime.ParseExact(fromDate, "dd/MM/yyyy", null);
                DateTime to = string.IsNullOrEmpty(toDate)
                    ? now.Date
                    : DateTime.ParseExact(toDate, "dd/MM/yyyy", null);

                int totalDays = (to - from).Days + 1;
                decimal[] revenueData = new decimal[totalDays];
                string[] chartLabels = new string[totalDays];

                for (int i = 0; i < totalDays; i++)
                {
                    DateTime day = from.AddDays(i);
                    var revenue = db.Orders
                        .Where(o => o.status == "Đã xác nhận"
                            && DbFunctions.TruncateTime(o.order_date) == day.Date)
                        .Sum(o => (decimal?)o.total_amount) ?? 0;

                    revenueData[i] = revenue;
                    chartLabels[i] = day.ToString("dd/MM");
                }

                var confirmedOrders = db.Orders
                    .Where(o => o.status == "Đã xác nhận"
                        && DbFunctions.TruncateTime(o.order_date) >= from
                        && DbFunctions.TruncateTime(o.order_date) <= to);

                var cancelledOrders = db.Orders
                    .Where(o => o.status == "Đã hoàn hàng"
                        && DbFunctions.TruncateTime(o.order_date) >= from
                        && DbFunctions.TruncateTime(o.order_date) <= to);

                ViewBag.FromDate = from.ToString("dd/MM/yyyy");
                ViewBag.ToDate = to.ToString("dd/MM/yyyy");
                ViewBag.RevenueData = revenueData;
                ViewBag.ChartLabels = chartLabels;
                ViewBag.SoldOrders = confirmedOrders.Count();
                ViewBag.CancelledOrders = cancelledOrders.Count();
                ViewBag.Revenue = confirmedOrders.Sum(o => (decimal?)o.total_amount) ?? 0;
                ViewBag.SoldProducts = confirmedOrders
                    .Join(db.OrderItems, o => o.order_id, oi => oi.order_id, (o, oi) => oi)
                    .Sum(oi => (int?)oi.quantity) ?? 0;

                ViewBag.BestSellingProducts = confirmedOrders
                    .Join(db.OrderItems, o => o.order_id, oi => oi.order_id, (o, oi) => oi)
                    .GroupBy(oi => oi.Product.product_name)
                    .Select(g => new BestSellerViewModel
                    {
                        ProductName = g.Key,
                        TotalSold = g.Sum(x => x.quantity)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(2)
                    .ToList();
            }

            return View();
        }

        // Xuất báo cáo PDF
        
        public ActionResult ExportReportPdf(string rangeType = "month")
        {
            using (var db = new DBiphoneEntities1())
            {
                var confirmedOrders = db.Orders
                    .Where(o => o.status == "Đã xác nhận")
                    .OrderByDescending(o => o.order_date)
                    .ToList();

                var cancelledOrders = db.Orders
                    .Where(o => o.status == "Đã hủy")
                    .ToList();

                DateTime now = DateTime.Now;
                decimal[] revenueData;
                string[] chartLabels;

                if (rangeType == "month")
                {
                    revenueData = new decimal[12];
                    chartLabels = Enumerable.Range(1, 12).Select(i => "Th" + i).ToArray();

                    for (int i = 1; i <= 12; i++)
                    {
                        revenueData[i - 1] = confirmedOrders
                            .Where(o => o.order_date.Value.Month == i && o.order_date.Value.Year == now.Year)
                            .Sum(o => (decimal?)o.total_amount) ?? 0;
                    }
                }
                else if (rangeType == "week")
                {
                    revenueData = new decimal[4];
                    chartLabels = new[] { "Tuần 1", "Tuần 2", "Tuần 3", "Tuần 4" };

                    for (int i = 0; i < 4; i++)
                    {
                        DateTime start = new DateTime(now.Year, now.Month, 1).AddDays(i * 7);
                        DateTime end = start.AddDays(6);

                        revenueData[i] = confirmedOrders
                            .Where(o => o.order_date >= start && o.order_date <= end)
                            .Sum(o => (decimal?)o.total_amount) ?? 0;
                    }
                }
                else // 7 ngày gần nhất
                {
                    revenueData = new decimal[7];
                    chartLabels = Enumerable.Range(0, 7)
                        .Select(i => now.AddDays(-6 + i).ToString("dd/MM"))
                        .ToArray();

                    for (int i = 0; i < 7; i++)
                    {
                        DateTime day = now.Date.AddDays(-6 + i);
                        revenueData[i] = confirmedOrders
                            .Where(o => DbFunctions.TruncateTime(o.order_date) == day)
                            .Sum(o => (decimal?)o.total_amount) ?? 0;
                    }
                }

                ViewBag.RevenueData = revenueData;
                ViewBag.ChartLabels = chartLabels;
                ViewBag.RangeType = rangeType;
                ViewBag.SoldOrders = confirmedOrders.Count;
                ViewBag.CancelledOrders = cancelledOrders.Count;
                ViewBag.Revenue = confirmedOrders.Sum(o => (decimal?)o.total_amount) ?? 0;
                ViewBag.SoldProducts = confirmedOrders
                    .Join(db.OrderItems, o => o.order_id, oi => oi.order_id, (o, oi) => oi)
                    .Sum(oi => (int?)oi.quantity) ?? 0;

                ViewBag.OrderDetails = confirmedOrders;

                // ✅ Lấy danh sách top sản phẩm bán chạy kèm số lượng bị hủy
                var bestSellingProducts = db.OrderItems
                    .GroupBy(oi => oi.Product.product_name)
                    .Select(g => new BestSellerViewModel
                    {
                        ProductName = g.Key,
                        TotalSold = g.Where(x => x.Order.status == "Đã xác nhận").Sum(x => (int?)x.quantity) ?? 0,
                        CancelledSold = g.Where(x => x.Order.status == "Đã hoàn hàng").Sum(x => (int?)x.quantity) ?? 0
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(5)
                    .ToList();

                ViewBag.BestSellingProducts = bestSellingProducts;

                return new Rotativa.ViewAsPdf("ReportPdf")
                {
                    FileName = "ThongKe_BaoCao.pdf",
                    PageSize = Rotativa.Options.Size.A4,
                    PageOrientation = Rotativa.Options.Orientation.Portrait
                };
            }
        }


    }
}
