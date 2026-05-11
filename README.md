# OS Scheduling Simulator - Unit Testing Project

## Descriere generala

Acest proiect reprezinta un simulator simplificat de sistem de operare, dezvoltat pentru materia **Calitatea Sistemelor Software**.

Aplicatia simuleaza executia unor procese utilizator pe unul sau mai multe procesoare, folosind un algoritm de planificare **Round-Robin preventiv**. Sistemul include si o simulare simplificata a memoriei virtuale, unde procesele pot fi incarcate in RAM sau mutate pe disc in functie de memoria disponibila.

Proiectul a fost dezvoltat in doua faze:

1. **Faza 1 - dezvoltarea aplicatiei**
   - implementarea simulatorului;
   - citirea configuratiei din fisier;
   - executia proceselor pe procesoare;
   - gestionarea memoriei;
   - generarea logului de simulare;
   - afisarea grafica a executiei printr-un grafic Gantt.

2. **Faza 2 - unit testing**
   - testarea unitara a claselor dezvoltate in faza 1;
   - folosirea de mock-uri pentru izolarea modulelor;
   - generarea raportului de code coverage.

---

## Tehnologii folosite

Proiectul este dezvoltat in **C#** folosind platforma **.NET**.

Pentru interfata grafica a fost folosita biblioteca **Avalonia UI**.

Pentru testare au fost folosite urmatoarele instrumente:

- **xUnit** - framework pentru testare unitara;
- **Moq** - framework pentru mocking;
- **coverlet.collector** - colectare code coverage;
- **ReportGenerator** - generare raport HTML pentru coverage.

---

## Structura proiectului

Structura principala a solutiei este:

```text
Calitatea Sistemelor Software
|
|-- OS.sln
|
|-- OS
|   |-- Core
|   |   |-- Activity.cs
|   |   |-- IMemoryManager.cs
|   |   |-- MemoryManager.cs
|   |   |-- Models.cs
|   |   |-- Process.cs
|   |   |-- Processor.cs
|   |   |-- Scheduler.cs
|   |
|   |-- ViewModels
|   |   |-- MainViewModel.cs
|   |
|   |-- Views
|   |   |-- MainWindow.axaml
|   |   |-- MainWindow.axaml.cs
|   |
|   |-- App.axaml
|   |-- Program.cs
|   |-- input.txt
|   |-- simulation_log.txt
|   |-- OS.csproj
|
|-- OS.Tests
|   |-- Core
|   |   |-- ActivityTests.cs
|   |   |-- MemoryManagerTests.cs
|   |   |-- ProcessTests.cs
|   |   |-- ProcessorTests.cs
|   |   |-- SchedulerTests.cs
|   |   |-- SimulationEventTests.cs
|   |
|   |-- ViewModels
|   |   |-- GanttSegmentTests.cs
|   |   |-- MainWindowViewModelTests.cs
|   |
|   |-- OS.Tests.csproj
|
|-- coveragereport
|   |-- index.html
```

---

## Descrierea modulelor principale

### Activity

Clasa `Activity` descrie o activitate a unui proces.

O activitate poate fi de doua tipuri:

```csharp
Execution
SysCall
```

Fiecare activitate are:

- `Type` - tipul activitatii;
- `Duration` - durata activitatii.

Aceasta clasa este folosita de procese pentru a sti ce trebuie sa execute.

---

### Process

Clasa `Process` reprezinta un proces utilizator.

Un proces contine:

- `Id` - identificatorul procesului;
- `MemoryRequired` - memoria necesara;
- `ReleaseTime` - momentul in care procesul apare in sistem;
- `LastProcessorId` - ultimul procesor pe care a rulat;
- `CurrentStatus` - starea curenta a procesului;
- `Activities` - coada de activitati;
- `RemainingTimeInActivity` - timpul ramas din activitatea curenta;
- `ReadyAtTick` - momentul de timp de la care procesul poate rula dupa incarcarea in RAM.

Starile posibile ale procesului sunt:

```csharp
OnDisk
Ready
Running
Blocked
Finished
```

---

### Processor

Clasa `Processor` reprezinta un procesor al sistemului.

Un procesor are:

- `Id` - identificatorul procesorului;
- `CurrentProcess` - procesul care ruleaza pe acel procesor;
- `TimeSpentInSlice` - timpul petrecut de proces in cuanta curenta.

Proprietatea:

```csharp
IsFree
```

indica daca procesorul este liber.

---

### MemoryManager

Clasa `MemoryManager` gestioneaza memoria RAM simulata.

