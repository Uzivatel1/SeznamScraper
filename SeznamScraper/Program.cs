using SeznamScraper.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Registrace slu�eb

// P�id�n� MVC (controller + views) do kontejneru slu�eb
builder.Services.AddControllersWithViews();

// Nastaven� Entity Frameworku a p�ipojen� k SQL Serveru (�et�zec je v appsettings.json)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Ujisti se, �e m�me bal��ek Microsoft.EntityFrameworkCore.SqlServer

// Povolen� CORS (Cross-Origin Resource Sharing) � nutn� pro komunikaci z prohl�e�ov�ch roz���en�
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()   // Povolit po�adavky ze v�ech dom�n
              .AllowAnyHeader()   // Povolit v�echny hlavi�ky
              .AllowAnyMethod();  // Povolit v�echny HTTP metody (GET, POST, ...)
    });
});

var app = builder.Build();

// Konfigurace HTTP pipeline (zpracov�n� p��choz�ch po�adavk�)

// Pokud aplikace neb�� v re�imu v�voje, zapne se glob�ln� error handler a HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // P�esm�rov�n� na vlastn� chybovou str�nku
    app.UseHsts(); // HTTP Strict Transport Security � vynucen� HTTPS
}

// P�esm�rov�n� v�ech HTTP po�adavk� na HTTPS (v�choz� chov�n�)
app.UseHttpsRedirection();

// Serv�rov�n� statick�ch soubor� (nap�. CSS, JS, Bootstrap)
app.UseStaticFiles();

// Povolit CORS politiku, nadefinovanou v��e
app.UseCors();

// Nastaven� sm�rov�n�
app.UseRouting();

// Autentiza�n� middleware pro p��padn� roz���en� aplikace o autentizaci
app.UseAuthorization();

// Nastaven� v�choz�ho routu: /Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); // Spu�t�n� aplikace