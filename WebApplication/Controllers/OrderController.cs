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
using static System.Net.Mime.MediaTypeNames;

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

        private string Decrypt(string encrypted)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            return Encoding.UTF8.GetString(data);
        }

        [HttpGet]
        public JsonResult GetCustomerDetails(string data)
        {
            string id = Decrypt(data);
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
        public JsonResult GetCategoryList()
        {
            List<SelectListItem> CategoryList = db.Categories.Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryID.ToString()
            }).ToList();
            return Json(CategoryList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetProductList(string data)
        {
            int id = Convert.ToInt32(Decrypt(data));
            List<SelectListItem> ProductList = db.Products.Where(x => x.CategoryID == id && x.UnitsInStock > 0).Select(x => new SelectListItem
            {
                Text = x.ProductName,
                Value = x.ProductID.ToString()
            }).ToList();
            return Json(ProductList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetProductDetails(string data)
        {
            int id = int.Parse(Decrypt(data));
            Products ThisProduct = db.Products.Where(x => x.ProductID == id).FirstOrDefault();
            return Json(new
                {
                    ProductUnitsInStock = ThisProduct.UnitsInStock,
                    ProductUnitPrice = ThisProduct.UnitPrice,
                },
                JsonRequestBehavior.AllowGet
            );
        }

        [HttpGet]
        public ActionResult PartialViewOrderDetails(OrderDetailsModel OrderDetailsModel)
        {
            var NestedOrderModel = new NestedOrderModel();
            ViewBag.DropDownList_CategoryName = db.Categories.OrderBy(x => x.CategoryID).Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryID.ToString()
            });

            // Update
            if (OrderDetailsModel.CategoryID != 0 && OrderDetailsModel.ProductID != 0)
            {
                ViewBag.DropDownList_ProductName = db.Products.OrderBy(x => x.ProductID).Select(x => new SelectListItem
                {
                    Text = x.ProductName,
                    Value = x.ProductID.ToString()
                });
                NestedOrderModel = new NestedOrderModel
                {
                    OrderDetailsModel = new OrderDetailsModel
                    {
                        CategoryID = OrderDetailsModel.CategoryID,
                        CategoryName = OrderDetailsModel.CategoryName,
                        OrderID = OrderDetailsModel.OrderID,
                        OrderQuantity = OrderDetailsModel.OrderQuantity,
                        ProductID = OrderDetailsModel.ProductID,
                        ProductName = OrderDetailsModel.ProductName,
                        ProductUnitPrice = OrderDetailsModel.ProductUnitPrice
                    },
                };
            }
            else
            {
                NestedOrderModel = new NestedOrderModel
                {
                    OrderDetailsModel = new OrderDetailsModel
                    {
                        OrderID = OrderDetailsModel.OrderID
                    },
                };
            
            }

            return PartialView(NestedOrderModel);
        }


        private IReadOnlyCollection<OrderDetailsModel> GetOrderDetailsList()
        {
            return (from m in db.OrderDetails.AsEnumerable()
                    select new OrderDetailsModel()
                    {
                        CategoryID = m.Products.CategoryID.Value,
                        CategoryName = m.Products.Categories.CategoryName,
                        OrderTotalCost = m.Quantity * m.Products.UnitPrice.Value,
                        OrderID = m.OrderID,
                        OrderQuantity = m.Quantity,
                        ProductID = m.ProductID,
                        ProductName = m.Products.ProductName,
                        ProductUnitPrice = m.Products.UnitPrice.Value,
                    }).ToList();
        }

        private Orders CreateOrders(NestedOrderModel NestedOrderModel)
        {
            int NewOrderID = 0;
            var mOrder = new Orders
            {
                OrderDate = NestedOrderModel.OrderModel.OrderDate,
                EmployeeID = NestedOrderModel.OrderModel.EmployeeID,
                CustomerID = NestedOrderModel.OrderModel.CustomerID
            };
            db.Orders.Add(mOrder);

            try
            {
                db.SaveChanges();
                NewOrderID = mOrder.OrderID;
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine(exception.Message);
                TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
            }

            return db.Orders.Find(NewOrderID);
        }

        private OrderDetails CreateOrderDetails(NestedOrderModel NestedOrderModel)
        {
            var mOrderDetails = new OrderDetails
            {
                OrderID = NestedOrderModel.OrderDetailsModel.OrderID.Value,
                ProductID = NestedOrderModel.OrderDetailsModel.ProductID,
                Quantity = (short)NestedOrderModel.OrderDetailsModel.OrderQuantity
            };
            db.OrderDetails.Add(mOrderDetails);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine(exception.Message);
                TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
            }

            return mOrderDetails;
        }

        private bool DeleteOrders(int OrderID)
        {
            var mOrders = db.Orders.Where(x => x.OrderID == OrderID).FirstOrDefault();
            IList<OrderDetails> mOrderDetails = (from x in db.OrderDetails where x.OrderID == OrderID select x).ToList();

            if (mOrderDetails != null)
            {
                foreach (OrderDetails OrderDetailsList in mOrderDetails)
                {
                    DeleteOrderDetails(OrderID, OrderDetailsList.ProductID);
                }
                db.Orders.Remove(mOrders);

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                    return false;
                }
                return true;
            }
            else
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. ID not found.");
            }
            return false;
        }

        private bool DeleteOrderDetails(int OrderID, int ProductID)
        {
            var mOrderDetails = db.OrderDetails.FirstOrDefault(x => x.OrderID == OrderID && x.ProductID == ProductID);
            if (mOrderDetails != null)
            {
                db.OrderDetails.Remove(mOrderDetails);

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                    return false;
                }
                return true;
            }
            else
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. ID not found.");
            }

            return false;
        }

        private NestedOrderModel RetrieveOrders(int OrderID)
        {
            var mNestedOrderModel = new NestedOrderModel();
            var mOrder = db.Orders.Find(OrderID);
            if (mOrder != null)
            {
                var OrderDetailList = from x in GetOrderDetailsList() where x.OrderID == OrderID select x;
                mNestedOrderModel = new NestedOrderModel()
                {
                    OrderModel = new OrderModel()
                    {
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
                        OrderGrandTotal = OrderDetailList.Sum(x => x.OrderTotalCost),
                        OrderDetailList = OrderDetailList,
                    },
                };
            }
            return mNestedOrderModel;
        }

        private void TriggerBootstrapAlerts(AlertType Type, string Message)
        {
            string _type;
            switch (Type)
            {
                case AlertType.Info: _type = "alert-info"; break;
                case AlertType.Warning: _type = "alert-warning"; break;
                case AlertType.Danger: _type = "alert-danger"; break;
                case AlertType.Success: _type = "alert-success"; break;
                default: _type = "alert-danger"; break;
            }

            TempData["Alerts"] = new Alerts
            {
                Type = _type,
                Message = Message
            };
        }

        private Orders UpdateOrders(int OrderID, NestedOrderModel NestedOrderModel)
        {
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
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                }
            }

            return mOrder;
        }

        private OrderDetails UpdateOrderDetails(int OrderID, int ProductID, NestedOrderModel NestedOrderModel)
        {
            var mOrderDetails = new OrderDetails();
            if (DeleteOrderDetails(OrderID, ProductID))
            {
                mOrderDetails = new OrderDetails
                {
                    OrderID = OrderID,
                    ProductID = NestedOrderModel.OrderDetailsModel.ProductID,
                    Quantity = (short)NestedOrderModel.OrderDetailsModel.OrderQuantity
                };
                db.OrderDetails.Add(mOrderDetails);
                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                }
            }
            return mOrderDetails;
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

        public ActionResult Details(int? OrderID, string Mode)
        {
            if (string.IsNullOrEmpty(Mode) || !(Mode == "Edit" || Mode == "View"))
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 3. Invalid model.");
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

            var mNestedOrderModel = new NestedOrderModel();
            if (OrderID == null)
            {
                if (Mode == "Edit")
                {
                    mNestedOrderModel = new NestedOrderModel
                    {
                        OrderModel = new OrderModel
                        {
                            OrderDate = DateTime.Now,
                        },
                    };
                }
                else
                {
                    TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 5. Invalid model.");
                    return RedirectToAction("Index");
                }
            }
            else
            {
                mNestedOrderModel = RetrieveOrders(OrderID.Value);
                if (mNestedOrderModel == null)
                {
                    TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 4. Invalid model.");
                    return RedirectToAction("Index");
                }
            }

            return View(mNestedOrderModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(int? OrderID, int? ProductID, string Mode, string button, NestedOrderModel NestedOrderModel)
        {
            ModelState.Clear();
            TempData.Clear();

            if (NestedOrderModel is null ||
                string.IsNullOrEmpty(Mode) || Mode != "Edit" ||
                string.IsNullOrEmpty(button) || !(button == "CreateOrders" || button == "UpdateOrders" || button == "DeleteOrders" || button == "CreateOrderDetails" || button == "UpdateOrderDetails" || button == "DeleteOrderDetails"))
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 1. Invalid model.");
                return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
            }

            if (((button == "UpdateOrders" || button == "DeleteOrders") && !OrderID.HasValue) ||
                ((button == "CreateOrderDetails" || button == "UpdateOrderDetails" || button == "DeleteOrderDetails") && !(OrderID.HasValue && ProductID.HasValue)))
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 2. Invalid model.");
                return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
            }


            int? NewOrderID = null;
            var mOrder = new Orders();
            var mOrderDetails = new OrderDetails();

            if (button.Contains("Orders"))
            {
                if (button == "DeleteOrders")
                {
                    if (DeleteOrders(OrderID.Value))
                    {
                        TriggerBootstrapAlerts(AlertType.Success, "Successfully delete order report.");
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Failed to delete order report.");
                    }
                    return RedirectToAction("Index");
                }
                else
                {
                    if (ModelState.IsValid && TryValidateModel(NestedOrderModel.OrderModel, "OrderModel"))
                    {
                        if (button == "CreateOrders")
                        {
                            mOrder = CreateOrders(NestedOrderModel);
                            if (mOrder == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to create new order report.");
                            }
                            else
                            {
                                NewOrderID = mOrder.OrderID;
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully create new order report.");
                            }
                        }
                        if (button == "UpdateOrders")
                        {
                            mOrder = UpdateOrders(OrderID.Value, NestedOrderModel);
                            if (mOrder == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to update order report.");
                            }
                            else
                            {
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully update order report.");
                            }
                            NewOrderID = OrderID.Value;
                        }
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. Invalid model.");
                        return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
                    }
                }
            }

            if (button.Contains("OrderDetails"))
            {
                if (button == "DeleteOrderDetails")
                {
                    if (DeleteOrderDetails(OrderID.Value, ProductID.Value))
                    {
                        TriggerBootstrapAlerts(AlertType.Success, "Successfully delete order details.");
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Failed to delete order details.");
                    }
                }
                else
                {
                    if (ModelState.IsValid && TryValidateModel(NestedOrderModel.OrderDetailsModel, "OrderDetailsModel"))
                    {
                        if (button == "CreateOrderDetails")
                        {
                            mOrderDetails = CreateOrderDetails(NestedOrderModel);
                            if (mOrderDetails == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to create order details.");
                            }
                            else
                            {
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully create order details.");
                            }
                        }
                        if (button == "UpdateOrderDetails")
                        {
                            mOrderDetails = UpdateOrderDetails(OrderID.Value, ProductID.Value, NestedOrderModel);
                            if (mOrderDetails == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to update order details.");
                            }
                            else
                            {
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully update order details.");
                            }
                        }
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. Invalid model.");
                        return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
                    }
                }
                NewOrderID = OrderID.Value;
            }

            return RedirectToAction("Details", new { OrderID = NewOrderID, Mode = "View" });
        }

    }
}
