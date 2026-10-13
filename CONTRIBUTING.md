# Contributing / Como contribuir

**English** · [Português brasileiro](#português-brasileiro)

## English

Thank you for considering a contribution to Lagosystem.Modules.NET. We welcome code, new extension methods, bug reports, benchmarks, API design discussions, performance fixes, documentation, tests, translations and compatibility feedback.

### Open an Issue first

Choose the appropriate [issue form](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose). Describe the problem, expected behavior, reproducible example, relevant runtime/OS, and possible compatibility impact. **Do not submit secrets, proprietary source, customer data or security vulnerabilities in a public Issue.**

### Pull requests

1. Fork the repository and create a focused feature/fix branch.
2. Discuss breaking public API changes before coding. Keep naming, nullability and semantics consistent.
3. Prefer explicit C# types when reasonable, readable code and verified documentation XML for public APIs.
4. Add/update unit tests, edge cases, culture-specific behavior and boundary checks when feasible.
5. Update both language manuals and the backlog when externally visible behavior changes.
6. Use informative Conventional Commits such as `fix(validation): ...` and describe behavior, migration impact and testing honestly.
7. Only add independently authored code or third-party code with verifiable compatible licensing and required notices. Do not restore the former `StringList` implementation.
8. Open a PR and link the corresponding Issue; complete the PR checklist.

**Do not introduce automatic GitHub Actions workflows.** CI, compilation, unit-test execution and package publication require a specific authorization from the project owner for each execution. Include commands/results *only if you personally executed them*.

### Vulnerabilities

Follow [SECURITY.md](SECURITY.md). Never disclose exploit details through public Issues.

## Português brasileiro

Agradecemos contribuições com código, métodos de extensão, correções, benchmarks, melhorias de desempenho, documentação, testes, traduções, ideias de API e relatórios de erros.

### Abra uma Issue

Utilize os [formulários](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose), informando comportamento atual/esperado, reproduções, ambiente (SO, SDK, versão) e possíveis impactos de compatibilidade. **Não publique senhas, dados de clientes, código proprietário ou vulnerabilidades de segurança em Issues públicas.**

### Pull Requests

1. Faça fork e crie uma branch específica para a alteração.
2. Discuta previamente alterações incompatíveis na API pública.
3. Priorize legibilidade, tipagem explícita quando apropriado e documentação XML para APIs públicas.
4. Inclua/atualize testes de unidade, casos de borda e peculiaridades culturais quando possível.
5. Atualize a documentação nos dois idiomas e marque o andamento do TODO.
6. Utilize mensagens de commit descritivas, preferencialmente Conventional Commits.
7. Envie apenas código próprio ou dependências com direitos e licenças compatíveis comprováveis. Não reintroduza o antigo `StringList`.
8. Abra um PR com Issue relacionada, impacto e validação realizada.

**Não crie workflows automáticos do GitHub Actions.** CI, compilação, testes e publicação de pacote exigem autorização específica do mantenedor para cada execução. Nunca declare teste bem-sucedido sem tê-lo executado.

Vulnerabilidades devem seguir [SECURITY.md](SECURITY.md), não Issues públicas.
