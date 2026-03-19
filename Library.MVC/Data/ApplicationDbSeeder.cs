using Bogus;
using Library.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Library.MVC.Data
{
    public static class ApplicationDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (await userManager.FindByEmailAsync("admin@library.com") == null)
            {
                var admin = new IdentityUser
                {
                    UserName = "admin@library.com",
                    Email = "admin@library.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (context.Books.Any()) return;

            var categories = new[] { "Fiction", "Science", "History", "Biography", "Technology", "Art", "Philosophy" };

            var bookFaker = new Faker<Book>()
                .RuleFor(b => b.Title, f => f.Lorem.Sentence(3).TrimEnd('.'))
                .RuleFor(b => b.Author, f => f.Name.FullName())
                .RuleFor(b => b.Isbn, f => f.Commerce.Ean13())
                .RuleFor(b => b.Category, f => f.PickRandom(categories))
                .RuleFor(b => b.IsAvailable, _ => true);

            var books = bookFaker.Generate(20);
            context.Books.AddRange(books);

            var memberFaker = new Faker<Member>()
                .RuleFor(m => m.FullName, f => f.Name.FullName())
                .RuleFor(m => m.Email, (f, m) => f.Internet.Email(m.FullName))
                .RuleFor(m => m.Phone, f => f.Phone.PhoneNumber());

            var members = memberFaker.Generate(10);
            context.Members.AddRange(members);

            await context.SaveChangesAsync();

            var savedBooks = context.Books.ToList();
            var savedMembers = context.Members.ToList();
            var random = new Random(42);
            var now = DateTime.Now;
            var loans = new List<Loan>();

            for (int i = 0; i < 5; i++)
            {
                var book = savedBooks[i];
                var loanDate = now.AddDays(-random.Next(30, 60));
                loans.Add(new Loan
                {
                    BookId = book.Id,
                    MemberId = savedMembers[random.Next(savedMembers.Count)].Id,
                    LoanDate = loanDate,
                    DueDate = loanDate.AddDays(14),
                    ReturnedDate = loanDate.AddDays(random.Next(1, 13))
                });
            }

            for (int i = 5; i < 10; i++)
            {
                var book = savedBooks[i];
                var loanDate = now.AddDays(-random.Next(1, 7));
                loans.Add(new Loan
                {
                    BookId = book.Id,
                    MemberId = savedMembers[random.Next(savedMembers.Count)].Id,
                    LoanDate = loanDate,
                    DueDate = loanDate.AddDays(14),
                    ReturnedDate = null
                });
                book.IsAvailable = false;
            }

            for (int i = 10; i < 15; i++)
            {
                var book = savedBooks[i];
                var loanDate = now.AddDays(-random.Next(20, 40));
                loans.Add(new Loan
                {
                    BookId = book.Id,
                    MemberId = savedMembers[random.Next(savedMembers.Count)].Id,
                    LoanDate = loanDate,
                    DueDate = loanDate.AddDays(14),
                    ReturnedDate = null
                });
                book.IsAvailable = false;
            }

            context.Loans.AddRange(loans);
            await context.SaveChangesAsync();
        }
    }
}