# ADR-006 — SQLite e dados locais

Status: Aceito para a fundação.

SQLite é a fonte oficial. O banco fica sob `%LOCALAPPDATA%/MandiocaCaipira/Data`, separado do executável. `PRAGMA user_version` marca o schema; migrations são sequenciais e transacionais. Antes de uma migration de banco existente, deve-se gerar backup SQLite em `%LOCALAPPDATA%/MandiocaCaipira/Backups`. Banco sem versão e com tabelas desconhecidas não é migrado automaticamente. Dados reais, backups e logs não entram no repositório.
