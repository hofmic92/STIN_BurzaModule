zadání: https://docs.google.com/document/d/1muLy1wJLMlQxcWXMzU_PFRWjz-uHgMjKZSq3zxltfY4/edit?tab=t.0

User Interface - Honza
JSON objekt - Martin
Sehnání dat - Filip


_________________________________________________________________________________
co kód splňuje ze zadání (by AI)
_________________________________________________________________________________

Zadání: Modul Burza
1. Modul, který bude na definovaný interval (0:00, 6:00, 12:00, 18:00) nebo na ruční start získávat aktuální nebo historická data pro definované položky (např. Microsoft) na burze

    Splněno:
        Interval: StockDataService (background service) je nastaven tak, aby spouštěl FetchAndProcessStockData na časy 0:00, 6:00, 12:00 a 18:00 (pomocí targetTimes).
        Ruční start: Endpoint /data umožňuje ruční získání dat (surových nebo filtrovaných podle parametru filter).
        Historická data: FilterItems získává historická data (10 dní, definováno v appsettings.json jako HistoryDays), což odpovídá otázce "kolik dní zpět?" (zadání to neomezuje, 10 dní je rozumné).
        Definované položky: Data jsou získávána pro položky definované v API (např. Microsoft, Google), a uživatel může definovat svůj seznam oblíbených položek (viz níže).
    Chybí:
        Zadání se ptá "Stahujeme vždy vše nebo inkrementálně?". Tvůj kód stahuje vše pokaždé (FetchStockData), což je funkční, ale inkrementální stahování (jen nová data od posledního stahování) není implementováno. To by vyžadovalo sledování posledního timestampu a úpravu logiky.

2. Umožnit uživateli definovat svůj seznam oblíbených položek

    Splněno:
        FavoritesManager ukládá oblíbené položky do favorites.json a poskytuje metody AddFavorite, RemoveFavorite a GetFavorites.
        V /ui je integrována možnost přidávat a odebírat oblíbené položky přes formulář a JavaScript (query parametry action a companyName).
        Výchozí seznam oblíbených je definován v appsettings.json (Favorites).
    Chybí:
        Zadání zmiňuje "persistence oblíbených položek" – tvoje řešení již má persistence do souboru, což je v pořádku, ale nemá explicitní "mazání úložiště" přes UI (můžeš to přidat jako tlačítko v /ui).

3. Umožnit uživateli definovat základní filtr na vývoj položky v čase

    Částečně splněno:
        FilterItems implementuje filtry podle zadání:
            "Odfiltrovat ty, co poslední 3 dny pracovní klesaly" (používá declineDays).
            "Odfiltrovat takové, které za posledních 5 pracovních dní měly více než dva poklesy" (používá maxDeclines).
        Filtry jsou nastaveny v appsettings.json (DeclineDays a MaxDeclines), což umožňuje uživateli je definovat přes konfiguraci.
    Chybí:
        Uživatel nemůže dynamicky měnit filtry přes UI (např. přes formulář v /ui). Aktuálně jsou filtry statické a mění se pouze přes změnu appsettings.json. To lze doplnit přidáním formuláře a uložení do konfigurace.

4. Odfiltrovat ty co poslední 3 dny pracovní klesaly

    Splněno:
        FilterItems kontroluje last3WorkingDays.All(d => d.PriceChange < 0) s vyloučením víkendů (DayOfWeek != Saturday && DayOfWeek != Sunday).

5. Odfiltrovat takové, které za posledních 5 pracovních dní měly více než dva poklesy

    Splněno:
        FilterItems kontroluje last5WorkingDays.Count(d => d.PriceChange < 0) > maxDeclines (maxDeclines = 2).

