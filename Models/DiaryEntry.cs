using System;
using System.ComponentModel.DataAnnotations;

namespace Diary.Models
{
    public class DiaryEntry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Název je povinný.")]
        [StringLength(100, ErrorMessage = "Název nesmí být delší než 100 znaků.")]
        public string Title { get; set; }

        // Vlastnost 'Content' byla odstraněna

        [Required(ErrorMessage = "Musíte vybrat typ záznamu.")]
        public string ResourceType { get; set; }

        [Required(ErrorMessage = "Musíte vybrat žánr.")]
        public string Genre { get; set; }

        [Required(ErrorMessage = "Rok vydání je povinný.")]
        [Range(0, 2050, ErrorMessage = "Rok musí být mezi 0 - 2050.")]
        public int Year { get; set; }

        [Display(Name = "Datum vytvoření")]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Přečteno")]
        [Required(ErrorMessage = "Musíte zvolit, zda je záznam přečtený.")]
        public bool IsRead { get; set; }

        public string? Username { get; set; }

    }
}