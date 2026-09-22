# ADR-008 — Semântica temporal

Status: Aceito.

`DataOperacao` representa a data civil da operação e é persistida como `YYYY-MM-DD`. `CriadoEm` representa o instante técnico de entrada no sistema e é normalizado para UTC no formato ISO 8601 `yyyy-MM-ddTHH:mm:ss.fffffffZ`.

A conversão pertence à camada de persistência/domínio; Forms não constroem representações persistentes. Importações futuras preservam a data civil original em `DataOperacao`, enquanto `CriadoEm` registra o instante da importação. Datas de cancelamento e estorno seguem a mesma representação UTC dos instantes técnicos.
