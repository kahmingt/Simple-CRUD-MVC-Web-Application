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


        private IReadOnlyCollection<OrderDetailsModel> MapOrderDetails()
        {
            return (from m in db.OrderDetails.AsEnumerable() select new OrderDetailsModel()
            {
                CategoryID = m.Products.CategoryID.Value,
                CategoryName = m.Products.Categories.CategoryName,
                OrderTotalCost = m.Quantity * m.UnitPrice * (decimal)(1 - m.Discount),
                OrderID = m.OrderID,
                OrderQuantity = m.Quantity,
                ProductID = m.ProductID,
                ProductName = m.Products.ProductName,
                ShipperID = m.Orders.Shippers.ShipperID,
                ShipperCompanyName = m.Orders.Shippers.CompanyName,
            }).ToList();
        }

        public ActionResult Details(string Mode, int? OrderID)
        {
            if (!(Mode == "View" || Mode == "Edit"))
            {
                return RedirectToAction("Index");
            }

            ViewBag.DropDownList_CustomerName = db.Customers.OrderBy(x => x.ContactName).Select(x => new SelectListItem
            { 
                Text = x.ContactName,
                Value = x.CustomerID.ToString()
            });
            ViewBag.DropDownList_EmployeeName = db.Employees.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).Select(x => new SelectListItem
            {
                Text = x.FirstName + " " + x.LastName,
                Value = x.EmployeeID.ToString()
            });
            ViewBag.DropDownList_CategoryName = db.Categories.OrderBy(x => x.CategoryName).Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryID.ToString()
            });

            var orderDetailModel = (from m in MapOrderDetails() where m.OrderID == OrderID select m);
            var model = new OrderDetailPageModel();

            if (OrderID == null)
            {
                if (Mode == "View")
                {
                    return RedirectToAction("Index");
                }
            }
            else
            {
                if (orderDetailModel != null)
                {
                    var mOrder = db.Orders.Find(OrderID);
                    model = new OrderDetailPageModel
                    {
                        OrderDetailList = orderDetailModel,
                        CustomerAddress = mOrder.Customers.Address + ", " +
                                          mOrder.Customers.PostalCode + ", " +
                                          mOrder.Customers.City + ", " +
                                          mOrder.Customers.Country,
                        CustomerContactNumber = mOrder.Customers.Phone,
                        CustomerID = mOrder.CustomerID,
                        CustomerName = mOrder.Customers.ContactName,
                        EmployeeID = mOrder.EmployeeID.Value,
                        EmployeeName = mOrder.Employees.FirstName + " " +
                                       mOrder.Employees.LastName,
                        OrderDate = mOrder.OrderDate.Value,
                        OrderGrandTotal = orderDetailModel.Sum(x=>x.OrderTotalCost),
                    };
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

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
