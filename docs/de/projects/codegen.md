---
title: Nextended.CodeGen
---
# Nextended.CodeGen

📚 **[Vollständige API-Referenz](/de/projects/codegen-api)** — jeder öffentliche Typ und Member, erzeugt aus der kompilierten Assembly.

🇬🇧 [This page in English](/projects/codegen)

Roslyn-Source-Generator — DTOs und Interfaces aus Ihren Entities, stark typisierte Klassen aus JSON/XML, Lookup-Tabellen aus Excel und Dokumentation aus Quelldateien.
[![NuGet](https://img.shields.io/nuget/v/Nextended.CodeGen.svg)](https://www.nuget.org/packages/Nextended.CodeGen/)

---

## Installation

```bash
dotnet add package Nextended.CodeGen
```

## Was das Paket macht

Roslyn-Source-Generator — DTOs und Interfaces aus Ihren Entities, stark typisierte Klassen aus JSON/XML, Lookup-Tabellen aus Excel und Dokumentation aus Quelldateien.

Die **vollständige Referenz** — alle Typen, Builder, Optionen und ausführlich kommentierte
Codebeispiele — steht auf der englischen Seite:

[📖 Nextended.CodeGen — vollständige Referenz (englisch)](/projects/codegen)
Ebenso ausführlich und ebenfalls englisch ist das Paket-README:

[📄 Nextended.CodeGen/README.md](https://github.com/fgilde/Nextended/blob/main/Nextended.CodeGen/README.md)

## Ausführbares Beispiel

Im Repository liegt ein vollständiger, startbarer AppHost:

**[CodeGenSample](https://github.com/fgilde/Nextended/tree/main/Tests/TestProjects/CodeGenSample)**

```bash
git clone https://github.com/fgilde/Nextended.git
cd Nextended/Tests/TestProjects/CodeGenSample
dotnet run
```

## Unterstützte Frameworks

- `netstandard2.0`

## Plattform

Build-time (analyzer)

## Abhängigkeiten

- Nextended.Core
- Microsoft.CodeAnalysis.CSharp
- ClosedXML

## Links

- 📦 [NuGet-Paket](https://www.nuget.org/packages/Nextended.CodeGen/)
- 📖 [Vollständige Referenz (englisch)](/projects/codegen)
- 🧑‍💻 [Quellcode](https://github.com/fgilde/Nextended/tree/main/Nextended.CodeGen)
- 📄 [Paket-README](https://github.com/fgilde/Nextended/blob/main/Nextended.CodeGen/README.md)
- 🐛 [Fehler melden](https://github.com/fgilde/Nextended/issues)

## Lizenz

GPL-3.0-or-later — siehe [LICENSE](https://github.com/fgilde/Nextended/blob/main/LICENSE).