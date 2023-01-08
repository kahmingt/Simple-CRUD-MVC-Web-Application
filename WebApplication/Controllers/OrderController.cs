using WebApplication.Database;
using WebApplication.Models;

using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Web.Mvc;
using System.Reflection;
using System.Net;

namespace WebApplication.Controllers
{
    //[Authorize]
    public class OrderController : Controller
    {
        private NorthwindEntities db = new NorthwindEntities();


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string decrypt(string encrypted)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            return Encoding.UTF8.GetString(data);
        }

        [HttpGet]
        public JsonResult GetCategoryList()
        {
            List<SelectListItem> CategoryList = db.Categories.Select(x => new SelectListItem { Text = x.CategoryName, Value = x.CategoryID.ToString() }).ToList();
            return Json(CategoryList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCustomerDetails(string data)
        {
            string id = decrypt(data);
            Customers ThisCustomer = db.Customers.Where(x => x.CustomerID == id).FirstOrDefault();

            var address = "";
            address += (string.IsNullOrWhiteSpace(ThisCustomer.Address) ? "" : ThisCustomer.Address);
            address += (string.IsNullOrWhiteSpace(ThisCustomer.PostalCode) ? "" : ", " + ThisCustomer.PostalCode);
            address += (string.IsNullOrWhiteSpace(ThisCustomer.City) ? "" : ", " + ThisCustomer.City);
            address += (string.IsNullOrWhiteSpace(ThisCustomer.Region) ? "" : ", " + ThisCustomer.Region);

            return Json(new
            {
                Address = address,
                Contact = ThisCustomer.Phone
            },
                JsonRequestBehavior.AllowGet
            );
        }

        [HttpGet]
        public JsonResult GetProductDetails(string data)
        {
            int id = int.Parse(decrypt(data));
            Products ThisProduct = db.Products.Where(x => x.ProductID == id).FirstOrDefault();
            OrderDetails ThisOrderDetail = db.OrderDetails.Where(x => x.ProductID == id).FirstOrDefault();

            return Json(new
                {
                    ProductUnitsInStock = ThisProduct.UnitsInStock,
                    ProductUnitPrice = ThisOrderDetail.UnitPrice,
                },
                JsonRequestBehavior.AllowGet
            );
        }

        [HttpGet]
        public JsonResult GetProductList(string data)
        {
            int id = Convert.ToInt32(decrypt(data));
            List<SelectListItem> ProductList = db.Products.Where(x => x.CategoryID == id).Select(x => new SelectListItem { Text = x.ProductName, Value = x.ProductID.ToString() }).ToList();
            return Json(ProductList, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult _PartialView_OrderDetailsView(OrderDetailsModel OrderDetailsModel)
        {
            ViewBag.DropDownList_CategoryName = db.Categories.OrderBy(x => x.CategoryID).Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryID.ToString()
            });
            ViewBag.DropDownList_ProductName = db.Products.OrderBy(x => x.ProductID).Select(x => new SelectListItem
            {
                Text = x.ProductName,
                Value = x.ProductID.ToString()
            });
            return PartialView(OrderDetailsModel);
        }


        private IReadOnlyCollection<OrderDetailsModel> GetOrderDetailsList()
        {
            return (from m in db.OrderDetails.AsEnumerable()
                    select new OrderDetailsModel() {
                        CategoryID = m.Products.CategoryID.Value,
                        CategoryName = m.Products.Categories.CategoryName,
                        OrderTotalCost = m.Quantity * m.UnitPrice,
                        OrderID = m.OrderID,
                        OrderQuantity = m.Quantity,
                        ProductID = m.ProductID,
                        ProductName = m.Products.ProductName,
                        ProductUnitPrice = m.UnitPrice,
                    }).ToList();
        }

        private NestedOrderModel RetrieveOrder(int OrderID)
        {
            var NestedOrderModel = new NestedOrderModel();
            var mOrder = db.Orders.Find(OrderID);
            if (mOrder != null)
            {
                var OrderDetailList = (from m in GetOrderDetailsList() where m.OrderID == OrderID select m);
                NestedOrderModel = new NestedOrderModel()
                {
                    OrderModel = new OrderModel()
                    {
                        OrderDetailList = OrderDetailList,
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
                        OrderGrandTotal = OrderDetailList.Sum(x => x.OrderTotalCost)
                    }
                };
            }
            return NestedOrderModel;
        }


        /// Unverified
        private NestedOrderModel UpdateOrder(int OrderID)
        {
            var NestedOrderModel = new NestedOrderModel();
            var mOrder = db.Orders.Find(OrderID);
            if (mOrder != null)
            {
                mOrder.OrderDate = NestedOrderModel.OrderModel.OrderDate;
                mOrder.EmployeeID = NestedOrderModel.OrderModel.EmployeeID;
                mOrder.CustomerID = NestedOrderModel.OrderModel.CustomerID;

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                }
            }
            return NestedOrderModel;
        }

        private bool DeleteOrderDetails(int? OrderID)
        {
            if (OrderID != null)
            {
                var Model = new OrderDetails { OrderID = OrderID.Value };
                if (Model != null)
                {
                    db.OrderDetails.Attach(Model);
                    db.OrderDetails.Remove(Model);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateException exception)
                    {
                        Debug.WriteLine(exception.Message);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private bool DeleteOrder(int? OrderID)
        {
            if (OrderID != null)
            {
                var Model = new Orders { OrderID = OrderID.Value };
                if (Model != null)
                {
                    db.Orders.Attach(Model);
                    db.Orders.Remove(Model);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateException exception)
                    {
                        Debug.WriteLine(exception.Message);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        [HttpGet]
        public ActionResult Delete(int? OrderID, string Button)
        {
            if (OrderID == null)
            {
                ViewBag.AlertMessage = "Unknown parameter found! Delete action cancelled.";
                ViewBag.AlertType = "Danger";
            }
            else
            {
                if (Button == "Order")
                {
                    if (!DeleteOrderDetails(OrderID.Value))
                    {
                        ViewBag.AlertMessage = "Unable to delete Order Detail. Delete action cancelled.";
                        ViewBag.AlertType = "Danger";
                        return RedirectToAction("Details", new { OrderID = OrderID });
                    }
                    if (!DeleteOrder(OrderID.Value))
                    {
                        ViewBag.AlertMessage = "Unable to delete Order. Delete action cancelled.";
                        ViewBag.AlertType = "Danger";
                        return RedirectToAction("Details", new { OrderID = OrderID });
                    }

                    ViewBag.AlertMessage = "Delete successfully!";
                    ViewBag.AlertType = "Success";

                }
                else if (Button == "OrderDetails")
                {
                    if (!DeleteOrderDetails(OrderID.Value))
                    {
                        ViewBag.AlertMessage = "Unable to delete Order Detail. Delete action cancelled.";
                        ViewBag.AlertType = "Danger";
                        return RedirectToAction("Details", new { OrderID = OrderID });
                    }

                    ViewBag.AlertMessage = "Delete successfully!";
                    ViewBag.AlertType = "Success";
                }
                else
                {
                    ViewBag.AlertMessage = "Unknown parameter found! Delete action cancelled.";
                    ViewBag.AlertType = "Danger";
                    return RedirectToAction("Details", new { OrderID = OrderID });
                }
            }
            return RedirectToAction("Index");
        }


        public ActionResult Details(string Mode, int? OrderID)
        {
            if (!string.IsNullOrEmpty(Mode) && !(Mode == "EditOrder" || Mode == "EditOrderDetails"))
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

            // New Report
            if (OrderID == null)
            {
                if (Mode == "EditOrder")
                {
                    var NestedOrderModel = new NestedOrderModel
                    {
                        OrderModel = new OrderModel
                        {
                            OrderDate = DateTime.Now,
                        },
                    };
                    return View(NestedOrderModel);
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            // Retrieve Order Report & Order Details
            else
            {
                var NestedOrderModel = RetrieveOrder(OrderID.Value);
                if (NestedOrderModel != null)
                {
                    return View(NestedOrderModel);
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
        }



        // Do i still have all the OrderDetails list on posted?

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(int? OrderID, string Mode, NestedOrderModel NestedOrderModel)
        {
            ModelState.Clear();

            if (!string.IsNullOrEmpty(Mode) && !(Mode == "EditOrder" || Mode == "EditOrderDetails"))
            {
                return RedirectToAction("Index");
            }

            int? NewOrderID = null;
            if (ModelState.IsValid && TryValidateModel(NestedOrderModel.OrderModel, "OrderModel") &&
               Mode == "EditOrder")
            {
                var mOrder = new Orders();
                if (OrderID == null)
                {
                    mOrder.OrderDate = NestedOrderModel.OrderModel.OrderDate;
                    mOrder.EmployeeID = NestedOrderModel.OrderModel.EmployeeID;
                    mOrder.CustomerID = NestedOrderModel.OrderModel.CustomerID;
                    db.Orders.Add(mOrder);
                }
                else
                {
                    // Update Order Report
                    mOrder = db.Orders.Find(OrderID);
                    if (mOrder != null)
                    {
                        mOrder.OrderDate = NestedOrderModel.OrderModel.OrderDate;
                        mOrder.EmployeeID = NestedOrderModel.OrderModel.EmployeeID;
                        mOrder.CustomerID = NestedOrderModel.OrderModel.CustomerID;
                    }
                    else
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }
                }

                try
                {
                    db.SaveChanges();
                    NewOrderID = mOrder.OrderID;

                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                }
            }

            return RedirectToAction("Details", new { OrderID = NewOrderID });
        }

        public ActionResult Index(
            string Page,
            string Search,
            string Sort)
        {

            // Populate OrderIndexList based on which column to display
            var OrderIndexList = db.OrderDetails.ToList();
            var Model = OrderIndexList.GroupBy(x => x.OrderID, (key, item) => new OrderIndexListModel()
            {
                OrderID = key,
                CustomerName = item.First().Orders.Customers.ContactName,
                OrderDate = item.First().Orders.OrderDate.Value,
                TotalCost = (from m in GetOrderDetailsList() where m.OrderID == key select m.OrderTotalCost).Sum()
            });

            if (int.TryParse(Page, out int page))
            {
                return View(Model.ToPagedList(page, 10));
            }
            else
            {
                return View(Model.ToPagedList(1, 10));
            }
        }



    }
}
