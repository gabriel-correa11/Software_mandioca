# Changelog v1.2.0

Resumo de todas as alteracoes feitas nesta sessao de desenvolvimento, desde o commit `529d784`.

---

## 1. Novidades (NovidadesService)

**Arquivo criado:** `src/MandiocaCaipira.Desktop/Services/NovidadesService.cs`

Modal "O que ha de novo" exibido automaticamente quando o usuario abre o app apos uma atualizacao de versao.

- Compara a versao atual (`MauiProgram.Versao`) com a ultima versao registrada em `lastversion.txt` no diretorio de dados
- Se mudou, exibe modal com os destaques da versao
- O changelog fica em um dicionario estatico dentro da propria classe
- Ao fechar, grava a versao atual em `lastversion.txt` para nao mostrar de novo
- Propriedade `PrimeiraVez` detecta se o arquivo nunca existiu (usuario que reinstalou via Setup.exe), permitindo mostrar mensagem extra sobre a instalacao antiga

**Registrado em:** `MauiProgram.cs` — `public const string Versao = "1.2.0"` + `builder.Services.AddSingleton(new NovidadesService(Versao))`

**Modal (MainLayout.razor):**
- Estilo limpo inspirado nas release notes do VS Code
- Titulo "Mandioca Caipira 1.2.0" + data
- Secao "Destaques" com cada feature em paragrafo: **Titulo.** Descricao
- Se `PrimeiraVez`, nota sobre apagar a pasta antiga
- Funciona nos temas claro e escuro

---

## 2. Dark Mode

**Arquivos alterados:**
- `wwwroot/app.css` — variaveis semanticas + bloco `[data-theme="dark"]`
- `wwwroot/index.html` — script inline para persistencia do tema
- `Components/Layout/MainLayout.razor` — toggle de tema

### Como funciona

1. **CSS:** todas as cores da interface foram mapeadas para variaveis semanticas (`--bg`, `--bg-card`, `--bg-header`, `--text`, `--text-secondary`, `--border`, etc.). O bloco `[data-theme="dark"]` redefine todas essas variaveis.

2. **Persistencia:** `localStorage.setItem('theme', t)` no browser. Script inline no `index.html` aplica o tema antes do Blazor carregar (evita flash de tema errado):
   ```javascript
   (function() {
       var t = localStorage.getItem('theme');
       if (t) document.documentElement.setAttribute('data-theme', t);
   })();
   ```

3. **Toggle:** icone de lua/sol no rodape do sidebar, ao lado da versao. Usa SVG inline (Feather icons). Chama `setTheme()` via JS interop.

### Variaveis semanticas adicionadas

| Variavel | Claro | Escuro |
|----------|-------|--------|
| `--bg` | #F5F5F5 | #121212 |
| `--bg-card` | white | #1e1e1e |
| `--bg-header` | white | #1a1a1a |
| `--bg-hover` | #FAFAFA | #252525 |
| `--bg-selected` | #E8F5E9 | #1a2e1a |
| `--bg-input` | white | #252525 |
| `--border` | #EEEEEE | #2a2a2a |
| `--border-strong` | #E0E0E0 | #3a3a3a |
| `--text` | #212121 | #eee |
| `--text-secondary` | #757575 | #aaa |
| `--text-muted` | #9E9E9E | #888 |
| `--sidebar-bg` | #2E7D32 | #1a2e1a |
| `--metric-bg` | #E8F5E9 | #1a2e1a |
| `--btn-secondary-bg` | #EEEEEE | #2a2a2a |

---

## 3. Correcao de Cores Semanticas

**Problema:** varias paginas usavam `var(--success)`, `var(--danger)`, `var(--surface)` e `var(--green)` que nunca foram definidas no CSS. No navegador renderizavam como preto ou transparente.

**Correcao:**

| Arquivo | De | Para |
|---------|-----|------|
| FluxoCaixa.razor (4x) | `var(--success)` | `var(--green-700)` |
| FluxoCaixa.razor (4x) | `var(--danger)` | `var(--red-600)` |
| ConferenciaSaldoPage.razor (2x) | `var(--success)` | `var(--green-700)` |
| ConferenciaSaldoPage.razor (2x) | `var(--danger)` | `var(--red-600)` |
| ConferenciaSaldoPage.razor (1x) | `var(--surface)` | `var(--bg)` |
| NovaCompra.razor (1x) | `var(--green)` | `var(--green-700)` |
| NovaCompra.razor (1x) | `var(--danger)` | `var(--red-600)` |

---

## 4. Redesenho da Home

**Arquivo:** `src/MandiocaCaipira.Desktop/Components/Pages/Home.razor`

### Antes
- 5 summary cards identicos em grid (vendas, quantidade, valor, recebimentos, saldo)
- Card "Acesso rapido" com 4 botoes
- Subtitulo generico "Resumo do dia e acesso rapido"

### Depois
- **Dois metrics de destaque** em grid 1fr 1fr:
  - "Saldo em aberto" — 28px, vermelho, font-weight 700
  - "Vendido hoje" — 28px, verde, font-weight 700
