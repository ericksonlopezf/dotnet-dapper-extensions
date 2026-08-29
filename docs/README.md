# Documentation Hub — EricksonLopez.DapperExtensions

Welcome to the official documentation and reference center for **EricksonLopez.DapperExtensions**.

---

## 📚 Documentation Index

| Guide | Description | Target Audience |
|---|---|---|
| 🚀 [**Quick Start**](quickstart.md) | Set up and initialize the library in under 5 minutes. | Developers starting out |
| 📖 [**Getting Started**](getting-started.md) | In-depth onboarding guide covering core concepts and configuration. | All developers |
| 🏛️ [**Architecture & Functional Map**](architecture.md) | System architecture, layer transitions, lifecycle sequence diagrams, and ADRs. | Architects & Senior Devs |
| 📋 [**API Reference**](api-reference.md) | Formal Microsoft Learn-style reference for all public types, methods, and options. | Developers & Integrators |
| 💡 [**Best Practices & Guidelines**](best-practices.md) | Recommended patterns, ADR-016 resilience scoping rules, and anti-patterns. | Developers & Architects |
| 🍳 [**Cookbook (Recipes)**](cookbook.md) | Production recipes for real-world scenarios (Outbox, Bulk, Keyset, Resilient Sagas). | Application Developers |
| ⚡ [**Performance & Tuning Guide**](performance-guide.md) | Zero-allocation optimization, Native AOT trimming, and throughput benchmarks. | Performance Engineers |
| 🛠️ [**Troubleshooting Guide**](troubleshooting.md) | Diagnosis of SQLSTATE error codes, connection timeouts, and AOT trimming warnings. | DevOps & Developers |
| 🔄 [**Migration Guide**](migration-guide.md) | Steps for migrating from Vanilla Dapper or Entity Framework Core. | Migration Teams |
| ❓ [**Frequently Asked Questions (FAQ)**](faq.md) | Technical FAQs, tradeoffs, and architectural justifications. | General Reference |
| 🛡️ [**CI/CD, Build & Quality Gates**](ci-cd-and-quality.md) | GitHub Actions workflows, Stryker mutation testing, Codecov, and SonarCloud. | DevOps & Maintainers |
| 📦 [**NuGet Packages & Ecosystem**](nuget-packages.md) | Package inventory, Central Package Management (CPM), and compatibility matrices. | Package Consumers |
| 🎯 [**Showcase Project**](../samples/EricksonLopez.DapperExtensions.Showcase) | Executable reference project demonstrating progressive levels (Level 00 to Level 10). | Hands-on Learners |

---

## 🏗️ Ecosystem Package Architecture

```
src/
├── EricksonLopez.DapperExtensions                (Core Library)
├── EricksonLopez.DapperExtensions.DependencyInjection (Generic Host & ASP.NET Core)
├── EricksonLopez.DapperExtensions.SourceGenerators     (Roslyn Incremental Generator)
├── EricksonLopez.DapperExtensions.PostgreSql          (PostgreSQL Dialect Provider)
├── EricksonLopez.DapperExtensions.SqlServer          (SQL Server Dialect Provider)
├── EricksonLopez.DapperExtensions.MySql              (MySQL Dialect Provider)
├── EricksonLopez.DapperExtensions.MariaDb            (MariaDB Dialect Provider)
├── EricksonLopez.DapperExtensions.Oracle             (Oracle Dialect Provider)
├── EricksonLopez.DapperExtensions.Sqlite             (SQLite Dialect Provider)
├── EricksonLopez.DapperExtensions.HealthChecks       (Database Connectivity Probes)
└── EricksonLopez.DapperExtensions.OpenTelemetry      (Distributed Tracing & Metrics)

samples/
└── EricksonLopez.DapperExtensions.Showcase           (Official Executable Reference)

benchmarks/
└── EricksonLopez.DapperExtensions.PostgreSql.Benchmarks (BenchmarkDotNet Suite)
```

---

## 🏛️ Architecture Decision Records (ADR)

All design decisions and architectural invariants are formally documented in [ADR Directory](adr/README.md).
