CREATE TABLE ContaCaixa (
    Id INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL CONSTRAINT CK_Conta_Nome CHECK (length(trim(Nome)) > 0),
    Ativa INTEGER NOT NULL DEFAULT 1 CONSTRAINT CK_Conta_Ativa CHECK (Ativa IN (0, 1))
) STRICT;

CREATE TABLE CategoriaMovimento (
    Id INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL CONSTRAINT CK_Cat_Nome CHECK (length(trim(Nome)) > 0),
    Tipo TEXT NOT NULL CONSTRAINT CK_Cat_Tipo CHECK (Tipo IN ('Entrada', 'Saida')),
    Natureza TEXT NOT NULL CONSTRAINT CK_Cat_Natureza CHECK (Natureza IN ('Operacional', 'NaoOperacional')),
    Ativa INTEGER NOT NULL DEFAULT 1 CONSTRAINT CK_Cat_Ativa CHECK (Ativa IN (0, 1))
) STRICT;

CREATE TABLE MovimentoCaixa (
    Id INTEGER PRIMARY KEY,
    Tipo TEXT NOT NULL CONSTRAINT CK_Mov_Tipo CHECK (Tipo IN ('Entrada', 'Saida')),
    DataMovimento TEXT NOT NULL CONSTRAINT CK_Mov_Data CHECK (is_operation_date(DataMovimento)),
    DataRegistro TEXT NOT NULL CONSTRAINT CK_Mov_Registro CHECK (is_utc_instant(DataRegistro)),
    ContaId INTEGER NOT NULL REFERENCES ContaCaixa(Id) ON DELETE RESTRICT,
    CategoriaId INTEGER NOT NULL REFERENCES CategoriaMovimento(Id) ON DELETE RESTRICT,
    ValorCentavos INTEGER NOT NULL CONSTRAINT CK_Mov_Valor CHECK (ValorCentavos > 0),
    Descricao TEXT NOT NULL CONSTRAINT CK_Mov_Descricao CHECK (length(trim(Descricao)) > 0),
    Beneficiario TEXT,
    OrigemTipo TEXT NOT NULL CONSTRAINT CK_Mov_Origem CHECK (OrigemTipo IN ('Manual', 'PagamentoCliente', 'CompraMandioca')),
    OrigemId INTEGER,
    Status TEXT NOT NULL DEFAULT 'Ativo' CONSTRAINT CK_Mov_Status CHECK (Status IN ('Ativo', 'Cancelado')),
    CanceladoEm TEXT,
    MotivoCancelamento TEXT,
    CONSTRAINT CK_Mov_Cancelamento CHECK (
        (CanceladoEm IS NULL AND MotivoCancelamento IS NULL) OR
        (CanceladoEm IS NOT NULL AND is_utc_instant(CanceladoEm) AND length(trim(MotivoCancelamento)) > 0)
    ),
    CONSTRAINT CK_Mov_OrigemId CHECK (
        (OrigemTipo = 'Manual' AND OrigemId IS NULL) OR
        (OrigemTipo <> 'Manual' AND OrigemId IS NOT NULL)
    )
) STRICT;

CREATE TABLE ConferenciaSaldo (
    Id INTEGER PRIMARY KEY,
    ContaId INTEGER NOT NULL REFERENCES ContaCaixa(Id) ON DELETE RESTRICT,
    DataConferencia TEXT NOT NULL CONSTRAINT CK_Conf_Data CHECK (is_operation_date(DataConferencia)),
    SaldoInformadoCentavos INTEGER NOT NULL,
    SaldoSistemaCentavos INTEGER NOT NULL,
    DiferencaCentavos INTEGER NOT NULL,
    Observacao TEXT,
    CriadoEm TEXT NOT NULL CONSTRAINT CK_Conf_CriadoEm CHECK (is_utc_instant(CriadoEm))
) STRICT;

