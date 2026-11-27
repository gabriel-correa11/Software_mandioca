---
name: Mandioca Caipira
description: Sistema de gestão para fornecedor de mandioca descascada
colors:
  green-700: "#2E7D32"
  green-600: "#388E3C"
  green-500: "#43A047"
  green-50: "#E8F5E9"
  neutral-bg: "#F5F5F5"
  neutral-surface: "#FAFAFA"
  neutral-white: "#FFFFFF"
  neutral-200: "#EEEEEE"
  neutral-300: "#E0E0E0"
  neutral-500: "#9E9E9E"
  neutral-600: "#757575"
  neutral-700: "#616161"
  neutral-800: "#424242"
  neutral-900: "#212121"
  red-600: "#E53935"
  red-50: "#FFEBEE"
  blue-600: "#1E88E5"
typography:
  body:
    fontFamily: "'Segoe UI', system-ui, -apple-system, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5
  title:
    fontFamily: "'Segoe UI', system-ui, -apple-system, sans-serif"
    fontSize: "20px"
    fontWeight: 600
    lineHeight: 1.3
  label:
    fontFamily: "'Segoe UI', system-ui, -apple-system, sans-serif"
    fontSize: "12px"
    fontWeight: 500
    lineHeight: 1.4
    letterSpacing: "0.3px"
  display:
    fontFamily: "'Segoe UI', system-ui, -apple-system, sans-serif"
    fontSize: "22px"
    fontWeight: 700
    lineHeight: 1.2
rounded:
  sm: "6px"
  md: "8px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "14px"
  lg: "18px"
  xl: "24px"
components:
  button-primary:
    backgroundColor: "{colors.green-700}"
    textColor: "{colors.neutral-white}"
    rounded: "{rounded.sm}"
    padding: "8px 18px"
  button-primary-hover:
    backgroundColor: "{colors.green-600}"
    textColor: "{colors.neutral-white}"
  button-secondary:
    backgroundColor: "{colors.neutral-200}"
    textColor: "{colors.neutral-800}"
    rounded: "{rounded.sm}"
    padding: "8px 18px"
  button-danger:
    backgroundColor: "{colors.red-600}"
    textColor: "{colors.neutral-white}"
    rounded: "{rounded.sm}"
    padding: "8px 18px"
  card:
    backgroundColor: "{colors.neutral-white}"
    rounded: "{rounded.md}"
    padding: "18px"
  input:
    backgroundColor: "{colors.neutral-white}"
    textColor: "{colors.neutral-900}"
    rounded: "{rounded.sm}"
    padding: "8px 12px"
---

# Design System: Mandioca Caipira

## Overview

**Creative North Star: "A Bancada Limpa"**

O sistema visual do Mandioca Caipira é uma bancada de trabalho bem organizada: tudo tem lugar, nada distrai, e o que importa está à mão. O verde da mandioca marca a identidade sem invadir; a interface é predominantemente neutra, com o verde reservado para ações primárias e confirmações. A densidade é média — compacta o suficiente para tabelas com muitos registros, mas com respiro suficiente para não cansar o olho durante o dia inteiro de uso.

A filosofia é de ferramenta, não de vitrine. Nenhum elemento decorativo existe sem função. Sombras são rasas e servem apenas para separar camadas (card sobre fundo). Tipografia é a do sistema operacional — Segoe UI no Windows — porque o usuário já conhece e porque elimina carregamento de fontes em um app offline. O resultado é um sistema que parece ter sido feito por quem entende o negócio, não por um template genérico.

**Key Characteristics:**
- Neutro com acento verde funcional
- Densidade média, otimizada para tabelas e formulários
- Zero decoração sem função
- Fonte do sistema, sem dependências externas
- Hierarquia por peso tipográfico, não por cor ou tamanho excessivo

## Colors

A paleta é contida. O verde é o único acento cromático, derivado da cor natural do produto. Vermelho sinaliza dívida, erro ou perigo. Azul aparece pontualmente para links. O resto é escala de cinza.

