// Reaguje na kliknutí na ikonu rozšíření
chrome.browserAction.onClicked.addListener((tab) => {
  // Pošle zprávu content skriptu na aktivní záložku, aby zahájil scraping
  chrome.tabs.sendMessage(tab.id, { action: "scrape" });
});

// Naslouchá zprávám od content skriptu (např. s HTML obsahem stránky)
chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.action === "sendToApi") {
    // Odesílá HTML na backend API na localhost
    fetch("http://localhost:5109/api/home", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ html: msg.html }),
    })
      .then((res) => {
        // Pokud server odpoví chybou, zobrazí se zpráva v rozhraní
        if (!res.ok) {
          return res.text().then((text) => {
            chrome.tabs.sendMessage(sender.tab.id, {
              action: "error",
              message: "Server error: " + text,
            });
          });
        }

        // V případě úspěchu:
        return res.json().then((data) => {
          // Pošle zpět do content skriptu úspěšné zpracování
          chrome.tabs.sendMessage(sender.tab.id, {
            action: "success",
            count: data.count,
          });

          // Zobrazí desktopovou notifikaci s počtem komentářů
          chrome.notifications.create({
            type: "basic",
            iconUrl: "icon.png", // Ikona 48x48 v root složce doplňku
            title: "Seznam Scraper",
            message: "Staženo komentářů: " + data.count,
          });
        });
      })
      .catch((err) => {
        // Pokud dojde k síťové chybě nebo jiné chybě, zobrazí se zpráva
        chrome.tabs.sendMessage(sender.tab.id, {
          action: "error",
          message: "Stahování se neprovedlo: " + err.message,
        });
      });
  }
});
