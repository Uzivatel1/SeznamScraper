using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeznamScraper.Data;
using SeznamScraper.Models;
using System.Net;
using System.Text.RegularExpressions;

// API controller, kter� obsluhuje ukl�d�n� koment��� ze Seznam profilov�ch str�nek
[ApiController]
[Route("api/[controller]")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // T��da pro p��jem HTML payloadu (p��mo cel� HTML dokument)
    public class HtmlPayload
    {
        public required string Html { get; set; }
    }

    // Endpoint pro p��m� zpracov�n� HTML str�nky
    [HttpPost]
    public async Task<IActionResult> SaveHtml([FromBody] HtmlPayload payload)
    {
        var html = payload.Html;

        // Najdeme cel� jm�no u�ivatele v tagu <h1 class="font-bold text-xl">
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

        // Rozd�len� jm�na
        var fullName = nameMatch.Groups[1].Value.Trim();
        var nameParts = fullName.Split(' ');
        if (nameParts.Length < 2)
            return BadRequest("Invalid user name format.");

        // Najdeme u�ivatele v DB, p��padn� vytvo��me
        var user = await _context.Users
                                 .FirstOrDefaultAsync(u => u.FirstName == nameParts[0] && u.LastName == nameParts[1]);

        if (user == null)
        {
            user = new User { FirstName = nameParts[0], LastName = nameParts[1] };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Najdeme bloky koment���
        var commentBlocks = Regex.Matches(
            html,
            @"<div[^>]*class=""[^""]*szn-diskuze-comment-content[^""]*"".*?>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        var comments = new List<string>();

        foreach (Match block in commentBlocks)
        {
            string blockHtml = block.Groups[1].Value;

            // Najdeme jednotliv� odstavce koment��e
            var paragraphMatches = Regex.Matches(
                blockHtml,
                @"<p[^>]*class=""atm-paragraph""[^>]*>(.*?)</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            // Spoj�me jednotliv� odstavce do jednoho koment��e
            var rawText = string.Join(" ", paragraphMatches
                .Select(p => p.Groups[1].Value.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            // Odstran�me HTML tagy
            var textWithoutTags = Regex.Replace(rawText, "<.*?>", "");

            // Dek�dujeme HTML entity a normalizujeme mezery
            var cleanedText = Regex.Replace(WebUtility.HtmlDecode(textWithoutTags).Trim(), @"\s+", " ");

            if (!string.IsNullOrWhiteSpace(cleanedText))
                comments.Add(cleanedText);
        }

        if (comments.Count == 0)
            return BadRequest("No comments found.");

        // Na�teme existuj�c� koment��e a normalizujeme je
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

        // Ulo��me nov� koment��e
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

    // T��da pro vstup URL adresy (ru�n� vlo�en� odkazu z formul��e)
    public class UrlPayload
    {
        public required string Url { get; set; }
    }

    // Endpoint, kter� st�hne HTML ze zadan� URL a p�epo�le do SaveHtml
    [HttpPost("from-input")]
    public async Task<IActionResult> SaveFromUrl([FromBody] UrlPayload payload)
    {
        string url = payload.Url;

        // Validace URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !url.StartsWith("http"))
            return BadRequest("Neplatn� URL adresa.");

        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync(uri);

            // Znovupou�it� metody SaveHtml pro parsov�n� a ulo�en�
            return await SaveHtml(new HtmlPayload { Html = html });
        }
        catch (Exception ex)
        {
            return BadRequest("Chyba p�i stahov�n� str�nky: " + ex.Message);
        }
    }

    // Z�kladn� str�nka projektu
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View("Index");
    }
}