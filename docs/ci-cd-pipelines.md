# CI/CD Pipelines — EricksonLopez.DapperExtensions

> **Redirect Notice**: This document is superseded by the authoritative reference:
> 👉 **[CI/CD, Build & Quality Engineering](ci-cd-and-quality.md)**

The comprehensive CI/CD pipeline documentation — including all 10 GitHub Actions workflow breakdowns, quality gates, release automation, supply chain security, and Mermaid flow diagrams — is maintained in [`ci-cd-and-quality.md`](ci-cd-and-quality.md).

---

## Quick Reference

| Pipeline | Trigger | Purpose |
|---|---|---|
| `ci.yml` | push/PR → `main`, `develop` | CI orchestrator (delegates to reusable workflows) |
| `dotnet-build-test.yml` | `workflow_call` | Build, test, SonarCloud, Codecov |
| `aot-smoke-test.yml` | push/PR/dispatch | NativeAOT compile + execute gate |
| `publish.yml` | `v*.*.*` tag / dispatch | Pack + Sigstore attest + NuGet push |
| `release-please.yml` | push → `main` | Conventional Commits versioning + release PRs |
| `mutation-testing.yml` | Mon 04:00 UTC / dispatch | 11-package Stryker matrix + consolidated gate |
| `benchmark-regression-gate.yml` | PR touching src/ or benchmarks/ | Performance regression check (10% threshold) |
| `benchmarks.yml` | `workflow_call` / dispatch | On-demand BenchmarkDotNet suite |
| `weekly-benchmarks.yml` | Sun 02:00 UTC / dispatch | Deep cross-TFM benchmark + baseline commit |
| `repo-compliance.yml` | push/PR → `main` / dispatch | Architecture, licensing, compliance verification |

See [ci-cd-and-quality.md](ci-cd-and-quality.md) for full documentation.