Responsabilitati principale:

- calculeaza memoria libera;
- verifica daca un proces este deja incarcat in RAM;
- incarca un proces in RAM;
- elimina procese din RAM cand memoria este insuficienta;
- calculeaza timpul de transfer intre disc si RAM.

Metoda principala este:

```csharp
EnsureInRam(Process p)
```

Aceasta metoda adauga procesul in RAM daca nu exista deja si returneaza intarzierea de transfer.

---

### IMemoryManager

Interfata `IMemoryManager` a fost introdusa pentru a permite testarea izolata a clasei `Scheduler`.

Prin aceasta interfata, `Scheduler` nu mai depinde direct de implementarea concreta `MemoryManager`, ci de o abstractizare. Astfel, in testele unitare poate fi folosit un mock.

Exemplu:

```csharp
var memoryMock = new Mock<IMemoryManager>();
```

Aceasta modificare nu schimba logica aplicatiei, ci doar face codul mai usor de testat.

---

### Scheduler

Clasa `Scheduler` este componenta principala a simulatorului.

Responsabilitati:

- gestioneaza timpul curent al simularii;
- lanseaza procesele la momentul potrivit;
- planifica procesele pe procesoare;
- aplica algoritmul Round-Robin;
- trateaza preemptia proceselor;
- genereaza evenimentele de simulare;
- interactioneaza cu managerul de memorie;
- simuleaza procesul de sistem periodic.

Metoda principala este:

```csharp
Ticks()
```

La fiecare tick, schedulerul:

1. verifica daca trebuie lansat procesul de sistem;
2. muta procesele lansate in coada ready;
3. executa procesele aflate pe procesoare;
4. verifica finalizarea sau preemptia proceselor;
5. planifica procesele ready pe procesoarele libere;
6. avanseaza timpul global al simularii.

---

### SimulationEvent

`SimulationEvent` este un record care descrie un eveniment aparut in simulare.

Contine:

- `Time` - momentul evenimentului;
- `ProcessId` - procesul implicat, daca exista;
- `ProcessorId` - procesorul implicat, daca exista;
- `Action` - actiunea efectuata;
- `Duration` - durata actiunii.

Exemple de actiuni:

```text
SCHEDULED
EXECUTING
PREEMPTED
FINISHED
WAITING_FOR_MEMORY
SYSTEM_CALL_HANDLING
MEMORY_TRANSFER
SWITCH_ACTIVITY
```

---

### MainWindowViewModel

`MainWindowViewModel` face legatura intre logica simulatorului si interfata grafica.

Responsabilitati:

- incarca fisierul de configurare;
- porneste simularea pas cu pas;
- actualizeaza timpul curent;
- actualizeaza folosirea RAM;
- actualizeaza incarcarea CPU;
- genereaza segmentele pentru graficul Gantt;
- afiseaza evenimentele;
- exporta logul simularii.

Nu au fost testate componentele grafice Avalonia, ci doar logica proprie din ViewModel.

---

## Formatul fisierului de intrare

Fisierul `input.txt` contine parametrii simularii si lista de procese.

Structura generala este:

```text
numProcessors ramSize diskRate timeSlice systemPeriod processData...
```

Unde:

- `numProcessors` - numarul de procesoare;
- `ramSize` - cantitatea de memorie RAM;
- `diskRate` - rata de transfer disc/RAM, introdusa ca intreg si impartita la 10 in cod;
- `timeSlice` - cuanta Round-Robin;
- `systemPeriod` - perioada procesului de sistem.

Pentru fiecare proces se citesc:

```text
pid releaseTime memoryRequired numberOfActivities activityType duration ...
```

Unde:

- `pid` - identificatorul procesului;
- `releaseTime` - momentul lansarii;
- `memoryRequired` - memoria ceruta;
- `numberOfActivities` - numarul de activitati;
- `activityType` - tipul activitatii;
- `duration` - durata activitatii.

In implementarea actuala:

```text
activityType = 1 -> Execution
activityType != 1 -> SysCall
```

---

## Algoritmul Round-Robin

Round-Robin este algoritmul de planificare folosit pentru procesele utilizator.

Fiecare proces primeste o cuanta de timp pe procesor. Daca procesul nu termina executia in acea cuanta, este preemptat si pus inapoi in coada ready.

Exemplu:

```text
Cuanta = 5
Procesul P1 are nevoie de 12 unitati de timp
```

Executia poate fi impartita astfel:

```text
P1 ruleaza 5 unitati
P1 este preemptat
P1 ruleaza inca 5 unitati
P1 este preemptat
P1 ruleaza ultimele 2 unitati
P1 se termina
```

