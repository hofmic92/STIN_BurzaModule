zadání: https://docs.google.com/document/d/1muLy1wJLMlQxcWXMzU_PFRWjz-uHgMjKZSq3zxltfY4/edit?tab=t.0

User Interface - Honza
JSON objekt - Martin
Sehnání dat - Filip


_________________________________________________________________________________
co kód splňuje ze zadání (by AI)
_________________________________________________________________________________

### **Zadání: Modul Burza**

#### **Modul, který bude na definovaný interval (0:00, 6:00, 12:00, 18:00) nebo na ruční start získávat aktuální nebo historická data pro definované položky (např. Microsoft) na burze (umožnit uživateli definovat svůj seznam oblíbených položek).**
- **Splněno**:
  - **Interval**: `StockDataService` v `Program.cs` spouští `FetchAndProcessStockData` na časech 0:00, 6:00, 12:00 a 18:00 (pomocí `targetTimes`).
  - **Ruční start**: Endpoint `/data` umožňuje ruční získání dat (surových nebo filtrovaných pomocí parametru `filter`).
  - **Historická data**: `FilterItems` v `StockService.cs` stahuje historická data (10 dní, definováno v `appsettings.json` jako `HistoryDays`).
  - **Definované položky**: Data jsou získávána pro položky z API, a uživatel může definovat oblíbené položky přes `FavoritesManager` (uloženo v `favorites.json` a inicializováno z `appsettings.json`).
- **Chybí**: Zadání se ptá "Stahujeme vždy vše nebo inkrementálně?". Tvůj kód stahuje vždy vše (`FetchStockData`), inkrementální stahování (jen nová data) není implementováno.

#### **Umožnit uživateli definovat základní filtr na vývoj položky v čase**
- **Částečně splněno**:
  - `FilterItems` implementuje filtry podle `DeclineDays` (výchozí 3) a `MaxDeclines` (výchozí 2) z `appsettings.json`.
  - Filtry jsou definovány v konfiguraci, ale uživatel je nemůže dynamicky měnit přes UI (jen staticky přes `appsettings.json`).
- **Chybí**: Dynamická změna filtrů přes UI (např. formulář).

#### **Odfiltrovat ty, co poslední 3 dny pracovní klesaly**
- **Splněno**:
  - `FilterItems` kontroluje `last3WorkingDays.All(d => d.PriceChange < 0)` s vyloučením víkendů.

#### **Odfiltrovat takové, které za posledních 5 pracovních dní měly více než dva poklesy**
- **Splněno**:
  - `FilterItems` kontroluje `last5WorkingDays.Count(d => d.PriceChange < 0) > maxDeclines` (maxDeclines = 2).

#### **Poslat požadavek na získání doporučení do modulu zprávy**
- **Splněno**:
  - `SendToNewsModule` posílá filtrované položky na `http://localhost:8000/rating` pomocí HTTP POST.

---

### **Modul Burza – krok II**

#### **Vezme výsledek hodnocení modulu zprávy a k těm položkám, které mají rating větší než uživatelem definovanou hodnotu doplní o doporučení prodat**
- **Splněno**:
  - `SendToNewsModule` zpracovává odpověď z `rating`, porovnává `item.getRating()` s `userRatingThreshold` (výchozí 5 z `appsettings.json`) a pokud je vyšší, volá `item.setSell()`, což nastaví `Sell` na 1 podle logiky v `Item.cs`.

#### **Výsledek pošle modulu zprávy**
- **Splněno**:
  - `SendToNewsModule` posílá položky s doporučením k prodeji (`Sell = 1`) na `http://localhost:8000/salestock`.

---

### **Mimo funkční požadavky**

#### **1. Obě UI musí běžet na PC i mobilu**
- **Splněno**:
  - `/ui` endpoint v `Program.cs` používá Bootstrap a přidané CSS (`@media (max-width: 768px)`) zajišťuje responzivitu pro PC i mobilní zařízení.

