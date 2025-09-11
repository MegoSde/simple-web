# Kravspecifikation v1.0

## Aktører
```mermaid
graph TB
    subgraph System
        S[Web Application]
    end

    %% Aktører
    V([👤 Visitor]) -->|Læser indhold, søger| S
    M([📈 Marketing]) -->|Opdaterer indhold, Se statistik| S
    U([💻 Udvikler]) -->|Udvikler funktioner & tests| S
    D([🖥️ Infrastruktur]) -->|Infrastruktur, Deployment, overvågning, CI/CD| S
    SOC([🛡️ SOC]) -->|Overvåger logs & sikkerhedshændelser| S

    %% Anti-aktør
    H([💀 Hacker]) -.->|Forsøger angreb: SQLi, DDoS, XSS| S
    SOC -.->|Opdager & Mitigerer| H

```

## User Stories, Use Cases og Krav

### 👤 Visitor - Se indhold
**Som Visitor vil jeg kunne læse indhold på hjemmesiden, så jeg kan finde de informationer jeg har brug for.**

#### Use Case – Se indhold
- **Aktør:** Visitor  
- **Forudsætning:** Sitet er tilgængeligt via et domæne (fx https://example.com)  
- **Hovedforløb:**
  1. Visitor åbner forsiden i en browser.  
  2. Systemet returnerer indholdet (tekst, billeder).  
  3. Visitor kan navigere til undersider via menu eller links.  
- **Udvidelser:**
  - Hvis siden ikke findes → systemet returnerer en 404-fejlside.  

#### Funktionelle krav
- **VI-FK-01:** Systemet skal præsentere en forside med indhold (tekst og billeder).  
- **VI-FK-02:** Systemet skal understøtte navigering til undersider via menupunkter.  
- **VI-FK-03:** Systemet skal returnere en brugervenlig 404-side, hvis indhold ikke findes.  

#### Ikke-funktionelle krav
- **VI-NF-01:** Forsiden skal loade på under 1 sekund ved normal belastning.  
- **VI-NF-02:** Alt indhold skal være tilgængeligt med gyldig HTML og CSS (WCAG + W3C-valideret).  
- **VI-NF-03:** Core Web Vitals (LCP, CLS, FID) skal ligge inden for Google’s “Good” threshold.  
- **VI-NF-04:** Hjemmesiden skal kun være tilgængelig via HTTPS.  
- **VI-NF-05:** Hjemmesiden skal opnå en Lighthouse score på 100 i alle kategorier (Performance, Accessibility, Best Practices, SEO).  

---

### 👤 Visitor – Søg på sitet
**Som Visitor vil jeg kunne søge på hjemmesiden, så jeg hurtigt kan finde relevant indhold.**

#### Use Case – Søg indhold
- **Aktør:** Visitor  
- **Forudsætning:** Sitet er tilgængeligt og indeholder sider med tekstindhold.  
- **Hovedforløb:**
  1. Visitor indtaster en søgetekst i søgefeltet.  
  2. Systemet matcher forespørgslen mod indhold.  
  3. Systemet returnerer en liste med søgeresultater.  
  4. Visitor vælger et resultat og bliver sendt til den tilsvarende side.  
- **Udvidelser:**
  - Hvis der ikke findes resultater → systemet viser en tom-resultat-side med forslag.  

#### Funktionelle krav
- **VI-FK-04:** Systemet skal tilbyde et søgefelt på hjemmesiden.  
- **VI-FK-05:** Systemet skal kunne returnere en liste med relevante søgeresultater baseret på indhold.  
- **VI-FK-06:** Systemet skal give feedback, hvis søgningen ikke giver resultater.  

#### Ikke-funktionelle krav
- **VI-NF-06:** Søgefunktionen skal returnere resultater på under 2 sekunder.  
- **VI-NF-07:** Søgeresultater skal rangordnes efter relevans.  
- **VI-NF-08:** Søgefunktionen må ikke afsløre interne fejl (fx SQL-fejl) i brugerfladen.  
- **VI-NF-09:** Søgefunktionen skal være tilgængelig via HTTPS.  
- **VI-NF-10:** Søgeresultat-siden skal bidrage til en samlet Lighthouse score på 100 i alle kategorier.  
