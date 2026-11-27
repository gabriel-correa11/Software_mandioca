# Product

<!-- impeccable:product-schema 1 -->

## Platform

web (MAUI Blazor Hybrid desktop, Windows)

## Users

O dono de uma empresa de mandioca descascada. Opera sozinho o sistema em dois PCs — um no escritório e outro na produção. Não tem conhecimento técnico; precisa de um sistema direto que funcione sem configuração.

## Product Purpose

Gerenciar vendas, cobranças e compras de um fornecedor de mandioca descascada. O dono vende para mercados e restaurantes, registra vendas a prazo, controla quem pagou e quem deve, compra matéria-prima de fornecedores rurais, e precisa saber quanto dinheiro entrou e saiu no dia. O sucesso é: não perder venda, não esquecer cobrança, saber o saldo real do caixa.

## Positioning

Sistema feito sob medida para o fluxo de um fornecedor de mandioca — compra por quilo, vende por quilo, controla fiado por cliente, e reconcilia caixa. Nenhum ERP genérico faz isso sem configuração.

## Operating Context

- O dono registra vendas durante o dia conforme entrega nos clientes
- No fim do dia confere o caixa e vê pendências
- Pagamentos são recebidos em dinheiro, PIX ou depósito bancário
- Compras de mandioca bruta são feitas de fornecedores rurais, pagas na hora ou a prazo
- O app roda em Windows 10/11, em PCs simples com tela ~15"
- Sem internet confiável — o app funciona 100% offline, dados locais em SQLite
- Atualização automática via GitHub Releases quando há conexão

## Capabilities and Constraints

Funcionalidades: cadastro de clientes, registro de vendas (quantidade em gramas, preço por kg), pagamentos com alocação automática nas vendas mais antigas, pendências (fiado), histórico, fornecedores, compras de mandioca, fluxo de caixa (entradas/saídas manuais e automáticas), conferência de saldo bancário, backup/restauração, atualização automática.

Restrições: valores monetários armazenados como centavos (long), tabelas SQLite STRICT, sem exclusão de dados (apenas cancelamento com motivo), migrações versionadas.

## Brand Commitments

Nome: Mandioca Caipira. Sem logotipo formal. O verde é a cor primária associada ao produto (mandioca/agricultura).

## Evidence on Hand

Dados reais do cliente já em produção. Sem material de marketing, logotipo, ou brand guide.

## Product Principles

1. **Funciona sem internet.** Tudo local, sincronização é luxo.
2. **Nenhum dado some.** Cancelamento com motivo, nunca exclusão.
3. **Direto ao ponto.** O dono não tem tempo pra menus profundos ou configurações.
4. **Dinheiro sempre bate.** Centavos, não decimais flutuantes. Conferência de saldo real vs sistema.
5. **Atualiza sozinho.** O dono não sabe instalar software; o app cuida disso.
