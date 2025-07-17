using Microsoft.AspNetCore.Identity;

namespace Diary.Models
{
    public class FavoriteEntry
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int DiaryEntryId { get; set; }
        public DiaryEntry DiaryEntry { get; set; }

        public DateTime FavoritedAt { get; set; }
    }
}
