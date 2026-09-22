# ADR-002 — Unidades e arredondamento

Status: Aceito.

Quantidades são armazenadas em gramas (`INTEGER`); dinheiro e preço por kg, em centavos (`INTEGER`). O total da venda é calculado uma vez com aritmética decimal exata: `Round(gramas × centavosPorKg / 1000, 0, MidpointRounding.AwayFromZero)`. O total precisa ser pelo menos um centavo. A UI apresentará a quantidade em kg. Não se usa `double` nem `REAL` em cálculos financeiros.

As tabelas SQLite são `STRICT` para impedir que a tipagem dinâmica aceite `REAL` ou texto em colunas financeiras inteiras.
