using GestionArticles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionArticles.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly AppDbContext db;
        private readonly UserManager<IdentityUser> userManager;

        public ReviewController(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int rating, string? comment)
        {
            var userId = userManager.GetUserId(User);

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Note invalide.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // Sécurité : le client doit avoir acheté ce produit
            bool hasBought = await db.OrderItems
                .AnyAsync(i => i.ProductId == productId && i.Order.UserId == userId);
            if (!hasBought)
            {
                TempData["ErrorMessage"] = "Vous devez avoir acheté ce produit pour le noter.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // Un seul avis par produit et par client
            bool already = await db.Reviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == userId);
            if (already)
            {
                TempData["ErrorMessage"] = "Vous avez déjà donné votre avis sur ce produit.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            comment = comment?.Trim();
            if (comment != null && comment.Length > 500) comment = comment.Substring(0, 500);

            db.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = userId!,
                UserName = User.Identity?.Name ?? "",
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Merci pour votre avis !";
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
}