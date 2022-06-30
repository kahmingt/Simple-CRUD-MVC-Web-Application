$(document).ready(function () {

    $('#CustomerID').on("change", function () {
        getCustomerDetails();
    });
});



//
// Get Customer details
//
function getCustomerDetails() {
    var CustomerIdSelected = $('#CustomerID :selected').val();
    console.log("__DEBUG__ #CustomerID selected value: " + CustomerIdSelected);

    if (isAlphabet(CustomerIdSelected)) {
        $.ajax({
            type: "GET",
            url: "/Order/GetCustomerDetails/",
            data: { data: btoa(CustomerIdSelected) },
            contentType: "application/x-www-form-urlencoded; charset=UTF-8",
            dataType: "text",
            cache: false,
            success: function (json) {
                var data = JSON.parse(json)
                $('#CustomerAddress').val(data["Address"]);
                $('#CustomerContactNumber').val(data["Contact"]);
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

function isAlphabet(string) {
    return string.length >= 1 && string.match(/[a-zA-Z]/);
}