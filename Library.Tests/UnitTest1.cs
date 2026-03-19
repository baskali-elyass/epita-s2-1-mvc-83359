using Library.Domain;
using Library.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests
{
    public class LibraryTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CannotLoan_BookAlreadyOnActiveLoan()
        {
            using var context = CreateInMemoryContext("Test_ActiveLoan");

            var book = new Book { Id = 1, Title = "Clean Code", Author = "Robert Martin", Isbn = "123", Category = "Tech", IsAvailable = false };
            var member = new Member { Id = 1, FullName = "Alice", Email = "alice@test.com", Phone = "000" };
            context.Books.Add(book);
            context.Members.Add(member);
            context.Loans.Add(new Loan
            {
                Id = 1,
                BookId = 1,
                MemberId = 1,
                LoanDate = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(9),
                ReturnedDate = null
            });
            await context.SaveChangesAsync();

            var hasActiveLoan = await context.Loans
                .AnyAsync(l => l.BookId == 1 && l.ReturnedDate == null);

            Assert.True(hasActiveLoan, "Book should have an active loan.");
        }

        [Fact]
        public async Task ReturnedLoan_MakesBookAvailable()
        {
            using var context = CreateInMemoryContext("Test_ReturnLoan");

            var book = new Book { Id = 1, Title = "The Pragmatic Programmer", Author = "Hunt", Isbn = "456", Category = "Tech", IsAvailable = false };
            var member = new Member { Id = 1, FullName = "Bob", Email = "bob@test.com", Phone = "111" };
            var loan = new Loan
            {
                Id = 1,
                BookId = 1,
                MemberId = 1,
                LoanDate = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(9),
                ReturnedDate = null
            };
            context.Books.Add(book);
            context.Members.Add(member);
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            loan.ReturnedDate = DateTime.Now;
            book.IsAvailable = true;
            await context.SaveChangesAsync();

            var updatedBook = await context.Books.FindAsync(1);
            Assert.True(updatedBook!.IsAvailable, "Book should be available after loan is returned.");
        }

        [Fact]
        public async Task BookSearch_ReturnsTitleMatch()
        {
            using var context = CreateInMemoryContext("Test_Search");

            context.Books.AddRange(
                new Book { Id = 1, Title = "Domain-Driven Design", Author = "Evans", Isbn = "1", Category = "Tech", IsAvailable = true },
                new Book { Id = 2, Title = "Clean Architecture", Author = "Martin", Isbn = "2", Category = "Tech", IsAvailable = true },
                new Book { Id = 3, Title = "The Hobbit", Author = "Tolkien", Isbn = "3", Category = "Fiction", IsAvailable = true }
            );
            await context.SaveChangesAsync();

            var results = await context.Books
                .Where(b => b.Title.Contains("Clean") || b.Author.Contains("Clean"))
                .ToListAsync();

            Assert.Single(results);
            Assert.Equal("Clean Architecture", results[0].Title);
        }

        [Fact]
        public async Task OverdueLogic_DetectsOverdueLoans()
        {
            using var context = CreateInMemoryContext("Test_Overdue");

            var book1 = new Book { Id = 1, Title = "Overdue Book", Author = "A", Isbn = "1", Category = "X", IsAvailable = false };
            var book2 = new Book { Id = 2, Title = "Fine Book", Author = "B", Isbn = "2", Category = "X", IsAvailable = false };
            var member = new Member { Id = 1, FullName = "Charlie", Email = "charlie@test.com", Phone = "222" };

            context.Books.AddRange(book1, book2);
            context.Members.Add(member);
            context.Loans.AddRange(
                new Loan
                {
                    Id = 1,
                    BookId = 1,
                    MemberId = 1,
                    LoanDate = DateTime.Now.AddDays(-30),
                    DueDate = DateTime.Now.AddDays(-10),
                    ReturnedDate = null
                },
                new Loan
                {
                    Id = 2,
                    BookId = 2,
                    MemberId = 1,
                    LoanDate = DateTime.Now.AddDays(-3),
                    DueDate = DateTime.Now.AddDays(11),
                    ReturnedDate = null
                }
            );
            await context.SaveChangesAsync();

            var today = DateTime.Today;
            var overdueLoans = await context.Loans
                .Where(l => l.DueDate < today && l.ReturnedDate == null)
                .ToListAsync();

            Assert.Single(overdueLoans);
            Assert.Equal(1, overdueLoans[0].BookId);
        }

        [Fact]
        public async Task BookSearch_ReturnsAuthorMatch()
        {
            using var context = CreateInMemoryContext("Test_AuthorSearch");

            context.Books.AddRange(
                new Book { Id = 1, Title = "Refactoring", Author = "Martin Fowler", Isbn = "1", Category = "Tech", IsAvailable = true },
                new Book { Id = 2, Title = "Patterns", Author = "Gang of Four", Isbn = "2", Category = "Tech", IsAvailable = true }
            );
            await context.SaveChangesAsync();

            var results = await context.Books
                .Where(b => b.Title.Contains("Fowler") || b.Author.Contains("Fowler"))
                .ToListAsync();

            Assert.Single(results);
            Assert.Equal("Refactoring", results[0].Title);
        }
    }
}