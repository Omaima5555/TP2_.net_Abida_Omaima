$(document).on("click", ".PlusProducts", function (event) {
event.preventDefault();
var recordtoupdate = $(this).attr("data-id");
if (recordtoupdate !== '') {
$.post("/Panier/PlusProduct", { "id": recordtoupdate }, function (data) {
if (data.ct == "1") {
$('#totalapayer').text(data.Total);
$("#quantite_" + recordtoupdate).text(data.Quatite);
$("#rquantite_" + recordtoupdate).text(data.Quatite);
$("#total_" + recordtoupdate).text(data.TotalRow);
// Redirection après la mise à jour
window.location.href = "/Panier"; // Redirection vers panier
}
});
}
});
$(document).on("click", ".MinProducts", function (event) {
event.preventDefault();
var recordtoupdate = $(this).attr("data-id");
if (recordtoupdate !== '') {
$.post("/Panier/MinusProduct", { "id": recordtoupdate }, function (data) {
if (data.ct == "1") {
$('#totalapayer').text(data.Total);
$("#quantite_" + recordtoupdate).text(data.Quatite);
$("#rquantite_" + recordtoupdate).text(data.Quatite);
$("#total_" + recordtoupdate).text(data.TotalRow);
// Redirection après la mise à jour
window.location.href = "/Panier";
}
});
}
});
$(document).on("click", ".RemoveLink", function (event) {
event.preventDefault();
// Get the id from the link
var recordtoupdate = $(this).attr("data-id");
if (recordtoupdate != '') {
// Perform the ajax post
$.post("/Panier/RemoveProduct", { "id": recordtoupdate },
function (data) {
$("#row-" + recordtoupdate).fadeOut('slow');
$('#totalapayer').text(data.Total);
window.location.href = "/Panier";
});
}
});
