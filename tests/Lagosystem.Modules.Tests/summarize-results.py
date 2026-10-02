"""Resume TRX/cobertura no Actions e rejeita execuções sem testes descobertos."""

import os
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    results_directory = Path(sys.argv[1])
    reports = sorted(results_directory.rglob("*.trx"))
    if not reports:
        print("::error::Nenhum relatório TRX do Modules foi produzido.")
        return 1

    namespace = {"trx": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    total = passed = failed = not_executed = 0
    for report in reports:
        counters = ET.parse(report).find("trx:ResultSummary/trx:Counters", namespace)
        if counters is None:
            print(f"::error::Relatório TRX sem contadores: {report}")
            return 1
        total += int(counters.get("total", "0"))
        passed += int(counters.get("passed", "0"))
        failed += int(counters.get("failed", "0"))
        not_executed += int(counters.get("notExecuted", "0"))

    summary = [
        "## Lagosystem.Modules",
        "",
        "| Total | Aprovados | Falhas | Não executados |",
        "| ---: | ---: | ---: | ---: |",
        f"| {total} | {passed} | {failed} | {not_executed} |",
        "",
    ]
    for report in sorted(results_directory.rglob("coverage.cobertura.xml")):
        coverage = ET.parse(report).getroot()
        line_rate = float(coverage.get("line-rate", "0")) * 100
        branch_rate = float(coverage.get("branch-rate", "0")) * 100
        summary.append(f"Cobertura do Modules: **{line_rate:.2f}% das linhas** e **{branch_rate:.2f}% dos desvios**.")
        summary.append("")
    summary.append("Logs, TRX e Cobertura XML estão no artifact `modules-results-*` desta execução.")
    text = "\n".join(summary) + "\n"
    print(text)
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        with Path(summary_path).open("a", encoding="utf-8") as output:
            output.write(text)

    if total == 0 or passed != total or failed > 0 or not_executed > 0:
        print("::error::A suíte precisa descobrir e aprovar todos os testes, sem casos ignorados.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
