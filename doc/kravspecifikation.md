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

### 📈 Marketing – Opdatere indhold
**Som Marketing-medarbejder vil jeg kunne opdatere indhold på hjemmesiden, så kampagner og information altid er aktuelle.**

#### Use Case – Opdatere indhold
- **Aktør:** Marketing  
- **Forudsætning:** Marketing har adgang til et simpelt redigeringsværktøj eller en aftalt proces med Udvikler/Infrastruktur.  
- **Hovedforløb:**
  1. Marketing anmoder om at ændre tekst/billeder (via CMS, formular eller pull request-lignende proces).  
  2. Systemet gør ændringerne tilgængelige i et *preview-miljø*.  
  3. Marketing godkender ændringerne i preview.  
  4. Systemet publicerer ændringerne i produktion med zero downtime.  
- **Udvidelser:**
  - Hvis indholdet ikke kan valideres → systemet giver en fejlbesked.  

#### Funktionelle krav
- **MA-FK-01:** Systemet skal understøtte opdatering af eksisterende tekstindhold.  
- **MA-FK-02:** Systemet skal understøtte upload og visning af billeder.  
- **MA-FK-03:** Systemet skal sikre, at ændringer kan ses i et *preview-miljø* inden publicering.  
- **MA-FK-04:** Systemet skal publicere ændringer til hjemmesiden uden nedetid.  

#### Ikke-funktionelle krav
- **MA-NF-01:** Udrulning af ændringer skal ske med zero downtime.  
- **MA-NF-02:** Indholdsændringer skal kunne ses af Visitors senest 1 minut efter publicering.  
- **MA-NF-03:** Preview-miljøet skal være isoleret fra produktion, men afspejle samme design og performance.  
- **MA-NF-04:** Preview og publicering skal kun ske via HTTPS.  
- **MA-NF-05:** Opdateringer skal ikke kompromittere performance eller Lighthouse score (100 i alle kategorier).  

---

### 📈 Marketing – Se statistik
**Som Marketing-medarbejder vil jeg kunne se statistik over besøg og brugeradfærd, så jeg kan vurdere effekten af kampagner.**

#### Use Case – Se statistik
- **Aktør:** Marketing  
- **Forudsætning:** Sitet indsamler basis-analyse data (fx page views).  
- **Hovedforløb:**
  1. Marketing åbner statistikværktøjet.  
  2. Systemet viser rapporter over sidevisninger og søgeadfærd.  
  3. Marketing bruger informationen til at evaluere kampagner.  
- **Udvidelser:**
  - Hvis data ikke er tilgængelige → systemet viser en fejlbesked og logger fejlen.  

#### Funktionelle krav
- **MA-FK-05:** Systemet skal indsamle og gemme data om sidevisninger.  
- **MA-FK-06:** Systemet skal vise simple rapporter (fx mest besøgte sider, hyppige søgeord).  
- **MA-FK-07:** Systemet skal give mulighed for at filtrere rapporter efter periode (fx dag/uge/måned).  

#### Ikke-funktionelle krav
- **MA-NF-06:** Statistik skal være tilgængelig uden at påvirke performance for Visitors.  
- **MA-NF-07:** Statistikdata skal opdateres mindst én gang i timen.  
- **MA-NF-08:** Statistikvisningen skal kun være tilgængelig via HTTPS og kræve autentifikation.  
- **MA-NF-09:** Statistikmodulet skal ikke påvirke hjemmesidens Lighthouse score (100 i alle kategorier).  

### 💻 Udvikler – Tilføje funktionalitet
**Som Udvikler vil jeg kunne tilføje ny funktionalitet til hjemmesiden, så systemet kan udvikles og forbedres løbende.**

#### Use Case – Tilføje funktioner
- **Aktør:** Udvikler  
- **Forudsætning:** Udvikleren arbejder i et versionsstyringssystem (fx Git).  
- **Hovedforløb:**
  1. Udvikler planlægger en ny funktion på Kanban/Scrum board.  
  2. Tasken på boardet refererer til et konkret krav i kravspecifikationen.  
  3. Udvikler opretter en ny gren (branch) i versionsstyring.  
  4. Udvikler implementerer og tester funktionen i et testmiljø.  
  5. E2E-tests og code review køres.  
  6. Funktionen merges til hovedbranch, hvis tests og review er godkendt.  
- **Udvidelser:**
  - Hvis test fejler → ændringen må ikke merges til hovedbranch.  

#### Funktionelle krav
- **UD-FK-01:** Systemet skal bruge versionsstyring (fx Git) til alt kodearbejde.  
- **UD-FK-02:** Systemet skal understøtte branches til udvikling og integration.  
- **UD-FK-03:** Systemet skal have et Kanban- eller Scrum board til planlægning og opgavestyring.  
- **UD-FK-04:** Alle tasks på Kanban/Scrum boardet skal kunne henføres til et eller flere krav i kravspecifikationen.  
- **UD-FK-05:** Nye funktioner skal kunne testes i et separat testmiljø inden de udrulles.  
- **UD-FK-06:** Systemet skal køre automatiserede E2E-tests på ændringer før produktion.  

#### Ikke-funktionelle krav
- **UD-NF-01:** Alle commits skal følge en aftalt versionsstrategi (fx semantisk versionering).  
- **UD-NF-02:** Udviklingsteamet skal følge en fælles Code of Conduct for samarbejde og kommunikation.  
- **UD-NF-03:** Testmiljøet skal afspejle produktionen, så fejl kan opdages tidligt.  
- **UD-NF-04:** E2E-tests skal fuldføres på under 5 minutter for at understøtte hurtig feedback.  
- **UD-NF-05:** Versionshistorik skal bevares, så tidligere versioner altid kan gendannes.  
