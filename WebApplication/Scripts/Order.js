// https://stackoverflow.com/questions/3709597/wait-until-all-jquery-ajax-requests-are-done
// https://stackoverflow.com/questions/19194177/jquery-select-change-not-firing
// https://stackoverflow.com/questions/44963298/jqueryi-want-to-make-a-particular-table-row-readonly-based-on-column-td-value
// https://stackoverflow.com/questions/7767593/jquery-passing-this-to-a-function
// https://stackoverflow.com/questions/6658752/click-event-doesnt-work-on-dynamically-generated-elements
// https://stackoverflow.com/questions/7858385/how-to-add-values-to-an-array-of-objects-dynamically-in-javascript


$(document).ready(function () {

    console.log("Welcome");

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


    ///
    /// [OrderDetails]
    ///

    /// Edit OrderDetails entry
    $(document).on("click", "#ButtonOrderDetailEdit", function () {
        console.log("on click #ButtonOrderDetailEdit");

        var model = GetTableRowOrderDetailsInJson($(this));
        return $.ajax({
            type: "GET",
            url: "/Order/_PartialView_OrderDetailsView",
            contentType: "application/json; charset=UTF-8",
            data: model,
            dataType: "html",
            cache: false,
            success: function (response) {
                console.log("[HTTPGet] _PartialView_OrderDetailsView success!");
                $('#ModalOrderDetailEdit .modal-body').html(response);
                $('#ModalOrderDetailEdit').modal('show');
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        });
    });

    /// Display bootstrap model OrderDetails edit entry
    $('#ModalOrderDetailEdit').on("show.bs.modal", function (event) {
        var OrderID = $('#ModalOrderDetailEdit #OrderID').val();
        if (OrderID) {
            console.log("Edit Old OrderDetails @OrderID=" + OrderID);
            GetProductStockStatus($('#ModalOrderDetailEdit'));
        }
        else {
            console.log("Unable to edit OrderDetails without valid OrderID ??????");
            event.preventDefault();
        }
    });

    /// Get filtered product list based on Product Category
    $(document).on("change", '#CategoryID', function () {
        $('#ModalOrderDetailEdit #CategoryName').val($('#ModalOrderDetailEdit #CategoryID').children(':selected').text());
        GetProductList($('#ModalOrderDetailEdit'));
    });

    /// Compute & update order total cost
    $(document).on("change", '#ProductID', function () {

        $.when(GetProductStockStatus($('#ModalOrderDetailEdit'))).done(function () {
            $('#ModalOrderDetailEdit #ProductName').val($('#ModalOrderDetailEdit #ProductID').children(':selected').text());

            // Update MaxOrderQuantity & TotalCost
            //UpdateOrderQuantityAttribute($('#ModalOrderDetailEdit'));
            UpdateOrderTotalCost($('#ModalOrderDetailEdit'));

            //console.log("{change} #ProductID >> UpdateOrderGrandTotal()")
            //UpdateOrderGrandTotal();
        });
    });

    /// Validate #OrderQuantity input
    $(document).on("input keyup keydown mouseup mousedown select contextmenu paste drop focusout", '#OrderQuantity', function (event) {
        var OrderQuantity = $(this).val();
        var MaxOrderQuantity = $('#ModalOrderDetailEdit #ProductUnitsInStock').val();
    
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
            $('#ModalOrderDetailEdit #OrderQuantity').val(MaxOrderQuantity);
            this.setCustomValidity("Maximum order quantity is " + MaxOrderQuantity + ".");
            this.reportValidity();
        }

        if (!$(this).hasClass(("have-error"))) {
            UpdateOrderTotalCost($('#ModalOrderDetailEdit'));
        }
    });


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
function GetCategoryList(ThisObject) {
    var DOMCategoryID = ThisObject.find('#CategoryID');
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

function GetProductList(ThisObject) {
    var DOMProductID = ThisObject.find('#ProductID')
    var CategoryIdSelected = ThisObject.find('#CategoryID').children(':selected').val();

    if ($.isNumeric(CategoryIdSelected)) {
        ThisObject.find('#ProductID').prop('disabled', false);
    
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
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        });
    }
    else {
        // Reset
        ThisObject.find('#ProductID>option:eq()').prop('selected', true);
        ThisObject.find('#ProductID').prop('disabled', true);
    }
}

function GetProductStockStatus(ThisObject) {
    var ProductIdSelected = ThisObject.find('#ProductID').children(':selected').val();

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
                    ThisObject.find('#ProductUnitsInStock').val(Data["ProductUnitsInStock"]);
                    ThisObject.find('#ProductUnitPrice').val(TwoDecimalPlaces(Data["ProductUnitPrice"]));
                },
                error: function (xhr, status, thrownError) {
                    alert(xhr.status);
                    alert(thrownError);
                }
            })
    ).done(function () {
        console.log("'" + ThisObject.find('#ProductName').val() + "' maximum purchase quantity is " + ThisObject.find('#ProductUnitsInStock').val());
        ThisObject.find('#OrderQuantity').attr("max", ThisObject.find('#ProductUnitsInStock').val());
    });
}

function GetTableRowOrderDetailsInJson(ThisObject) {
    var ModelObject = {};
    ThisObject.closest("tr").each(function () {
        if ($(":input")) {
            $('input', this).each(function () {
                if ($(this).val()) {
                    //console.log($(this).attr("id") + "=" + $(this).val());
                    ModelObject[$(this).attr("id")] = $(this).val()
                }
            });
        }
    });
    ThisObject.closest("tr").find("td").each(function () {
        if ($(this).attr("id")) {
            //console.log($(this).attr("id") + "=" + $(this).html().replace("$", ""));
            ModelObject[$(this).attr("id")] = $(this).html().replace("$", "");
        }
    });
    return ModelObject
}

function IsAlphabet(string) {
    return string.length >= 1 && string.match(/[a-zA-Z]/);
}

function TwoDecimalPlaces(number, symbol) {
    if (!$.isNumeric(number)) return;
    number = parseFloat(number).toFixed(2);
    var n = number.split('').reverse().join("");
    var n2 = n.replace(/\d\d\d(?!$)/g, "$&,");
    var res = n2.split('').reverse().join('');
    res = (symbol == null) ? (number) : (number = symbol + number);
    return res;
}

function UpdateOrderTotalCost(ThisObject) {
    var OrderQuantity = parseInt(ThisObject.find('#OrderQuantity').val());
    var ProductUnitPrice = parseFloat(ThisObject.find('#ProductUnitPrice').val());
    var TotalCost = OrderQuantity * ProductUnitPrice;

    //console.log("OrderQuantity: " + OrderQuantity);
    //console.log("ProductDiscount: " + ProductDiscount);
    //console.log("ProductUnitPrice: " + ProductUnitPrice);
    //console.log("TotalCost: " + TotalCost);
    ThisObject.find("#OrderTotalCost").val(TwoDecimalPlaces(TotalCost))
}

