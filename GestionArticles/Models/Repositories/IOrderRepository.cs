namespace GestionArticles.Models.Repositories
{
    public interface IOrderRepository
 {
        Order GetById (int Id);

        void Add (Order o);
    }
}
