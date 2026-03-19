using Library.Domain;
using Library.MVC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;




namespace Library.MVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? category, string? availability)
        {
            IQueryable<Book> query = _context.Books;

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(b => b.Category == category);

            if (availability == "available")
                query = query.Where(b => b.IsAvailable);
            else if (availability == "onloan")
                query = query.Where(b => !b.IsAvailable);

            var books = await query.OrderBy(b => b.Title).ToListAsync();

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Availability = availability;
            ViewBag.Categories = await _context.Books
                .Select(b => b.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return View(books);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (!ModelState.IsValid) return View(book);
            book.IsAvailable = true;
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id) return NotFound();
            if (!ModelState.IsValid) return View(book);
            _context.Update(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}



namespace Library.MVC.Controllers
{
    public class MembersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index() =>
            View(await _context.Members.OrderBy(m => m.FullName).ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            if (!ModelState.IsValid) return View(member);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Member member)
        {
            if (id != member.Id) return NotFound();
            if (!ModelState.IsValid) return View(member);
            _context.Update(member);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}



namespace Library.MVC.Controllers
{
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();
            return View(loans);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Members = new SelectList(
                await _context.Members.OrderBy(m => m.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.Books = new SelectList(
                await _context.Books.Where(b => b.IsAvailable).OrderBy(b => b.Title).ToListAsync(), "Id", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bookId, int memberId)
        {
            var hasActiveLoan = await _context.Loans
                .AnyAsync(l => l.BookId == bookId && l.ReturnedDate == null);

            if (hasActiveLoan)
            {
                ModelState.AddModelError("", "This book is already on an active loan.");
                ViewBag.Members = new SelectList(
                    await _context.Members.OrderBy(m => m.FullName).ToListAsync(), "Id", "FullName");
                ViewBag.Books = new SelectList(
                    await _context.Books.Where(b => b.IsAvailable).OrderBy(b => b.Title).ToListAsync(), "Id", "Title");
                return View();
            }

            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound();

            var loan = new Loan
            {
                BookId = bookId,
                MemberId = memberId,
                LoanDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14)
            };

            book.IsAvailable = false;
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null) return NotFound();

            loan.ReturnedDate = DateTime.Now;
            loan.Book.IsAvailable = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}



namespace Library.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName) && !await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null)
                await _roleManager.DeleteAsync(role);
            return RedirectToAction(nameof(Roles));
        }
    }
}