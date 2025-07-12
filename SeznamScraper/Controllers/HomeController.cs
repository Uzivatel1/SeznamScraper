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

        // Pokus�me se naj�t jm�no u�ivatele v <h1> tagu (nap�. <h1 class="font-bold text-xl">Jm�no P��jmen�</h1>)
        var nameMatch = Regex.Match(
            html,
            @"<h1[^>]*class=""[^""]*\bfont-bold\b[^""]*\btext-xl\b[^""]*""[^>]*>(.*?)</h1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // Pokud nenalezeno, ulo��me snippet HTML pro lad�n� a vrac�me chybu
        if (!nameMatch.Success)
        {
            var snippet = html.Substring(0, Math.Min(1000, html.Length));
            System.IO.File.WriteAllText("debug-snippet.html", snippet);
            return BadRequest("User name not found in the provided HTML.");
        }

        // Rozd�len� jm�na a p��jmen�
        var fullName = nameMatch.Groups[1].Value.Trim();
        var nameParts = fullName.Split(' ');
        if (nameParts.Length < 2)
            return BadRequest("Invalid user name format.");

        // Vyhled�n� u�ivatele v datab�zi
        var user = await _context.Users
                                 .FirstOrDefaultAsync(u => u.FirstName == nameParts[0] && u.LastName == nameParts[1]);

        // Pokud neexistuje, p�id�me nov�ho
        if (user == null)
        {
            user = new User { FirstName = nameParts[0], LastName = nameParts[1] };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Najdeme v�echny bloky koment���
        var commentBlocks = Regex.Matches(
            payload.Html,
            @"<div[^>]*class=""[^""]*szn-diskuze-comment-content[^""]*"".*?>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        var comments = new List<string>();

        // Pro ka�d� blok koment��� spoj�me v�echny <p> do jednoho celku
        foreach (Match block in commentBlocks)
        {
            string blockHtml = block.Groups[1].Value;

            var paragraphMatches = Regex.Matches(
                blockHtml,
                @"<p[^>]*class=""atm-paragraph""[^>]*>(.*?)</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            // Spojen� v�ech <p> do jednoho koment��e
            var commentText = string.Join(" ", paragraphMatches
                .Select(p => WebUtility.HtmlDecode(p.Groups[1].Value.Trim()))
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            if (!string.IsNullOrWhiteSpace(commentText))
                comments.Add(commentText);
        }

        // Pokud nejsou ��dn� koment��e, vrac�me chybu
        if (comments.Count == 0)
            return BadRequest("No comments found.");

        // Na�teme st�vaj�c� koment��e u�ivatele z datab�ze
        var existingComments = await _context.Comments
            .Where(c => c.UserId == user.Id)
            .Select(c => c.Text)
            .ToListAsync();

        // Odfiltrujeme duplicitn� koment��e (ji� ulo�en� nebo v r�mci payloadu)
        var newComments = comments
            .Where(c => !existingComments.Contains(c))
            .Distinct();

        // Ulo��me nov� koment��e do datab�ze
        foreach (var comment in newComments)
        {
            _context.Comments.Add(new Comment
            {
                Text = comment,
                UserId = user.Id
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { count = comments.Count }); // Vr�t�me po�et v�ech koment��� (v�etn� duplicit)
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