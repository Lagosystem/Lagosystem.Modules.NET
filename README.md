# Lagosystem.Modules.NET

**Reusable, open-source .NET utilities under Apache-2.0.**

**English:** [Developer manual](docs/README.en.md) · **Português (Brasil):** [Manual de desenvolvimento](docs/README.pt-BR.md)

Lagosystem.Modules is a general-purpose .NET Standard 2.1 library intended for applications beyond the Lagosystem platform. The library provides string and collection extensions, validation and formatting helpers, numeric and financial calculations, conversions, diagnostic utilities, resource-based error messages, and memory utilities.

> **Pre-release / Pré-lançamento:** The standalone repository is being extracted from the former Foundation monorepo. There is **no published NuGet release or verified standalone build yet**. Do not infer readiness from existing test source files.

## Scope / Escopo

| Area | Overview |
| --- | --- |
| `Extensions` | String, enumeration, numerical and enumerable helpers |
| `Validations` | Generic values, email, numeric formats, Brazilian documents and identifiers |
| `Formatting` | Product identifiers, barcodes and Brazil-specific formats |
| `Calc/Financials` | Financial cash-flow and security calculations |
| `Conversions` | File, imaging and color-model utilities |
| `Memory` | Array-pool-backed memory helpers |
| `Helpers` | Shared validation, error and formatting helpers |
| `LanguagesResource` | Localized exception messages and resource files |
| `Settings` / diagnostics | Runtime options, diagnostics and exception callbacks |

Current code is designed for `netstandard2.1`; the existing test project targets `net10.0`. Actual compatibility and test results remain subject to validation.

## Community / Comunidade

- [Bugs and suggestions / Erros e sugestões](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose)
- [Contributing / Como contribuir](CONTRIBUTING.md)
- [Development backlog / Lista de tarefas](TODO.md)
- [Security / Segurança](SECURITY.md)
- [Code of conduct / Código de conduta](CODE_OF_CONDUCT.md)
- [Authors / Autoria](AUTHORS.md)

We accept bug reports, documentation fixes, API proposals, performance improvements, localization and code contributions. General questions may be opened as an Issue until another support channel is announced.

Aceitamos relatos de erros, melhorias na documentação, sugestões de API, desempenho, traduções e código. Dúvidas gerais podem ser registradas em Issues enquanto não houver outro canal divulgado.

## Policy / Política

- No automatic GitHub Actions workflows have been added. CI, builds, unit tests and NuGet publishing are planned, **not claimed to have run**.
- No Embarcadero `StringList` implementation is included. Historical imports must also exclude that file and related tests.
- Third-party source requires license and attribution review before inclusion.

## License / Licença

Apache License, Version 2.0 — see [LICENSE](LICENSE) and [NOTICE](NOTICE). The library is not licensed under the GPL license used by the separate Lagosystem Bridge project.
