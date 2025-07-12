// Naslouchá zprávám ze background skriptu
chrome.runtime.onMessage.addListener((msg) => {
  // Když přijde požadavek na "scrape", získáme celý HTML dokument
  if (msg.action === "scrape") {
    const html = document.documentElement.outerHTML;

    // Pošleme HTML zpět do background.js pro další zpracování
    chrome.runtime.sendMessage({ action: "sendToApi", html });
  }

  // Úspěšně zpracováno – zobrazíme krátké toast oznámení v pravém dolním rohu
  if (msg.action === "success") {
    const toast = document.createElement("div");
    toast.textContent = `Staženo komentářů: ${msg.count}`;

    // Styl toast notifikace (zelený box s textem)
    toast.style.cssText = `
      position: fixed;
      bottom: 20px;
      right: 20px;
      background: #0a0;
      color: white;
      padding: 12px 18px;
      border-radius: 8px;
      font-weight: bold;
      z-index: 9999;
      box-shadow: 0 0 6px rgba(0,0,0,0.3);
    `;

    // Přidání na stránku a automatické odstranění po 4 vteřinách
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
  }

  // Pokud došlo k chybě – zobrazíme klasické okno s chybovou zprávou
  if (msg.action === "error") {
    alert(msg.message);
  }
});
