**Seznam Scraper**

Tento nástroj umožňuje stahování komentářů z veřejných profilů na webu Seznam.cz (https://www.seznam.cz) pomocí rozšíření prohlížeče nebo ručně přes webové rozhraní.

Funkce

- Automatické stahování komentářů z profilů Seznamu (např. `https://www.seznam.cz/profil/...`)
- Ukládání komentářů a uživatelských jmen do SQL databáze
- Webové rozhraní s možností ručního zadání URL profilu
- Rozšíření pro Chrome / Edge / Opera a Firefox

1. Stažení a spuštění v Git Bash

1.1. Klonování projektu
- git clone https://github.com/Uzivatel1/SeznamScraper.git
- cd SeznamScraper

1.2. Spuštění aplikace
- dotnet ef migrations add InitialCreate --project SeznamScraper
- dotnet ef database update --project SeznamScraper
- dotnet run --project SeznamScraper/SeznamScraper.csproj

Aplikace poběží na http://localhost:5109

2. Instalace rozšíření

2.1. Pro Chrome / Edge / Opera
- Otevřete chrome://extensions/
- Zapněte Režim pro vývojáře
- Klikněte na Načíst rozbalené
- Vyberte složku ChromeExtension (i přes případný nápis "Hledání neodpovídají žádné položky.")

2.2. Pro Firefox
- Otevřete about:debugging#/runtime/this-firefox
- Klikněte na Načíst dočasný doplněk
- Vyberte soubor manifest.json ve složce FirefoxExtension

3. Použití za běhu aplikace

Automatické
- Otevřete libovolný profil Seznamu, např.: https://www.seznam.cz/profil/jmeno-uzivatele...
- Na záložce profilu klikněte na ikonu rozšíření

Ručně přes web
- Přejděte na úvodní stránku aplikace (např. http://localhost:5109)
- Vložte URL profilu do formuláře
- Klikněte na Odeslat

Uložené komentáře lze prohlédnout v tabulce `Comments` databáze `SeznamScraper` přes SQL Server Object Explorer ve Visual Studiu 2022.

4. Technologie

- ASP.NET Core 8.0
- Entity Framework Core (SQL Server)
- JavaScript (Content Scripts, Background Scripts)
- Bootstrap (web UI)
- Chrome/Firefox Extensions (Manifest v2 i v3)

5. Struktura projektu

- SeznamScraper/
- ├── wwwroot/
- ├── Controllers/
- │   └── HomeController.cs       ← API logika
- ├── Data/
- │   ├── ApplicationDbContext.cs
- ├── ChromeExtension/
- ├── FirefoxExtension/
- ├── Models/
- │   ├── User.cs
- │   └── Comment.cs
- └── Views/
- │   └── Home/
- │       └── Index.cshtml        ← Webový formulář
