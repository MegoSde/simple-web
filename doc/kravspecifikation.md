# Kravspecifikation – Læringsprojekt: Web Application i Produktionsmiljø

## Problemformulering
Hvordan kan man sætte en simpel hjemmeside i drift i et miljø, der ligner et produktionsmiljø, og samtidig sikre at både udvikling, infrastruktur og cybersikkerhed spiller sammen?
Løsningen skal give elever på hovedforløb 1 i datateknikeruddannelsen mulighed for at arbejde praktisk med deres respektive specialer (infrastruktur, programmering og cybersikkerhed) i et fælles projekt.

## Formål
Formålet er at give eleverne en realistisk øvelse, hvor de oplever:
- hvordan en simpel hjemmeside kan blive til et driftklart system,
- hvordan krav og tekniske valg dokumenteres og spores,
- hvordan man kan arbejde tværfagligt med udvikling, drift og sikkerhed,
- hvordan man implementerer funktionelle og ikke-funktionelle krav i praksis.

Projektet træner samarbejde, dokumentation, deployment, overvågning og sikkerhed, med fokus på at bygge en løsning, der ligner en professionel produktionsopsætning – men på et niveau, der er tilgængeligt for elever på H1.

## Kort beskrivelse af systemet
Systemet er en simpel hjemmeside, der:
- indeholder statiske sider med indhold,
- har en enkel søgefunktion,
- kan opdateres med nyt indhold af Marketing,
- understøtter logging, overvågning, backup og sikkerhedsbeskyttelse.

Systemet skal:
- kunne deployes med zero downtime,
- være optimeret til Core Web Vitals,
- have alle relevante sikkerhedsheaders,
- opnå 100 i alle Lighthouse-kategorier.

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

### 👤 Visitor - Se indhold (US-01)
**Som Visitor vil jeg kunne læse indhold på hjemmesiden, så jeg kan finde de informationer jeg har brug for.**

