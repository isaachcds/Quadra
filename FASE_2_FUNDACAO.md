# Fase 2 — Fundação técnica e visual

## 1. Correções técnicas aplicadas

- A inicialização do SQLite passou a usar uma tarefa compartilhada e thread-safe. Chamadas concorrentes aguardam a mesma criação de tabela; uma falha ou cancelamento libera uma nova tentativa. O schema, o nome e o caminho de `quadra.db3` não foram alterados.
- A exclusão foi centralizada em `LibraryCleanupService.DeleteAsync` e agora é usada pela biblioteca e pelos detalhes. A ordem é: caches de leitura, capa e cópia interna, registro SQLite. O arquivo externo original nunca é removido. Ausência de arquivo ou diretório é tratada como sucesso; se uma falha inesperada ocorrer antes da última etapa, o registro permanece e a operação pode ser repetida.
- Importação, capas, páginas extraídas/renderizadas e o marcador de EPUB usam gravação atômica no mesmo volume: `.partial`, fechamento, validação de tamanho e renomeação. Parciais são removidos em falha ou cancelamento.
- A importação valida assinatura/estrutura básica de PDF, CBZ, CBR e EPUB antes de tornar a cópia definitiva.
- O progresso do leitor de imagens/PDF passou a usar debounce curto, sequência monotônica e serialização de escrita. A interface muda imediatamente, apenas o último valor é persistido e há flush ao sair.
- Operações longas de importação, preparação e carregamento receberam cancelamento ligado ao ciclo de vida das páginas, evitando atualização tardia de navegações antigas.
- Caminhos internos do EPUB são decodificados e normalizados dentro da raiz extraída. Traversal, caminhos absolutos e URLs externas são rejeitados também em capítulos, imagens, CSS e `BaseUrl`.
- O WebView de EPUB bloqueia JavaScript e esquemas não reconhecidos. Links HTTP/HTTPS são cancelados e só abertos no navegador do sistema após confirmação.
- Arquivos de cache vazios deixam de ser considerados válidos e são regenerados.
- Os avisos de nulabilidade comprovadamente falsos nas constantes Android e nas chaves já filtradas dos arquivos compactados foram anotados de forma explícita, sem alteração funcional.

## 2. Testes criados

O projeto `Quadra.App.Tests` usa testes unitários puros, sem inicializar MAUI. Ele cobre:

- ordenação natural de nomes, números, zeros, extensões e caixa;
- progresso não iniciado, em andamento, concluído, total zero, valor acima do total e unidade de capítulos para EPUB;
- resolução segura de caminhos EPUB válidos, subpastas, `../`, absolutos, traversal codificado e saída da raiz;
- política dos formatos CBR, CBZ, PDF, EPUB e extensão inválida;
- concorrência, falha e nova tentativa da inicialização compartilhada.

## 3. Design tokens criados

Os dicionários foram registrados em `App.xaml`, mas nenhum novo estilo foi aplicado às Pages existentes:

- `Resources/Styles/Colors.xaml` — cores semânticas com variações de tema;
- `Resources/Styles/Typography.xaml` — escalas tipográficas;
- `Resources/Styles/Spacing.xaml` — espaçamento, raios e alvo mínimo de toque;
- `Resources/Styles/Controls.xaml` — estilos base reutilizáveis;
- `Resources/Styles/Reader.xaml` — tokens específicos dos leitores;
- `Resources/Strings/Strings.xaml` — primeiro conjunto de strings globais futuras.

Os recursos anteriores foram preservados para não mudar a aparência atual.

## 4. Paleta escura

| Token | Valor |
|---|---:|
| Background | `#131313` |
| SurfaceLowest | `#0E0E0E` |
| SurfaceLow | `#1C1B1B` |
| Surface | `#201F1F` |
| SurfaceHigh | `#2A2A2A` |
| SurfaceHighest | `#353534` |
| TextPrimary | `#E5E2E1` |
| TextSecondary | `#BACAC5` |
| Outline | `#859490` |
| OutlineMuted | `#3C4A46` |
| AccentPrimary | `#57F1DB` |
| AccentPrimaryMuted | `#2EA898` |
| AccentOnPrimary | `#003731` |
| AccentSecondary | `#FFB2B9` |
| AccentTertiary | `#FFD1AA` |
| Error | `#FFB4AB` |

## 5. Paleta clara

O tema claro usa fundo off-white quente, superfícies quentes discretas, texto grafite e turquesa mais escuro para manter contraste. As mesmas chaves semânticas são resolvidas por `AppThemeBinding`, permitindo trocar o tema sem espalhar cores nas telas.

