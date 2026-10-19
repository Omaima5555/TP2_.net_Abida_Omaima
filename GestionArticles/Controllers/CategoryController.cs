using GestionArticles.Models;
using GestionArticles.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionArticles.Controllers
{
    
    [Authorize(Roles = "Admin,Manager")]
    public class CategoryController : Controller
    {
        readonly ICategoryRepository CategRepository;

        public CategoryController(ICategoryRepository categRepository)
        {
            {
                CategRepository = categRepository;
            }
        }

        // GET: CategoryController
        [AllowAnonymous]
        public ActionResult Index()
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            return View(categories);
        }

        // GET: CategoryController/Details/5
        public ActionResult Details(int id)
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            var category = CategRepository.GetById(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // GET: CategoryController/Create
        public ActionResult Create()
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            return View();
        }

        // POST: CategoryController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category c)
        {
            if (ModelState.IsValid)
            {
                CategRepository.Add(c);

                return RedirectToAction(nameof(Index));
            }

            return View(c);
        }

        // GET: CategoryController/Edit/5
        public ActionResult Edit(int id)
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            var category = CategRepository.GetById(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: CategoryController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Category c)
        {
            if (ModelState.IsValid)
            {
                var category = CategRepository.Update(c);

                if (category != null)
                    return RedirectToAction(nameof(Index));

                return NotFound();
            }

            return View(c);
        }

        // GET: CategoryController/Delete/5
        public ActionResult Delete(int id)
        {
            var categories = CategRepository.GetAll();
            ViewData["Categories"] = categories;

            var category = CategRepository.GetById(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: CategoryController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            CategRepository.Delete(id);

            return RedirectToAction(nameof(Index));
        }
    }
}