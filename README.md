# Quadra

Quadra é um aplicativo Android de leitura e organização de quadrinhos, livros e documentos desenvolvido com **.NET MAUI**.

O projeto foi criado com foco em uma experiência rápida, organizada, personalizável e offline para importar, organizar e ler arquivos **CBR, CBZ, PDF e EPUB** diretamente no celular.

> **Status atual:** Fase 2 concluída — `v0.2.0-alpha`

---

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Funcionalidades](docs/FEATURES.md)
- [Roadmap](docs/ROADMAP.md)
- [Registros de desenvolvimento](docs/development/README.md)

---

## Sobre o projeto

O Quadra é desenvolvido inicialmente para Android e tem como prioridade o funcionamento local e offline.

O aplicativo permite:

- importar arquivos CBR, CBZ, PDF e EPUB;
- organizar obras em uma biblioteca local;
- gerar capas automaticamente;
- pesquisar, filtrar e ordenar a biblioteca;
- organizar obras em coleções;
- acompanhar o progresso de leitura;
- continuar exatamente de onde parou;
- consultar o histórico recente;
- personalizar a experiência de leitura;
- abrir arquivos diretamente pelo Android usando **Abrir com Quadra**;
- manter os dados e arquivos processados localmente.

A interface utiliza uma identidade visual própria, com foco em leitura, organização e destaque para as capas das obras.

---

## Como executar

Pré-requisitos: .NET 10 SDK, workload .NET MAUI, Android SDK configurado e Visual Studio 2022 compatível ou a CLI do .NET.

Caso o workload MAUI ainda não esteja instalado:

```bash
dotnet workload install maui
```

Para restaurar e compilar o target Android:

```bash
dotnet restore
dotnet build .\Quadra.App\Quadra.App.csproj -f net10.0-android
```

---

# Funcionalidades

## Biblioteca

- Importação de CBR, CBZ, PDF e EPUB
- Biblioteca persistente com SQLite
- Grade responsiva de obras
- Capas geradas automaticamente
- Seção **Continuar lendo**
- Busca local
- Filtros por formato
- Filtros por status de leitura
- Ordenação
- Progresso visual
- Estados de carregamento, erro e biblioteca vazia
- Navegação inferior entre as principais áreas

### Busca

A busca considera:

- título;
- nome original do arquivo;
- formato.

Possui:

- correspondência parcial;
- busca case-insensitive;
- normalização de acentos;
- debounce durante digitação.

### Filtros

Por formato:

- Todos
- EPUB
- PDF
- CBR/CBZ

Por estado:

- Não iniciado
- Em andamento
- Concluído

### Ordenação

- Importados recentemente
- Última leitura
- Título A–Z
- Título Z–A
- Menor progresso
- Maior progresso

A ordenação selecionada é persistida localmente.

---

# Detalhes da obra

Cada obra possui uma tela de detalhes com dados reais da biblioteca.

São exibidos:

- capa;
- título;
- nome original;
- formato;
- progresso;
- página ou capítulo atual;
- última leitura;
- informações do arquivo.

A ação principal muda de acordo com o estado:

- **Começar leitura**
- **Continuar leitura**
- **Ler novamente**

A tela também permite:

- adicionar ou remover a obra de coleções;
- criar novas coleções;
- excluir a obra da biblioteca.

---

# Coleções

O Quadra permite organizar as obras em coleções personalizadas.

Funcionalidades:

- criar coleção;
- editar nome e descrição;
- excluir coleção;
- adicionar obras existentes;
- remover obras sem excluir os arquivos;
- permitir que uma obra pertença a várias coleções;
- exibir mosaico de capas;
- quantidade de obras;
- progresso geral da coleção;
- abrir detalhes de cada obra.

A persistência utiliza uma relação **N:N** no SQLite.

Excluir uma coleção não exclui as obras associadas.

---

# Histórico

O histórico utiliza os dados reais de última leitura das obras.

Exibe:

- capa;
- título;
- formato;
- data e hora da última leitura;
- progresso;
- posição atual;
- ação para continuar ou reiniciar a leitura.

As obras são ordenadas da leitura mais recente para a mais antiga.

---

# Leitor CBR e CBZ

- Leitor nativo por páginas
- Extração local para cache
- Ordenação natural das imagens
- Navegação horizontal por swipe
- Navegação opcional pelas laterais
- Toque central para mostrar ou ocultar controles
- Controles com auto-hide
- Modo foco
- Contador de páginas
- Slider de progresso
- Salvamento automático
- Retomada da leitura
- Detecção de conclusão
- Opção de ler novamente

---

# Leitor PDF

