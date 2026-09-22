# ADR-005 — Resumo diário

Status: Aceito.

Vendas realizadas, vendas canceladas, quantidade vendida, valor vendido e recebimentos realizados são indicadores distintos. Os indicadores operacionais usam `DataOperacao`. Recebimento de uma venda antiga pertence à data da operação do pagamento, não à data da venda. O relatório não integra a etapa de fundação.
