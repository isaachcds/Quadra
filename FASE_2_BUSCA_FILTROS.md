# Fase 2 — Busca, filtros e ordenação

## Objetivo

A Biblioteca agora combina busca local, formato, estado de leitura e ordenação sem alterar SQLite, `LibraryItem`, importação, detalhes, leitores, progresso, exclusão ou caches.

## Pipeline único

`LibraryPresentationLogic.ApplyPipeline` processa sempre a coleção fonte completa nesta ordem:

1. busca;
2. formato;
3. estado de leitura;
4. ordenação;
5. substituição única da lista apresentada.

`LibraryViewModel.Itens` permanece como fonte intacta. Os objetos `LibraryBookViewData` são construídos somente ao carregar, importar ou excluir itens. Busca e filtros reutilizam esses objetos e não repetem consultas ao banco ou `File.Exists` a cada caractere.

## Busca

A busca considera somente dados reais:

- título;
- nome original do arquivo;
- formato/extensão armazenada.

O texto é aparado, convertido para uma representação sem distinção de caixa e normalizado para remover diacríticos. Assim, `hobbit` encontra `O Hóbbit`.

Existe debounce de 275 ms. Cada alteração cancela a atualização anterior. A consulta é local e a atualização da lista é enviada para a thread principal.

O campo substitui o cabeçalho sem abrir nova Page. Fechar mantém o texto durante a vida da Page; limpar remove imediatamente o critério.

## Formatos

- `All`: todos;
- `Comics`: CBR e CBZ;
- `Pdf`: somente PDF;
- `Epub`: somente EPUB.

Os chips rápidos e o painel usam a mesma propriedade aplicada. Abrir o painel sempre copia o valor atual.

## Estados de leitura

- `NotStarted`: `LastReadAt` ausente ou total inválido;
- `InProgress`: leitura registrada e posição anterior à conclusão;
- `Completed`: leitura registrada e posição na última página/capítulo ou além dela.

A regra é centralizada em `LibraryPresentationLogic.GetReadingStatus` e também é consumida pela apresentação da tela de detalhes. EPUB usa os mesmos valores persistidos, interpretados como capítulos pela tela de detalhes.

## Ordenações

- importados recentemente;
- última leitura, com nunca lidos ao final;
- título A–Z;
- título Z–A;
- menor progresso;
- maior progresso.

Títulos são comparados ignorando caixa e diacríticos, com comparação natural de números (`Manual 2` antes de `Manual 10`). Progresso é limitado entre 0 e 100%. Empates usam título ou data de importação para manter resultado determinístico.

## Painel de filtros

O painel inferior foi implementado apenas com controles MAUI existentes. Não foi adicionado pacote.

Ao abrir, ele cria uma cópia temporária de formato, estado e ordenação. Fechar pelo fundo ou pelo botão não altera os critérios aplicados. Somente **Aplicar filtros** confirma o estado temporário.

**Limpar filtros** dentro do painel restaura seus valores temporários. A operação global `LimparFiltrosBibliotecaCommand` também remove a busca e recalcula a coleção.

## Persistência

Somente `LibrarySortOption` é persistido em `Preferences`, pela chave `library_sort_option`.

O valor lido é validado por `ParseSortOption`. Valores desconhecidos retornam para `RecentlyImported`. Falhas de leitura ou escrita da preferência não impedem a Biblioteca de abrir.

Busca, formato e estado não são persistidos.

## Estados visuais

- Biblioteca realmente vazia continua usando `EmptyLibraryView`.
- Biblioteca com itens sem correspondência mostra **Nenhuma obra encontrada**.
- Quando somente formato está ativo, permanece a mensagem específica de formato.
- O estado sem resultados permite limpar busca, limpar filtros ou mostrar todos.
- O botão de filtros exibe um badge com o número de categorias não padrão.
- A seção **Continuar lendo** continua baseada na coleção fonte, não nos filtros.

## Acessibilidade

Foram adicionados:

- foco no campo ao abrir a busca;
- descrições semânticas para busca, limpeza, fechamento, filtro e resultado;
- descrição com quantidade de resultados;
- alvos de toque de 48 pontos;
- AutomationIds solicitados para busca, painel, badge, estados, ordenações, aplicação, limpeza e estado vazio.

## Testes

Os testes cobrem:

- busca exata, parcial, por arquivo, sem caixa, com espaços e sem diacríticos;
- busca vazia e sem resultado;
- todas as combinações de busca, formato e estado;
- limpeza/default;
- estados não iniciado, em andamento, concluído, EPUB, total zero e posições inválidas;
- todas as ordenações;
- nunca lidos ao final;
- comparação natural e desempates;
- contador de filtros;
- distinção entre Biblioteca vazia e resultado vazio;
- preferência inválida.

Não foram adicionados testes de screenshot.

## Limitações

- não há busca por autor, tags ou coleções porque esses dados não existem no modelo;
- o status depende de `LastReadAt` e dos campos de página/capítulo existentes;
- somente a ordenação é restaurada após reiniciar o aplicativo;
- a inspeção visual e os cenários manuais dependem de dispositivo Android disponível.

## Próximos passos

1. Validar temas claro e escuro em aparelho Android.
2. Exercitar manutenção dos critérios ao abrir detalhes e voltar.
3. Validar atualização da lista após importação e exclusão com filtros ativos.
4. Avaliar testes de interface automatizados quando houver infraestrutura Android dedicada.