CREATE INDEX IX_Mov_Data ON MovimentoCaixa(DataMovimento);
CREATE INDEX IX_Mov_Conta ON MovimentoCaixa(ContaId, DataMovimento);
CREATE INDEX IX_Mov_Categoria ON MovimentoCaixa(CategoriaId);
CREATE INDEX IX_Mov_Origem ON MovimentoCaixa(OrigemTipo, OrigemId);
CREATE INDEX IX_Conf_Conta ON ConferenciaSaldo(ContaId, DataConferencia);

-- Tipo do movimento deve ser compativel com o tipo da categoria
CREATE TRIGGER TR_Mov_TipoCategoria BEFORE INSERT ON MovimentoCaixa
BEGIN
    SELECT RAISE(ABORT, 'tipo do movimento incompativel com a categoria')
    WHERE (SELECT Tipo FROM CategoriaMovimento WHERE Id = NEW.CategoriaId) <> NEW.Tipo;
END;

-- Conta deve estar ativa
CREATE TRIGGER TR_Mov_ContaAtiva BEFORE INSERT ON MovimentoCaixa
BEGIN
    SELECT RAISE(ABORT, 'conta inativa nao aceita novo movimento')
    WHERE (SELECT Ativa FROM ContaCaixa WHERE Id = NEW.ContaId) = 0;
END;

-- Movimento cancelado nao pode ser alterado (exceto para cancelar)
CREATE TRIGGER TR_Mov_Imutavel BEFORE UPDATE ON MovimentoCaixa
WHEN OLD.Status = 'Cancelado'
  OR NEW.Tipo <> OLD.Tipo
  OR NEW.DataMovimento <> OLD.DataMovimento
  OR NEW.DataRegistro <> OLD.DataRegistro
  OR NEW.ContaId <> OLD.ContaId
  OR NEW.CategoriaId <> OLD.CategoriaId
  OR NEW.ValorCentavos <> OLD.ValorCentavos
  OR NEW.OrigemTipo <> OLD.OrigemTipo
  OR NEW.OrigemId IS NOT OLD.OrigemId
BEGIN
    SELECT RAISE(ABORT, 'movimento cancelado ou campos imutaveis nao podem ser alterados');
END;

CREATE TRIGGER TR_Mov_SemExclusao BEFORE DELETE ON MovimentoCaixa
BEGIN
    SELECT RAISE(ABORT, 'movimento nao pode ser excluido');
END;

CREATE TRIGGER TR_Conta_SemExclusao BEFORE DELETE ON ContaCaixa
BEGIN
    SELECT RAISE(ABORT, 'conta nao pode ser excluida');
END;

CREATE TRIGGER TR_Cat_SemExclusao BEFORE DELETE ON CategoriaMovimento
BEGIN
    SELECT RAISE(ABORT, 'categoria nao pode ser excluida');
END;

CREATE TRIGGER TR_Conf_SemExclusao BEFORE DELETE ON ConferenciaSaldo
BEGIN
    SELECT RAISE(ABORT, 'conferencia nao pode ser excluida');
END;

CREATE TRIGGER TR_Conf_Imutavel BEFORE UPDATE ON ConferenciaSaldo
BEGIN
    SELECT RAISE(ABORT, 'conferencia nao pode ser alterada');
END;

-- Seed: contas iniciais
INSERT INTO ContaCaixa (Nome) VALUES ('Dinheiro');
INSERT INTO ContaCaixa (Nome) VALUES ('Banco');

-- Seed: categorias - Entrada Operacional
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Recebimento de cliente', 'Entrada', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Venda a vista', 'Entrada', 'Operacional');

-- Seed: categorias - Saida Operacional
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Compra de mandioca', 'Saida', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Funcionario', 'Saida', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Combustivel', 'Saida', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Energia e agua', 'Saida', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Embalagem', 'Saida', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Manutencao', 'Saida', 'Operacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Outros', 'Saida', 'Operacional');

-- Seed: categorias - NaoOperacional
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Aporte do dono', 'Entrada', 'NaoOperacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Retirada do dono', 'Saida', 'NaoOperacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Ajuste de caixa', 'Entrada', 'NaoOperacional');
INSERT INTO CategoriaMovimento (Nome, Tipo, Natureza) VALUES ('Ajuste de caixa', 'Saida', 'NaoOperacional');
