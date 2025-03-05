# TextSearchEngine - Fuzzy Search & Ranking System

`TextSearchEngine` er en avansert fuzzy-søkemotor som bruker **Levenshtein-avstand, Jaro-Winkler og Trigram-similaritet** for å rangere søkeresultater. Systemet lar deg søke fleksibelt i tekstbaserte objekter, med støtte for **vektet scoring, caching og ekskluderende søkeord**.

---

## ✨ Funksjonalitet
- **Fuzzy Matching**: Søkeord matches fleksibelt mot tekstfelter.
- **Levenshtein-avstand**: Håndterer stavefeil og tilnærmet samsvar.
- **Jaro-Winkler**: Bedre nøyaktighet for korte ord.
- **Trigram Matching**: Bedre nøyaktighet for korte og fragmenterte ord.
- **Dynamiske vekter**: Høyere poengsum for viktige felter.

---

## 🔢 **Scoring (Poengsystem)**
Scoren beregnes basert på **en kombinasjon av metoder** og vektes for å gi mer relevante resultater.

| **Parameter**         | **Vekt** | **Beskrivelse** |
|----------------------|---------|---------------|
| `JaroWeight`        | 0.4     | Jaro-Winkler Similarity - best for korte ord. |
| `LevenshteinWeight` | 0.4     | Levenshtein-avstand - håndterer skrivefeil. |
| `TrigramWeight`     | 0.2     | Trigram-sammenligning - god for lengre ord og fragmenterte treff. |
| `FuzzynessLevel`    | -       | Styrer toleransen for unøyaktige treff. |

## 🎯 **Fuzzyness-nivåer**
Fuzzyness-nivået bestemmer hvor **fleksibelt søket er** med hensyn til feil og unøyaktigheter.

|**Nivå**      |**Threshold**      |**MaxDistance**      |**Beskrivelse**                  |
|--------------|-------------------|---------------------|---------------------------------|
|`Hight`      |0.65      |3      |Høy toleranse for feil. Brukes på korte og viktige felt som navn, der små feil ikke bør føre til at treff går tapt.  |
|`Medium`     |0.75      |2      |Moderat toleranse for feil. Brukes på mellomlange tekster som beskrivelser og kategori-felt.                         |
|`Low`        |0.85      |1      |Lav toleranse for feil. Brukes på lengre felt, der eksakt treff er viktigere enn fleksibilitet.                      |

---

## 🛠 **Bruk**

### **1️⃣ Definer hvilke egenskaper som skal søkes i**
Bruk `SearchPropertyBuilder` for å spesifisere feltene som skal inkluderes i søket.

```csharp
var builder = new SearchPropertyBuilder<PackageDto>()
    .Add(pkg => pkg.Name, 2.0, FuzzynessLevel.High)
    .Add(pkg => pkg.Description, 0.8, FuzzynessLevel.Low)
    .Add(pkg => pkg.Area.Name, 1.5, FuzzynessLevel.Medium)
    .AddCollection(pkg => pkg.Resources, r => r.Name, 1.2, FuzzynessLevel.High, detailed: true);
```

#### Forklaring:
> **`Add()`** brukes for enkle tekstfelt.  
> **`AddCollection()`** brukes for lister av objekter, med støtte for detaljert søk (`detailed = true`).
> ** Detailed vs Basic - Basic vil kombinere alle feltet på alle i listen til et felt å søke på. Detaild vil generere et felt pr objekt i listen.

---

### **2️⃣ Utfør søket**
```csharp
var results = FuzzySearch.PerformFuzzySearch(data, "eiendom", builder);
```

---

### **3️⃣ Logg og presenter resultater**
```csharp
foreach (var result in results.OrderByDescending(t => t.Score))
{
    Console.WriteLine($"🎯 Treff: {result.Object.Name}, Score: {result.Score}");
    
    foreach (var field in result.Fields.OrderByDescending(t => t.Score))
    {
        Console.WriteLine($"📌 {field.Field}: {string.Join(" | ", field.Words.Where(w => w.IsMatch).Select(w => $"[{w.Content}({w.Score})]"))}");
    }
}
```

#### Resultat
```
🎯 Treff: Eiendomsforvaltning, Score: 1.85
📌 Name: [Eiendomsforvaltning(1.00)]
📌 Description: [Eiendomsdrift(0.85)] og forvaltning

🎯 Treff: Eiendomsskatt, Score: 1.60
📌 Name: [Eiendomsskatt(1.00)]
📌 Description: Skatt for boliger og [næringsbygg(0.60)]

🎯 Treff: Boligutvikling, Score: 0.75
📌 Description: Utvikling av [eiendom(0.75)] og boliger

```

---

## 📌 **Modeller**

### **1️⃣ `SearchObject<T>`**
Representerer et søkeresultat.
```csharp
public class SearchObject<T>
{
    public T Object { get; }
    public double Score { get; }
    public List<SearchField> Fields { get; }
}
```
| Property | Beskrivelse |
|----------|------------|
| `Object` | Referanse til det funnet objektet. |
| `Score` | Total poengsum basert på søketreff. |
| `Fields` | Liste over felt med detaljerte treff. |

---

### **2️⃣ `SearchField`**
Inneholder detaljer om treff i et enkelt felt.
```csharp
public class SearchField
{
    public string Field { get; }
    public string Value { get; }
    public double Score { get; }
    public List<SearchWord> Words { get; }
}
```
| Property | Beskrivelse |
|----------|------------|
| `Field` | Navn på feltet der treffet ble funnet. |
| `Value` | Tekstverdien av feltet. |
| `Score` | Poengsum for treffet. |
| `Words` | Liste over matchede ord. |

---

### **3️⃣ `SearchWord`**
Inneholder detaljer om treff på et enkelt ord.
```csharp
public class SearchWord
{
    public string Content { get; set; }
    public string LowercaseContent { get; set; }
    public bool IsMatch { get; set; }
    public double Score { get; set; }
}
```
| Property | Beskrivelse |
|----------|------------|
| `Content` | Tekstverdien av feltet. |
| `LowercaseContent` | Tekstverdien av feltet i lowercase. |
| `IsMatch` | Bekrefter at ordet matcher søket. |
| `Score` | Poengsum for treffet. |

---

## 🏎 **Caching for raskere søk**
For å forbedre ytelsen støtter systemet **caching av søkedata**.

### **Caching-implementasjon**

```csharp
var cache = new SearchCache<PackageDto>(memoryCache);
cache.SetData(packages, TimeSpan.FromMinutes(30));
var cachedData = cache.GetData();
```

---

## 🎯 **Videre utvikling**
- [ ] **Paging**: La brukere page eller sett maxResult.
- [ ] **Ekskluderende søkeord**: Filtrer bort uønskede treff.
- [ ] **Caching**: Forhåndsgenerer søkedata for raskere søk.
- [ ] **Forbedret caching for store datasett** 🚀
- [ ] **Fonetisk matching (Soundex, Metaphone)** 🔍
- [ ] **Støtte for SQL-integrasjon** 🎯

---

## 📝 **Oppsummering**
`TextSearchEngine` gir en kraftig og fleksibel fuzzy-søkemotor med **vekting, høydepunktmarkering og samlingssøk**. 
Perfekt for systemer som trenger **rask og relevant søkefunksjonalitet**. 🚀
