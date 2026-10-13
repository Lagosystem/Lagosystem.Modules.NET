# Lagosystem.Modules.NET — Developer guide

[Home](../README.md) · [Português brasileiro](README.pt-BR.md) · [Contributing](../CONTRIBUTING.md) · [Backlog](../TODO.md)

## Purpose

A reusable library for general .NET development; Lagosystem ERP accounts, tenancy and hosted services are not required for using the library. The project targets .NET Standard 2.1, with a separate .NET 10 test project currently under migration.

## Main areas

- **Extensions:** string cleanup and visual-blank validation, safe substring operations, sequences, enum and numeric helpers.
- **Validations:** strings, numbers, email addresses and Brazilian documents and registrations (including CPF/CNPJ and more recent electoral/SUFRAMA support).
- **Formatting:** national document formats, product codes and EAN/GTIN-related helpers.
- **Finance:** financial mathematics, cash flows, amortization/periodic rates, depreciation, securities and irregular coupon calculations.
- **Conversion:** file transformations, imaging helpers, additive/subtractive color models.
- **Memory:** pooled array utilities and efficient memory operations.
- **Diagnostics, settings and errors:** runtime behavior controls, event/callback diagnostics, centralized exception helpers and localized resources.

The listing describes source areas, **not a guarantee that every feature is production-ready**. See [TODO](../TODO.md) for the roadmap and outstanding validation.

## Planned standalone structure

```text
src/Lagosystem.Modules/
tests/Lagosystem.Modules.Tests/
docs/
.github/ISSUE_TEMPLATE/
LICENSE
NOTICE
```

## Developer prerequisites and validation commands

- .NET SDK compatible with the test project's target framework (.NET 10 for the current suite).
- A development environment on Windows, Linux or macOS.
- Restore, build and test locally **after** completing the source extraction:

```powershell
dotnet restore ./tests/Lagosystem.Modules.Tests/Lagosystem.Modules.Tests.csproj
dotnet build ./src/Lagosystem.Modules/Lagosystem.Modules.csproj --configuration Release
dotnet test ./tests/Lagosystem.Modules.Tests/Lagosystem.Modules.Tests.csproj --configuration Release
```

These are **instructions for contributors, not a record of successful executions**. The maintainer has not authorized running compilation, tests or CI during repository migration.

## Contributing and reporting

Use [Issue forms](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose) for defects, features and documentation suggestions; submit a Pull Request with focused commits, tests when practical and public API documentation in both languages. Please read [CONTRIBUTING](../CONTRIBUTING.md) and [SECURITY](../SECURITY.md) first.

## Release status

No NuGet package is announced as published or stable. Versioning, package validation, build matrix, signing and a manual-only release procedure will be designed separately. Do not create automatic GitHub Actions events.
