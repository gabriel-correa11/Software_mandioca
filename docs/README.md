# Fundação técnica

A aplicação WinForms usa SQLite local. `Data/InicializadorBanco.cs` instala o schema v1 incorporado ao executável e valida integridade, referências e objetos essenciais ao reabrir o banco. `BancoSqlite.cs` abre conexões com chaves estrangeiras ativas e registra a função de total da venda usada pelas constraints do banco. As tabelas são `STRICT`, garantindo que gramas e centavos sejam realmente persistidos como inteiros. A pasta de dados é independente da pasta de instalação.

`Data/FormatoTemporal.cs` normaliza datas de operação como `YYYY-MM-DD` e instantes técnicos em UTC ISO 8601. O schema valida essas representações. Clientes são inativados logicamente pelo campo `Ativo`; o histórico é preservado e novas vendas ou pagamentos para clientes inativos são recusados.

A etapa atual contém somente o formulário vazio gerado pelo template. Os fluxos de venda, pagamento e relatórios serão entregas posteriores.