- **Linha secundaria compacta:** "X vendas | X kg vendidos | R$ X recebidos" em um unico card
- **Tabela "Vendas de hoje"** mostrando cliente, quantidade, valor e saldo de cada venda do dia
- **Empty state** quando nao ha vendas
- Sem card "Acesso rapido" (a sidebar ja cumpre esse papel)
- Sem subtitulo

### Codigo novo
- Injeta `VendaService` para buscar `Vendas.Listar(inicio: hoje, fim: hoje)`
- Exibe `VendaResumo.Cliente`, `QuantidadeGramas`, `TotalCentavos`, `SaldoCentavos`

---

## 5. Remocao de Subtitulos Genericos

Subtitulos `<p>` removidos do `page-header` de 7 paginas:

| Pagina | Subtitulo removido |
|--------|--------------------|
| Home.razor | "Resumo do dia e acesso rapido" |
| NovaVenda.razor | "Preencha os dados e confira o total antes de registrar" |
| FluxoCaixa.razor | "Controle de entradas e saidas da empresa" |
| Clientes.razor | "Cadastro e gerenciamento de clientes" |
| Atualizar.razor | "Verifique e instale novas versoes do sistema" |
| Historico.razor | "Consulta de vendas e pagamentos por periodo" |
| NovoMovimento.razor | "Registre um movimento manual no caixa" |

Subtitulos **mantidos** (adicionam informacao real):

| Pagina | Subtitulo mantido | Motivo |
|--------|-------------------|--------|
| Pendencias.razor | "Vendas com saldo em aberto" | Define o que "pendencias" significa |
| ConferenciaSaldoPage.razor | "Compare o saldo do sistema com o saldo real (app do banco, gaveta)" | Explica a funcao nao-obvia |
| Pagamento.razor | "Informe o valor recebido e distribua entre as vendas em aberto" | Instrucao util |
| ImportarClientes.razor | "Selecione um arquivo (xlsx, csv ou txt) para importar nomes de clientes" | Formatos aceitos |
| Restaurar.razor | "Selecione um backup para restaurar. O banco atual sera substituido." | Aviso importante |

---

## 6. Total no Rodape de Pendencias

**Arquivo:** `src/MandiocaCaipira.Desktop/Components/Pages/Pendencias.razor`

Adicionado `<tfoot>` com soma total do saldo em aberto de todas as vendas pendentes. Aparece somente quando ha vendas na lista. Usa `font-weight:700` e classe `text-red`.

---

## 7. Diagnostico de Atualizacao (UpdateService)

**Arquivo:** `src/MandiocaCaipira.Desktop/Services/UpdateService.cs`

- Adicionada propriedade `IsInstalled` que verifica se o app foi instalado via Setup.exe (Velopack)
- Se `!IsInstalled`, o `CheckForUpdatesAsync` lanca `InvalidOperationException` com mensagem clara explicando que o usuario precisa reinstalar via Setup.exe
- Dados do usuario NAO sao afetados pela reinstalacao (ficam em `%LOCALAPPDATA%\MandiocaCaipira\`, separado do diretorio de instalacao)

---

## 8. Arquivos de Design (nao subir pro cliente)

Estes arquivos sao referencia interna de desenvolvimento e NAO devem ser incluidos no release:

- `PRODUCT.md` — contexto do produto (usuario, proposito, principios)
- `DESIGN.md` — design system documentado (cores, tipografia, componentes, regras)
- `.impeccable/design.json` — sidecar com tokens e componentes para ferramentas de design
- `doc/` — esta documentacao

### Como excluir do release

Esses arquivos ja nao sao incluidos no build de publicacao (`dotnet publish` so empacota o que esta no projeto MAUI). Se necessario, adicionar ao `.gitignore`:

```
PRODUCT.md
DESIGN.md
.impeccable/
```

---

## Resumo de Arquivos Alterados

```
NOVO  src/MandiocaCaipira.Desktop/Services/NovidadesService.cs
NOVO  PRODUCT.md
NOVO  DESIGN.md
NOVO  .impeccable/design.json
NOVO  doc/changelog-v1.2.0.md

ALTER src/MandiocaCaipira.Desktop/Components/Layout/MainLayout.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/Home.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/FluxoCaixa.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/ConferenciaSaldoPage.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/NovaCompra.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/NovaVenda.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/Clientes.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/Atualizar.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/Historico.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/NovoMovimento.razor
ALTER src/MandiocaCaipira.Desktop/Components/Pages/Pendencias.razor
ALTER src/MandiocaCaipira.Desktop/MauiProgram.cs
ALTER src/MandiocaCaipira.Desktop/wwwroot/app.css
ALTER src/MandiocaCaipira.Desktop/wwwroot/index.html
```

## Testes

119 testes passando (todos existentes + 16 novos do FluxoCaixa). Nenhum teste quebrado pelas alteracoes de UI.
