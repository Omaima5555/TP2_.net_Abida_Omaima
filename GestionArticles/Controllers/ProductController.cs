using GestionArticles.Models;
using GestionArticles.Models.Repositories;
using GestionArticles.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GestionArticles.Controllers
{
   
    //[Authorize(Roles = "Admin,Manager")]

    public class ProductController : Controller
    {
        readonly IProductRepository ProductRepository;
        readonly ICategoryRepository CategRepository;
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly AppDbContext db;



        public ProductController(
            IProductRepository ProdRepository,
            ICategoryRepository categRepository,
            IWebHostEnvironment hostingEnvironment,
            AppDbContext db)
        {
            ProductRepository = ProdRepository;
            CategRepository = categRepository;
            this.hostingEnvironment = hostingEnvironment;
            this.db = db;
        }

        // GET: ProductController

      
        [AllowAnonymous]
        public IActionResult Index(int? categoryId, int page = 1)
        {
            int pageSize = 4; // Nombre de produits par page

            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            IQueryable<Product> productsQuery = ProductRepository.GetAllProducts();
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            // Pagination
            var totalProducts = productsQuery.Count();
            var products = productsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.CategoryId = categoryId;

            return View(products);
        }

        [AllowAnonymous]

        // GET: ProductController/Details/5
        public ActionResult Details(int id)
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            var product = ProductRepository.GetById(id);
            if (product == null) return NotFound();

            // --- Avis du produit + note moyenne ---
            var reviews = db.Reviews
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.Reviews = reviews;
            ViewBag.ReviewCount = reviews.Count;
            ViewBag.AvgRating = reviews.Count > 0
                ? Math.Round(reviews.Average(r => r.Rating), 1)
                : 0.0;

            // --- Le client peut-il noter ce produit ? ---
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool already = false, canReview = false;
            if (userId != null)
            {
                already = db.Reviews.Any(r => r.ProductId == id && r.UserId == userId);
                canReview = !already &&
                            db.OrderItems.Any(i => i.ProductId == id && i.Order.UserId == userId);
            }
            ViewBag.CanReview = canReview;
            ViewBag.AlreadyReviewed = already;

            return View(product);
        }

        [Authorize(Roles = "Admin,Manager")]

        // GET: ProductController/Create
        public ActionResult Create()
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            ViewBag.CategoryId = new SelectList(
                CategRepository.GetAll(),
                "CategoryId",
                "CategoryName"
            );

            return View();
        }

        [Authorize(Roles = "Admin,Manager")]

        // POST: ProductController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;

                // Vérifier si une image a été sélectionnée
                if (model.ImagePath != null)
                {
                    // Le fichier sera enregistré dans wwwroot/images
                    string uploadsFolder = Path.Combine(
                        hostingEnvironment.WebRootPath,
                        "images"
                    );

                    // Générer un nom unique pour l'image
                    uniqueFileName = Guid.NewGuid().ToString()
                        + "_"
                        + model.ImagePath.FileName;

                    // Chemin complet du fichier
                    string filePath = Path.Combine(
                        uploadsFolder,
                        uniqueFileName
                    );

                    // Copier l'image dans wwwroot/images
                    model.ImagePath.CopyTo(
                        new FileStream(filePath, FileMode.Create)
                    );
                }

                // Créer un nouveau produit
                Product newProduct = new Product
                {
                    Name = model.Name,
                    Price = model.Price,
                    QteStock = model.QteStock,
                    CategoryId = model.CategoryId,
                   

                    // Enregistrer le nom de l'image dans la base de données
                    Image = uniqueFileName,
                    PromoPercent = model.PromoPercent
                };

                // Ajouter le produit dans la base de données
                ProductRepository.Add(newProduct);

                return RedirectToAction(
                    "Details",
                    new { id = newProduct.ProductId }
                );
            }

            // Recharger la liste des catégories en cas d'erreur
            ViewBag.CategoryId = new SelectList(
                CategRepository.GetAll(),
                "CategoryId",
                "CategoryName"
            );

            return View(model);
        }

        [Authorize(Roles = "Admin,Manager")]

        // GET: ProductController/Edit/5
        public ActionResult Edit(int id)
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            ViewBag.CategoryId = new SelectList(CategRepository.GetAll(),
            "CategoryId"
            ,
            "CategoryName");
            Product product = ProductRepository.GetById(id);
            EditViewModel productEditViewModel = new EditViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                QteStock = product.QteStock,
                CategoryId = product.CategoryId,
                ExistingImagePath = product.Image,
                PromoPercent = product.PromoPercent
            };
            return View(productEditViewModel);
        }

        [Authorize(Roles = "Admin,Manager")]

        // POST: ProductController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditViewModel model)
        {
            ViewBag.CategoryId = new SelectList(CategRepository.GetAll(), "CategoryId", "CategoryName");
            // Check if the provided data is valid, if not rerender the edit view
            // so the user can correct and resubmit the edit form
            if (ModelState.IsValid)
            {
                // Retrieve the product being edited from the database
                Product product = ProductRepository.GetById(model.ProductId);
                // Update the product object with the data in the model object
                product.Name = model.Name;
                product.Price = model.Price;
                product.QteStock = model.QteStock;
                product.CategoryId = model.CategoryId;
                product.PromoPercent = model.PromoPercent;
                // If the user wants to change the photo, a new photo will be
                // uploaded and the Photo property on the model object receives
                // the uploaded photo. If the Photo property is null, user did
                // not upload a new photo and keeps his existing photo
                if (model.ImagePath != null)
                {
                    // If a new photo is uploaded, the existing photo must be
                    // deleted. So check if there is an existing photo and delete
                    if (model.ExistingImagePath != null)
                    {
                        string filePath = Path.Combine(hostingEnvironment.WebRootPath, "images", model.ExistingImagePath);
                        System.IO.File.Delete(filePath);
                    }
                    // Save the new photo in wwwroot/images folder and update
                    // PhotoPath property of the product object which will be
                    // eventually saved in the database
                    product.Image = ProcessUploadedFile(model);
                }
                // Call update method on the repository service passing it the
                // product object to update the data in the database table
                Product updatedProduct = ProductRepository.Update(product);
                if (updatedProduct != null)
                    return RedirectToAction("Index");
                else
                    return NotFound();
            }
            return View(model);
        }
        [NonAction]
        private string ProcessUploadedFile(EditViewModel model)
        {
            string uniqueFileName = null;
            if (model.ImagePath != null)
            {
                string uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImagePath.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImagePath.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        [Authorize(Roles = "Admin,Manager")]

        // GET: ProductController/Delete/5
        public ActionResult Delete(int id)
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            var product = ProductRepository.GetById(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [Authorize(Roles = "Admin,Manager")]

        // POST: ProductController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            ProductRepository.Delete(id);

            return RedirectToAction(nameof(Index));
        }

      

        //search

        [AllowAnonymous]
        public ActionResult Search(string val)
        {
            ViewData["Categories"] = CategRepository.GetAll();

            var result = ProductRepository.FindByName(val);

            ViewBag.TotalPages = 1;
            ViewBag.CurrentPage = 1;
            ViewBag.CategoryId = null;

            return View("Index", result);
        }
    }
}