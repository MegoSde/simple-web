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

### 💻🖥️ Udvikler & Infrastruktur – Dokumentere proces og overholdelse af krav
**Som Udvikler/Infrastruktur-team vil vi kunne dokumentere vores arkitekturvalg og løbende opdatere et oversigtsdokument, så vi kan vise hvordan systemet opfylder de opstillede krav.**

#### Use Case – Dokumentation af proces og krav
- **Aktører:** Udvikler + Infrastruktur  
- **Forudsætning:** Projektet har en fælles kravspecifikation.  
- **Hovedforløb:**
  1. Teamet træffer et teknisk eller arkitektonisk valg (fx valg af webserver, deploy-strategi).  
  2. Valget dokumenteres kortfattet med begrundelse i et procesdokument (fx README eller wiki).  
  3. Teamet markerer i kravspecifikationen, hvilke krav valget understøtter.  
  4. Når krav ændres eller nye tilføjes, opdateres dokumentationen.  
- **Udvidelser:**
  - Hvis et krav ikke kan opfyldes → teamet beskriver hvorfor, og foreslår en alternativ løsning.  

#### Funktionelle krav
- **UI-FK-01:** Teamet skal dokumentere arkitekturvalg med begrundelser i et procesdokument.  
- **UI-FK-02:** Teamet skal føre en oversigt over, hvilke krav der er opfyldt af systemet.  
- **UI-FK-03:** Dokumentationen skal løbende opdateres, når systemet ændres.  
- **UI-FK-04:** Dokumentationen skal være tilgængelig for alle aktører i projektet (f.eks. i Git-repo).  

#### Ikke-funktionelle krav
- **UI-NF-01:** Dokumentationen skal være enkel, kortfattet og konsistent i struktur (max ½ side per beslutning).  
- **UI-NF-02:** Oversigten over krav skal opdateres mindst én gang per iteration/sprint.  
- **UI-NF-03:** Dokumentationen skal være versionsstyret (gemmes i Git).  
- **UI-NF-04:** Processen for dokumentation må ikke forsinke udvikling/udrulning væsentligt (maks. 10 min. pr. arkitekturvalg).

### 🖥️ Infrastruktur – Deployment
**Som Infrastruktur-ansvarlig vil jeg kunne deploye systemet med en strategi der sikrer zero downtime, så hjemmesiden altid er tilgængelig for brugerne.**

#### Use Case – Deployment
- **Aktør:** Infrastruktur  
- **Forudsætning:** Systemet er klar til udrulning fra versionsstyring.  
- **Hovedforløb:**
  1. Infrastruktur vælger en deploy-strategi (Blue/Green, Rolling, Canary).  
  2. Deployment udføres uden nedetid.  
  3. Hvis deployment fejler → rollback udføres automatisk.  

#### Funktionelle krav
- **IN-FK-01:** Systemet skal understøtte zero downtime deployment.  
- **IN-FK-02:** Deployment-processen skal have en rollback-mekanisme.  

#### Ikke-funktionelle krav
- **IN-NF-01:** Deployment skal kunne gennemføres på under 5 minutter.  
- **IN-NF-02:** Rollback skal kunne gennemføres på under 2 minutter.  

---

### 🖥️ Infrastruktur – Overvågning
**Som Infrastruktur-ansvarlig vil jeg kunne overvåge servere og services, så jeg hurtigt kan reagere på fejl eller nedbrud.**

#### Use Case – Overvågning
- **Aktør:** Infrastruktur  
- **Forudsætning:** Systemet kører i produktion.  
- **Hovedforløb:**
  1. Systemet opsamler data om servere og services (CPU, RAM, disk, netværk, svartider).  
  2. Systemet genererer alarmer ved fejl eller nedbrud.  
  3. Infrastruktur modtager og reagerer på alarmer.  

#### Funktionelle krav
- **IN-FK-03:** Systemet skal overvåge servere og services.  
- **IN-FK-04:** Systemet skal generere alarmer ved fejl, nedbrud eller ressourceoverskridelse.  

#### Ikke-funktionelle krav
- **IN-NF-03:** Overvågning skal ske med maks. 1 minuts forsinkelse.  
- **IN-NF-04:** Alarmer skal være tilgængelige for drift/SOC senest 30 sekunder efter fejl registreres.  

---

### 🖥️ Infrastruktur – Backup
**Som Infrastruktur-ansvarlig vil jeg kunne lave backup og gendanne systemet, så data og funktioner ikke går tabt ved fejl eller nedbrud.**

#### Use Case – Backup
- **Aktør:** Infrastruktur  
- **Forudsætning:** Systemet er i drift med kode og data.  
- **Hovedforløb:**
  1. Systemet tager automatiske backups af kode og data.  
  2. Backups gemmes sikkert og kan testes.  
  3. Systemet gendannes fra backup ved behov.  

#### Funktionelle krav
- **IN-FK-05:** Systemet skal tage regelmæssige backups af kode og data.  
- **IN-FK-06:** Systemet skal understøtte restore/gendannelse fra backup.  
- **IN-FK-07:** Backup-processen skal testes regelmæssigt.  

#### Ikke-funktionelle krav
- **IN-NF-05:** Backup skal tages mindst én gang i døgnet.  
- **IN-NF-06:** Backup skal testes mindst én gang om ugen.  

---

### 🖥️ Infrastruktur – Dokumentation
**Som Infrastruktur-ansvarlig vil jeg kunne dokumentere vores arkitektur og drift, så alle aktører har et fælles overblik over systemet.**

#### Use Case – Dokumentation
- **Aktør:** Infrastruktur  
- **Forudsætning:** Projektet har en fælles kravspecifikation og versionsstyring.  
- **Hovedforløb:**
  1. Infrastruktur planlægger drift- og vedligeholdelsesopgaver på Kanban board.  
  2. Alle tasks linkes til kravspecifikationen.  
  3. Infrastruktur udarbejder nødvendige diagrammer og beskrivelser.  
  4. Dokumentationen gemmes versionsstyret i Git og opdateres løbende.  

#### Funktionelle krav
- **IN-FK-08:** Infrastruktur skal dokumentere netværksantologi (netværksdiagram).  
- **IN-FK-09:** Infrastruktur skal dokumentere komponent- og deployment-diagrammer.  
- **IN-FK-10:** Infrastruktur skal dokumentere driftprocesser (deployment, overvågning, backup).  
- **IN-FK-11:** Infrastruktur skal bruge et Kanban board til planlægning af tasks.  
- **IN-FK-12:** Alle infrastruktur-tasks på Kanban boardet skal kunne henføres til et eller flere krav i kravspecifikationen.  

#### Ikke-funktionelle krav
- **IN-NF-07:** Dokumentationen skal være enkel, kortfattet og opdateres løbende.  
- **IN-NF-08:** Dokumentationen skal være versionsstyret (fx i Git).  
- **IN-NF-09:** Dokumentationen må maks. tage 10 min. at opdatere pr. ændring.  

