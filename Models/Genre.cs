using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Diary.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();
    }
}
