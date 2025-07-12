// Reakce na kliknutí na ikonu rozšíření v prohlížeči
chrome.action.onClicked.addListener((tab) => {
  // Vykoná skript v aktivní záložce (seznam.cz profil)
  chrome.scripting.executeScript({
    target: { tabId: tab.id },
    func: scrapeAndSendToApi, // Funkce, která se má spustit na stránce
  });
});

// Funkce, která běží přímo v kontextu stránky (např. seznam.cz/profil/...)
function scrapeAndSendToApi() {
  // Získá celý HTML obsah aktuální stránky
  const html = document.documentElement.outerHTML;

  // Odešle HTML na backend API pomocí POST požadavku
  fetch("http://localhost:5109/api/home", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ html }), // Zabalí HTML do JSON objektu
  })
    .then((res) => {
      if (!res.ok) {
        // Pokud server vrátí chybu, vypíše ji jako alert
        return res.text().then((text) => {
          alert("Server error: " + text);
        });
      }
      // Pokud bylo volání úspěšné, zobrazí počet komentářů
      return res.json().then((data) => {
        alert("Staženo komentářů: " + data.count);
        console.log(data.links); // Pokud někdy v budoucnu vrátíš odkazy, zobrazí se zde
      });
    })
    // Zachytí síťovou chybu nebo chybu připojení k API
    .catch((err) => alert("Stahování se neprovedlo: " + err.message));
}