- Leitura totalmente offline
- Renderização local usando Android `PdfRenderer`
- Cache das páginas renderizadas
- Mesmo leitor utilizado por CBR e CBZ
- Swipe
- Navegação lateral opcional
- Progresso e retomada
- Auto-hide dos controles
- Detecção de conclusão
- Opção de ler novamente

---

# Zoom e gestos

Em CBR, CBZ e PDF:

- zoom por toque duplo;
- movimentação da imagem ampliada;
- bloqueio da mudança de página enquanto ampliado;
- retorno à escala normal com novo toque duplo;
- swipe liberado automaticamente ao voltar para escala normal.

O pinch-to-zoom ainda não faz parte da versão atual e está planejado como melhoria futura.

---

# Leitor EPUB

- Extração local
- Leitura offline
- Navegação por capítulos
- Scroll vertical
- WebView protegido
- HTML, CSS e imagens internas
- Progresso e retomada
- Controles em overlay
- Auto-hide
- Modo foco
- Navegação entre capítulos
- Personalização da aparência

## Aparência EPUB

Preferências disponíveis:

- Tema Claro
- Tema Escuro
- Tema Sépia
- Fonte do sistema
- Sans Serif
- Serif
- Tamanho do texto
- Espaçamento entre linhas
- Margens
- Alinhamento à esquerda
- Texto justificado

As preferências são persistidas localmente.

---

# Configurações

## Aparência

- Tema do sistema
- Tema claro
- Tema escuro

## Biblioteca

- Preferência de ordenação

## Leitura

- Apenas deslizar
- Deslizar e tocar nas laterais
- Resumo das preferências EPUB

## Armazenamento

Exibe dados reais sobre:

- biblioteca;
- capas;
- cache CBR/CBZ;
- cache PDF;
- cache EPUB;
- espaço disponível.

Também é possível limpar caches regeneráveis sem apagar:

- biblioteca;
- arquivos das obras;
- banco;
- progresso.

---

# Abrir com Quadra

No Android, arquivos compatíveis podem ser enviados diretamente para o Quadra através da ação **Abrir com**.

Formatos suportados:

- CBR
- CBZ
- PDF
- EPUB

O aplicativo:

1. recebe a URI fornecida pelo Android;
2. abre o conteúdo através do `ContentResolver`;
3. valida o arquivo;
4. reutiliza o mesmo pipeline de importação da Biblioteca;
5. copia o arquivo para o armazenamento interno;
6. adiciona a obra à biblioteca;
7. abre a tela de detalhes.

O arquivo original externo não é alterado.

---

# Capas

- CBR
- CBZ
- PDF
- EPUB 2
- EPUB 3
- Fallback para EPUB sem capa corretamente definida

Para PDF, a primeira página é renderizada localmente.

---

# Segurança e integridade

O Quadra possui proteções para processamento de arquivos locais.

Entre elas:

- escrita atômica com arquivos `.partial`;
- verificação de espaço disponível;
- limites para arquivos e conteúdo extraído;
- limites de páginas e bitmaps;
- validação estrutural dos formatos;
- prevenção de path traversal em EPUB;
- sanitização de referências HTML/CSS;
- bloqueio de scripts externos;
- tratamento seguro de `content://`;
- regeneração de caches incompletos.

---

# Armazenamento

O aplicativo utiliza armazenamento interno para:

- obras importadas;
- capas;
- cache de quadrinhos;
- cache de PDFs;
- conteúdo extraído de EPUB;
- banco SQLite.

O arquivo original selecionado pelo usuário é preservado.

---

# Formatos suportados

| Formato | Importação | Capa | Leitura | Progresso | Offline |
|---|---:|---:|---:|---:|---:|
| CBR | ✅ | ✅ | ✅ | ✅ | ✅ |
| CBZ | ✅ | ✅ | ✅ | ✅ | ✅ |
| PDF | ✅ | ✅ | ✅ | ✅ | ✅ |
| EPUB | ✅ | ✅ | ✅ | ✅ | ✅ |

---

# Tecnologias utilizadas

- .NET 10
- .NET MAUI
- C#
- XAML
- CommunityToolkit.Mvvm
- SQLite
- sqlite-net-pcl
- SourceGear.sqlite3
- SharpCompress
- VersOne.Epub
- Android PdfRenderer
- Android ContentResolver
- WebView
- Shell Navigation
- Dependency Injection

---

# Arquitetura

O Quadra utiliza o padrão **MVVM**, separando interface, apresentação, serviços, infraestrutura e persistência.

Estrutura principal:

```text
Quadra.App/
├── Controls/
├── Data/
├── Infrastructure/
├── Models/
├── Pages/
├── Platforms/
├── Policies/
├── Presentation/
├── Resources/
├── Services/
│   ├── Covers/
│   ├── Import/
│   ├── Readers/
│   └── Storage/
└── ViewModels/
