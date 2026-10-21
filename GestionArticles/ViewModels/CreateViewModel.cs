using GestionArticles.Models;
using System.ComponentModel.DataAnnotations;

namespace GestionArticles.ViewModels
{
    public class CreateViewModel
    {
        public int ProductId { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 5)]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Prix en dinar :")]
        public float Price { get; set; }
        [Required]
        [Display(Name = "Quantité en unité :")]
        public int QteStock { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Required]
        [Display(Name = "Image :")]
        public IFormFile? ImagePath { get; set; }

        [Range(0, 90, ErrorMessage = "La promotion doit être entre 0 et 90 %")]
        [Display(Name = "Promotion (%)")]
        public int PromoPercent { get; set; }

    }
}
