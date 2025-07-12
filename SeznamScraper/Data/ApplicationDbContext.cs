using SeznamScraper.Models;
using Microsoft.EntityFrameworkCore;

namespace SeznamScraper.Data
{
    // Třída ApplicationDbContext reprezentuje připojení k databázi pomocí EntityFrameworkCore
    public class ApplicationDbContext : DbContext
    {
        // Konstruktor – zde předáváme nastavení (např. connection string) z Program.cs
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // DbSet<User> znamená, že bude v databázi existovat tabulka "Users"
        // Pomocí této vlastnosti lze přidávat, číst, upravovat a mazat uživatele
        public DbSet<User> Users { get; set; }

        // DbSet<Comment> reprezentuje tabulku "Comments" v databázi
        // Podobně jako výše – práce s komentáři, CRUD operace
        public DbSet<Comment> Comments { get; set; }
    }
}
