using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Internal.Models
{
    public class OrderViewPageModel
    {
        public string CategoryName { get; set; }

        public string CustomerName { get; set; }

        public DateTime OrderDate { get; set; }

        public int OrderID { get; set; }

        public decimal TotalCost { get; set; }
    }

    public class OrderDetailsModel
    { 
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Please select Product Category.")]
        public string CategoryName { get; set; }

        public decimal OrderTotalCost { get; set; }

        //public float OrderDiscount { get; set; }

        public int OrderID { get; set; }

        public int OrderQuantity { get; set; }

        //public decimal OrderUnitPrice { get; set; }

        public int ProductID { get; set; }

        [Required(ErrorMessage = "Please select Product Name.")]
        public string ProductName { get; set; }

        //public short ProductUnitsInStock { get; set; }

        public int ShipperID { get; set; }

        public string ShipperCompanyName { get; set; }
    }

    public class OrderDetailPageModel
    {
        public IEnumerable<OrderDetailsModel> OrderDetailList { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerContactNumber { get; set; }

        public string CustomerID { get; set; }

        [Required(ErrorMessage = "Please select Customer Name.")]
        public string CustomerName { get; set; }

        public int EmployeeID { get; set; }

        [Required(ErrorMessage = "Please select Employee Name.")]
        public string EmployeeName { get; set; }

        [Required(ErrorMessage = "Please select Order Date.")]
        public DateTime OrderDate { get; set; }

        public decimal OrderGrandTotal { get; set; }
    }

}
