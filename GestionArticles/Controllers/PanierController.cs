using GestionArticles.Models;
using GestionArticles.Models.Help;
using GestionArticles.Models.Repositories;
using GestionArticles.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace GestionArticles.Controllers
{
    public class PanierController : Controller
    {
        readonly IProductRepository productRepository;
        readonly IOrderRepository orderRepository;
        private readonly UserManager<IdentityUser> userManager;
        private readonly AppDbContext db;                       // ← nouveau (décrément du stock)

        public PanierController(IProductRepository productRepository,
                                IOrderRepository orderRepository,
                                UserManager<IdentityUser> userManager,
                                AppDbContext db)                // ← nouveau
        {
            this.productRepository = productRepository;
            this.orderRepository = orderRepository;
            this.userManager = userManager;
            this.db = db;
        }

        public ActionResult Index()
        {
            ViewBag.Liste = ListeCart.Instance.Items;
            ViewBag.total = ListeCart.Instance.GetSubTotal();
            return View();
        }

        // ---------------------------------------------------------------
        // Ajout au panier : on vérifie le stock RÉEL (lu en base)
        // ---------------------------------------------------------------
        public ActionResult AddProduct(int id)
        {
            Product pp = productRepository.GetById(id);
            var inCart = ListeCart.Instance.Items.FirstOrDefault(x => x.Prod.ProductId == id)?.quantite ?? 0;

            if (pp == null || pp.QteStock <= inCart)
            {
                TempData["ErrorMessage"] = pp == null
                    ? "Produit introuvable."
                    : $"Stock insuffisant : seulement {pp.QteStock} disponible(s).";
                return RedirectToAction("Index", "Product");
            }

            ListeCart.Instance.AddItem(pp);
            ViewBag.Liste = ListeCart.Instance.Items;
            ViewBag.total = ListeCart.Instance.GetSubTotal();
            //return View();
            return View("Index");
        }

        [HttpPost]
        public ActionResult PlusProduct(int id)
        {
            Product pp = productRepository.GetById(id);
            var inCart = ListeCart.Instance.Items.FirstOrDefault(x => x.Prod.ProductId == id)?.quantite ?? 0;

            // Refus si on dépasse le stock disponible
            if (pp == null || inCart + 1 > pp.QteStock)
                return Json(new { ct = 0, message = $"Stock insuffisant : {pp?.QteStock ?? 0} disponible(s)." });

            ListeCart.Instance.AddItem(pp);

            Item trouve = null;
            foreach (Item a in ListeCart.Instance.Items)
            {
                if (a.Prod.ProductId == pp.ProductId)
                    trouve = a;
            }

            var results = new
            {
                ct = 1,
                Total = ListeCart.Instance.GetSubTotal(),
                Quatite = trouve.quantite,
                TotalRow = trouve.TotalPrice
            };
            return Json(results);
        }

        [HttpPost]
        public ActionResult MinusProduct(int id)
        {
            Product pp = productRepository.GetById(id);
            ListeCart.Instance.SetLessOneItem(pp);

            Item trouve = null;
            foreach (Item a in ListeCart.Instance.Items)
            {
                if (a.Prod.ProductId == pp.ProductId)
                    trouve = a;
            }

            if (trouve != null)
            {
                var results = new
                {
                    Total = ListeCart.Instance.GetSubTotal(),
                    Quatite = trouve.quantite,
                    TotalRow = trouve.TotalPrice,
                    ct = 1
                };
                return Json(results);
            }
            else
            {
                return Json(new { ct = 0 });
            }
        }

        [HttpPost]
        public ActionResult RemoveProduct(int id)
        {
            Product pp = productRepository.GetById(id);
            ListeCart.Instance.RemoveItem(pp);

            var results = new
            {
                Total = ListeCart.Instance.GetSubTotal(),
            };
            return Json(results);
        }

        // ---------------------------------------------------------------
        // Checkout (GET)
        // ---------------------------------------------------------------
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
            }

            var cartItems = ListeCart.Instance.Items.ToList();
            var totalAmount = ListeCart.Instance.GetSubTotal();

            var viewModel = new OrderViewModel
            {
                CartItems = cartItems.Select(item => new CartItemViewModel
                {
                    ProductName = item.Prod.Name,
                    Quantity = item.quantite,
                    Price = item.Prod.FinalPrice
                }).ToList(),
                TotalAmount = totalAmount
            };

            return View(viewModel);
        }

        // ---------------------------------------------------------------
        // Checkout (POST) : vérification du stock AVANT d'aller sur Stripe
        // ---------------------------------------------------------------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(OrderViewModel model)
        {
            var items = ListeCart.Instance.Items.ToList();
            if (!items.Any()) return RedirectToAction("Index");

            // Le stock a pu changer depuis l'ajout au panier : on relit la base
            foreach (var i in items)
            {
                var fresh = productRepository.GetById(i.Prod.ProductId);
                if (fresh == null || fresh.QteStock < i.quantite)
                {
                    TempData["ErrorMessage"] =
                        $"Stock insuffisant pour « {i.Prod.Name} » (disponible : {fresh?.QteStock ?? 0}). Modifiez votre panier.";
                    return RedirectToAction("Index");
                }
            }

            decimal rate = 0.30m;   // 1 TND ≈ 0,30 EUR (Stripe n'accepte pas le TND)

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "Panier", null, Request.Scheme)
                             + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = Url.Action("Index", "Panier", null, Request.Scheme),
                Metadata = new Dictionary<string, string>
                {
                    { "address", model.Address ?? "" },
                    { "user_id", userManager.GetUserId(User) ?? "" }
                },
                LineItems = items.Select(i => new SessionLineItemOptions
                {
                    Quantity = i.quantite,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = (long)Math.Round((decimal)i.Prod.FinalPrice * rate * 100m),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = i.Prod.Name
                        }
                    }
                }).ToList()
            };

            var session = new SessionService().Create(options);
            return Redirect(session.Url);
        }

        // ---------------------------------------------------------------
        // Retour de Stripe : création de la commande + DIMINUTION DU STOCK
        // ---------------------------------------------------------------
        [Authorize]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return RedirectToAction("Index");

            // On demande à Stripe l'état réel du paiement
            var session = await new SessionService().GetAsync(session_id);
            var user = await userManager.GetUserAsync(User);

            session.Metadata.TryGetValue("user_id", out var paidBy);
            if (session.PaymentStatus != "paid" || user == null || paidBy != user.Id)
                return RedirectToAction("Index");

            // Si cette session a déjà créé une commande, on ne la recrée pas
            // (et donc le stock n'est jamais diminué deux fois)
            var existing = orderRepository.GetBySessionId(session.Id);
            if (existing != null)
                return RedirectToAction("Confirmation", new { orderId = existing.Id });

            session.Metadata.TryGetValue("address", out var address);
            var items = ListeCart.Instance.Items.ToList();

            var order = new Order
            {
                CustomerName = user.UserName,
                Email = user.Email,
                Address = address ?? "",
                TotalAmount = ListeCart.Instance.GetSubTotal(),
                OrderDate = DateTime.Now,
                UserId = user.Id,
                StripeSessionId = session.Id,
                Items = items.Select(i => new OrderItem
                {
                    ProductId = i.Prod.ProductId,
                    ProductName = i.Prod.Name,
                    Quantity = i.quantite,
                    Price = i.Prod.FinalPrice
                }).ToList()
            };

            // Commande + diminution du stock dans UNE transaction :
            // soit tout est enregistré, soit rien.
            using (var tx = db.Database.BeginTransaction())
            {
                orderRepository.Add(order);

                foreach (var i in items)
                {
                    int qty = i.quantite;
                    int pid = i.Prod.ProductId;

                    // Décrément atomique côté SQL. Le client a déjà payé :
                    // on ne descend jamais sous 0.
                    db.Products
                      .Where(p => p.ProductId == pid)
                      .ExecuteUpdate(s => s.SetProperty(
                          p => p.QteStock,
                          p => p.QteStock >= qty ? p.QteStock - qty : 0));
                }

                tx.Commit();
            }

            ListeCart.Instance.Items.Clear();

            return RedirectToAction("Confirmation", new { orderId = order.Id });
        }

        // GET: /Panier/Confirmation
        [Authorize]
        public IActionResult Confirmation(int orderId)
        {
            var order = orderRepository.GetById(orderId);
            if (order == null || order.UserId != userManager.GetUserId(User)) return NotFound();
            return View(order);
        }
    }
}
