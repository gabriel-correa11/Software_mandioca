# Fundação técnica

A aplicação WinForms usa SQLite local. `Data/InicializadorBanco.cs` instala o schema v1 incorporado ao executável e valida integridade, referências e objetos essenciais ao reabrir o banco. `BancoSqlite.cs` abre conexões com chaves estrangeiras ativas e registra a função de total da venda usada pelas constraints do banco. As tabelas são `STRICT`, garantindo que gramas e centavos sejam realmente persistidos como inteiros. A pasta de dados é independente da pasta de instalação.

`Data/FormatoTemporal.cs` normaliza datas de operação como `YYYY-MM-DD` e instantes técnicos em UTC ISO 8601. O schema valida essas representações. Clientes são inativados logicamente pelo campo `Ativo`; o histórico é preservado e novas vendas ou pagamentos para clientes inativos são recusados.

A V1 operacional inclui cadastro de clientes, vendas, pagamentos com distribuição entre vendas, pendências, histórico, resumo diário, cancelamentos, estornos, comprovantes em PDF e backup manual.

## Stack e bibliotecas

- **Microsoft.Data.Sqlite:** acesso e persistência no banco SQLite oficial da aplicação.
- **Serilog + Serilog.Sinks.File:** logging de diagnóstico local em `%LOCALAPPDATA%/MandiocaCaipira/Logs`. Os arquivos rodam diariamente, também rodam ao atingir 10 MiB e são limitados aos 14 arquivos mais recentes.
- **FuzzySharp:** dependência aprovada para futura sugestão assistida de possíveis clientes duplicados. Similaridade textual nunca confirma ou altera clientes automaticamente.
- **ClosedXML:** dependência aprovada para futura importação e exportação de planilhas. Nenhum fluxo de planilha foi implementado nesta etapa.
- **PDFsharp-GDI:** geração local de recibos e comprovantes em PDF. Usa licença MIT e não transmite dados para serviços externos.

## Interface e operação

A interface utiliza controles nativos do WinForms com navegação administrativa, apresentação em pt-BR e suporte a DPI por monitor. Recibos ficam em `%LOCALAPPDATA%/MandiocaCaipira/Recibos` e backups manuais em `%LOCALAPPDATA%/MandiocaCaipira/Backups`. O SQLite permanece como fonte oficial; PDFs são sempre recriados a partir dos registros persistidos.

O logging registra inicialização, encerramento, falhas inesperadas e eventos relevantes do bootstrap e das migrations do SQLite. Não deve receber conteúdo integral de registros nem dados pessoais desnecessários.
