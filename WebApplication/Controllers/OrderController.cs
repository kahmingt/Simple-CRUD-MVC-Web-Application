using Internal.Database;
using Internal.Models;
using System.Collections.Generic;
using PagedList;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Text;

namespace Internal.Controllers
{
    //[Authorize]
    public class OrderController : Controller
    {
        private NorthwindEntities db = new NorthwindEntities();

        public ActionResult Index(
        string Page,
        string Search,
        string Sort
        )
        {
            var orderDetails = db.OrderDetails.ToList();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                orderDetails = db.OrderDetails.Where(x =>
                    x.OrderID.ToString().Contains(Search) ||
                    x.Orders.Customers.ContactName.Contains(Search) ||
                    x.Products.Categories.CategoryName.Contains(Search)).ToList();
            }


            var model = orderDetails.GroupBy(x => x.OrderID, (key, item) => new OrderViewPageModel
            {
                OrderID = key,
                CategoryName = item.First().Products.Categories.CategoryName,
                CustomerName = item.First().Orders.Customers.ContactName,
                OrderDate = item.First().Orders.OrderDate.Value,
                TotalCost = (from m in MapOrderDetails() where m.OrderID == key select m.OrderTotalCost).Sum(),
            });

            if (int.TryParse(Page, out int page))
            {
                return View(model.ToPagedList(page, 10));
            }
            else
            {
                return View(model.ToPagedList(1, 10));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