#### Use Case – Se indhold
- **VIS-FK-01-001:**** Visitor
- **VIS-FK-01-002:**** Sitet er tilgængeligt via et domæne (fx https://example.com)
- **VIS-FK-01-003:****
  1. Visitor åbner forsiden i en browser.
  2. Systemet returnerer indholdet (tekst, billeder).
  3. Visitor kan navigere til undersider via menu eller links.
- **VIS-FK-01-004:****
  - Hvis siden ikke findes → systemet returnerer en 404-fejlside.

#### Funktionelle krav
- **VIS-FK-01-005:**** Systemet skal præsentere en forside med indhold (tekst og billeder).
- **VIS-FK-01-006:**** Systemet skal understøtte navigering til undersider via menupunkter.
- **VIS-FK-01-007:**** Systemet skal returnere en brugervenlig 404-side, hvis indhold ikke findes.

#### Ikke-funktionelle krav
- **VIS-NF-01-008:**** Forsiden skal loade på under 1 sekund ved normal belastning.
- **VIS-NF-01-009:**** Alt indhold skal være tilgængeligt med gyldig HTML og CSS (WCAG + W3C-valideret).
- **VIS-NF-01-010:**** Core Web Vitals (LCP, CLS, FID) skal ligge inden for Google’s “Good” threshold.
- **VIS-NF-01-011:**** Hjemmesiden skal kun være tilgængelig via HTTPS.
- **VIS-NF-01-012:**** Hjemmesiden skal opnå en Lighthouse score på 100 i alle kategorier (Performance, Accessibility, Best Practices, SEO).

---

### 👤 Visitor – Søg på sitet (US-02)
**Som Visitor vil jeg kunne søge på hjemmesiden, så jeg hurtigt kan finde relevant indhold.**

#### Use Case – Søg indhold
- **VIS-FK-02-013:**** Visitor
- **VIS-FK-02-014:**** Sitet er tilgængeligt og indeholder sider med tekstindhold.
- **VIS-FK-02-015:****
  1. Visitor indtaster en søgetekst i søgefeltet.
  2. Systemet matcher forespørgslen mod indhold.
  3. Systemet returnerer en liste med søgeresultater.
  4. Visitor vælger et resultat og bliver sendt til den tilsvarende side.
- **VIS-FK-02-016:****
  - Hvis der ikke findes resultater → systemet viser en tom-resultat-side med forslag.

#### Funktionelle krav
- **VIS-FK-02-017:**** Systemet skal tilbyde et søgefelt på hjemmesiden.
- **VIS-FK-02-018:**** Systemet skal kunne returnere en liste med relevante søgeresultater baseret på indhold.
- **VIS-FK-02-019:**** Systemet skal give feedback, hvis søgningen ikke giver resultater.

#### Ikke-funktionelle krav
- **VIS-NF-02-020:**** Søgefunktionen skal returnere resultater på under 2 sekunder.
- **VIS-NF-02-021:**** Søgeresultater skal rangordnes efter relevans.
- **VIS-NF-02-022:**** Søgefunktionen må ikke afsløre interne fejl (fx SQL-fejl) i brugerfladen.
- **VIS-NF-02-023:**** Søgefunktionen skal være tilgængelig via HTTPS.
- **VIS-NF-02-024:**** Søgeresultat-siden skal bidrage til en samlet Lighthouse score på 100 i alle kategorier.

### 📈 Marketing – Opdatere indhold (US-03)
**Som Marketing-medarbejder vil jeg kunne opdatere indhold på hjemmesiden, så kampagner og information altid er aktuelle.**

#### Use Case – Opdatere indhold
- **MAR-FK-03-025:**** Marketing
- **MAR-FK-03-026:**** Marketing har adgang til et simpelt redigeringsværktøj eller en aftalt proces med Udvikler/Infrastruktur.
- **MAR-FK-03-027:****
  1. Marketing anmoder om at ændre tekst/billeder (via CMS, formular eller pull request-lignende proces).
  2. Systemet gør ændringerne tilgængelige i et *preview-miljø*.
  3. Marketing godkender ændringerne i preview.
  4. Systemet publicerer ændringerne i produktion med zero downtime.
- **MAR-FK-03-028:****
  - Hvis indholdet ikke kan valideres → systemet giver en fejlbesked.

#### Funktionelle krav
- **MAR-FK-03-029:**** Systemet skal understøtte opdatering af eksisterende tekstindhold.
- **MAR-FK-03-030:**** Systemet skal understøtte upload og visning af billeder.
- **MAR-FK-03-031:**** Systemet skal sikre, at ændringer kan ses i et *preview-miljø* inden publicering.
- **MAR-FK-03-032:**** Systemet skal publicere ændringer til hjemmesiden uden nedetid.

#### Ikke-funktionelle krav
- **MAR-NF-03-033:**** Udrulning af ændringer skal ske med zero downtime.
- **MAR-NF-03-034:**** Indholdsændringer skal kunne ses af Visitors senest 1 minut efter publicering.
- **MAR-NF-03-035:**** Preview-miljøet skal være isoleret fra produktion, men afspejle samme design og performance.
- **MAR-NF-03-036:**** Preview og publicering skal kun ske via HTTPS.
- **MAR-NF-03-037:**** Opdateringer skal ikke kompromittere performance eller Lighthouse score (100 i alle kategorier).

---

### 📈 Marketing – Se statistik (US-04)
**Som Marketing-medarbejder vil jeg kunne se statistik over besøg og brugeradfærd, så jeg kan vurdere effekten af kampagner.**

#### Use Case – Se statistik
- **MAR-FK-04-038:**** Marketing
- **MAR-FK-04-039:**** Sitet indsamler basis-analyse data (fx page views).
- **MAR-FK-04-040:****
  1. Marketing åbner statistikværktøjet.
  2. Systemet viser rapporter over sidevisninger og søgeadfærd.
  3. Marketing bruger informationen til at evaluere kampagner.
- **MAR-FK-04-041:****
  - Hvis data ikke er tilgængelige → systemet viser en fejlbesked og logger fejlen.

#### Funktionelle krav
- **MAR-FK-04-042:**** Systemet skal indsamle og gemme data om sidevisninger.
- **MAR-FK-04-043:**** Systemet skal vise simple rapporter (fx mest besøgte sider, hyppige søgeord).
- **MAR-FK-04-044:**** Systemet skal give mulighed for at filtrere rapporter efter periode (fx dag/uge/måned).

#### Ikke-funktionelle krav
- **MAR-NF-04-045:**** Statistik skal være tilgængelig uden at påvirke performance for Visitors.
- **MAR-NF-04-046:**** Statistikdata skal opdateres mindst én gang i timen.
- **MAR-NF-04-047:**** Statistikvisningen skal kun være tilgængelig via HTTPS og kræve autentifikation.
- **MAR-NF-04-048:**** Statistikmodulet skal ikke påvirke hjemmesidens Lighthouse score (100 i alle kategorier).

### 💻 Udvikler – Tilføje funktionalitet (US-05)
**Som Udvikler vil jeg kunne tilføje ny funktionalitet til hjemmesiden, så systemet kan udvikles og forbedres løbende.**

#### Use Case – Tilføje funktioner
- **DEV-FK-05-049:**** Udvikler
- **DEV-FK-05-050:**** Udvikleren arbejder i et versionsstyringssystem (fx Git).
- **DEV-FK-05-051:****
  1. Udvikler planlægger en ny funktion på Kanban/Scrum board.
  2. Tasken på boardet refererer til et konkret krav i kravspecifikationen.
  3. Udvikler opretter en ny gren (branch) i versionsstyring.
  4. Udvikler implementerer og tester funktionen i et testmiljø.
  5. E2E-tests og code review køres.
  6. Funktionen merges til hovedbranch, hvis tests og review er godkendt.
- **DEV-FK-05-052:****
  - Hvis test fejler → ændringen må ikke merges til hovedbranch.

#### Funktionelle krav
- **DEV-FK-05-053:**** Systemet skal bruge versionsstyring (fx Git) til alt kodearbejde.
- **DEV-FK-05-054:**** Systemet skal understøtte branches til udvikling og integration.
- **DEV-FK-05-055:**** Systemet skal have et Kanban- eller Scrum board til planlægning og opgavestyring.
- **DEV-FK-05-056:**** Alle tasks på Kanban/Scrum boardet skal kunne henføres til et eller flere krav i kravspecifikationen.
- **DEV-FK-05-057:**** Nye funktioner skal kunne testes i et separat testmiljø inden de udrulles.
- **DEV-FK-05-058:**** Systemet skal køre automatiserede E2E-tests på ændringer før produktion.

#### Ikke-funktionelle krav
- **DEV-NF-05-059:**** Alle commits skal følge en aftalt versionsstrategi (fx semantisk versionering).
- **DEV-NF-05-060:**** Udviklingsteamet skal følge en fælles Code of Conduct for samarbejde og kommunikation.
- **DEV-NF-05-061:**** Testmiljøet skal afspejle produktionen, så fejl kan opdages tidligt.
- **DEV-NF-05-062:**** E2E-tests skal fuldføres på under 5 minutter for at understøtte hurtig feedback.
- **DEV-NF-05-063:**** Versionshistorik skal bevares, så tidligere versioner altid kan gendannes.

### 💻🖥️ Udvikler & Infrastruktur – Dokumentere proces og overholdelse af krav (US-06)
**Som Udvikler/Infrastruktur-team vil vi kunne dokumentere vores arkitekturvalg og løbende opdatere et oversigtsdokument, så vi kan vise hvordan systemet opfylder de opstillede krav.**

#### Use Case – Dokumentation af proces og krav
- **DIN-FK-06-064:**** Udvikler + Infrastruktur
- **DIN-FK-06-065:**** Projektet har en fælles kravspecifikation.
- **DIN-FK-06-066:****
  1. Teamet træffer et teknisk eller arkitektonisk valg (fx valg af webserver, deploy-strategi).
  2. Valget dokumenteres kortfattet med begrundelse i et procesdokument (fx README eller wiki).
  3. Teamet markerer i kravspecifikationen, hvilke krav valget understøtter.
  4. Når krav ændres eller nye tilføjes, opdateres dokumentationen.
- **DIN-FK-06-067:****
  - Hvis et krav ikke kan opfyldes → teamet beskriver hvorfor, og foreslår en alternativ løsning.

#### Funktionelle krav
- **DIN-FK-06-068:**** Teamet skal dokumentere arkitekturvalg med begrundelser i et procesdokument.
- **DIN-FK-06-069:**** Teamet skal føre en oversigt over, hvilke krav der er opfyldt af systemet.
- **DIN-FK-06-070:**** Dokumentationen skal løbende opdateres, når systemet ændres.
- **DIN-FK-06-071:**** Dokumentationen skal være tilgængelig for alle aktører i projektet (f.eks. i Git-repo).

#### Ikke-funktionelle krav
- **DIN-NF-06-072:**** Dokumentationen skal være enkel, kortfattet og konsistent i struktur (max ½ side per beslutning).
- **DIN-NF-06-073:**** Oversigten over krav skal opdateres mindst én gang per iteration/sprint.
- **DIN-NF-06-074:**** Dokumentationen skal være versionsstyret (gemmes i Git).
- **DIN-NF-06-075:**** Processen for dokumentation må ikke forsinke udvikling/udrulning væsentligt (maks. 10 min. pr. arkitekturvalg).

### 🖥️ Infrastruktur – Deployment (US-07)
**Som Infrastruktur-ansvarlig vil jeg kunne deploye systemet med en strategi der sikrer zero downtime, så hjemmesiden altid er tilgængelig for brugerne.**

#### Use Case – Deployment
- **INF-FK-07-076:**** Infrastruktur
- **INF-FK-07-077:**** Systemet er klar til udrulning fra versionsstyring.
- **INF-FK-07-078:****
  1. Infrastruktur vælger en deploy-strategi (Blue/Green, Rolling, Canary).
  2. Deployment udføres uden nedetid.
  3. Hvis deployment fejler → rollback udføres automatisk.

#### Funktionelle krav
- **INF-FK-07-079:**** Systemet skal understøtte zero downtime deployment.
- **INF-FK-07-080:**** Deployment-processen skal have en rollback-mekanisme.

#### Ikke-funktionelle krav
- **INF-NF-07-081:**** Deployment skal kunne gennemføres på under 5 minutter.
- **INF-NF-07-082:**** Rollback skal kunne gennemføres på under 2 minutter.

---

### 🖥️ Infrastruktur – Overvågning (US-08)
**Som Infrastruktur-ansvarlig vil jeg kunne overvåge servere og services, så jeg hurtigt kan reagere på fejl eller nedbrud.**

#### Use Case – Overvågning
- **INF-FK-08-083:**** Infrastruktur
- **INF-FK-08-084:**** Systemet kører i produktion.
- **INF-FK-08-085:****
  1. Systemet opsamler data om servere og services (CPU, RAM, disk, netværk, svartider).
  2. Systemet genererer alarmer ved fejl eller nedbrud.
  3. Infrastruktur modtager og reagerer på alarmer.

#### Funktionelle krav
- **INF-FK-08-086:**** Systemet skal overvåge servere og services.
- **INF-FK-08-087:**** Systemet skal generere alarmer ved fejl, nedbrud eller ressourceoverskridelse.

#### Ikke-funktionelle krav
- **INF-NF-08-088:**** Overvågning skal ske med maks. 1 minuts forsinkelse.
- **INF-NF-08-089:**** Alarmer skal være tilgængelige for drift/SOC senest 30 sekunder efter fejl registreres.

---

### 🖥️ Infrastruktur – Backup (US-09)
**Som Infrastruktur-ansvarlig vil jeg kunne lave backup og gendanne systemet, så data og funktioner ikke går tabt ved fejl eller nedbrud.**

#### Use Case – Backup
- **INF-FK-09-090:**** Infrastruktur
- **INF-FK-09-091:**** Systemet er i drift med kode og data.
- **INF-FK-09-092:****
  1. Systemet tager automatiske backups af kode og data.
  2. Backups gemmes sikkert og kan testes.
  3. Systemet gendannes fra backup ved behov.

#### Funktionelle krav
- **INF-FK-09-093:**** Systemet skal tage regelmæssige backups af kode og data.
- **INF-FK-09-094:**** Systemet skal understøtte restore/gendannelse fra backup.
- **INF-FK-09-095:**** Backup-processen skal testes regelmæssigt.

#### Ikke-funktionelle krav
- **INF-NF-09-096:**** Backup skal tages mindst én gang i døgnet.
- **INF-NF-09-097:**** Backup skal testes mindst én gang om ugen.

---

### 🖥️ Infrastruktur – Dokumentation (US-10)
**Som Infrastruktur-ansvarlig vil jeg kunne dokumentere vores arkitektur og drift, så alle aktører har et fælles overblik over systemet.**

#### Use Case – Dokumentation
- **INF-FK-10-098:**** Infrastruktur
- **INF-FK-10-099:**** Projektet har en fælles kravspecifikation og versionsstyring.
- **INF-FK-10-100:****
  1. Infrastruktur planlægger drift- og vedligeholdelsesopgaver på Kanban board.
  2. Alle tasks linkes til kravspecifikationen.
  3. Infrastruktur udarbejder nødvendige diagrammer og beskrivelser.
  4. Dokumentationen gemmes versionsstyret i Git og opdateres løbende.

#### Funktionelle krav
- **INF-FK-10-101:**** Infrastruktur skal dokumentere netværksantologi (netværksdiagram).
- **INF-FK-10-102:**** Infrastruktur skal dokumentere komponent- og deployment-diagrammer.
- **INF-FK-10-103:**** Infrastruktur skal dokumentere driftprocesser (deployment, overvågning, backup).
- **INF-FK-10-104:**** Infrastruktur skal bruge et Kanban board til planlægning af tasks.
- **INF-FK-10-105:**** Alle infrastruktur-tasks på Kanban boardet skal kunne henføres til et eller flere krav i kravspecifikationen.

#### Ikke-funktionelle krav
- **INF-NF-10-106:**** Dokumentationen skal være enkel, kortfattet og opdateres løbende.
- **INF-NF-10-107:**** Dokumentationen skal være versionsstyret (fx i Git).
- **INF-NF-10-108:**** Dokumentationen må maks. tage 10 min. at opdatere pr. ændring.

### 🛡️ SOC – Logindsamling (US-11)
**Som SOC-ansvarlig vil jeg kunne indsamle og gemme logs fra systemet, så jeg kan opdage og analysere sikkerhedshændelser.**

#### Use Case – Logindsamling
- **SOC-FK-11-109:**** SOC
- **SOC-FK-11-110:**** Systemet er i drift og genererer logs.
- **SOC-FK-11-111:****
  1. SOC konfigurerer central logindsamling (fx webserver-, applikations- og systemlogs).
  2. Logs sendes til et sikkert centralt logsystem.
  3. Logs gemmes i minimum 30 dage.
  4. SOC kan søge i og filtrere logs.

#### Funktionelle krav
- **SOC-FK-11-112:**** Systemet skal indsamle logs fra servere, services og applikationen.
- **SOC-FK-11-113:**** Logs skal sendes til et centralt system.
- **SOC-FK-11-114:**** Logs skal gemmes i minimum 30 dage.

#### Ikke-funktionelle krav
- **SOC-NF-11-115:**** Logs skal overføres krypteret.
- **SOC-NF-11-116:**** Logs skal være søgbare inden for 1 minut efter de er oprettet.

---

### 🛡️ SOC – Alarmhåndtering (US-12)
**Som SOC-ansvarlig vil jeg kunne modtage og håndtere alarmer, så jeg hurtigt kan reagere på sikkerhedshændelser.**

#### Use Case – Alarmhåndtering
- **SOC-FK-12-117:**** SOC
- **SOC-FK-12-118:**** Logindsamling og overvågning er opsat.
- **SOC-FK-12-119:****
  1. Systemet genererer en alarm (fx gentagne loginforsøg, DoS, XSS-forsøg).
  2. Alarmen sendes til SOC.
  3. SOC vurderer alarmen og kategoriserer den (fx kritisk, høj, middel, lav).
  4. SOC eskalerer hændelsen efter procedurer.

#### Funktionelle krav
- **SOC-FK-12-120:**** Systemet skal generere alarmer baseret på definerede sikkerhedsmønstre.
- **SOC-FK-12-121:**** Alarmer skal kategoriseres efter alvorlighed.

#### Ikke-funktionelle krav
- **SOC-NF-12-122:**** Alarmer skal være tilgængelige for SOC senest 30 sekunder efter registrering.
- **SOC-NF-12-123:**** Alarmer må ikke overses (skal logges centralt og markeres som “behandlet”).

---

### 🛡️ SOC – Incident Response (US-13)
**Som SOC-ansvarlig vil jeg kunne reagere på sikkerhedshændelser, så systemet hurtigt kan sikres og gendannes.**

#### Use Case – Incident Response
- **SOC-FK-13-124:**** SOC
- **SOC-FK-13-125:**** En alarm er registreret.
- **SOC-FK-13-126:****
  1. SOC identificerer hændelsen via alarmer og logs.
  2. SOC aktiverer en responsprocedure (fx blokering af IP, nedlukning af service).
  3. SOC dokumenterer hændelsen og tiltag.
  4. Systemet gendannes til normal drift.

#### Funktionelle krav
- **SOC-FK-13-127:**** SOC skal kunne iværksætte afværgeforanstaltninger (fx blokering via WAF/firewall).
- **SOC-FK-13-128:**** SOC skal dokumentere hændelser og respons.

#### Ikke-funktionelle krav
- **SOC-NF-13-129:**** Incident response skal iværksættes inden for 5 minutter ved kritiske hændelser.
- **SOC-NF-13-130:**** Hændelsesrapport skal være tilgængelig senest 24 timer efter hændelsen.

---

### 🛡️ SOC – Rapportering (US-14)
**Som SOC-ansvarlig vil jeg kunne udarbejde rapporter over sikkerhedshændelser, så organisationen kan evaluere og forbedre sikkerheden.**

#### Use Case – Rapportering
- **SOC-FK-14-131:**** SOC
- **SOC-FK-14-132:**** Logs og hændelsesdata er gemt.
- **SOC-FK-14-133:****
  1. SOC genererer en rapport (fx ugentlig/månedlig).
  2. Rapporten opsummerer alarmer, hændelser og respons.
  3. Rapporten deles med Drift og Ledelse.

#### Funktionelle krav
- **SOC-FK-14-134:**** Systemet skal understøtte generering af rapporter baseret på logs og hændelser.
- **SOC-FK-14-135:**** Rapporten skal inkludere antal hændelser, kategorisering og respons.

#### Ikke-funktionelle krav
- **SOC-NF-14-136:**** Rapporten skal kunne genereres automatisk.
- **SOC-NF-14-137:**** Rapporten skal udarbejdes mindst én gang pr. måned.

### 💀 Hacker – SQL Injection (US-15)
**Som Hacker vil jeg forsøge at manipulere med søgefunktionen via SQL injection, så jeg kan få adgang til data, jeg ikke burde se.**

#### Use Case – SQL Injection
- **HAK-FK-15-138:**** Hacker
- **HAK-FK-15-139:**** Systemet har en søgefunktion.
- **HAK-FK-15-140:****
  1. Hacker indtaster ondsindet SQL-kode i søgefeltet.
  2. Systemet sender input videre til databasen.
  3. Hacker får adgang til data eller ændrer indhold.

#### Funktionelle anti-krav
- **HAK-FK-15-141:**** Hacker skal ikke kunne ændre eller tilgå data via SQL injection.

#### Ikke-funktionelle anti-krav
- **HAK-NF-15-142:**** Alle database-forespørgsler skal parameteriseres og valideres.
- **HAK-NF-15-143:**** Fejlmeddelelser må ikke afsløre databaseinformation.

---

### 💀 Hacker – XSS (Cross-Site Scripting) (US-16)
**Som Hacker vil jeg forsøge at indsætte ondsindet JavaScript i indhold eller søgefelter, så det afvikles hos andre brugere.**

#### Use Case – XSS
- **HAK-FK-16-144:**** Hacker
- **HAK-FK-16-145:**** Systemet viser brugerinput eller søgeresultater.
- **HAK-FK-16-146:****
  1. Hacker indtaster JavaScript i inputfelt.
  2. Systemet viser input uden korrekt escaping.
  3. Andre brugeres browser afvikler koden.

#### Funktionelle anti-krav
- **HAK-FK-16-147:**** Hacker skal ikke kunne afvikle scripts i andre brugeres browser via XSS.

#### Ikke-funktionelle anti-krav
- **HAK-NF-16-148:**** Alt brugerinput skal valideres og escapes korrekt.
- **HAK-NF-16-149:**** Systemet skal have en Content-Security-Policy (CSP), der forhindrer indlæsning af uautoriseret JavaScript.

---

### 💀 Hacker – DDoS (Distributed Denial of Service) (US-17)
**Som Hacker vil jeg forsøge at overbelaste hjemmesiden med mange forespørgsler, så den ikke er tilgængelig for almindelige brugere.**

#### Use Case – DDoS
- **HAK-FK-17-150:**** Hacker
- **HAK-FK-17-151:**** Systemet er offentligt tilgængeligt på internettet.
- **HAK-FK-17-152:****
  1. Hacker sender tusindvis af forespørgsler til systemet.
  2. Systemets ressourcer bliver overbelastet.
  3. Almindelige brugere oplever nedetid eller langsomme svartider.

#### Funktionelle anti-krav
- **HAK-FK-17-153:**** Hacker skal ikke kunne forhindre almindelige brugere i at tilgå hjemmesiden via DDoS.

#### Ikke-funktionelle anti-krav
- **HAK-NF-17-154:**** Systemet skal understøtte rate limiting og request filtering.
- **HAK-NF-17-155:**** Systemet skal være beskyttet bag en WAF eller loadbalancer, der kan mitigere simple DoS-forsøg.
