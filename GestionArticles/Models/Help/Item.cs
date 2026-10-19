namespace GestionArticles.Models.Help
{
    public class Item
    {
        public int quantite { get; set; }
        private int _ProduitId;
        public Product _product = null;
        public Product Prod
        {
            get { return _product; }
            set { _product = value; }
        }
        public string Description
        {
            get { return _product.Name; }
        }
        public float UnitPrice
        {
            get { return _product.Price; }
        }
        public int categoryId
        {
            get { return _product.CategoryId; }
        }
        public Category category
        {
            get { return _product.Category; }
        }
        public float TotalPrice
        {
            get { return _product.Price * quantite; }
        }
        public Item(Product p)
        {
            this.Prod = p;
        }
        public bool Equals(Item item)
        {
            return item.Prod.ProductId == this.Prod.ProductId;
        }
    }
}
