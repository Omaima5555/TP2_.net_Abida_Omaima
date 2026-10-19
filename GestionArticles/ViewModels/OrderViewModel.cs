namespace GestionArticles.ViewModels
{
    public class OrderViewModel
    {
        public string Address { get; set; }
        public string PaymentMethod { get; set; }
        public float TotalAmount { get; set; }
        public List<CartItemViewModel> CartItems { get; set; }
    }
}