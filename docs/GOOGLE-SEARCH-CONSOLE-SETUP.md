# Google Search Console Setup Guide - InsightLearn

**Documento**: Google Search Console Configuration
**Data**: 2025-12-16
**Versione**: 1.0
**Sito**: https://www.insightlearn.cloud

---

## 📋 Indice

1. [Prerequisiti](#prerequisiti)
2. [Verifica Ownership del Sito](#verifica-ownership)
3. [Invio Sitemap Dinamica](#invio-sitemap)
4. [Request Manual Indexing](#manual-indexing)
5. [Configurazione Settings](#configurazione-settings)
6. [Monitoraggio Metriche SEO](#monitoraggio-metriche)
7. [Troubleshooting](#troubleshooting)

---

## 🎯 Prerequisiti

Prima di iniziare, assicurati di avere:

- [x] Account Google attivo (es. marcello.pasqui@gmail.com)
- [x] Accesso al codice sorgente del sito
- [x] Capacità di modificare file HTML (index.html)
- [x] Sito pubblicato e accessibile pubblicamente
- [x] Google Analytics 4 già configurato (opzionale ma consigliato)

---

## 🔐 1. Verifica Ownership del Sito

Google Search Console richiede la verifica che tu sia il proprietario del dominio.

### Step 1.1: Accedi a Google Search Console

1. Vai su: https://search.google.com/search-console
2. Accedi con il tuo account Google (es. marcello.pasqui@gmail.com)
3. Clicca **"Aggiungi proprietà"** (Add property)

### Step 1.2: Scegli Tipo di Proprietà

**Opzione raccomandata**: URL-prefix property
- Inserisci: `https://www.insightlearn.cloud`
- Clicca **"Continua"**

### Step 1.3: Verifica con Meta Tag HTML

1. Google mostrerà diverse opzioni di verifica. Seleziona **"HTML tag"**
2. Copia il codice di verifica (formato: `<meta name="google-site-verification" content="CODICE_QUI" />`)
3. Il meta tag è **già presente** in `src/InsightLearn.WebAssembly/wwwroot/index.html` alla linea 55:
   ```html
   <meta name="google-site-verification" content="VERIFICATION_CODE" />
   ```
4. **AZIONE RICHIESTA**: Sostituisci `VERIFICATION_CODE` con il codice ricevuto da Google
5. Fai commit e deploy della modifica
6. Aspetta 2-3 minuti che il deploy sia completato
7. Torna su Google Search Console e clicca **"Verifica"**

**Esito atteso**: ✅ "Verifica riuscita - Sei il proprietario di https://www.insightlearn.cloud"

---

## 🗺️ 2. Invio Sitemap Dinamica

La sitemap dinamica è fondamentale per far scoprire a Google tutte le pagine del sito.

### Step 2.1: Verifica Sitemap Funzionante

1. Apri browser e visita: https://www.insightlearn.cloud/api/seo/sitemap.xml
2. Verifica che il file XML venga visualizzato correttamente
3. Controlla che contenga:
   - 9 URL statici (homepage, courses, categories, about, faq, contact, pricing, instructors, blog)
   - N URL dinamici per corsi pubblicati (fino a 500)
   - N URL per categorie
   - N URL per istruttori (fino a 100)

**Esempio output sitemap**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://www.insightlearn.cloud/</loc>
    <lastmod>2025-12-16</lastmod>
    <changefreq>daily</changefreq>
    <priority>1.0</priority>
  </url>
  <url>
    <loc>https://www.insightlearn.cloud/courses</loc>
    <lastmod>2025-12-16</lastmod>
    <changefreq>daily</changefreq>
    <priority>0.9</priority>
  </url>
  <!-- ... altri URL ... -->
</urlset>
```

### Step 2.2: Invia Sitemap a Google

1. In Google Search Console, vai su **"Sitemap"** nel menu laterale
2. In "Aggiungi una nuova sitemap", inserisci: `api/seo/sitemap.xml`
3. Clicca **"Invia"**

**Esito atteso**:
- Status: ✅ "Riuscita" (Success)
- URL trovati: ~500+ (numero varia in base ai corsi nel database)
- Data ultimo aggiornamento: Oggi

**Nota**: Google impiega 1-7 giorni per elaborare completamente la sitemap.

---

## 🔍 3. Request Manual Indexing

Per velocizzare l'indicizzazione delle pagine principali, usa il tool di richiesta manuale.

### Step 3.1: Homepage e Pagine Principali

Per ogni URL importante, richiedi l'indicizzazione manuale:

1. In Google Search Console, vai su **"Controllo URL"** (URL Inspection)
2. Inserisci l'URL completo (esempio: `https://www.insightlearn.cloud/`)
3. Clicca **"Testa URL pubblicato"**
4. Aspetta il risultato del test (30-60 secondi)
5. Se il test ha esito positivo, clicca **"Richiedi indicizzazione"** (Request Indexing)
6. Conferma la richiesta

**URL da indicizzare manualmente (priorità alta)**:

| URL | Priorità | Motivo |
|-----|----------|--------|
| https://www.insightlearn.cloud/ | 🔴 ALTA | Homepage - pagina più importante |
| https://www.insightlearn.cloud/courses | 🔴 ALTA | Catalogo corsi - pagina chiave per SEO |
| https://www.insightlearn.cloud/about | 🟡 MEDIA | About page - credibilità del sito |
| https://www.insightlearn.cloud/faq | 🟡 MEDIA | FAQ con FAQPage schema (rich snippets) |
| https://www.insightlearn.cloud/contact | 🟡 MEDIA | Contact page - fiducia utenti |
| https://www.insightlearn.cloud/pricing | 🟢 BASSA | Pricing page - conversioni |
| https://www.insightlearn.cloud/instructors | 🟢 BASSA | Become instructor page |

**Limite**: Puoi richiedere l'indicizzazione di massimo **10 URL al giorno** per sito.

**Tempo di elaborazione**: 1-3 giorni per la maggior parte delle pagine.

---

## ⚙️ 4. Configurazione Settings

### Step 4.1: Imposta Target Country

1. Vai su **"Impostazioni"** (Settings) → **"Internazionalizzazione"**
2. Imposta **"Paese di destinazione"**: Italia (o "Non impostato" per audience internazionale)

**Raccomandazione**: Lascia "Non impostato" per InsightLearn (audience globale).

### Step 4.2: Collega Google Analytics

1. Vai su **"Impostazioni"** → **"Associazioni"** (Associations)
2. Clicca **"Associa servizio"** → **"Google Analytics"**
3. Seleziona la proprietà GA4 esistente: `G-J972BQGNY7`
4. Conferma l'associazione

**Benefici**:
- Dati combinati Search Console + Analytics
- Metriche di conversione più dettagliate
- Report unificati su traffico organico

### Step 4.3: Configura Email Notifications

1. Vai su **"Impostazioni"** → **"Utenti e autorizzazioni"**
2. Abilita notifiche per:
   - Errori critici di scansione
   - Penalizzazioni manuali
   - Problemi di sicurezza
   - Nuove funzionalità Search Console

---

## 📊 5. Monitoraggio Metriche SEO

Dopo 7-14 giorni dall'invio della sitemap, inizierai a vedere dati in Google Search Console.

### Metriche Chiave da Monitorare

**1. Copertura (Coverage)**
- Vai su: **"Copertura"** → **"Tutte le pagine conosciute"**
- Verifica:
  - ✅ Pagine indicizzate: Target 500+ (dopo 2-4 settimane)
  - ⚠️ Pagine escluse: Devono essere < 10%
  - ❌ Errori: Devono essere 0

**2. Prestazioni (Performance)**
- Vai su: **"Prestazioni"** → **"Risultati di ricerca"**
- Monitora:
  - **Clic totali**: Trend crescente mese su mese
  - **Impressioni**: Quante volte il sito appare in SERP
  - **CTR medio**: Target > 5% (media settore education: 7-10%)
  - **Posizione media**: Target < 10 (top 10 risultati)

**3. Esperienza (Experience)**
- Vai su: **"Esperienza"** → **"Core Web Vitals"**
- Verifica:
  - LCP (Largest Contentful Paint): < 2.5s (buono)
  - FID (First Input Delay): < 100ms (buono)
  - CLS (Cumulative Layout Shift): < 0.1 (buono)

**4. Usabilità Mobile**
- Vai su: **"Esperienza"** → **"Usabilità mobile"**
- Verifica: 0 errori di usabilità

**5. Dati Strutturati (Structured Data)**
- Vai su: **"Esperienza"** → **"Dati strutturati"**
- Verifica presenza di:
  - Organization schema
  - WebSite schema (con SearchAction)
  - Course schema (per ogni corso)
  - FAQPage schema (sulla pagina FAQ)

---

## 🛠️ 6. Troubleshooting

### Problema: "URL non indicizzato"

**Cause possibili**:
1. Sitemap non ancora elaborata → Aspetta 7 giorni
2. Robots.txt blocca il crawling → Verifica https://www.insightlearn.cloud/robots.txt
3. Contenuto duplicato → Usa tag canonical
4. Qualità contenuto bassa → Migliora testo e struttura

**Soluzione**:
1. Usa tool "Controllo URL" per capire il motivo
2. Leggi il messaggio di errore/avviso
3. Correggi il problema
4. Richiedi nuovamente l'indicizzazione

### Problema: "Server error (5xx)" in Coverage

**Causa**: API backend non risponde correttamente.

**Soluzione**:
1. Verifica che `/api/seo/sitemap.xml` sia accessibile
2. Controlla log backend per errori SQL
3. Verifica connection string database
4. Aumenta timeout nginx se necessario

### Problema: "Sitemap non trovata"

**Causa**: Percorso sitemap errato o file non servito correttamente.

**Soluzione**:
1. Verifica URL manualmente: https://www.insightlearn.cloud/api/seo/sitemap.xml
2. Controlla che nginx serva il file con Content-Type `application/xml`
3. Verifica che non ci sia redirect 301/302
4. Re-invia sitemap con percorso corretto

### Problema: "Crawl budget insufficiente"

**Causa**: Google non sta scansionando tutte le pagine del sito.

**Soluzione**:
1. Riduci numero URL in sitemap (massimo 500 prioritari)
2. Migliora velocità sito (PageSpeed > 90)
3. Elimina contenuto duplicato
4. Usa tag `<link rel="canonical">` su tutte le pagine

---

## 📈 Target Metriche (12 mesi)

Obiettivi SEO da raggiungere entro 12 mesi dall'implementazione:

| Metrica | Attuale (Mese 0) | Target 6 mesi | Target 12 mesi |
|---------|------------------|---------------|----------------|
| **Pagine indicizzate** | 0 | 300+ | 700+ |
| **Impressioni/mese** | 0 | 50K+ | 200K+ |
| **Clic organici/mese** | 0 | 2.5K+ | 15K+ |
| **CTR medio** | 0% | 5%+ | 7%+ |
| **Posizione media** | N/A | 25 | 15 |
| **Domain Authority** | ~5 | 25+ | 35+ |
| **Backlinks** | <100 | 1K+ | 6K+ |

---

## ✅ Checklist Implementazione

Prima di chiudere questo documento, verifica di aver completato tutti gli step:

### Verifica Iniziale
- [ ] Account Google Search Console creato
- [ ] Proprietà `https://www.insightlearn.cloud` aggiunta
- [ ] Meta tag verifica inserito in index.html (linea 55)
- [ ] Deploy completato e sito verificato

### Sitemap
- [ ] Sitemap dinamica accessibile: `/api/seo/sitemap.xml`
- [ ] Sitemap contiene 500+ URL (corsi + pagine statiche)
- [ ] Sitemap inviata a Google Search Console
- [ ] Status sitemap: "Riuscita" (dopo 1-7 giorni)

### Indexing Requests
- [ ] Homepage richiesta indicizzazione manuale
- [ ] /courses richiesta indicizzazione manuale
- [ ] /about richiesta indicizzazione manuale
- [ ] /faq richiesta indicizzazione manuale
- [ ] /contact richiesta indicizzazione manuale

### Configurazioni
- [ ] Google Analytics collegato (G-J972BQGNY7)
- [ ] Email notifications abilitate
- [ ] Target country configurato (o lasciato "Non impostato")

### Monitoraggio Setup
- [ ] Bookmark Google Search Console dashboard
- [ ] Calendario reminder: Check metriche ogni lunedì
- [ ] Alert configurati per errori critici

---

## 📚 Risorse Utili

**Google Docs Ufficiali**:
- [Centro assistenza Search Console](https://support.google.com/webmasters)
- [Guida SEO Starter](https://developers.google.com/search/docs/beginner/seo-starter-guide)
- [Core Web Vitals](https://web.dev/vitals/)

**Tools SEO**:
- Google Search Console: https://search.google.com/search-console
- Google PageSpeed Insights: https://pagespeed.web.dev
- Google Rich Results Test: https://search.google.com/test/rich-results
- Google Mobile-Friendly Test: https://search.google.com/test/mobile-friendly

**Documentazione InsightLearn**:
- [SEO Competitive Analysis](SEO-COMPETITIVE-ANALYSIS-2025-12-12.md)
- [SEO Optimization Guide](SEO_OPTIMIZATION_GUIDE.md)
- [Blazor SEO Implementation Examples](BLAZOR_SEO_IMPLEMENTATION_EXAMPLES.md)

---

**Ultimo Aggiornamento**: 2025-12-16
**Prossima Revisione**: 2026-01-16 (verifica metriche dopo 1 mese)
**Autore**: Claude Code (implementazione SEO v2.1.0-dev)
