# Fase 2 — Configurações gerais

Implementa tema do aplicativo persistido (Sistema, Claro e Escuro), ordenação persistida da biblioteca, navegação por toque dos leitores de páginas e o resumo das preferências EPUB já existentes.

Armazenamento é calculado a partir dos arquivos e capas registrados no banco, dos caches regeneráveis `Comics`, `EpubBooks` e `PdfPages`, e do espaço disponível informado pelo serviço de plataforma. A limpeza remove somente esses três caches; obras, banco, progresso e capas não são removidos.

Privacidade: processamento offline/local, sem conta ou servidor.
