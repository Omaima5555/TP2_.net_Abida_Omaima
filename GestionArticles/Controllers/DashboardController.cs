using GestionArticles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionArticles.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db) => _db = db;

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Data(int days = 30)
        {
            days = Math.Clamp(days, 1, 365);
            var today = DateTime.Now.Date;
            var from = today.AddDays(-(days - 1));
            var prevFrom = from.AddDays(-days);

            var orders = _db.Orders.AsNoTracking().Where(o => o.OrderDate >= from);
            var prevOrders = _db.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= prevFrom && o.OrderDate < from);

            // ---------- Cartes ----------
            var orderCount = await orders.CountAsync();
            var revenue = await orders.SumAsync(o => (double?)o.TotalAmount) ?? 0;
            var prevRevenue = await prevOrders.SumAsync(o => (double?)o.TotalAmount) ?? 0;
            var customers = await orders.Select(o => o.UserId).Distinct().CountAsync();
            double? growth = prevRevenue > 0
                ? Math.Round((revenue - prevRevenue) / prevRevenue * 100, 1)
                : null;

            var lowStockCount = await _db.Products.CountAsync(p => p.QteStock <= 5);
            var avgRating = await _db.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0;
            var reviewCount = await _db.Reviews.CountAsync();

            // ---------- Ventes par jour (jours sans vente = 0) ----------
            var byDay = (await orders.GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Day = g.Key, Total = g.Sum(o => (double)o.TotalAmount), Count = g.Count() })
                .ToListAsync()).ToDictionary(x => x.Day);

            var dates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
            var labels = dates.Select(d => d.ToString("dd/MM")).ToList();
            var revenueDaily = dates.Select(d => byDay.TryGetValue(d, out var x) ? Math.Round(x.Total, 2) : 0).ToList();
            var ordersDaily = dates.Select(d => byDay.TryGetValue(d, out var x) ? x.Count : 0).ToList();

            // ---------- Produits et catégories ----------
            var items = _db.OrderItems.AsNoTracking().Where(i => i.Order.OrderDate >= from);

            var top = await items.GroupBy(i => i.ProductName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Qty).Take(5).ToListAsync();

            // Les anciennes commandes (ProductId = 0) sont ignorées ici
            var byCategory = await (from it in items
                                    join p in _db.Products on it.ProductId equals p.ProductId
                                    group it by p.Category.CategoryName into g
                                    select new { Name = g.Key, Total = g.Sum(x => (double)(x.Price * x.Quantity)) })
                                   .ToListAsync();

            // ---------- Tableaux ----------
            var topCustomers = await orders.GroupBy(o => o.Email)
                .Select(g => new { Email = g.Key, Total = g.Sum(o => (double)o.TotalAmount), Count = g.Count() })
                .OrderByDescending(x => x.Total).Take(5).ToListAsync();

            var lowStock = await _db.Products.AsNoTracking()
                .Where(p => p.QteStock <= 5).OrderBy(p => p.QteStock).Take(8)
                .Select(p => new { p.Name, Category = p.Category.CategoryName, Qty = p.QteStock })
                .ToListAsync();

            var recentRaw = await _db.Orders.AsNoTracking()
                .OrderByDescending(o => o.OrderDate).Take(8)
                .Select(o => new { o.Id, o.Email, o.TotalAmount, o.OrderDate }).ToListAsync();
            var recent = recentRaw.Select(o => new
            {
                id = o.Id,
                email = o.Email,
                total = Math.Round(o.TotalAmount, 2),
                date = o.OrderDate.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return Json(new
            {
                kpis = new
                {
                    revenue = Math.Round(revenue, 2),
                    growth,
                    orders = orderCount,
                    avgBasket = orderCount == 0 ? 0 : Math.Round(revenue / orderCount, 2),
                    customers,
                    lowStock = lowStockCount,
                    avgRating = Math.Round(avgRating, 1),
                    reviews = reviewCount
                },
                labels,
                revenueDaily,
                ordersDaily,
                top,
                byCategory,
                topCustomers,
                lowStock,
                recent
            });
        }
    }
}