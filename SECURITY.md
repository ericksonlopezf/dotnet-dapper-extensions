# Security Policy — EricksonLopez.DapperExtensions

## Supported Versions

Security updates are actively maintained for the following versions of the **EricksonLopez.DapperExtensions** ecosystem packages:

| Package Version | Supported | Notes |
|---|:---:|---|
| `2.0.x` | ✅ Yes | Current stable major release (.NET 8.0, .NET 9.0, .NET 10.0) |
| `1.2.x` | ✅ Yes | Previous stable release (Critical security patches only) |
| `< 1.2.0` | ❌ No | End of life — users are urged to upgrade to `2.0.0+` |

---

## Reporting a Vulnerability

If you discover a potential security vulnerability in any of the `EricksonLopez.DapperExtensions` packages, **please do not disclose it via a public GitHub issue.**

Please report security issues privately via email:

📧 **Maintainer Contact:** [ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com)

### Submission Details
Please provide the following details to expedite triage and resolution:
1. **Affected Package(s) and Version(s)**: (e.g., `EricksonLopez.DapperExtensions.PostgreSql v2.0.0`)
2. **Vulnerability Type**: (e.g., SQL Injection risk, parameter leakage, DoS, concurrency violation)
3. **Proof of Concept / Reproduction Steps**: Minimal code snippet or unit test reproducing the vulnerability
4. **Impact Assessment**: Technical risk and potential attack surface
5. **Suggested Mitigation**: (Optional) Proposed code fix or architectural remedy

### Response Timeline
- **Acknowledgement**: Within 48 hours.
- **Initial Triage & Assessment**: Within 5 business days.
- **Target Patch Delivery**: Within 14 business days for critical severity issues.
- **Security Advisory & Attribution**: Published via GitHub Security Advisories upon patch release, with full credit to the reporter.

---

## Supply Chain Security & Attestation

All official NuGet packages in the `EricksonLopez.DapperExtensions` ecosystem implement modern supply chain security standards:

1. **Sigstore Provenance Attestation**: Packages released through GitHub Actions generate build provenance attestations via `actions/attest-build-provenance` backed by Sigstore OIDC identities.
2. **NuGet Trusted Publishing**: Package releases use short-lived OIDC tokens authenticated with GitHub Actions identities (`NuGet/login@v1`), eliminating long-lived API keys.
3. **Strong Name Assembly Signing**: Production assemblies are signed with an official Strong Name Key (`.snk`) to ensure binary integrity and identity verification.
4. **Automated Dependency Auditing**: Continuous build configuration enforces `NuGetAudit=true`, `NuGetAuditMode=all`, and `NuGetAuditLevel=low` across all packable projects.
5. **Reproducible & Deterministic Builds**: Compiled with `Deterministic=true`, `ContinuousIntegrationBuild=true`, and embedded SourceLink metadata.

---

## Known Security Boundaries

- **Raw SQL & Parameterization**: The library requires developers to provide parameterized SQL queries. Bulk helpers (`BulkParameters`, `BulkDataTableBuilder`, `BulkBuilder`) use strict type-safe ADO.NET parameter binding to prevent SQL injection vulnerabilities. Direct concatenation of untrusted user input into raw SQL strings by consuming applications bypasses parameterization and is strongly discouraged.
- **Savepoint Naming**: Savepoint names generated via `CreateSavepointAsync(name)` should use alphanumeric identifiers. Consuming applications should never pass unvalidated external strings as savepoint identifiers.
