# ADR-001 — Pagamentos e alocações

Status: Aceito.

Um pagamento pertence a um cliente e representa um recebimento real. Pode ter zero ou mais alocações para vendas do mesmo cliente. A soma alocada não ultrapassa o valor do pagamento. O valor não alocado é crédito disponível do cliente. Não dividimos artificialmente um recebimento em pagamentos separados e não usamos saldo negativo de venda para representar crédito.

Consequência: saldo e crédito são consultas derivadas. A inserção de alocações exige validação transacional e integridade entre tabelas.
