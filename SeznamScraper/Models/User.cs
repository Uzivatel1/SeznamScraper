namespace SeznamScraper.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
