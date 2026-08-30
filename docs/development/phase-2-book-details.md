# Fase 2 — Tela de detalhes

## Objetivo

A tela de detalhes foi redesenhada com a identidade visual **Digital Codex**, sem alterar importação, banco, rotas, leitores, progresso persistido, caches ou exclusão unificada.

## Estrutura da tela

1. Cabeçalho de superfície com voltar, título e exclusão.
2. Capa destacada usando `BookCoverView` e placeholder local.
3. Título e nome original do arquivo.
4. Card **Leitura atual**, com estado, percentual, posição e barra de progresso.
5. Ação principal para começar, continuar ou ler novamente.
6. Indicador de preparação do leitor.
7. Seção **Sobre o arquivo**.
8. Ação de exclusão com aparência de perigo.

A barra inferior do Shell permanece oculta. O conteúdo usa `ScrollView`, respeita a safe area e mantém alvos de toque de no mínimo 48 pontos.

## Dados reais exibidos

- título;
- capa existente ou placeholder;
- formato;
- nome original;
- data de importação;
- total de páginas para CBR, CBZ e PDF;
- total de capítulos para EPUB;
- estado, percentual e posição de leitura;
- data e hora da última leitura, quando existente;
- tamanho da cópia interna, obtido sem persistência adicional.

O caminho interno nunca é mostrado.

## Estados

- **Carregando:** indicador enquanto banco e arquivo são consultados.
- **Válido:** conteúdo completo e ações disponíveis.
- **Arquivo ausente:** mensagem amigável, retorno à Biblioteca e exclusão do registro.
- **Erro ao carregar:** mensagem segura, tentativa novamente e retorno.
- **Preparando leitura:** ação principal desabilitada e indicador contextual.
- **Excluindo:** bloqueio visual temporário até a exclusão unificada terminar.

## Progresso

O estado visual considera `LastReadAt` para distinguir uma obra nunca aberta da primeira página ou capítulo, sem alterar o schema:

- não iniciado: 0% e **Começar leitura**;
- em andamento: percentual limitado entre 0 e 100, posição real e **Continuar leitura**;
- concluído: 100% e **Ler novamente**.

## Ações

- voltar;
- começar, continuar ou ler novamente usando o comando existente;
- excluir usando `LibraryCleanupService`, com confirmação de que somente a cópia interna e os dados do Quadra serão removidos.

Não foram adicionadas ações falsas ou desabilitadas.

## Componentes e tokens

Foram reutilizados:

- `BookCoverView`;
- `PrimaryButtonStyle`, `SecondaryButtonStyle`, `DangerButtonStyle` e `CardBorderStyle`;
- estilos tipográficos existentes;
- tokens de espaçamento, raio, superfície, texto, contorno, destaque e erro;
- strings do `Resources/Strings/Strings.xaml`.

Os únicos novos recursos gráficos são ícones SVG locais de voltar, informações e exclusão. A Page não contém cores hexadecimais.

## Acessibilidade

Foram adicionados descrições semânticas, título semântico, truncamento, alvos mínimos de toque e os AutomationIds solicitados:

- `BookDetailsPage`;
- `BookDetailsBackButton`;
- `BookDetailsCover`;
- `BookDetailsTitle`;
- `BookDetailsProgress`;
- `BookDetailsPrimaryAction`;
- `BookDetailsDeleteButton`;
- `BookDetailsFileInfo`.

## Testes

`BookDetailsPresentationTests` cobre:

- unidade correta para CBR, CBZ, PDF e EPUB;
- estados não iniciado, em andamento e concluído;
- três textos da ação principal;
- percentual limitado entre 0 e 100;
- arquivo ausente;
- tamanho em KB, MB e GB.

Não foram criados testes de screenshot.

## Funcionalidades futuras omitidas

- edição de metadados;
- favorito persistente;
- coleções;
- histórico detalhado;
- compartilhamento;
- download;
- avaliações e dados externos.

## Limitações

- autor, descrição, ano, série e tags não existem no modelo atual e não são exibidos;
- o estado “não iniciado” depende da ausência de `LastReadAt`, pois não há um campo específico no schema;
- o tamanho do arquivo fica indisponível quando a cópia interna não pode ser consultada;
- não há teste visual por screenshot nesta etapa.

## Próximos passos

1. Validar a tela em aparelho Android nos temas claro e escuro.
2. Exercitar os quatro formatos e a retomada após voltar do leitor.
3. Avaliar metadados editáveis somente em uma futura evolução incremental do banco.
4. Manter ações futuras omitidas até existirem fluxos reais.
