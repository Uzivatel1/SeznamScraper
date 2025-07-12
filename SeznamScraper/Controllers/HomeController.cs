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

        // Najdeme celé jméno uživatele v tagu <h1 class="font-bold text-xl">
        var nameMatch = Regex.Match(
            html,
            @"<h1[^>]*class=""[^""]*\bfont-bold\b[^""]*\btext-xl\b[^""]*""[^>]*>(.*?)</h1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        if (!nameMatch.Success)
        {
            var snippet = html.Substring(0, Math.Min(1000, html.Length));
            System.IO.File.WriteAllText("debug-snippet.html", snippet);
            return BadRequest("User name not found in the provided HTML.");
        }

        // Rozdìlení jména
        var fullName = nameMatch.Groups[1].Value.Trim();
        var nameParts = fullName.Split(' ');
        if (nameParts.Length < 2)
            return BadRequest("Invalid user name format.");

        // Najdeme uživatele v DB, pøípadnì vytvoøíme
        var user = await _context.Users
                                 .FirstOrDefaultAsync(u => u.FirstName == nameParts[0] && u.LastName == nameParts[1]);

        if (user == null)
        {
            user = new User { FirstName = nameParts[0], LastName = nameParts[1] };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Najdeme bloky komentáøù
        var commentBlocks = Regex.Matches(
            html,
            @"<div[^>]*class=""[^""]*szn-diskuze-comment-content[^""]*"".*?>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        var comments = new List<string>();

        foreach (Match block in commentBlocks)
        {
            string blockHtml = block.Groups[1].Value;

            // Najdeme jednotlivé odstavce komentáøe
            var paragraphMatches = Regex.Matches(
                blockHtml,
                @"<p[^>]*class=""atm-paragraph""[^>]*>(.*?)</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            // Spojíme jednotlivé odstavce do jednoho komentáøe
            var rawText = string.Join(" ", paragraphMatches
                .Select(p => p.Groups[1].Value.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            // Odstraníme HTML tagy
            var textWithoutTags = Regex.Replace(rawText, "<.*?>", "");

            // Dekódujeme HTML entity a normalizujeme mezery
            var cleanedText = Regex.Replace(WebUtility.HtmlDecode(textWithoutTags).Trim(), @"\s+", " ");

            if (!string.IsNullOrWhiteSpace(cleanedText))
                comments.Add(cleanedText);
        }

        if (comments.Count == 0)
            return BadRequest("No comments found.");

        // Naèteme existující komentáøe a normalizujeme je
        var existingComments = await _context.Comments
            .Where(c => c.UserId == user.Id)
            .Select(c => c.Text)
            .ToListAsync();

        string Normalize(string text) =>
            Regex.Replace(WebUtility.HtmlDecode(text).Trim(), @"\s+", " ");

        var normalizedExisting = existingComments
            .Select(Normalize)
            .ToHashSet();

        var newComments = comments
            .Select(Normalize)
            .Where(c => !normalizedExisting.Contains(c))
            .Distinct();

        // Uložíme nové komentáøe
        foreach (var comment in newComments)
        {
            _context.Comments.Add(new Comment
            {
                Text = comment,
                UserId = user.Id
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { count = comments.Count });
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