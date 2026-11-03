CREATE TABLE Fornecedor (
    Id INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL CONSTRAINT CK_Fornecedor_Nome CHECK (length(trim(Nome)) > 0),
    Apelido TEXT,
    Telefone TEXT,
    Ativo INTEGER NOT NULL DEFAULT 1 CONSTRAINT CK_Fornecedor_Ativo CHECK (Ativo IN (0, 1)),
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Fornecedor_CriadoEm CHECK (is_utc_instant(CriadoEm))
) STRICT;

CREATE TABLE ConfiguracaoRendimento (
    Id INTEGER PRIMARY KEY,
    VigenteDesde TEXT NOT NULL CONSTRAINT CK_Config_VigenteDesde CHECK (is_operation_date(VigenteDesde)),
    PercentualPadraoPerdaBp INTEGER NOT NULL CONSTRAINT CK_Config_Percentual CHECK (PercentualPadraoPerdaBp > 0 AND PercentualPadraoPerdaBp < 10000),
    GramasPorSacoPadrao INTEGER NOT NULL CONSTRAINT CK_Config_GramasSaco CHECK (GramasPorSacoPadrao > 0),
    TamanhoSacoGramas INTEGER NOT NULL CONSTRAINT CK_Config_TamanhoSaco CHECK (TamanhoSacoGramas > 0)
) STRICT;

CREATE TABLE CompraMandioca (
    Id INTEGER PRIMARY KEY,
    FornecedorId INTEGER NOT NULL REFERENCES Fornecedor(Id) ON DELETE RESTRICT,
    DataCompra TEXT NOT NULL CONSTRAINT CK_Compra_DataCompra CHECK (is_operation_date(DataCompra)),
    PesoBrutoGramas INTEGER NOT NULL CONSTRAINT CK_Compra_PesoBruto CHECK (PesoBrutoGramas > 0),
    PesoCascadoGramas INTEGER NOT NULL CONSTRAINT CK_Compra_PesoCascado CHECK (PesoCascadoGramas > 0),
    CustoCentavos INTEGER,
    Status TEXT NOT NULL DEFAULT 'Pendente' CONSTRAINT CK_Compra_Status CHECK (Status IN ('Pendente', 'Paga', 'Cancelada')),
    CaminhoFoto TEXT,
    Observacao TEXT,
    SnapshotPerdaBp INTEGER NOT NULL,
    SnapshotGramasPorSaco INTEGER NOT NULL,
    SnapshotTamanhoSacoGramas INTEGER NOT NULL,
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Compra_CriadoEm CHECK (is_utc_instant(CriadoEm)),
    CanceladaEm TEXT,
    MotivoCancelamento TEXT,
    CONSTRAINT CK_Compra_PesoCascadoMenor CHECK (PesoCascadoGramas <= PesoBrutoGramas),
    CONSTRAINT CK_Compra_CustoPositivo CHECK (CustoCentavos IS NULL OR CustoCentavos >= 0),
    CONSTRAINT CK_Compra_Cancelamento CHECK (
        (CanceladaEm IS NULL AND MotivoCancelamento IS NULL) OR
        (CanceladaEm IS NOT NULL AND is_utc_instant(CanceladaEm) AND length(trim(MotivoCancelamento)) > 0)
    )
) STRICT;

CREATE INDEX IX_Compra_Fornecedor_Data ON CompraMandioca(FornecedorId, DataCompra);
CREATE INDEX IX_Compra_Data ON CompraMandioca(DataCompra);
CREATE INDEX IX_Fornecedor_Ativo_Nome ON Fornecedor(Ativo, Nome);
CREATE INDEX IX_Config_VigenteDesde ON ConfiguracaoRendimento(VigenteDesde);

CREATE TRIGGER TR_Compra_FornecedorAtivo BEFORE INSERT ON CompraMandioca
BEGIN
    SELECT RAISE(ABORT, 'fornecedor inativo nao aceita nova compra')
    WHERE (SELECT Ativo FROM Fornecedor WHERE Id = NEW.FornecedorId) = 0;
END;

CREATE TRIGGER TR_Compra_Imutavel BEFORE UPDATE ON CompraMandioca
WHEN NEW.FornecedorId <> OLD.FornecedorId OR NEW.PesoBrutoGramas <> OLD.PesoBrutoGramas
  OR NEW.PesoCascadoGramas <> OLD.PesoCascadoGramas OR NEW.DataCompra <> OLD.DataCompra
  OR NEW.CriadoEm <> OLD.CriadoEm
  OR NEW.SnapshotPerdaBp <> OLD.SnapshotPerdaBp
  OR NEW.SnapshotGramasPorSaco <> OLD.SnapshotGramasPorSaco
  OR NEW.SnapshotTamanhoSacoGramas <> OLD.SnapshotTamanhoSacoGramas
  OR (OLD.Status = 'Cancelada')
BEGIN
    SELECT RAISE(ABORT, 'compra cancelada ou campos imutaveis nao podem ser alterados');
END;

CREATE TRIGGER TR_Compra_SemExclusao BEFORE DELETE ON CompraMandioca
BEGIN
    SELECT RAISE(ABORT, 'compra nao pode ser excluida');
END;

CREATE TRIGGER TR_Fornecedor_SemExclusao BEFORE DELETE ON Fornecedor
BEGIN
    SELECT RAISE(ABORT, 'fornecedor nao pode ser excluido');
END;

CREATE TRIGGER TR_ConfigRendimento_SemExclusao BEFORE DELETE ON ConfiguracaoRendimento
BEGIN
    SELECT RAISE(ABORT, 'configuracao de rendimento nao pode ser excluida');
END;

-- Insere configuracao padrao inicial: 30% perda, 21 kg/saco, saco de 30 kg
INSERT INTO ConfiguracaoRendimento (VigenteDesde, PercentualPadraoPerdaBp, GramasPorSacoPadrao, TamanhoSacoGramas)
VALUES ('2020-01-01', 3000, 21000, 30000);
