using GestionArticles.Models;
using GestionArticles.Models.Help;
using GestionArticles.Models.Repositories;
using GestionArticles.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionArticles.Controllers
{
    public class PanierController : Controller
    {

        readonly IProductRepository productRepository;
        readonly IOrderRepository orderRepository;
        private readonly UserManager<IdentityUser> userManager;

        public PanierController(IProductRepository productRepository,
                                IOrderRepository orderRepository,
                                UserManager<IdentityUser> userManager)
        {
            this.productRepository = productRepository;
            this.orderRepository = orderRepository;
            this.userManager = userManager;
        }

        public ActionResult Index()
        {
            ViewBag.Liste = ListeCart.Instance.Items;
            ViewBag.total = ListeCart.Instance.GetSubTotal();
            return View();
        }

        public ActionResult AddProduct(int id)
        {
            Product pp = productRepository.GetById(id);
            ListeCart.Instance.AddItem(pp);
            ViewBag.Liste = ListeCart.Instance.Items;
            ViewBag.total = ListeCart.Instance.GetSubTotal();
            return View();
        }

        [HttpPost]
        public ActionResult PlusProduct(int id)
        {
            Product pp = productRepository.GetById(id);
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
                var results = new
                {
                    ct = 0
                };
                return Json(results);
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

        [Authorize]
        // GET: /Order/Checkout
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
                    Price = item.Prod.Price
                }).ToList(),
                TotalAmount = totalAmount
            };

            return View(viewModel);
        }

        // POST : /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(OrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = userManager.GetUserAsync(User).Result;
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Utilisateur non authentifié.";
                    return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
                }

                // Recharger les articles du panier depuis la source (ListeCart)
                var cartItems = ListeCart.Instance.Items.ToList();
                model.CartItems = cartItems.Select(item => new CartItemViewModel
                {
                    ProductName = item.Prod.Name,
                    Quantity = item.quantite,
                    Price = item.Prod.Price
                }).ToList();
                model.TotalAmount = ListeCart.Instance.GetSubTotal();

                var order = new Order
                {
                    CustomerName = user.UserName,
                    Email = user.Email,
                    Address = model.Address,
                    TotalAmount = model.TotalAmount,
                    OrderDate = DateTime.Now,
                    UserId = user.Id,
                    Items = model.CartItems.Select(item => new OrderItem
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price
                    }).ToList()
                };

                orderRepository.Add(order);
                ListeCart.Instance.Items.Clear();

                TempData["SuccessMessage"] = "Votre commande a été passée avec succès.";
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }

            TempData["ErrorMessage"] = "Une erreur est survenue. Veuillez vérifier les informations.";
            return View(model);
        }

        // GET: /Order/Confirmation
        public IActionResult Confirmation(int orderId)
        {
            var order = orderRepository.GetById(orderId);
            return View(order);
        }
    }


}