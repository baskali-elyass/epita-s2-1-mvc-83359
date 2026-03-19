namespace Library.Domain
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }

    public class Member
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }

    public class Loan
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int MemberId { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedDate { get; set; }

        public Book Book { get; set; } = null!;
        public Member Member { get; set; } = null!;
    }
}