### Primary
- **Verde Mandioca** (#2E7D32): Sidebar, botões primários, badges de sucesso, valores positivos. É a cor do negócio — usada com economia para manter autoridade.
- **Verde Médio** (#388E3C): Hover de botões primários. Meio-tom para transição.
- **Verde Claro** (#43A047): Foco de inputs. Aparece apenas como anel de foco sutil.
- **Verde Fundo** (#E8F5E9): Background de alertas de sucesso, badges, metric boxes. Tonal, nunca saturado.

### Neutral
- **Fundo App** (#F5F5F5): Background geral da área de conteúdo.
- **Superfície** (#FAFAFA): Hover de linhas de tabela.
- **Branco** (#FFFFFF): Cards, inputs, cabeçalho de página.
- **Borda Leve** (#EEEEEE): Bordas padrão — separa sem pesar.
- **Borda Forte** (#E0E0E0): Bordas de inputs e divisores mais visíveis.
- **Texto Apagado** (#9E9E9E): Placeholders, timestamps, informação terciária.
- **Texto Secundário** (#757575): Labels, descrições, metadata.
- **Texto Principal** (#212121): Corpo de texto, valores, nomes.

### Semantic
- **Vermelho Dívida** (#E53935): Valores negativos, pendências, botão de cancelar. Sinaliza o que precisa de atenção.
- **Vermelho Fundo** (#FFEBEE): Background de alertas de erro.
- **Azul Info** (#1E88E5): Usado pontualmente. Reservado para links quando houver.

### Named Rules
**A Regra do Verde Contido.** Verde só aparece em: sidebar, botões primários, badges/alertas de sucesso, e valores monetários positivos. Se o verde estiver em mais de 15% de uma tela, está demais.

## Typography

**Body Font:** Segoe UI (com system-ui, -apple-system, sans-serif como fallback)

**Character:** Tipografia utilitária de sistema. Sem personalidade própria — a familiaridade é o ponto. O usuário não percebe a fonte, percebe os dados.

### Hierarchy
- **Display** (700, 22px, 1.2): Valores grandes em summary cards. Números que o dono precisa ver de longe.
- **Title** (600, 20px, 1.3): Títulos de página. Um por tela, sempre no topo.
- **Body** (400, 14px/13.5px, 1.5): Texto geral, células de tabela, conteúdo de formulário. O tamanho base do sistema.
- **Label** (500, 12px-12.5px, 1.4, letter-spacing 0.3px): Labels de formulário, cabeçalhos de tabela (uppercase), categorias na sidebar. Sempre em caixa alta nos cabeçalhos de tabela.

### Named Rules
**A Regra do Peso, Não do Tamanho.** Hierarquia se faz com font-weight (400 → 500 → 600 → 700), não inflando font-size. A diferença entre o menor texto (12px) e o maior (22px) é de menos de 2x.

## Layout

O app usa um layout shell fixo: sidebar de 220px à esquerda, conteúdo flex à direita. A sidebar nunca colapsa — é um app desktop em tela ~15", não precisa de responsividade.

Dentro do conteúdo, a estrutura é sempre: `page-header` (título + descrição) no topo com borda inferior, seguido de `page-body` com scroll vertical. O padding do body é 20px vertical e 24px horizontal.

Tabelas ocupam 100% da largura. Formulários usam grid de colunas com `minmax(200px, 1fr)` e gap de 14px. Summary cards usam grid com `minmax(180px, 1fr)`.

O ritmo de espaçamento é: 4px (micro), 8px (entre elementos próximos), 14px (entre form groups e gaps de grid), 18px (padding interno de cards), 24px (padding da página).

## Elevation & Depth

O sistema é essencialmente plano. Sombras existem mas são quase invisíveis — servem para dar uma leve separação entre o card e o fundo, não para criar drama visual.

### Shadow Vocabulary
- **Sombra Rasa** (`0 1px 2px rgba(0,0,0,0.06)`): Cards e summary cards. Percepção de que o card está "pousado" sobre o fundo, sem flutuar.
- **Sombra Base** (`0 1px 3px rgba(0,0,0,0.1), 0 1px 2px rgba(0,0,0,0.06)`): Reservada. Usada em componentes que precisam de mais separação (dropdowns, modais futuros).

### Named Rules
**A Regra da Sombra Invisível.** Se o usuário perceber a sombra conscientemente, ela está forte demais. Sombras são estruturais, não estéticas.

## Shapes

Cantos levemente arredondados: 6px para elementos interativos (botões, inputs, badges), 8px para containers (cards, alertas). Nenhum elemento é totalmente redondo (pill) exceto badges de status pequenos (10px border-radius por serem pequenos). Nenhum canto é reto (0px).

A forma dominante é o retângulo de cantos suaves. Bordas são finas (1px) e em cinza claro. A borda é o principal mecanismo de separação visual, não a sombra.

## Components

### Buttons
- **Shape:** Cantos suaves (6px)
- **Primary:** Verde #2E7D32 com texto branco, padding 8px 18px, font-weight 500, font-size 13.5px
- **Hover:** Clareia para #388E3C, transição de 150ms
- **Secondary:** Cinza #EEEEEE com texto #424242
- **Danger:** Vermelho #E53935 com texto branco
- **Disabled:** opacity 0.5, cursor not-allowed
- **Small variant:** padding 5px 12px, font-size 12.5px

### Cards / Containers
- **Corner Style:** 8px border-radius
- **Background:** Branco (#FFFFFF)
- **Shadow:** Sombra rasa (0 1px 2px rgba(0,0,0,0.06))
- **Border:** 1px solid #EEEEEE
- **Internal Padding:** 18px no body, 14px 18px no header
- **Header:** Separado por borda inferior, font-weight 600, font-size 14px

### Inputs / Fields
- **Style:** Fundo branco, borda 1px solid #E0E0E0, border-radius 6px, padding 8px 12px
- **Focus:** Borda muda para verde #43A047, anel externo de 3px em rgba(67,160,71,0.12)
- **Font:** 14px, herda font-family

### Navigation (Sidebar)
- **Background:** Verde escuro #2E7D32
- **Item:** Texto branco a 85% opacidade, padding 10px 16px, font-size 13.5px
- **Hover:** Background rgba(255,255,255,0.1), texto branco 100%
- **Active:** Background rgba(255,255,255,0.18), texto branco, font-weight 500
- **Section dividers:** Labels uppercase em rgba(255,255,255,0.4), font-size 10px, letter-spacing 1px
- **Footer:** Versão + ícone de tema em rgba(255,255,255,0.5)

### Tables
- **Header:** Uppercase, 12px, font-weight 600, cor secundária, letter-spacing 0.3px, borda inferior 2px
- **Cells:** Padding 10px 14px, borda inferior 1px em cinza claro
- **Hover:** Background #FAFAFA
- **Selected:** Background verde claro #E8F5E9

### Summary Cards (Metric Boxes)
- **Layout:** Grid responsivo com minmax(180px, 1fr)
- **Label:** 12px uppercase em cor secundária
- **Value:** 22px, font-weight 700. Verde para valores positivos, vermelho para negativos.

## Do's and Don'ts

### Do:
- **Do** usar o verde apenas para ações primárias, confirmações e a sidebar. Nunca como decoração.
- **Do** manter todos os valores monetários alinhados à direita nas tabelas.
- **Do** usar a mesma estrutura page-header + page-body em toda tela nova.
- **Do** mostrar estados vazios com texto centralizado em cor apagada (#9E9E9E).
- **Do** usar bordas finas (1px #EEEEEE) como separador principal, não sombras.

### Don't:
- **Don't** usar mais de uma cor de acento por tela além do verde e vermelho semântico.
- **Don't** usar border-radius maior que 10px em nenhum componente (exceto badges de status).
- **Don't** adicionar ícones decorativos. Ícones só onde há ambiguidade sem eles.
- **Don't** usar fontes externas. Segoe UI e o stack de sistema são suficientes.
- **Don't** criar sombras com blur maior que 3px. Se precisa de mais separação, use borda.
