using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionArticles.Models
{
    public class Product
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
        public string? Image { get; set; }

        [Range(0, 90, ErrorMessage = "La promotion doit être entre 0 et 90 %")]
        [Display(Name = "Promotion (%)")]
        public int PromoPercent { get; set; }      // 0 = pas de promotion

        [NotMapped]
        public float FinalPrice =>
            PromoPercent > 0 ? (float)Math.Round(Price * (100 - PromoPercent) / 100.0, 2) : Price;

    }
}