#### **2. Moduly musí běžet mimo localhost**
- **Částečně splněno**:
  - `launchSettings.json` používá `0.0.0.0:5000`, což umožňuje přístup z venku, pokud je aplikace nasazena na serveru.
- **Chybí**: Aktuálně je testování lokální. V produkčním prostředí je potřeba nasazení na server s veřejnou IP a zajištění, aby News modul běžel na `partner:8000`.

---

### **Obecné**

#### **1. Libovolný jazyk**
- **Splněno**:
  - Používá C# s ASP.NET Core, což je povolené.

#### **2. Splnit test coverage > 80%**
- **Chybí**:
  - Testy nejsou implementovány. Testovací projekt (`STIN_BurzaModule.Tests`) existuje, ale neobsahuje funkční testy, a pokrytí není generováno ani nahráváno (viz předchozí problém s Codecov).

#### **3. Využít CD/CI - git**
- **Částečně splněno**:
  - Kód je v GitHubu (https://github.com/hofmic92/STIN_BurzaModule), což splňuje základní požadavek na Git.
- **Chybí**: CI/CD pipeline (např. GitHub Actions) je nastaven, ale selhává kvůli chybějícím souborům pokrytí. Potřebuješ opravit generování pokrytí (viz předchozí doporučení).

---

### **DSP (Dodatečné požadavky)**

#### **Modely UI pro Burza a Zprávy**
- **Částečně splněno**:
  - `/ui` endpoint je model pro Burzu a zobrazuje data a zprávy. Zprávy jsou načítány z `liststock`, ale zatím nejsou plně dynamické (používá fallback statických dat, pokud API selže).
- **Chybí**: Oddělené UI pro Zprávy není implementováno, zadání očekává společné UI, ale zprávy by měly být dynamicky propojeny s Burzou.

#### **Persistence oblíbených položek**
- **Splněno**:
  - `FavoritesManager` ukládá oblíbené položky do `favorites.json`.

#### **Co se myslí: získávat aktuální nebo historická data pro definované položky, kolik dní zpět? Stahujeme vždy vše nebo inkrementálně?**
- **Částečně splněno**:
  - Historická data jsou stahována (10 dní, podle `HistoryDays`).
  - Stahuje se vždy vše, inkrementální stahování není implementováno.
- **Chybí**: Inkrementální stahování a explicitní specifikace období (10 dní je implicitní).

#### **Mazání úložiště**
- **Částečně splněno**:
  - `FavoritesManager` má metodu `ClearStorage`, ale není volána z `/ui` (můžeš přidat tlačítko).
- **Chybí**: Explicitní ovládání mazání přes UI.

#### **Externalizovat konfiguraci**
- **Splněno**:
  - Konfigurace je externalizována v `appsettings.json` (např. `StockApi`, `NewsModule`, `UserSettings`).

#### **JSON: name<string>, date<timestamp>, rating<int -10, 0, 10>, sell<0,1>**
- **Splněno**:
  - `Item.cs` definuje privátní pole s gettery/settery pro `Name`, `Date`, `Rating` (validace -10 až 10 v `setRating`), `Sell` (0/1 v `setSell`).
  - Validace špatných dat s logováním je v `FetchStockData`.

#### **Endpoint definition: url:port\endpoint, URL – partner, Port: 8000, Burza: rating, News: liststock, salestock**
- **Splněno**:
  - Burza volá `rating` a News poskytuje `liststock` a `salestock` na `localhost:8000` (konfigurace v `appsettings.json`). V produkci by mělo být `partner:8000`.

#### **Z libovolného API stahovat zprávy, které se týkají položek získaných modulem burza za dané období - za jaké období?**
- **Částečně splněno**:
  - `/ui` volá `liststock` a načítá zprávy, ale pouze jako fallback statických dat, pokud API selže.
- **Chybí**: Dynamické propojení zpráv s položkami Burzy a specifikace období (např. 10 dní podle `HistoryDays`).

#### **Mají málo zpráv k dispozici (UI pro všechny společně - kolik je málo - parametr)**
- **Částečně splněno**:
  - `MaxNewsItems` (50) a `MinNewsCount` (3) jsou definovány v `appsettings.json`, a `/ui` kontroluje počet negativních zpráv.
- **Chybí**: Dynamické vynucení limitů z News modulu.

#### **Která mají negativní hodnocení, jak s 0?**
- **Splněno**:
  - `/ui` filtruje negativní zprávy (`Rating < 0`) a 0 je považováno za neutrální.

#### **Mapování hodnocení na int <-10, 10>**
- **Splněno**:
  - `Item.cs` kontroluje rozsah v `setRating` (výjimka při mimo -10 až 10).

#### **Jak se hodnotí zprávy bez genAI**
- **Chybí**:
  - Zadání požaduje specifikaci, jak se hodnotí zprávy bez generativní AI. Tvůj kód spoléhá na News modul (`rating`), ale nejsou uvedeny vlastní pravidla.

#### **Co se stane pokud by přišlo hodnocení na firmu, která nebyla v původním seznamu - validace zpráv**
- **Splněno**:
  - `SendToNewsModule` ignoruje firmy mimo `favorites` a loguje varování (`Received rating for unknown company`).

#### **Jak se udržuje stav komunikace mezi Burzou a Zprávami, pokud je více klientů**
- **Splněno**:
  - `StateManager` používá `ConcurrentQueue` pro ukládání stavu (clientId a položky) pro více klientů.

#### **Pokud nemá položku s doporučením koupit, tak se nakoupí UI společné pro všechny položky - kolik se nakoupí**
- **Splněno**:
  - `SendToNewsModule` nakupuje `defaultBuyAmount` (výchozí 1 z `appsettings.json`) akcií, pokud `getSell()` je null nebo 0.
- **Chybí**: Explicitní zobrazení nákupu v UI.

#### **Timeouty pro čtení z externích REST - retry 5x po n sec**
- **Splněno**:
  - `FetchStockData` má retry mechanismus (5 pokusů, 2 sekundy prodleva podle `RetryCount` a `RetryDelaySeconds`).

#### **Limity na zprávy**
- **Částečně splněno**:
  - `MaxNewsItems` a `MinNewsCount` jsou definovány, ale nejsou dynamicky vynuceny z News modulu.
- **Chybí**: Reálné limity z externího API.

---

### **Shrnutí**
#### **Splněno**
- Periodické a ruční získávání dat (interval 6 hodin, `/data`).
- Historická data (10 dní).
- Definice a persistence oblíbených položek.
- Filtrace podle 3 dní poklesu a >2 poklesů za 5 dní.
- Komunikace s News modulem (poslání na `rating`, zpracování a odeslání na `salestock`).
- Doporučení k prodeji podle ratingu.
- Responzivní UI.
- Externalizovaná konfiguraci.
- Validace a logování špatných dat.
- Stav komunikace pro více klientů.
- Nákup, pokud není doporučení k prodeji.
- Retry mechanismus.
- JSON struktura a mapování ratingů.
- Ignorování neznámých firem.
- Negativní hodnocení a neutrální 0.

#### **Částečně splněno**
- Dynamické nastavení filtrů (statické v `appsettings.json`, chybí UI forma).
- Běh mimo localhost (funguje s `0.0.0.0`, ale vyžaduje nasazení).
- CI/CD (Git je použit, pipeline selhává kvůli pokrytí).
- Zobrazení zpráv (dynamické načítání z `liststock`, ale s fallbackem).
- Mazání úložiště (metoda existuje, chybí UI ovládání).
- Limity na zprávy (definováno, ale ne dynamicky z News modulu).

#### **Chybí**
- Test coverage > 80 % (testy nejsou implementovány).
- Inkrementální stahování dat.
- Dynamické propojení zpráv s Burzou a specifikace období.
- Specifikace hodnocení zpráv bez genAI.
- Produkční nasazení mimo localhost.
- Dynamické zobrazení nákupu v UI.
