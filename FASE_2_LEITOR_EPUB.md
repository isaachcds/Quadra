# Fase 2 — Leitor EPUB

## Escopo implementado

- Redesign de `LeitorEpubPage` no padrão Digital Codex, em tela cheia e sem barra inferior do Shell.
- Overlays superior e inferior com modo foco, ocultação automática após quatro segundos, retorno, capítulo, progresso e navegação anterior/próxima.
- Painel de aparência para tema, fonte, tamanho, espaçamento, margens e alinhamento.

## Preferências de leitura

As opções são persistidas em `Preferences` com chaves próprias de EPUB. Valores inválidos são descartados e substituídos por defaults seguros:

- tema: Claro;
- fonte: Sistema;
- tamanho: 18 px, limitado entre 14 e 28;
- espaçamento: 1,7, limitado entre 1,2 e 2,4;
- margens: 20 px, limitadas entre 12 e 48;
- alinhamento: Justificado.

## CSS e WebView

`AparenciaLeituraEpub` centraliza a geração do CSS. O estilo usa as cores EPUB existentes em `Leitor.xaml` e preserva títulos, ênfase, imagens, tabelas e links locais. Alterações de aparência recarregam somente o HTML do capítulo atual; a Page captura e restaura a posição vertical com JavaScript controlado pelo aplicativo.

## Segurança preservada

- JavaScript do WebView continua desabilitado na configuração Android.
- Caminhos locais continuam validados por `EpubPathResolver`.
- Referências de HTML e CSS permanecem limitadas à raiz extraída da obra.
- Scripts, handlers `on*`, `iframe`, `object`, `embed`, `base`, `srcset` e meta refresh são removidos antes da exibição.
- Links HTTP/HTTPS continuam exigindo confirmação explícita; outros destinos externos são bloqueados.

## Lifecycle e progresso

- Carregamento e auto-hide usam cancelamento.
- `FecharAsync` usa o coordenador idempotente existente, cancela operações e faz flush final de progresso.
- A Page não atualiza scroll ou abre links após sair.
- O capítulo atual continua sendo salvo em `CurrentPage` e `TotalPages`, sem alterar o banco.

## Warnings MVVMTK0045

Os sete campos `[ObservableProperty]` de `LeitorEpubViewModel` foram convertidos para propriedades parciais. Isso elimina os avisos WinRT localmente, sem alteração ampla do restante dos ViewModels.

## Validação manual pendente

- Verificar os três temas em um dispositivo Android físico.
- Confirmar a preservação aproximada da posição vertical ao alterar cada opção de aparência.
- Abrir um link interno, um link HTTPS e uma referência externa malformada.
- Sair durante carregamento e durante troca de capítulo.
