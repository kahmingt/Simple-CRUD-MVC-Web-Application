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

}