In simulator, preemptia este marcata prin evenimentul:

```text
PREEMPTED
```

---

## Managementul memoriei

Un proces poate fi executat doar daca este incarcat in RAM.

Daca procesul nu este in RAM, `MemoryManager` il incarca si calculeaza timpul de transfer.

Timpul de transfer este calculat folosind:

```text
memoryRequired * diskTransferRate
```

Daca nu exista destula memorie libera, `MemoryManager` elimina procese existente din RAM.

Observatie: cerinta initiala mentioneaza politica LRU, dar implementarea actuala elimina primul proces din lista `RamProcesses`. Acest comportament este mai apropiat de FIFO decat de LRU real.

---

## Testare unitara

Pentru faza 2 au fost create teste unitare intr-un proiect separat:

```text
OS.Tests
```

Framework folosit:

```text
xUnit
```

Pentru mocking a fost folosit:

```text
Moq
```

Pentru coverage au fost folosite:

```text
coverlet.collector
ReportGenerator
```

---

## Clase testate

Au fost testate urmatoarele clase:

```text
OS.Core.Activity
OS.Core.Process
OS.Core.Processor
OS.Core.MemoryManager
OS.Core.Scheduler
OS.Core.SimulationEvent
OS.ViewModels.GanttSegment
OS.ViewModels.MainWindowViewModel
```

Nu au fost testate:

```text
OS.App
OS.Program
OS.MainWindow
```

Acestea tin de pornirea aplicatiei si de interfata grafica Avalonia.

---

## Teste pentru Activity

Testele pentru `Activity` verifica:

- valorile implicite;
- setarea tipului `Execution`;
- setarea tipului `SysCall`;
- durata zero;
- durata negativa.

Testele au evidentiat ca `Activity` accepta durata negativa fara validare.

---

## Teste pentru Process

Testele pentru `Process` verifica:

- initializarea proprietatilor;
- valorile implicite ale starii;
- schimbarea starii procesului;
- schimbarea ultimului procesor folosit;
- activitati goale;
- memorie negativa;
- timp de lansare negativ;
- lista de activitati null.

Testele au evidentiat ca `Process` accepta date invalide fara validare.

---

## Teste pentru Processor

Testele pentru `Processor` verifica:

- initializarea identificatorului;
- procesor liber la creare;
- procesor ocupat cand are proces curent;
- revenirea la starea libera;
- timpul petrecut in cuanta;
- identificator negativ.

---

## Teste pentru MemoryManager

Testele pentru `MemoryManager` verifica:

- initializarea memoriei totale;
- calculul memoriei libere;
- incarcarea unui proces in RAM;
- evitarea duplicarii unui proces deja incarcat;
- eliminarea unui proces cand RAM-ul este insuficient;
- eliminarea mai multor procese cand este nevoie;
- rata de transfer zero;
- proces mai mare decat RAM-ul total;
- memorie negativa;
- memorie totala negativa.

Testele au evidentiat ca `MemoryManager` permite incarcarea unui proces mai mare decat memoria totala si ca permite valori negative.

---

## Teste pentru Scheduler

Pentru `Scheduler`, interactiunea cu `MemoryManager` a fost simulata folosind `Moq` si interfata `IMemoryManager`.

Astfel, `Scheduler` a fost testat independent de implementarea concreta a memoriei.

Testele pentru `Scheduler` verifica:

- crearea corecta a procesoarelor;
- planificarea procesului lansat la timpul curent;
- apelarea metodei `EnsureInRam`;
- generarea evenimentului `SCHEDULED`;
- verificarea starii `IsFinished`;
- resetarea schedulerului;
- generarea evenimentului `EXECUTING`;
- finalizarea procesului;
- preemptia Round-Robin;
- asteptarea transferului de memorie;
- generarea procesului de sistem periodic;
- preferinta pentru ultimul procesor folosit.

Exemplu de mocking:

```csharp
var memoryMock = new Mock<IMemoryManager>();

memoryMock
    .Setup(m => m.EnsureInRam(It.IsAny<Process>()))
    .Returns(0);
```

---

## Teste pentru SimulationEvent

Testele pentru `SimulationEvent` verifica:

- initializarea corecta a proprietatilor;
- evenimente fara `ProcessId`;
- evenimente fara `ProcessorId`;
- egalitatea intre record-uri cu aceleasi valori;
- diferenta intre record-uri cu valori diferite;
- timp negativ;
- durata negativa;
- actiune null.

