using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeznamScraper.Data;
using SeznamScraper.Models;
using System.Net;
using System.Text.RegularExpressions;

// API controller, který obsluhuje ukládání komentáøù ze Seznam profilových stránek
[ApiController]
[Route("api/[controller]")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Tøída pro pøíjem HTML payloadu (pøímo celý HTML dokument)
    public class HtmlPayload
    {
        public required string Html { get; set; }
    }

    // Endpoint pro pøímé zpracování HTML stránky
    [HttpPost]
    public async Task<IActionResult> SaveHtml([FromBody] HtmlPayload payload)
    {
        var html = payload.Html;

        // Pokusíme se najít jméno uživatele v <h1> tagu (napø. <h1 class="font-bold text-xl">Jméno Pøíjmení</h1>)
        var nameMatch = Regex.Match(
            html,
            @"<h1[^>]*class=""[^""]*\bfont-bold\b[^""]*\btext-xl\b[^""]*""[^>]*>(.*?)</h1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // Pokud nenalezeno, uložíme snippet HTML pro ladìní a vracíme chybu
        if (!nameMatch.Success)
        {
            var snippet = html.Substring(0, Math.Min(1000, html.Length));
            System.IO.File.WriteAllText("debug-snippet.html", snippet);
            return BadRequest("User name not found in the provided HTML.");
        }

        // Rozdìlení jména a pøíjmení
        var fullName = nameMatch.Groups[1].Value.Trim();
        var nameParts = fullName.Split(' ');
        if (nameParts.Length < 2)
            return BadRequest("Invalid user name format.");

        // Vyhledání uživatele v databázi
        var user = await _context.Users
                                 .FirstOrDefaultAsync(u => u.FirstName == nameParts[0] && u.LastName == nameParts[1]);

        // Pokud neexistuje, pøidáme nového
        if (user == null)
        {
            user = new User { FirstName = nameParts[0], LastName = nameParts[1] };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Najdeme všechny bloky komentáøù
        var commentBlocks = Regex.Matches(
            payload.Html,
            @"<div[^>]*class=""[^""]*szn-diskuze-comment-content[^""]*"".*?>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        var comments = new List<string>();

        // Pro každý blok komentáøù spojíme všechny <p> do jednoho celku
        foreach (Match block in commentBlocks)
        {
            string blockHtml = block.Groups[1].Value;

            var paragraphMatches = Regex.Matches(
                blockHtml,
                @"<p[^>]*class=""atm-paragraph""[^>]*>(.*?)</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            // Spojení všech <p> do jednoho komentáøe
            var commentText = string.Join(" ", paragraphMatches
                .Select(p => WebUtility.HtmlDecode(p.Groups[1].Value.Trim()))
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            if (!string.IsNullOrWhiteSpace(commentText))
                comments.Add(commentText);
        }

        // Pokud nejsou žádné komentáøe, vracíme chybu
        if (comments.Count == 0)
            return BadRequest("No comments found.");

        // Naèteme stávající komentáøe uživatele z databáze
        var existingComments = await _context.Comments
            .Where(c => c.UserId == user.Id)
            .Select(c => c.Text)
            .ToListAsync();

        // Odfiltrujeme duplicitní komentáøe (již uložené nebo v rámci payloadu)
        var newComments = comments
            .Where(c => !existingComments.Contains(c))
            .Distinct();

        // Uložíme nové komentáøe do databáze
        foreach (var comment in newComments)
        {
            _context.Comments.Add(new Comment
            {
                Text = comment,
                UserId = user.Id
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { count = comments.Count }); // Vrátíme poèet všech komentáøù (vèetnì duplicit)
    }

    // Tøída pro vstup URL adresy (ruèní vložení odkazu z formuláøe)
    public class UrlPayload
    {
        public required string Url { get; set; }
    }

    // Endpoint, který stáhne HTML ze zadané URL a pøepošle do SaveHtml
    [HttpPost("from-input")]
    public async Task<IActionResult> SaveFromUrl([FromBody] UrlPayload payload)
    {
        string url = payload.Url;

        // Validace URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !url.StartsWith("http"))
            return BadRequest("Neplatná URL adresa.");

        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync(uri);

            // Znovupoužití metody SaveHtml pro parsování a uložení
            return await SaveHtml(new HtmlPayload { Html = html });
        }
        catch (Exception ex)
        {
            return BadRequest("Chyba pøi stahování stránky: " + ex.Message);
        }
    }

    // Základní stránka projektu
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View("Index");
    }
}