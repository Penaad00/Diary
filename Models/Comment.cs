namespace Diary.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string UserId { get; set; }           
        public int DiaryEntryId { get; set; }        
        public string Content { get; set; }         
        public DateTime CreatedAt { get; set; }

        public DiaryEntry DiaryEntry { get; set; }
    }
}
