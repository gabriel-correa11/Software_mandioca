CREATE TABLE Cliente (
    Id INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL CONSTRAINT CK_Cliente_Nome CHECK (length(trim(Nome)) > 0),
    Ativo INTEGER NOT NULL DEFAULT 1 CONSTRAINT CK_Cliente_Ativo CHECK (Ativo IN (0, 1)),
    Origem TEXT NOT NULL CONSTRAINT CK_Cliente_Origem CHECK (Origem IN ('Manual', 'Importacao')),
    ReferenciaExterna TEXT,
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Cliente_CriadoEm CHECK (is_utc_instant(CriadoEm))
) STRICT;

CREATE TABLE Venda (
    Id INTEGER PRIMARY KEY,
    ClienteId INTEGER NOT NULL REFERENCES Cliente(Id) ON DELETE RESTRICT,
    QuantidadeGramas INTEGER NOT NULL CONSTRAINT CK_Venda_Quantidade CHECK (QuantidadeGramas > 0),
    PrecoCentavosPorKg INTEGER NOT NULL CONSTRAINT CK_Venda_Preco CHECK (PrecoCentavosPorKg > 0),
    DataOperacao TEXT NOT NULL CONSTRAINT CK_Venda_DataOperacao CHECK (is_operation_date(DataOperacao)),
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Venda_CriadoEm CHECK (is_utc_instant(CriadoEm)),
    Origem TEXT NOT NULL CONSTRAINT CK_Venda_Origem CHECK (Origem IN ('Manual', 'Importacao')),
    ReferenciaExterna TEXT,
    CanceladaEm TEXT,
    MotivoCancelamento TEXT,
    CONSTRAINT CK_Venda_TotalMinimo CHECK (sale_total_cents(QuantidadeGramas, PrecoCentavosPorKg) >= 1),
    CONSTRAINT CK_Venda_Cancelamento CHECK ((CanceladaEm IS NULL AND MotivoCancelamento IS NULL) OR
           (CanceladaEm IS NOT NULL AND is_utc_instant(CanceladaEm) AND length(trim(MotivoCancelamento)) > 0))
) STRICT;

CREATE TABLE Pagamento (
    Id INTEGER PRIMARY KEY,
    ClienteId INTEGER NOT NULL REFERENCES Cliente(Id) ON DELETE RESTRICT,
    ValorCentavos INTEGER NOT NULL CONSTRAINT CK_Pagamento_Valor CHECK (ValorCentavos > 0),
    DataOperacao TEXT NOT NULL CONSTRAINT CK_Pagamento_DataOperacao CHECK (is_operation_date(DataOperacao)),
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Pagamento_CriadoEm CHECK (is_utc_instant(CriadoEm)),
    Origem TEXT NOT NULL CONSTRAINT CK_Pagamento_Origem CHECK (Origem IN ('Manual', 'Importacao')),
    ReferenciaExterna TEXT,
    EstornadoEm TEXT,
    MotivoEstorno TEXT,
    CONSTRAINT CK_Pagamento_Estorno CHECK ((EstornadoEm IS NULL AND MotivoEstorno IS NULL) OR
           (EstornadoEm IS NOT NULL AND is_utc_instant(EstornadoEm) AND length(trim(MotivoEstorno)) > 0))
) STRICT;

CREATE TABLE AlocacaoPagamento (
    Id INTEGER PRIMARY KEY,
    PagamentoId INTEGER NOT NULL REFERENCES Pagamento(Id) ON DELETE RESTRICT,
    VendaId INTEGER NOT NULL REFERENCES Venda(Id) ON DELETE RESTRICT,
    ValorCentavos INTEGER NOT NULL CONSTRAINT CK_Alocacao_Valor CHECK (ValorCentavos > 0),
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Alocacao_CriadoEm CHECK (is_utc_instant(CriadoEm))
) STRICT;

CREATE INDEX IX_Venda_Cliente_Data ON Venda(ClienteId, DataOperacao);
CREATE INDEX IX_Venda_Data ON Venda(DataOperacao);
CREATE INDEX IX_Pagamento_Cliente_Data ON Pagamento(ClienteId, DataOperacao);
CREATE INDEX IX_Pagamento_Data ON Pagamento(DataOperacao);
CREATE INDEX IX_Alocacao_Pagamento ON AlocacaoPagamento(PagamentoId);
CREATE INDEX IX_Alocacao_Venda ON AlocacaoPagamento(VendaId);
CREATE INDEX IX_Cliente_Ativo_Nome ON Cliente(Ativo, Nome);

CREATE TRIGGER TR_Venda_ClienteAtivo BEFORE INSERT ON Venda
BEGIN
    SELECT RAISE(ABORT, 'cliente inativo nao aceita nova venda')
    WHERE (SELECT Ativo FROM Cliente WHERE Id = NEW.ClienteId) = 0;
END;

CREATE TRIGGER TR_Pagamento_ClienteAtivo BEFORE INSERT ON Pagamento
BEGIN
    SELECT RAISE(ABORT, 'cliente inativo nao aceita novo pagamento')
    WHERE (SELECT Ativo FROM Cliente WHERE Id = NEW.ClienteId) = 0;
