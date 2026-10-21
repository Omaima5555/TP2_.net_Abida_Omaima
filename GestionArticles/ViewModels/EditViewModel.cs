using System.ComponentModel.DataAnnotations;

namespace GestionArticles.ViewModels
{
    public class EditViewModel : CreateViewModel
    {
        public int ProductId { get; set; }
        public string? ExistingImagePath { get; set; }

        [Range(0, 90, ErrorMessage = "La promotion doit être entre 0 et 90 %")]
        [Display(Name = "Promotion (%)")]
        public int PromoPercent { get; set; }
    }
}
