# ADR-003 — Estorno de pagamento

Status: Aceito.

Pagamento incorreto é estornado com data e motivo, sem exclusão física. Suas alocações permanecem registradas, mas deixam de produzir efeito financeiro. A operação é atômica. Uma futura UI deve mostrar as vendas afetadas e pedir confirmação explícita. Estorno de lançamento não representa devolução real de dinheiro.
