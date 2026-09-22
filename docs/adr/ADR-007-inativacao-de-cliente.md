# ADR-007 — Inativação de cliente

Status: Aceito.

Cliente utiliza inativação lógica por meio de `Ativo INTEGER NOT NULL DEFAULT 1`. Não há exclusão física nem infraestrutura genérica de soft delete. Um cliente inativo permanece no cadastro e em todo o histórico, pode ser reativado e não aparece por padrão na seleção para novas operações.

Na V1, clientes inativos não aceitam novas vendas nem novos pagamentos. O banco protege essa regra, além da validação que será feita pelos serviços dos casos de uso.