END;

CREATE TRIGGER TR_Alocacao_Validar BEFORE INSERT ON AlocacaoPagamento
BEGIN
    SELECT RAISE(ABORT, 'pagamento estornado nao aceita alocacao')
    WHERE (SELECT EstornadoEm FROM Pagamento WHERE Id = NEW.PagamentoId) IS NOT NULL;

    SELECT RAISE(ABORT, 'venda cancelada nao aceita alocacao')
    WHERE (SELECT CanceladaEm FROM Venda WHERE Id = NEW.VendaId) IS NOT NULL;

    SELECT RAISE(ABORT, 'cliente do pagamento diferente do cliente da venda')
    WHERE (SELECT ClienteId FROM Pagamento WHERE Id = NEW.PagamentoId)
       <> (SELECT ClienteId FROM Venda WHERE Id = NEW.VendaId);

    SELECT RAISE(ABORT, 'alocacoes excedem o pagamento')
    WHERE (SELECT COALESCE(SUM(ValorCentavos), 0) FROM AlocacaoPagamento
           WHERE PagamentoId = NEW.PagamentoId) + NEW.ValorCentavos
        > (SELECT ValorCentavos FROM Pagamento WHERE Id = NEW.PagamentoId);

    SELECT RAISE(ABORT, 'alocacoes excedem o total da venda')
    WHERE (SELECT COALESCE(SUM(a.ValorCentavos), 0)
           FROM AlocacaoPagamento a JOIN Pagamento p ON p.Id = a.PagamentoId
           WHERE a.VendaId = NEW.VendaId AND p.EstornadoEm IS NULL) + NEW.ValorCentavos
        > (SELECT sale_total_cents(QuantidadeGramas, PrecoCentavosPorKg)
           FROM Venda WHERE Id = NEW.VendaId);
END;

CREATE TRIGGER TR_Alocacao_Imutavel BEFORE UPDATE ON AlocacaoPagamento
BEGIN
    SELECT RAISE(ABORT, 'alocacao imutavel');
END;

CREATE TRIGGER TR_Alocacao_SemExclusao BEFORE DELETE ON AlocacaoPagamento
BEGIN
    SELECT RAISE(ABORT, 'alocacao nao pode ser excluida');
END;

CREATE TRIGGER TR_Pagamento_Imutavel BEFORE UPDATE ON Pagamento
WHEN NEW.ClienteId <> OLD.ClienteId OR NEW.ValorCentavos <> OLD.ValorCentavos
  OR NEW.DataOperacao <> OLD.DataOperacao OR NEW.CriadoEm <> OLD.CriadoEm
  OR NEW.Origem <> OLD.Origem
  OR NEW.ReferenciaExterna IS NOT OLD.ReferenciaExterna
  OR OLD.EstornadoEm IS NOT NULL OR NEW.EstornadoEm IS NULL
BEGIN
    SELECT RAISE(ABORT, 'pagamento somente pode ser estornado uma vez');
END;

CREATE TRIGGER TR_Pagamento_SemExclusao BEFORE DELETE ON Pagamento
BEGIN
    SELECT RAISE(ABORT, 'pagamento nao pode ser excluido');
END;

CREATE TRIGGER TR_Venda_Imutavel BEFORE UPDATE ON Venda
WHEN NEW.ClienteId <> OLD.ClienteId OR NEW.QuantidadeGramas <> OLD.QuantidadeGramas
  OR NEW.PrecoCentavosPorKg <> OLD.PrecoCentavosPorKg
  OR NEW.DataOperacao <> OLD.DataOperacao OR NEW.CriadoEm <> OLD.CriadoEm
  OR NEW.Origem <> OLD.Origem
  OR NEW.ReferenciaExterna IS NOT OLD.ReferenciaExterna
  OR OLD.CanceladaEm IS NOT NULL OR NEW.CanceladaEm IS NULL
BEGIN
    SELECT RAISE(ABORT, 'venda somente pode ser cancelada uma vez');
END;

CREATE TRIGGER TR_Venda_Cancelar BEFORE UPDATE OF CanceladaEm ON Venda
WHEN NEW.CanceladaEm IS NOT NULL
BEGIN
    SELECT RAISE(ABORT, 'venda possui alocacoes financeiras validas')
    WHERE EXISTS (
        SELECT 1 FROM AlocacaoPagamento a
        JOIN Pagamento p ON p.Id = a.PagamentoId
        WHERE a.VendaId = NEW.Id AND p.EstornadoEm IS NULL
    );
END;

CREATE TRIGGER TR_Venda_SemExclusao BEFORE DELETE ON Venda
BEGIN
    SELECT RAISE(ABORT, 'venda nao pode ser excluida');
END;

CREATE TRIGGER TR_Cliente_SemExclusao BEFORE DELETE ON Cliente
BEGIN
    SELECT RAISE(ABORT, 'cliente nao pode ser excluido');
END;