---

## Teste pentru MainWindowViewModel

Pentru `MainWindowViewModel` au fost testate doar partile de logica dezvoltate in proiect:

- initializarea ViewModel-ului;
- incarcarea unei configuratii valide;
- tratarea unei configuratii invalide;
- tratarea unui fisier cu prea putine date;
- rularea unui tick;
- generarea unui segment Gantt;
- resetarea simularii;
- exportul logului.

Nu au fost testate:

- `OpenFileCommand`;
- dialogul Avalonia de selectie fisier;
- `DispatcherTimer`;
- layout-ul grafic;
- controalele UI.

Acestea apartin bibliotecii grafice si nu fac parte din logica ce trebuie testata in faza 2.

---

## Rezultatul testelor

Comanda folosita pentru rularea testelor:

```powershell
dotnet test
```

Rezultat obtinut:

```text
Total tests: 64
Failed: 0
Succeeded: 64
Skipped: 0
```

Toate testele au fost executate cu succes.

---

## Code coverage

Coverage-ul a fost generat folosind comanda:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

Raportul HTML a fost generat cu:

```powershell
reportgenerator -reports:"OS.Tests\TestResults\**\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Fisierul principal al raportului este:

```text
coveragereport/index.html
```

Rezultate coverage:

```text
Line coverage:   71.2%
Branch coverage: 69.2%
```

Clasele principale din `Core` au fost acoperite astfel:

```text
OS.Core.Activity          100%
OS.Core.MemoryManager     100%
OS.Core.Process           100%
OS.Core.Processor         100%
OS.Core.Scheduler         94.5%
OS.Core.SimulationEvent   100%
```

Procentul total este mai mic deoarece raportul include si clase care tin de pornirea aplicatiei si interfata grafica:

```text
OS.App
OS.Program
OS.MainWindow
```

Acestea nu au fost testate deoarece nu reprezinta logica principala a simulatorului si contin cod specific framework-ului Avalonia.

---

## Probleme identificate prin testare

Testele au evidentiat urmatoarele comportamente:

1. `Activity` permite durata negativa.
2. `Process` permite memorie negativa.
3. `Process` permite timp de lansare negativ.
4. `Process` permite lista de activitati null.
5. `SimulationEvent` permite timp negativ.
6. `SimulationEvent` permite durata negativa.
7. `SimulationEvent` permite actiune null.
8. `MemoryManager` permite incarcarea unui proces mai mare decat memoria totala.
9. `MemoryManager` permite valori negative pentru memoria procesului.
10. `MemoryManager` foloseste eliminarea primului proces din lista RAM, comportament mai apropiat de FIFO decat de LRU.
11. Activitatile de tip `SysCall` nu sunt tratate separat de activitatile de tip `Execution` in scheduler.
12. Procesul de sistem este logat prin evenimentul `SYSTEM_CALL_HANDLING`, dar nu ocupa efectiv procesorul ca proces real.

Conform cerintei fazei 2, aceste probleme au fost doar identificate prin testare, nu corectate.

---

## Cum se ruleaza proiectul

Din folderul solutiei:

```powershell
dotnet build
```

Pentru rularea aplicatiei:

```powershell
dotnet run --project OS
```

Sau se poate deschide fisierul:

```text
OS.sln
```

in Visual Studio si se ruleaza proiectul `OS`.

---

## Cum se ruleaza testele

Din folderul solutiei:

```powershell
dotnet test
```

Sau direct din folderul `OS.Tests`:

```powershell
dotnet test
```

---

## Cum se genereaza raportul de coverage

Pasul 1: rularea testelor cu colectare coverage:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

Pasul 2: generarea raportului HTML:

```powershell
reportgenerator -reports:"OS.Tests\TestResults\**\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Pasul 3: deschiderea raportului:

```powershell
start coveragereport\index.html
```

---

## Concluzie

In faza 2 au fost implementate teste unitare pentru clasele principale ale aplicatiei. Fiecare modul a fost testat separat, iar pentru interactiunea dintre `Scheduler` si `MemoryManager` a fost folosita tehnica de mocking prin `Moq`.

Au fost create 64 de teste, toate rulate cu succes. Testele acopera functionalitatea principala a simulatorului, inclusiv planificarea proceselor, preemptia Round-Robin, managementul memoriei, evenimentele de simulare si logica ViewModel-ului.

Raportul de coverage arata o acoperire buna pentru codul principal din `Core`, iar problemele descoperite prin teste au fost documentate fara a fi corectate, conform cerintei fazei 2.

