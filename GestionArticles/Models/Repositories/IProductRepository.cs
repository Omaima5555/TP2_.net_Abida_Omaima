namespace GestionArticles.Models.Repositories
{
    public interface IProductRepository
    {
        Product GetById(int Id);
        IList<Product> GetAll();
        void Add(Product t);
        Product Update(Product t);
        void Delete(int Id);
        public IList<Product> GetProductsByCategID(int? CategId);
        public IList<Product> FindByName(string name);
        public IQueryable<Product> GetAllProducts();


    }
}