## 6. Tipografia

Foram criados `DisplayLarge` (48/Bold), `HeadlineLarge` (32/Semibold), `HeadlineMobile` (28/Semibold), `HeadlineMedium` (24/Semibold), `BodyLarge` (18/Regular), `BodyMedium` (16/Regular), `LabelMedium` (14/Medium) e `LabelSmall` (12/Semibold).

A fonte Inter ainda não existe em `Resources/Fonts` e não foi baixada. OpenSans permanece como fallback temporário. A inclusão licenciada dos arquivos Inter e o registro das respectivas variações ficam para uma próxima tarefa.

## 7. Espaçamento

Foram definidos `SpacingXs=4`, `SpacingSm=8`, `SpacingMd=16`, `SpacingLg=24`, `SpacingXl=32`, `PageHorizontalMargin=20`, `GridGutter=16` e `TouchTargetMinimum=48`. Os valores antigos das Pages não foram substituídos nesta etapa.

## 8. Raios

Foram definidos `RadiusSmall=4`, `RadiusDefault=8`, `RadiusMedium=12`, `RadiusLarge=16`, `RadiusExtraLarge=24` e `RadiusPill=999`.

## 9. Estilos

Estão disponíveis, ainda sem aplicação nas telas: `PrimaryButtonStyle`, `SecondaryButtonStyle`, `DangerButtonStyle`, `CircularIconButtonStyle`, `FilterChipStyle`, `SelectedFilterChipStyle`, `CardBorderStyle`, `InputStyle`, `SectionTitleStyle`, `BodyTextStyle`, `SecondaryTextStyle`, `ProgressBarStyle`, `BottomNavigationItemStyle` e `ReaderOverlayStyle`.

## 10. Estrutura futura de componentes

A adoção visual deve ocorrer de forma incremental. Primeiro, criar controles pequenos e reutilizáveis para cartão de obra, botão circular, chip e estados vazios; depois substituir repetições uma Page por vez. Os controles devem consumir apenas tokens semânticos, manter bindings e comandos atuais e preservar handlers de gestos quando eles forem responsabilidade visual da Page.

Os leitores possuem tokens próprios para fundo, overlay, textos, progresso, superfície de controles, temas EPUB claro/escuro/sépia, margens, fonte e altura de linha. O CSS do EPUB ainda não os consome; essa integração deve ser feita junto da futura tela do leitor.

## 11. Telas que ainda não foram alteradas visualmente

- `LibraryPage`
- `BookDetailsPage`
- `ReaderPage`
- `EpubReaderPage`

As mudanças de code-behind nessas páginas são apenas de ciclo de vida, cancelamento, flush de progresso e segurança de navegação. Não houve redesign, navegação inferior, busca, filtros, coleções, histórico ou “Abrir com”.

## 12. Limitações restantes

- PDF continua sendo renderizado integralmente em PNG; renderização progressiva e política de cache por tamanho permanecem pendentes.
- Os limites atuais são globais e conservadores: importação de 4 GiB, 20 mil entradas, 16 GiB expandidos, 10 mil páginas, caminho de 512 caracteres, bitmap PDF de 40 milhões de pixels/16 mil px de altura, largura alvo de 1.400 px e reserva mínima de 512 MiB.
- A reserva mínima reduz o risco de esgotamento, mas não estima antecipadamente o custo total de cada documento.
- A sanitização de EPUB reduz referências externas e traversal, mas não é um mecanismo de DRM nem um sanitizador HTML completo. JavaScript permanece desabilitado como barreira principal.
- Não há migração nem alteração de schema; recursos futuros de biblioteca continuam exigindo uma estratégia própria de evolução do banco.
- A exclusão é repetível, mas uma falha de I/O inesperada interrompe o fluxo e mantém o registro. Uma futura UI pode apresentar ação de tentar novamente.
- Fluxos Android precisam de validação manual em aparelho/emulador com arquivos grandes, corrompidos e com pouco espaço.

## 13. Próximos passos

1. Executar smoke tests Android de importação e leitura nos quatro formatos, incluindo cancelamento e retomada.
2. Validar exclusão pelos dois pontos de entrada e simular arquivos/caches já ausentes.
3. Adicionar a família Inter ao projeto após revisar licença e pesos necessários.
4. Aplicar tokens primeiro a um componente pequeno e isolado, com comparação visual claro/escuro e acessibilidade.
5. Planejar cache limitado e renderização progressiva de PDF em uma tarefa separada.
6. Expandir testes de integração de armazenamento/SQLite em ambiente que permita isolar `FileSystem.AppDataDirectory`.
