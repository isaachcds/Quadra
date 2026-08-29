# Fase 2 — Biblioteca e navegação principal

## Telas alteradas

- `LibraryPage` recebeu o primeiro layout “Digital Codex”, com cabeçalho, estados explícitos, seção de continuidade, filtros e grade.
- `BookDetailsPage`, `ReaderPage` e `EpubReaderPage` apenas ocultam a barra inferior quando abertas. Seus layouts e comportamentos de leitura não foram redesenhados.
- `AppShell` passou a hospedar os quatro destinos principais em um `TabBar`.

## Páginas criadas

- `CollectionsPage`: estado futuro, sem coleções fictícias.
- `HistoryPage`: estado futuro, sem histórico fictício.
- `SettingsPage`: versão real do aplicativo e aviso de evolução gradual.

Biblioteca continua sendo o primeiro destino e a página inicial. As rotas existentes de detalhes, leitor de imagens/PDF e leitor EPUB continuam registradas fora dos destinos principais.

## Componentes criados

- `BookCoverView`: capa real quando o caminho existe, placeholder local e selo de formato.
- `LibraryBookCard`: capa, título em até duas linhas, progresso real, abertura de detalhes e exclusão existente.
- `ContinueReadingCard`: resumo da obra elegível mais recente e abertura segura dos detalhes.
- `EmptyLibraryView`: estado vazio acessível com o comando real de importação.

Os componentes não acessam banco, armazenamento ou navegação diretamente.

## Tokens utilizados

A Biblioteca e a navegação usam os tokens semânticos já preparados para fundo, superfícies, texto, contorno, destaque, tipografia, espaçamento e raios. Não foram adicionadas cores hexadecimais às Pages. Tema escuro e tema claro seguem o tema do sistema.

Os oito ícones de navegação e ações, o placeholder de livro e o símbolo temporário do Quadra são SVGs locais. Não há emoji, download de imagem, fonte de ícones do sistema ou raster do protótipo.

## Estados da Biblioteca

- **Carregando:** indicador próprio enquanto o SQLite é consultado; o estado vazio não aparece prematuramente.
- **Vazia:** mensagem aprovada, formatos suportados e importação real.
- **Preenchida:** conteúdo real em grade de duas colunas e botão destacado de importação.
- **Importando:** sobreposição controlada, comando desabilitado e indicador existente.
- **Erro:** mensagem persistente e ação de tentar novamente, além do alerta já esperado pelo fluxo atual.
- **Filtro vazio:** mensagem específica e ação para voltar a “Todos”, sem confundir com biblioteca vazia.

## Filtros rápidos

Os filtros `Todos`, `EPUB`, `PDF` e `CBR/CBZ` são locais e trabalham sobre a coleção já carregada. `CBR/CBZ` inclui ambos os formatos, “Todos” é o padrão e a ordenação recebida do banco é preservada. Nenhuma consulta, coluna ou migration foi adicionada.

## Continuar lendo

A seção só aparece para um item real que satisfaça simultaneamente:

- `TotalPages > 0`;
- `CurrentPage > 0`;
- `CurrentPage < TotalPages - 1`;
- `LastReadAt` preenchido.

Entre os elegíveis, vence o `LastReadAt` mais recente. Não iniciados e concluídos são excluídos. O clique abre detalhes, preservando o fluxo atual e evitando preparação pesada na Biblioteca.

## Acessibilidade

- alvos de toque de 44–48 dp ou maiores;
- descrições semânticas para capas, ícones, importação e filtros;
- descrição dos filtros informa o estado selecionado;
- `AutomationId` nos elementos principais solicitados;
- títulos limitados a duas linhas com truncamento final;
- safe area preservada nos quatro destinos principais;
- SVGs locais com nomes e títulos textuais no Shell.

## Funcionalidades ainda não implementadas

- busca;
- filtros avançados;
- coleções reais;
- histórico real;
- configurações completas ou seletor de tema;
- densidade configurável da grade;
- edição de metadados;
- “Abrir com”.

Não foram usados dados demonstrativos, autores, avaliações, estimativas de tempo ou progresso inventado.

## Limitações

- A grade usa duas colunas por segurança em smartphones; adaptação para três colunas pode ser feita posteriormente com uma regra simples baseada na largura disponível.
- O símbolo de quatro quadrantes é temporário e não substitui o futuro trabalho de identidade do ícone do aplicativo.
- A validação visual ainda precisa ser feita em aparelho/emulador com bibliotecas vazia, pequena e grande, capas de proporções diferentes e escalas de fonte elevadas.
- O `AutomationId` dos destinos foi aplicado aos `ShellContent`; a exposição exata nos elementos nativos da barra deve ser confirmada com a ferramenta de automação Android escolhida.

## Próximos passos

1. Executar smoke test visual e funcional em Android nos temas claro e escuro.
2. Ajustar densidade da grade após medir aparelhos estreitos e largos.
3. Validar leitor de tela e escalonamento de fonte.
4. Evoluir uma das páginas futuras em tarefa separada, sem misturar domínio com apresentação.
5. Redesenhar detalhes e leitores apenas depois da aprovação desta Biblioteca.
