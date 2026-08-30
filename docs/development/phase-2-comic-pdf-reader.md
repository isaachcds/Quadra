# Fase 2 — Leitor de CBR, CBZ e PDF

## Objetivo

O leitor compartilhado foi redesenhado para conteúdo em tela inteira com controles em overlay, preservando o motor atual de páginas, cache, progresso e zoom.

O leitor de EPUB não foi alterado.

## Ajustes estruturais

Três responsabilidades foram centralizadas:

1. `ReaderPresentationLogic` calcula contador, índices, progresso e decisão das zonas de toque.
2. `ReaderFocusState` controla visibilidade dos overlays e abertura das configurações.
3. `ReaderCloseCoordinator` garante que cancelamento e flush final ocorram uma única vez.

Isso permite testar as regras sem inicializar UI MAUI e evita duplicar o encerramento entre botão interno, botão físico e lifecycle.

## Conteúdo e overlays

O `CarouselView` permanece como conteúdo principal e ocupa toda a tela. Cada item continua usando `ZoomableImage` com `AspectFit`.

O overlay superior contém:

- voltar;
- título truncado em uma linha;
- contador atual/total;
- configurações.

O overlay inferior contém:

- slider limitado aos índices válidos;
- página anterior;
- contador;
- próxima página.

Os overlays são desenhados sobre o conteúdo e não alteram seu tamanho. Quando ocultos, uma linha fina mantém o progresso visível.

## Modo foco

- controles começam visíveis;
- são ocultados automaticamente após aproximadamente quatro segundos;
- toque central alterna a visibilidade;
- swipe, mudança de página, zoom e ações reiniciam o prazo;
- configurações abertas cancelam o auto-hide e mantêm os controles visíveis;
- `CancellationTokenSource` substitui qualquer tarefa anterior, evitando timers órfãos.

## Navegação

As preferências existentes continuam usando a chave `ReaderTapNavigationEnabled`:

- **Somente deslizar:** bordas não mudam de página e o centro alterna os controles.
- **Deslizar e tocar nas bordas:** esquerda volta, direita avança e centro alterna os controles.

As zonas são bloqueadas enquanto houver zoom, pan, swipe ou reconhecimento de toque duplo. O toque simples aguarda uma janela curta e é cancelado pelo toque duplo, evitando avanço duplicado.

Anterior e próximo são desabilitados nos limites. O slider atualiza a página e persiste somente quando o arraste termina.

## Zoom

Foram preservados:

- toque duplo para ampliar em 2,5× ou restaurar;
- pan com um dedo quando ampliado;
- bloqueio do swipe durante zoom;
- limites de translação;
- reset ao trocar ou reutilizar a página.

Pinch não foi adicionado.

## Progresso e lifecycle

O debounce de 250 ms continua sendo usado durante navegação normal. A mudança do slider gera uma única atualização ao finalizar o gesto.

No fechamento:

1. auto-hide é cancelado;
2. preparação é cancelada;
3. progresso final é gravado sem debounce;
4. chamadas repetidas reutilizam a mesma tarefa;
5. carregamento e auto-hide não atualizam UI depois da saída.

## Configurações

O antigo `ActionSheet` foi substituído por um painel interno contendo somente opções reais:

- Somente deslizar;
- Deslizar e tocar nas bordas;
- instrução para toque duplo.

Não foram adicionadas opções futuras.

## Estados

- preparando leitura, com voltar/cancelar;
- leitura válida;
- erro amigável sem caminho ou stack trace;
- nenhuma página válida;
- fechamento seguro durante preparação.

## Acessibilidade

Foram adicionados os AutomationIds solicitados para Page, Carousel, overlays, botões, contador, slider, zonas de toque, configurações, preferências e estados.

Os controles possuem descrições semânticas, títulos truncados e alvos mínimos de 48 pontos. Os valores selecionados no painel são anunciados.

## Recursos

- cores e dimensões vêm de `Reader.xaml` e dos estilos compartilhados;
- textos novos foram adicionados a `Strings.xaml`;
- `icon_back.svg`, `icon_close.svg` e `icon_settings.svg` foram reutilizados;
- somente `icon_forward.svg` foi criado.

A Page não contém cores hexadecimais.

## Testes

`ReaderPresentationLogicTests` cobre:

- contador em primeira, intermediária, última e página única;
- índices abaixo e acima do intervalo;
- disponibilidade de anterior e próximo;
- limites do slider;
- progresso entre 0 e 100%;
- estado inicial e toggle dos controles;
- abertura e fechamento das configurações;
- modos de swipe e toque lateral;
- bloqueio durante zoom, pan, swipe e toque duplo;
- fechamento idempotente e flush único.

Não foram criados testes de screenshot.

## Limitações

- não há pinch, leitura vertical, RTL ou miniaturas;
- PDF continua usando o renderer e cache existentes;
- o teste visual completo depende de aparelho Android desbloqueado;
- gestos simultâneos devem receber validação manual em aparelhos de diferentes fabricantes.

## Próximos passos

1. Validar CBR, CBZ e PDF em aparelho físico.
2. Testar swipe, bordas e toque duplo em sequência rápida.
3. Confirmar safe area em aparelhos com notch e navegação por gestos.
4. Validar tema e orientação suportada sem implementar rotação nesta etapa.