6. Poslat požadavek na získání doporučení do modulu zprávy

    Splněno:
        SendToNewsModule posílá filtrovaná data na rating endpoint (http://localhost:8000/rating) pomocí HTTP POST.

7. Vezme výsledek hodnocení modulu zprávy a k těm položkám, které mají rating větší než uživatelem definovanou hodnotu doplní o doporučení prodat

    Splněno:
        SendToNewsModule zpracovává odpověď z rating, porovnává item.Rating s userRatingThreshold (z appsettings.json) a pokud je vyšší, nastaví item.Sell = 1.

8. Výsledek pošle modulu zprávy

    Splněno:
        SendToNewsModule posílá položky s doporučením k prodeji na salestock endpoint (http://localhost:8000/salestock) pomocí HTTP POST.

Mimo funkční požadavky
1. Obě UI musí běžet na PC i mobilu

    Splněno:
        /ui používá Bootstrap a přidané CSS (@media (max-width: 768px)) zajišťuje responzivitu pro PC i mobilní zařízení.

2. Moduly musí běžet mimo localhost

    Částečně splněno:
        launchSettings.json používá 0.0.0.0:5000, což umožňuje přístup z venku (ne pouze localhost). To splňuje požadavek, pokud je aplikace nasazena na serveru.
    Chybí:
        Aktuálně testuješ lokálně. V produkčním prostředí musíš zajistit, aby Burza běžela na adrese dostupné zvenčí (např. server s veřejnou IP), a News modul na partner:8000. To vyžaduje nasazení a konfiguraci sítě.

Obecné požadavky
1. Libovolný jazyk

    Splněno:
        Používáš C# s ASP.NET Core, což je povolené.

2. Splnit test coverage > 80%

    Chybí:
        Testy nejsou implementovány. Potřebuješ přidat unit testy (např. pomocí xUnit) pro StockService, FilterItems, SendToNewsModule a FavoritesManager, aby pokrytí přesáhlo 80 %.

3. Využít CD/CI - git

    Částečně splněno:
        Kód je v GitHubu (https://github.com/hofmic92/STIN_BurzaModule/tree/dev-main), což splňuje základní požadavek na Git.
    Chybí:
        CI/CD pipeline (např. GitHub Actions) není nastavena. Potřebuješ přidat workflow pro automatické buildování, testování a nasazení.

DSP (Dodatečné požadavky)
Modely UI pro Burza a Zprávy

    Částečně splněno:
        /ui je model pro Burzu a zobrazuje data, oblíbené položky a zprávy. Nicméně UI pro Zprávy není oddělené – zadání požaduje společné UI, což je v pořádku, ale statické zprávy by měly být nahrazeny dynamickými daty z News modulu (liststock).
    Chybí:
        Dynamické načítání zpráv z News modulu.

Persistence oblíbených položek

    Splněno:
        FavoritesManager ukládá oblíbené položky do favorites.json.

Co se myslí: získávat aktuální nebo historická data pro definované položky, kolik dní zpět? Stahujeme vždy vše nebo inkrementálně?

    Splněno částečně:
        Historická data jsou stahována (10 dní), což odpovídá otázce "kolik dní zpět?".
        Stahuje se vždy vše, nikoli inkrementálně (viz bod 1 v "Modul Burza" – chybí sledování posledního timestampu).
    Chybí:
        Inkrementální stahování.

Mazání úložiště

    Splněno částečně:
        FavoritesManager má metodu ClearStorage, ale není volána z /ui. Můžeš přidat tlačítko pro mazání.
    Chybí:
        Explicitní ovládání mazání přes UI.

Externalizovat konfiguraci

    Splněno:
        Konfigurace je externalizována v appsettings.json (např. StockApi, NewsModule, UserSettings).

JSON struktura

    Splněno:
        Item.cs odpovídá požadované struktuře: name (string), date (timestamp), rating (int -10 až 10), sell (0 nebo 1).
        Validace špatných dat s logováním je implementována v FetchStockData.

Endpoint definition

    Splněno:
        Burza volá rating a salestock na partner:8000 (nyní localhost:8000 pro testování), což odpovídá zadání.

Z libovolného API stahovat zprávy, které se týkají položek získaných modulem burza za dané období

    Chybí:
        Zprávy jsou zatím statické v /ui. Potřebuješ volat liststock endpoint News modulu a stáhnout zprávy za dané období (např. 10 dní, podle HistoryDays).

Mají málo zpráv k dispozici (UI pro všechny společně - kolik je málo - parametr)

    Částečně splněno:
        MaxNewsItems (50) je definováno v appsettings.json a omezení je implementováno v /ui.
        Logování "Not enough negative news" je přítomno, pokud je počet negativních zpráv pod MinNewsCount (3).
    Chybí:
        Dynamické načítání zpráv z News modulu.

Která mají negativní hodnocení, jak s 0?

    Splněno:
        /ui filtruje negativní zprávy (Rating < 0) a zohledňuje 0 jako neutrální (není považováno za negativní).

Mapování hodnocení na int <-10, 10>

    Splněno:
        Item.cs má [Range(-10, 10)] pro Rating.

Jak se hodnotí zprávy bez genAI

    Chybí:
        Zadání se ptá, jak se hodnotí zprávy bez generativní AI. Tvůj kód má statické zprávy s pevnými ratingy, ale reálné hodnocení by mělo přicházet z News modulu (rating endpoint). Potřebuješ specifikaci od zadání nebo implementaci vlastní logiky.

Co se stane pokud by přišlo hodnocení na firmu, která nebyla v původním seznamu

    Splněno:
        SendToNewsModule ignoruje firmy, které nejsou v favorites, a loguje varování (_logger.LogWarning).

Jak se udržuje stav komunikace mezi Burzou a Zprávami, pokud je více klientů

    Splněno:
        StateManager používá ConcurrentQueue pro ukládání stavu komunikace (klient ID a data) pro více klientů.

Pokud nemá položku s doporučením koupit, tak se nakoupí UI společné pro všechny položky - kolik se nakoupí

    Splněno:
        SendToNewsModule nakupuje defaultBuyAmount (z appsettings.json) akcií, pokud není doporučení k prodeji (Sell = 0 nebo null).
    Chybí:
        Zadání se ptá "kolik se nakoupí" – defaultBuyAmount je nastaven na 1, což je rozumná hodnota, ale může být upraveno podle potřeby.

Timeouty pro čtení z externích REST - retry 5x po n sec

    Splněno:
        FetchStockData má retry mechanismus (5 pokusů, 2 sekundy prodleva, definováno v RetryCount a RetryDelaySeconds).

Limity na zprávy

    Částečně splněno:
        MaxNewsItems a MinNewsCount jsou definovány, ale nejsou dynamicky vynuceny z News modulu.
    Chybí:
        Reálné limity z externího API (např. liststock).

Shrnutí
Co je plně splněno

    Automatické a ruční získávání dat (interval 6 hodin, /data).
    Definice a persistence oblíbených položek.
    Filtrace podle 3 dní poklesu a >2 poklesů za 5 dní.
    Komunikace s News modulem (poslání na rating, zpracování a odeslání na salestock).
    Validace a logování špatných dat.
    Responzivní UI.
    Externalizovaná konfigurace.
    Stav komunikace pro více klientů.
    Nákup, pokud není doporučení k prodeji.
    Retry mechanismus.
    JSON struktura a mapování ratingů.

Co je částečně splněno

    Dynamické nastavení filtrů (statické v appsettings.json, chybí UI forma).
    Běh mimo localhost (funguje s 0.0.0.0, ale vyžaduje produkční nasazení).
    CI/CD (Git je použit, chybí pipeline).
    Zobrazení zpráv (statické, chybí dynamické načítání z liststock).
    Mazání úložiště (metoda existuje, chybí UI ovládání).
    Limity na zprávy (definováno, ale ne dynamicky z News modulu).

Co chybí

    Test coverage > 80 % (nutné přidat unit testy).
    Inkrementální stahování dat.
    Dynamické načítání zpráv z News modulu (liststock).
    Specifikace hodnocení zpráv bez genAI.
    Produkční nasazení mimo localhost.

Doporučení

    Testy: Přidej unit testy pro StockService a FavoritesManager (např. xUnit).
    Dynamické filtry: Přidej do /ui formulář pro změnu DeclineDays a MaxDeclines s uložením do appsettings.json.
    Zprávy: Implementuj volání liststock v /ui.
    CI/CD: Nastav GitHub Actions.
    Nasazení: Testuj na serveru s veřejnou IP.
