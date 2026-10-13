# Project roadmap / Roteiro do projeto

**Source inventory status:** checked boxes mean that the relevant implementation exists in the latest source tree, **not** that compatibility, tests or security review have passed. / Caixas marcadas indicam código existente, **não** que houve homologação ou execução de testes.

## Implemented source areas / Áreas já implementadas

- [x] String extension utilities, cleaning, Unicode blankness and safe substring helpers / Extensões de strings, limpeza, Unicode e substring segura.
- [x] Enumerable/collection, enumeration and numeric memory extensions / Extensões de coleções, enumerações e memória numérica.
- [x] Generic, numeric, email and Brazilian-document validation / Validações gerais, numéricas, e-mail e documentos brasileiros.
- [x] Product and document formatting / Formatação de produtos e documentos.
- [x] Financial calculations: cash flows, securities, depreciation, odd coupons / Cálculos financeiros: fluxo de caixa, títulos, depreciação e cupons irregulares.
- [x] File/image conversions and color-model contracts / Conversões de arquivos, imagens e modelos de cor.
- [x] Pooled-array memory helpers / Memória com arrays reutilizáveis.
- [x] Shared ThrowHelper utilities and configurable Diagnostics / Helpers de exceção e diagnósticos configuráveis.
- [x] Localized resource files: default, pt-BR, en-US and es-AR / Recursos de idioma.
- [x] SUFRAMA and voter-registration validation routines in the most recent source branch / Rotinas SUFRAMA e título eleitoral.
- [x] .NET Standard 2.1 library project and existing .NET 10 unit-test source project / Projetos de biblioteca e testes existentes.
- [x] Removal of the legacy StringList implementation from the latest source / Remoção do StringList do código atual.

## Pending / Pendências

- [ ] Complete sanitized source-history extraction, preserving original attribution/date metadata where technically feasible and branch ancestry; ensure `StringList` is absent **throughout public history**. / Concluir migração saneada de histórico e branches, sem StringList.
- [ ] Review all public types and methods and generate a verified API inventory from the extracted tree / Inventariar cada classe e método público automaticamente.
- [ ] Remove obsolete StringList-named resource keys and generated accessors that remain in the original latest source, confirming there are no remaining valid consumers / Eliminar chaves e propriedades antigas de resources relacionadas ao StringList, após validar referências.
- [ ] Validate license provenance of all source, resources, third-party dependencies and imagery / Validar direitos autorais de todos os materiais.
- [ ] Fix relative project references, packaging metadata and README examples after extraction / Ajustar caminhos e metadados.
- [ ] Compile with supported toolchains and test Windows, Linux and macOS / Compilar e testar plataformas.
- [ ] Expand unit-test coverage for every public API, including invalid inputs and culture-sensitive edge cases / Cobertura completa de testes e limites.
- [ ] Add performance benchmarks for hot paths, allocations, pooled buffers and Unicode `Rune` operations / Benchmarks e alocação.
- [ ] Perform nullability, thread-safety, exception, security and API compatibility reviews / Revisão de API, nulidade, segurança e concorrência.
- [ ] Expand translations, including en-GB and additional international locales / Ampliar idiomas.
- [ ] Establish SemVer, compatibility guarantees, changelog and deprecation policy / Versionamento e compatibilidade.
- [ ] Define and approve manual-only GitHub Actions/CI later; **do not add workflows or execute builds/tests now** / Planejar CI manual futuramente.
- [ ] Prepare signed/reproducible NuGet packaging, symbols, documentation and publication **after authorized validation** / Preparar NuGet após validação autorizada.
- [ ] Publish versioned API reference, usage snippets and migration examples / Criar referência de API e exemplos.

## Join the project / Participe

**Code / Código:** [Contributing](CONTRIBUTING.md) · [Open a PR](https://github.com/Lagosystem/Lagosystem.Modules.NET/pulls)

**Bugs / Erros:** [Open an Issue](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose)

**Ideas and questions / Ideias e dúvidas:** [Issues](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose)

**Security / Segurança:** [Private reporting instructions](SECURITY.md)

Proposals may describe a new extension method, type, formatter, validation rule, financial model, performance improvement, accessibility issue, localization or missing documentation. Please explain a practical use case, expected semantics, dependencies and backwards-compatibility impact.

Sugestões podem propor novos métodos, classes, regras de validação, cálculos, otimizações e traduções. Informe cenário real de uso, comportamento esperado, dependências e compatibilidade.
