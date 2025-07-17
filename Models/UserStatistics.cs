using Microsoft.AspNetCore.Identity;

namespace Diary.Models
{
    public class UserStatistics
    {
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int EntriesCount { get; set; }
        public int ReadBooksCount { get; set; }
        public int SeenFilmsCount { get; set; }
    }
}
