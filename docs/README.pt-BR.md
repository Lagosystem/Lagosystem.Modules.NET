# Lagosystem.Modules.NET — Manual do desenvolvedor

[Início](../README.md) · [English](README.en.md) · [Como contribuir](../CONTRIBUTING.md) · [Pendências](../TODO.md)

## Objetivo

Biblioteca reutilizável para aplicações .NET em geral, sem depender de conta, servidor, tenant ou serviço SaaS da Lagosystem. O projeto utiliza .NET Standard 2.1; há uma suíte de testes .NET 10 ainda em processo de extração para o repositório independente.

## Recursos

- **Extensões:** limpeza de strings, avaliação de texto visualmente vazio, substring segura, coleções, enumerações e números.
- **Validações:** strings, números, e-mails, documentos e inscrições brasileiras (incluindo CPF/CNPJ e novos validadores eleitorais/SUFRAMA).
- **Formatação:** documentos, produtos, EAN/GTIN e formatos específicos do Brasil.
- **Finanças:** matemática financeira, fluxos de caixa, taxas e períodos, depreciação, títulos financeiros e cupons irregulares.
- **Conversão:** arquivos, imagens e modelos de cores.
- **Memória:** buffers reutilizáveis e operações eficientes com pool de arrays.
- **Diagnósticos, configurações e exceções:** opções da biblioteca, callbacks, mensagens padronizadas e recursos traduzidos.

Essas áreas são **inventário do código existente, não atestado de estabilidade ou homologação**. Acompanhe as pendências em [TODO](../TODO.md).

## Estrutura independente planejada

```text
src/Lagosystem.Modules/
tests/Lagosystem.Modules.Tests/
docs/
.github/ISSUE_TEMPLATE/
LICENSE
NOTICE
```

## Preparação e validação futura

Instale um SDK .NET compatível com o projeto de testes (atualmente .NET 10). Quando a migração estiver concluída, os comandos para execução **pelo desenvolvedor, quando autorizado**, serão:

```powershell
dotnet restore ./tests/Lagosystem.Modules.Tests/Lagosystem.Modules.Tests.csproj
dotnet build ./src/Lagosystem.Modules/Lagosystem.Modules.csproj --configuration Release
dotnet test ./tests/Lagosystem.Modules.Tests/Lagosystem.Modules.Tests.csproj --configuration Release
```

**Não foram executados como parte desta etapa.** Não haverá workflow automático ou publicação no NuGet antes de planejamento específico e autorização.

## Comunidade

Abra uma [Issue](https://github.com/Lagosystem/Lagosystem.Modules.NET/issues/new/choose) para relatar defeitos, propor funcionalidades ou corrigir documentação. Envie Pull Requests pequenos, com testes quando possível, documentação XML e manuais EN/PT-BR atualizados. Leia [CONTRIBUTING](../CONTRIBUTING.md) e [SECURITY](../SECURITY.md).

## Distribuição

Apache-2.0. **Ainda não existe anúncio de pacote NuGet estável/publicado**. Compatibilidade, versões, release e testes serão definidos posteriormente.
