# Fase 2 — Coleções

Referências visuais: `cole_es` e `detalhes_da_cole_o_percy_jackson` do Stitch; foram usadas apenas para hierarquia, grade e identidade Digital Codex.

O banco possui `Collections` e `CollectionBooks`, com associação N:N entre coleção e obra. A criação das tabelas é idempotente e preserva `LibraryItems`. A remoção de uma obra limpa as relações; a remoção de uma coleção não remove obras.

Fluxos implementados: criação, edição e exclusão de coleção; inclusão e remoção de obras; abertura de detalhes da obra a partir da coleção; e painel de coleções nos detalhes da obra, inclusive criação de uma coleção associada à obra. Quantidade e progresso são recalculados a partir das obras reais. Subcoleções, universos e séries hierárquicas continuam fora do escopo desta fase.
