$(document).ready(function () {

    console.log("Welcome");
    var ModalButtonClicked;
    var ThisOrderIDValue = GetOrderIdFromModal($(this));
    console.log("OrderId  > " + GetOrderIdFromModal($(this)));


    ///
    /// [Order]
    ///
    
    /// Get and update Employee ID
    $('#EmployeeID').on("change", function () {
        console.log("on change #EmployeeID");
        $('#EmployeeName').val($('#EmployeeID :selected').text());
    });

    /// Get and update Customer Details
    $('#CustomerID').on("change", function () {
        console.log("on change #CustomerID");
        GetCustomerDetails();
        $('#CustomerName').val($('#CustomerID :selected').text());
    });

    /// Bootstrap modal
    $(document).on("click", '#ButtonDelete_Orders', function () {
        $('#FormDelete_Orders').attr('action', "/Order/Details?OrderID=" + ThisOrderIDValue + "&ProductID=&Mode=Edit");
    });



    ///
    /// [OrderDetails]
    ///

    /// Get filtered product based on category
    $(document).on("change", '#CategoryID', function () {
        $('#Modal_OrderDetails #CategoryName').val($('#Modal_OrderDetails #CategoryID').children(':selected').text());
        GetProductList($('#Modal_OrderDetails'));

        // Reset and update
        $('#Modal_OrderDetails').find("#OrderQuantity").val("0");
        $('#Modal_OrderDetails').find("#ProductUnitPrice").val("");
        UpdateOrderTotalCost($('#Modal_OrderDetails'));
    });

    /// Compute & update order total cost
    $(document).on("change", '#ProductID', function () {
        $.when(GetProductStockStatus($('#Modal_OrderDetails'))).done(function () {
            $('#Modal_OrderDetails #ProductName').val($('#Modal_OrderDetails #ProductID').children(':selected').text());
        });

        // Reset and update
        $('#Modal_OrderDetails').find("#OrderQuantity").val("1");
        $('#Modal_OrderDetails').find("#OrderQuantity").prop('disabled', false);
    });

    /// Validate #OrderQuantity input
    $(document).on("input keyup keydown mouseup mousedown select contextmenu paste drop focusout", '#OrderQuantity', function (event) {
        var OrderQuantity = $(this).val();
        var MaxOrderQuantity = $('#Modal_OrderDetails #ProductUnitsInStock').val();
    
        if (["keydown", "mousedown", "focusout"].indexOf(event.type) >= 0) {
            $(this).removeClass("have-error");
            $(this).css("border", "");
            this.setCustomValidity("");
        }
    
        // Remove '+' symbol
        // Use float, to ensure CustomValidity during paste/contextmenu fired!
        $('#OrderQuantity').val('');
        $('#OrderQuantity').val(parseFloat(OrderQuantity));
    
        if (event.keyCode == 69 ||
            event.keyCode == 107 ||
            event.keyCode == 109 || event.keyCode == 110 ||
            event.keyCode == 189 || event.keyCode == 190) {
            event.preventDefault();
            $(this).addClass("have-error");
            $(this).css("border", "2px solid red");
            this.setCustomValidity("Only numbers [0-9] are allowed.");
            this.reportValidity();
        }
        if (/[-]/.test(OrderQuantity)) {
            $(this).addClass("have-error");
            $(this).css("border", "2px solid red");
            this.setCustomValidity("No negative value is allowed.");
            this.reportValidity();
        }
        if (/[.]/.test(OrderQuantity)) {
            $(this).addClass("have-error");
            $(this).css("border", "2px solid red");
            this.setCustomValidity("No decimal place is allowed.");
            this.reportValidity();
        }
        if ("focusout".indexOf(event.type) >= 0) {
            if (OrderQuantity == 0) {
                $(this).addClass("have-error");
                $(this).css("border", "2px solid red");
                this.setCustomValidity("Order quantity cannot be zero.");
                this.reportValidity();
            }
            if (OrderQuantity == "") {
                $(this).addClass("have-error");
                $(this).css("border", "2px solid red");
                this.setCustomValidity("Order quantity cannot be left empty.");
                this.reportValidity();
            }
        }
    
        if (parseInt(OrderQuantity) > parseInt(MaxOrderQuantity)) {
            event.preventDefault();
            $('#Modal_OrderDetails #OrderQuantity').val(MaxOrderQuantity);
            this.setCustomValidity("Maximum order quantity is " + MaxOrderQuantity + ".");
            this.reportValidity();
        }

        if (!$(this).hasClass(("have-error"))) {
            UpdateOrderTotalCost($('#Modal_OrderDetails'));
        }
    });

    /// Bootstrap modal
    $(document).on("click", '#ButtonAdd_OrderDetails', function () {
        ModalButtonClicked = "ButtonAdd_OrderDetails";
        $.ajax({
            type: "GET",
            url: "/Order/PartialViewOrderDetails",
            contentType: "application/json; charset=UTF-8",
            data: { OrderID: ThisOrderIDValue },
            dataType: "html",
            cache: false,
            success: function (response) {
                console.log("[HTTPGet] PartialViewOrderDetails success!");
                $('#Modal_OrderDetails .modal-body').html(response);
                GetCategoryList($('#Modal_OrderDetails'));
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        });

        $('#Modal_OrderDetails .modal-header h4').text("Add New Order Details Entry");
        $('#Modal_OrderDetails #ModalButtonSubmit_OrderDetails').attr("value", "CreateOrderDetails");
        $('#Modal_OrderDetails #ModalButtonSubmit_OrderDetails').attr("title", "Create Order Details");
        $('#Modal_OrderDetails').modal('show');
    });

    $(document).on("click", '#ButtonEdit_OrderDetails', function () {
        ModalButtonClicked = "ButtonEdit_OrderDetails";
        $.ajax({
            type: "GET",
            url: "/Order/PartialViewOrderDetails",
            contentType: "application/json; charset=UTF-8",
            data: GetTableRowOrderDetailsInJson($(this)),
            dataType: "html",
            cache: false,
            success: function (response) {
                console.log("[HTTPGet] PartialViewOrderDetails success!");
                $('#Modal_OrderDetails .modal-body').html(response);
                GetProductList($('#Modal_OrderDetails'));
                GetProductStockStatus($('#Modal_OrderDetails'));
                UpdateOrderTotalCost($('#Modal_OrderDetails'));
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        });

        $('#Modal_OrderDetails .modal-header h4').text("Edit Order Details Entry");
        $('#Modal_OrderDetails #ModalButtonSubmit_OrderDetails').attr("value", "UpdateOrderDetails");
        $('#Modal_OrderDetails #ModalButtonSubmit_OrderDetails').attr("title", "Update Order Details");
        $('#Modal_OrderDetails').modal('show');
    });

    $(document).on("click", '#ButtonDelete_OrderDetails', function () {
        var ThisRowData = GetTableRowOrderDetailsInJson($(this));
        $('#FormDelete_OrderDetails').attr('action', "/Order/Details?OrderID=" + ThisRowData["OrderID"] + "&ProductID=" + ThisRowData["ProductID"] + "&Mode=Edit");
    });

    $(document).on("click", '#ModalButtonSubmit_OrderDetails', function () {
        console.log("ModalButtonClicked: " + ModalButtonClicked);
        var ThisProductIdValue = GetProductIdFromModal($('#Modal_OrderDetails'));
        $('#FormAddEdit_OrderDetails').attr("action", "/Order/Details?OrderID=" + ThisOrderIDValue + "&ProductID=" + ThisProductIdValue + "&Mode=Edit");
    });

    $('#Modal_OrderDetails').on('shown.bs.modal', function ()
    {
        if (ModalButtonClicked == "ButtonAdd_OrderDetails")
        {
            $('#Modal_OrderDetails').find('#CategoryID>option:eq()').prop('selected', true);
            $('#Modal_OrderDetails').find('#ProductID').prop('disabled', true);
            $('#Modal_OrderDetails').find("#OrderQuantity").prop('disabled', true);
            $('#Modal_OrderDetails').find("#OrderQuantity").val("1");
            UpdateOrderTotalCost($('#Modal_OrderDetails'));
        }
    })

});


///
/// [Order] Sub-functions Get & Update
///
function GetCustomerDetails() {
    var CustomerIdSelected = $('#CustomerID :selected').val();

    if (IsAlphabet(CustomerIdSelected)) {
        $.ajax({
            type: "GET",
            url: "/Order/GetCustomerDetails/",
            data: { data: btoa(CustomerIdSelected) },
            contentType: "application/x-www-form-urlencoded; charset=UTF-8",
            dataType: "text",
            cache: false,
            success: function (json) {
                var Data = JSON.parse(json)
                $('#CustomerAddress').val(Data["Address"]);
                $('#CustomerContactNumber').val(Data["Contact"]);
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        });
    }
    else {
        // Reset
        $('#CustomerContactNumber').val("");
        $('#CustomerAddress').val("");
    }
}


///
/// [OrderDetails] Sub-functions Get & Update
///
function GetCategoryList(ThisElement) {
    var DOMCategoryID = ThisElement.find('#CategoryID');
    return $.ajax({
        type: "GET",
        url: "/Order/GetCategoryList/",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        data: {},
        dataType: "json",
        cache: false,
        success: function (json) {
            console.log("[HttpGet] GetCategoryList successful!");
            DOMCategoryID.html('');
            DOMCategoryID.append('<option value="">-- Select Category --</option>');
            for (var i in json) {
                DOMCategoryID.append('<option value="' + json[i].Value + '">' + json[i].Text + '</option>');
            }
        },
        error: function (xhr, status, thrownError) {
            alert("Status code : " + xhr.status);
            alert(thrownError);
        }
    });
}

function GetOrderIdFromModal(ThisElement) {
    return ThisElement.find('#OrderID').val();
}

function GetProductIdFromModal(ThisElement) {
    return ThisElement.find('#ProductID').children(':selected').val();
}

function GetProductList(ThisElement) {
    var DOMProductID = ThisElement.find('#ProductID');
    var CategoryIdSelected = ThisElement.find('#CategoryID').children(':selected').val();
    var ProductIdSelected = $('#Modal_OrderDetails').find('#ProductID').children(':selected').val();
    console.log("ProductIdSelected: " + ProductIdSelected);

    if ($.isNumeric(CategoryIdSelected)) {
        ThisElement.find('#ProductID').prop('disabled', false);
    
        $.ajax({
            type: "GET",
            url: "/Order/GetProductList/",
            data: { data: btoa(CategoryIdSelected) },
            contentType: "application/x-www-form-urlencoded; charset=UTF-8",
            dataType: "json",
            cache: false,
            success: function (json) {
                console.log("[HttpGet] GetProductList successful!");
                DOMProductID.html('');
                DOMProductID.append('<option value="">-- Select Product --</option>');
                for (var i in json) {
                    DOMProductID.append('<option value="' + json[i].Value + '">' + json[i].Text + '</option>');
                }

                // Reselect ProductId
                if ($.isNumeric(ProductIdSelected))
                {
                    ThisElement.find('#ProductID > option[value=' + ProductIdSelected + ']').attr('selected', 'selected');
                }
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        });
    }
    else {
        // Reset
        ThisElement.find('#ProductID>option:eq()').prop('selected', true);
        ThisElement.find('#ProductID').prop('disabled', true);
    }
}

function GetProductStockStatus(ThisElement) {
    var ProductIdSelected = ThisElement.find('#ProductID').children(':selected').val();

    $.when(
        $.ajax({
                type: "GET",
                url: "/Order/GetProductDetails/",
                data: { data: btoa(ProductIdSelected) },
                contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                dataType: "html",
                success: function (json) {
                    console.log("[HttpGet] GetProductDetails successful!");
                    var Data = JSON.parse(json);
                    ThisElement.find('#ProductUnitsInStock').val(Data["ProductUnitsInStock"]);
                    ThisElement.find('#ProductUnitPrice').val(TwoDecimalPlaces(Data["ProductUnitPrice"]));
                },
                error: function (xhr, status, thrownError) {
                    alert(xhr.status);
                    alert(thrownError);
                }
            })
    ).done(function () {
        console.log("'" + ThisElement.find('#ProductName').val() + "' maximum purchase quantity is " + ThisElement.find('#ProductUnitsInStock').val());
        //console.log("'" + ThisElement.find('#ProductName').val() + "' unit price is " + ThisElement.find('#ProductUnitPrice').val());
        ThisElement.find('#OrderQuantity').attr("max", ThisElement.find('#ProductUnitsInStock').val());
        UpdateOrderTotalCost(ThisElement);
    });
}

function GetTableRowOrderDetailsInJson(ThisElement) {
    var ModelObject = {};
    ThisElement.closest("tr").each(function () {
        if ($(":input")) {
            $('input', this).each(function () {
                if ($(this).val()) {
                    //console.log($(this).attr("id") + "=" + $(this).val());
                    ModelObject[$(this).attr("id")] = $(this).val()
                }
            });
        }
    });
    ThisElement.closest("tr").find("td").each(function () {
        if ($(this).attr("id")) {
            //console.log($(this).attr("id") + "=" + $(this).html().replace("$", ""));
            ModelObject[$(this).attr("id")] = $(this).html().replace("$", "");
        }
    });
    return ModelObject
}

function IsAlphabet(String) {
    return String.length >= 1 && String.match(/[a-zA-Z]/);
}

function TwoDecimalPlaces(Number, Symbol) {
    if (!$.isNumeric(Number)) return;
    Number = parseFloat(Number).toFixed(2);
    var n = Number.split('').reverse().join("");
    var n2 = n.replace(/\d\d\d(?!$)/g, "$&,");
    var res = n2.split('').reverse().join('');
    res = (Symbol == null) ? (Number) : (Number = Symbol + Number);
    return res;
}

function UpdateOrderTotalCost(ThisElement) {
    var OrderQuantity = parseInt(ThisElement.find('#OrderQuantity').val());
    var ProductUnitPrice = parseFloat(ThisElement.find('#ProductUnitPrice').val());
    var TotalCost = OrderQuantity * ProductUnitPrice;

    //console.log("OrderQuantity: " + OrderQuantity);
    //console.log("ProductUnitPrice: " + ProductUnitPrice);
    //console.log("TotalCost: " + TotalCost);
    ThisElement.find("#OrderTotalCost").val(TwoDecimalPlaces(TotalCost))
}
