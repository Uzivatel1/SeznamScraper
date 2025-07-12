using SeznamScraper.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb

// Pøidání MVC (controller + views) do kontejneru služeb
builder.Services.AddControllersWithViews();

// Nastavení Entity Frameworku a pøipojení k SQL Serveru (øetìzec je v appsettings.json)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Ujisti se, že máme balíèek Microsoft.EntityFrameworkCore.SqlServer

// Povolení CORS (Cross-Origin Resource Sharing) – nutné pro komunikaci z prohlížeèových rozšíøení
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()   // Povolit požadavky ze všech domén
              .AllowAnyHeader()   // Povolit všechny hlavièky
              .AllowAnyMethod();  // Povolit všechny HTTP metody (GET, POST, ...)
    });
});

var app = builder.Build();

// Konfigurace HTTP pipeline (zpracování pøíchozích požadavkù)

// Pokud aplikace nebìží v režimu vývoje, zapne se globální error handler a HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Pøesmìrování na vlastní chybovou stránku
    app.UseHsts(); // HTTP Strict Transport Security – vynucení HTTPS
}

// Pøesmìrování všech HTTP požadavkù na HTTPS (výchozí chování)
app.UseHttpsRedirection();

// Servírování statických souborù (napø. CSS, JS, Bootstrap)
app.UseStaticFiles();

// Povolit CORS politiku, nadefinovanou výše
app.UseCors();

// Nastavení smìrování
app.UseRouting();

// Autentizaèní middleware pro pøípadné rozšíøení aplikace o autentizaci
app.UseAuthorization();

// Nastavení výchozího routu: /Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); // Spuštìní aplikace