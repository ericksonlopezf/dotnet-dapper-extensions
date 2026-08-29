## Description
<!-- Provide a concise explanation of the purpose of this Pull Request -->

## Affected Packages
<!-- Select all packages affected by this Pull Request -->
- [ ] `EricksonLopez.DapperExtensions` (Core)
- [ ] `EricksonLopez.DapperExtensions.DependencyInjection`
- [ ] `EricksonLopez.DapperExtensions.HealthChecks`
- [ ] `EricksonLopez.DapperExtensions.MariaDb`
- [ ] `EricksonLopez.DapperExtensions.MySql`
- [ ] `EricksonLopez.DapperExtensions.OpenTelemetry`
- [ ] `EricksonLopez.DapperExtensions.Oracle`
- [ ] `EricksonLopez.DapperExtensions.PostgreSql`
- [ ] `EricksonLopez.DapperExtensions.SourceGenerators`
- [ ] `EricksonLopez.DapperExtensions.Sqlite`
- [ ] `EricksonLopez.DapperExtensions.SqlServer`
- [ ] Documentation / Samples / Benchmarks

## Type of Change
- [ ] 🐛 Bug fix (non-breaking change fixing an issue)
- [ ] ✨ New feature (non-breaking change adding functionality)
- [ ] ⚡ Performance improvement
- [ ] 💥 Breaking change (fix or feature causing existing functionality to change)
- [ ] 📖 Documentation update
- [ ] 🔧 Maintenance / CI / Tooling

## Quality Checklist
- [ ] The solution builds successfully with zero warnings (`TreatWarningsAsErrors=true`).
- [ ] All unit and integration tests pass locally (`dotnet test EricksonLopez.DapperExtensions.slnx`).
- [ ] Stryker.NET mutation testing score meets or exceeds quality gates (Break: $\ge 95\%$, Low: $\ge 98\%$, High: $100\%$).
- [ ] Public API members are properly annotated with XML documentation comments.
- [ ] Code follows `.editorconfig` rules and Clean Architecture guidelines (one public type per file).
- [ ] `CHANGELOG.md` updated under `[Unreleased]` with Conventional Commits taxonomy if applicable.
- [ ] Documentation in `/docs/` and `README.md` updated if API surface or behavior changed.
