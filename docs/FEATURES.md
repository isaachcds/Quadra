# Funcionalidades

Esta página detalha as capacidades disponíveis na versão atual. Para uma visão rápida do projeto, consulte o [README](../README.md).

## Biblioteca

- Importação local de CBR, CBZ, PDF e EPUB.
- Capas geradas a partir do conteúdo quando possível.
- Busca local por título, nome original e formato, com normalização de acentos.
- Filtros por formato e estado de leitura, além de ordenação persistida.
- Seção de continuidade, estados de carregamento, vazio, erro e resultados vazios.

## Obras e coleções

- Tela de detalhes com capa, formato, progresso, posição, última leitura e dados do arquivo.
- Ação contextual para começar, continuar ou reler.
- Coleções personalizadas com nome e descrição, mosaico de capas, quantidade e progresso agregado.
- Relação N:N: uma obra pode pertencer a várias coleções sem duplicar arquivos físicos.
- Inclusão, remoção, edição e exclusão de coleções sem excluir as obras.

## Histórico e configurações

- Histórico cronológico baseado em `LastReadAt`, com progresso e retomada de leituras reais.
- Tema Sistema, Claro ou Escuro persistido.
- Preferência de ordenação da Biblioteca e de navegação por swipe ou toque lateral.
- Resumo e acesso às preferências EPUB.
- Diagnóstico do tamanho da biblioteca, caches e espaço livre, com limpeza apenas de caches regeneráveis.
- Processamento local, offline e sem conta ou servidor.

## Leitura

- **CBR/CBZ/PDF:** leitura paginada, swipe horizontal, controles em overlay, slider, retomada, conclusão e zoom por toque duplo com pan.
- **EPUB:** leitura vertical por capítulo, capítulos anterior/próximo, retomada, progresso e painel de aparência.
- **Aparência EPUB:** temas Claro, Escuro e Sépia; fonte Sistema, Sans Serif ou Serif; tamanho, entrelinha, margens e alinhamento persistidos.

## Android

- Suporte a **Abrir com Quadra** para CBR, CBZ, PDF e EPUB.
- Recebimento de arquivos por `content://` e cópia segura para o armazenamento interno.
- Abertura animada curta com a identidade do Quadra.

## Limites conhecidos

O aplicativo não oferece sincronização, metadados online, séries hierárquicas, sessões detalhadas de leitura ou download de conteúdo. Essas possibilidades estão registradas no [roadmap](ROADMAP.md).
