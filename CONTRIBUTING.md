# Contributing to EricksonLopez.DapperExtensions

Thank you for your interest in contributing to **EricksonLopez.DapperExtensions**! This document outlines our development process, coding standards, and quality gates.

---

## 🛠️ Prerequisites & Environment Setup

- **.NET SDK**: `.NET 10.0.100` or higher (pinned in `global.json` with `rollForward: "latestFeature"`).
- **IDE / Editor**: Visual Studio 2022 (v17.12+), JetBrains Rider (2024.3+), or VS Code with C# Dev Kit.
- **Docker / Testcontainers**: Required for running end-to-end integration test suites across containerized database engines.
- **Node.js**: Required for local execution of mutation gate scripts (`scripts/verify-mutation-gate.js`).

### Sibling Repository Dependencies

The `EricksonLopez.DapperExtensions` core project and dialect providers have local `ProjectReference` dependencies on two sibling repositories:

- **`dotnet-pagination`**: Provides `EricksonLopez.Pagination.Abstractions` and `EricksonLopez.Pagination` — pagination contracts and implementations.
- **`dotnet-sql-builder`**: Provides `EricksonLopez.SqlBuilder.Abstractions` — SQL builder abstraction contracts.

To build locally, ensure these sibling repositories are cloned to the same parent directory as `dotnet-dapper-extensions`:

```
<parent-dir>/
├── dotnet-dapper-extensions/   ← this repository
├── dotnet-pagination/           ← required sibling
└── dotnet-sql-builder/          ← required sibling
```

---

## 🚀 Building & Testing

### 1. Restore & Build
```bash
# Clone the repository and sibling dependencies
git clone https://github.com/ericksonlopezf/dotnet-dapper-extensions.git dotnet-dapper-extensions
cd dotnet-dapper-extensions

# Restore and build the solution (all 11 packages + tests + samples + benchmarks)
dotnet restore EricksonLopez.DapperExtensions.slnx
dotnet build EricksonLopez.DapperExtensions.slnx --configuration Release
```

### 2. Run Tests
```bash
# Run all unit tests across all projects
dotnet test EricksonLopez.DapperExtensions.slnx --configuration Release --filter "Category!=Integration"

# Run integration tests (requires Docker daemon)
dotnet test EricksonLopez.DapperExtensions.slnx --configuration Release --filter "Category=Integration"

# Run tests with code coverage collection
dotnet test EricksonLopez.DapperExtensions.slnx --configuration Release --collect:"XPlat Code Coverage"
```

### 3. Run Benchmarks
```bash
# Execute PostgreSQL BenchmarkDotNet suite
dotnet run --project benchmarks/EricksonLopez.DapperExtensions.PostgreSql.Benchmarks --configuration Release
```

### 4. Mutation Testing (Stryker.NET)
```bash
# Install dotnet-stryker tool
dotnet tool install --global dotnet-stryker

# Run mutation testing on a specific package (e.g. Core)
dotnet stryker --config-file stryker-config.json

# Run mutation testing on a dialect package (e.g. PostgreSql)
dotnet stryker --config-file stryker-postgresql-config.json
```

---

## 📏 Engineering & Code Standards

Every Pull Request must strictly comply with the following automated quality standards:

1. **Zero Warnings as Errors**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is enforced globally.
2. **Nullable Reference Types**: `<Nullable>enable</Nullable>` is mandatory across all source files.
3. **Strict Code Analysis**: `<AnalysisLevel>latest-recommended</AnalysisLevel>` and `<WarningLevel>5</WarningLevel>`.
4. **Clean Architecture & Single Responsibility**: One public type per file across all production libraries.
5. **Native AOT Compliance**: `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` is enforced. Zero reflection in hot paths. Use Roslyn source generators or `IDataReaderMapper<T>` for AOT-safe hydration.
6. **XML Documentation**: Every public type, method, and property must include comprehensive XML doc comments (`CS1591` is enforced as an error).

---

## 🌿 Branching & Commit Conventions

### Branch Strategy
- `main`: Production release branch. Contains only published and tagged code.
- `develop`: Primary integration branch. All feature branches branch from and target `develop`.
- `feature/<name>`: New capabilities or enhancements.
- `fix/<name>`: Bug fixes with regression tests.

### Conventional Commits
Commit messages must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification to ensure compatibility with [Release Please](https://github.com/googleapis/release-please):

- `feat: add cursor pagination support for Oracle dialect`
- `fix: propagate CancellationToken to ADO.NET CommandDefinition in resilience pipeline`
- `perf: optimize MultiMapBuilder dictionary allocation`
- `docs: update troubleshooting guide for SQLSTATE 25P02`
- `test: add integration test for Savepoint rollback in PostgreSQL`
- `chore: update CPM package versions in Directory.Packages.props`

---

## 🛡️ Quality Gates & PR Checklist

Before submitting a Pull Request, ensure:

- [ ] The solution builds with zero warnings (`dotnet build EricksonLopez.DapperExtensions.slnx -c Release`).
- [ ] All unit tests pass locally without flaky failures.
- [ ] Stryker.NET mutation testing score meets quality gates ($\ge 95\%$ Break threshold, $\ge 98\%$ Low, $100\%$ High).
- [ ] New functionality includes unit and/or integration tests.
- [ ] `CHANGELOG.md` is updated under `[Unreleased]` with clear, verifiable bullet points.
- [ ] Relevant documentation in `/docs/` and `README.md` is updated.

---

## 💬 Community

- [GitHub Issues](https://github.com/ericksonlopezf/dotnet-dapper-extensions/issues) — Bug reports and defect tracking.
- [GitHub Discussions](https://github.com/ericksonlopezf/dotnet-dapper-extensions/discussions) — Architecture Q&A, ideas, and feature proposals.

---

## 📜 Code of Conduct & License

By contributing to this repository, you agree to adhere to our [Code of Conduct](CODE_OF_CONDUCT.md) and agree that your contributions will be licensed under the [MIT License](LICENSE).
