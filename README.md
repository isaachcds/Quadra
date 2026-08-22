# Quadra

Quadra é um aplicativo de leitura e organização de quadrinhos, livros e documentos desenvolvido com **.NET MAUI**.

O projeto nasceu com o objetivo de oferecer uma experiência rápida, organizada, personalizável e totalmente local para importar, gerenciar e ler arquivos **CBR, CBZ, PDF e EPUB** diretamente no celular.

> **Status atual:** Fase 2 em desenvolvimento — `v0.1.0-alpha`

---

## Sobre o projeto

O Quadra está sendo desenvolvido inicialmente para **Android**, com foco em uma biblioteca local e funcionamento offline.

A proposta é permitir que o usuário:

* importe arquivos CBR, CBZ, PDF e EPUB;
* organize suas obras em uma biblioteca local;
* visualize capas geradas automaticamente;
* pesquise, filtre e ordene sua biblioteca;
* organize obras em coleções personalizadas;
* leia diretamente dentro do aplicativo;
* personalize a experiência de leitura;
* acompanhe o progresso;
* continue exatamente de onde parou;
* conclua uma obra e possa iniciar a leitura novamente;
* escolha diferentes formas de navegação;
* mantenha seus arquivos e dados localmente.

A interface segue a identidade visual **Digital Codex**, criada para combinar organização editorial, foco no conteúdo e uma experiência moderna de leitura.

Futuramente, o projeto poderá receber sincronização opcional com um servidor privado, mantendo a leitura local como núcleo da experiência.

---

# Funcionalidades atuais

## Biblioteca

* Importação de arquivos CBR, CBZ, PDF e EPUB
* Cópia segura para o armazenamento interno do aplicativo
* Biblioteca persistente com SQLite
* Interface baseada no design system Digital Codex
* Grade de capas
* Estado vazio dedicado
* Seção **Continuar lendo**
* Identificação visual do formato
* Progresso individual por obra
* Tela de detalhes
* Exclusão sem apagar o arquivo original do usuário
* Estados de carregamento, erro, importação e filtro vazio
* Navegação principal por barra inferior

### Busca

Busca local por:

* título;
* nome original do arquivo;
* formato.

A busca suporta:

* correspondência parcial;
* diferença entre maiúsculas e minúsculas ignorada;
* normalização de acentos;
* debounce para evitar processamento excessivo durante digitação.

### Filtros

Filtros por formato:

* Todos
* EPUB
* PDF
* CBR/CBZ

Filtros por estado:

* Não iniciado
* Em andamento
* Concluído

### Ordenação

* Importados recentemente
* Última leitura
* Título A–Z
* Título Z–A
* Menor progresso
* Maior progresso

A preferência de ordenação é persistida localmente.

---

## Capas

* Geração automática para CBR
* Geração automática para CBZ
* Renderização da primeira página de PDFs
* Extração da capa embutida de EPUB
* Suporte a EPUB 2 e EPUB 3
* Fallback para EPUBs sem metadados de capa corretamente definidos
* Placeholder local quando não há capa disponível

---

# Detalhes da obra

A tela de detalhes exibe somente informações reais disponíveis no arquivo ou na biblioteca:

* capa;
* título;
* nome original;
* formato;
* data de importação;
* número de páginas ou capítulos;
* progresso;
* última leitura;
* tamanho da cópia interna.

Estados de leitura:

* Não iniciado
* Em andamento
* Concluído

A ação principal muda automaticamente entre:

* **Começar leitura**
* **Continuar leitura**
* **Ler novamente**

A tela também permite gerenciar as coleções às quais a obra pertence.

---

# Coleções

O Quadra permite organizar a biblioteca em coleções personalizadas.

Funcionalidades atuais:

* criação de coleções;
* nome e descrição;
* edição;
* exclusão;
* mosaico de capas reais;
* quantidade de obras;
* progresso geral;
* adicionar obras existentes;
* remover obras;
* abrir detalhes de uma obra;
* uma obra pode pertencer a várias coleções.

A estrutura utiliza uma relação **N:N** no SQLite.

Excluir uma coleção não exclui as obras e não remove seus arquivos.

Recursos planejados para versões futuras:

* subcoleções;
* séries;
* sagas;
* universos;
* hierarquias entre coleções.

---

# Leitura de CBR e CBZ

* Leitor nativo baseado em páginas
* Extração das imagens para cache local
* Ordenação natural das páginas
* Navegação horizontal por swipe
* Navegação opcional por toque nas laterais
* Controles em overlay
* Modo foco
* Auto-hide da interface
* Navegação anterior/próxima
* Slider de progresso
* Contador de páginas
* Salvamento automático
* Retomada da leitura
* Detecção de conclusão
* Opção **Ler novamente**

---

# Leitura de PDF

* Leitura totalmente offline
* Renderização local pelo Android `PdfRenderer`
* Cache das páginas renderizadas
* Mesmo leitor utilizado por CBR e CBZ
* Navegação por swipe
* Navegação opcional pelas bordas
* Controles em overlay
* Progresso e retomada
* Detecção de conclusão
* Opção **Ler novamente**

O processamento inclui limites de segurança para páginas, dimensões e armazenamento.

---

# Zoom e gestos

Para CBR, CBZ e PDF:

* zoom por toque duplo;
* movimentação da imagem ampliada;
* bloqueio da troca de página enquanto ampliado;
* bloqueio da navegação pelas bordas durante zoom;
* restauração da escala com novo toque duplo;
* liberação automática do swipe ao retornar à escala normal.

O pinch-to-zoom não é utilizado atualmente para priorizar estabilidade.

---

# Leitura de EPUB

* Extração local
* Leitura offline
* Navegação por capítulos
* WebView protegido
* HTML, CSS e imagens internas
* Scroll vertical dentro dos capítulos
* Navegação entre capítulos
* Progresso e retomada
* Detecção de conclusão
* Controles em overlay
* Modo foco
* Personalização visual

## Aparência EPUB

Preferências disponíveis:

* Tema Claro
* Tema Escuro
* Tema Sépia
* Fonte do sistema
* Sans Serif
* Serif
* Tamanho do texto
* Espaçamento entre linhas
* Margens
* Alinhamento à esquerda
* Texto justificado

As preferências são persistidas localmente.

O CSS de leitura também adapta:

* imagens;
* tabelas;
* blocos de código;
* largura do conteúdo;
* margens;
* tipografia.

> A experiência de interação dos controles do leitor EPUB ainda está em refinamento durante a Fase 2.

---

# Configurações

A tela de Configurações utiliza apenas opções e informações reais do aplicativo.

## Aparência

* Tema do sistema
* Tema claro
* Tema escuro

## Biblioteca

* Preferência de ordenação

## Leitura

* Apenas deslizar
* Deslizar e tocar nas laterais
* Resumo das preferências EPUB

## Armazenamento

Informações reais sobre:

* biblioteca;
* capas;
* cache de quadrinhos;
* cache de EPUB;
* cache de PDF;
* espaço disponível.

Também é possível limpar caches regeneráveis sem apagar:

* obras;
* capas necessárias;
* banco;
* progresso.

## Privacidade

O Quadra atualmente:

* processa os arquivos localmente;
* mantém a biblioteca no dispositivo;
* não exige conta;
* não utiliza servidor para a leitura.

---

# Segurança e integridade

A Fase 2 adicionou diversas proteções ao processamento de arquivos.

## Importação atômica

Arquivos temporários utilizam `.partial` e somente são promovidos ao destino final após gravação válida.

Isso reduz o risco de arquivos incompletos serem considerados válidos.

## Espaço disponível

No Android, o espaço real do volume é consultado utilizando `StatFs`.

O aplicativo diferencia:

* espaço suficiente;
* espaço insuficiente;
* espaço desconhecido;
* limite de processamento excedido.

## Arquivos compactados

Existem limites para:

* quantidade de entradas;
* tamanho expandido;
* tamanho de caminhos;
* páginas;
* bitmaps PDF.

## EPUB

Proteções incluem:

* prevenção de path traversal;
* resolução segura de recursos;
* bloqueio de scripts provenientes do EPUB;
* bloqueio de navegação externa automática;
* confirmação antes de abrir links HTTP/HTTPS;
* sanitização de referências HTML/CSS.

---

# Armazenamento e limpeza

* SQLite para persistência da biblioteca
* Armazenamento interno dos arquivos importados
* Cache separado para quadrinhos, PDFs e EPUBs
* Escritas atômicas
* Limpeza de páginas extraídas de CBR/CBZ
* Limpeza de páginas renderizadas de PDF
* Limpeza do conteúdo extraído de EPUB
* Remoção das relações de coleção ao excluir uma obra
* Preservação do arquivo original externo

---

# Formatos suportados

| Formato | Importação | Capa | Leitura | Progresso | Offline |
| ------- | ---------: | ---: | ------: | --------: | ------: |
| CBR     |          ✅ |    ✅ |       ✅ |         ✅ |       ✅ |
| CBZ     |          ✅ |    ✅ |       ✅ |         ✅ |       ✅ |
| PDF     |          ✅ |    ✅ |       ✅ |         ✅ |       ✅ |
| EPUB    |          ✅ |    ✅ |       ✅ |         ✅ |       ✅ |

---

# Tecnologias utilizadas

* .NET 10
* .NET MAUI
* C#
* XAML
* CommunityToolkit.Mvvm
* SQLite
* sqlite-net-pcl
* SourceGear.sqlite3
* SharpCompress
* VersOne.Epub
* Android PdfRenderer
* WebView
* Shell Navigation
* Dependency Injection

---

# Arquitetura

O Quadra utiliza **MVVM** e separa as responsabilidades do aplicativo em áreas específicas.

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
```

A organização evita concentrar apresentação, políticas e infraestrutura dentro de uma única pasta de serviços.

Os conceitos próprios do domínio utilizam progressivamente nomenclatura em português, enquanto convenções técnicas do .NET permanecem em inglês.

Exemplos:

```text
BibliotecaPage
BibliotecaViewModel

DetalhesObraPage
DetalhesObraViewModel

LeitorPage
LeitorViewModel

LeitorEpubPage
LeitorEpubViewModel

ColecoesPage
DetalhesColecaoPage
```

---

# Processamento dos formatos

## CBR

```text
CBR
└── SharpCompress
    └── extração segura
        └── cache local
            └── LeitorPage
```

## CBZ

```text
CBZ
└── System.IO.Compression
    └── extração segura
        └── cache local
            └── LeitorPage
```

## PDF

```text
PDF
└── ILeitorPdfService
    └── Android PdfRenderer
        └── cache local
            └── LeitorPage
```

## EPUB

```text
EPUB
└── ILeitorEpubService
    └── extração e sanitização
        └── capítulos locais
            └── WebView
                └── LeitorEpubPage
```

---

# Testes

O projeto possui um projeto separado de testes automatizados:

```text
Quadra.App.Tests
```

Atualmente:

> **133 testes automatizados aprovados**

A suíte cobre áreas como:

* ordenação natural;
* progresso;
* estados de leitura;
* filtros;
* busca;
* ordenação da biblioteca;
* caminhos EPUB;
* formatos suportados;
* inicialização concorrente do SQLite;
* armazenamento;
* escrita atômica;
* apresentação dos detalhes;
* comportamento dos leitores;
* XAML crítico.

Os builds Android Debug e Release são utilizados como validação obrigatória nas etapas principais.

---

# Design System

A Fase 2 introduziu a identidade **Digital Codex**.

Princípios:

* visual moderno e editorial;
* grade modular;
* capas como elemento principal;
* superfícies em camadas;
* fundo grafite;
* destaque turquesa;
* bordas discretas;
* poucos efeitos decorativos;
* foco na leitura;
* suporte a tema claro e escuro.

Os recursos visuais são centralizados em:

```text
Resources/
└── Styles/
    ├── Colors.xaml
    ├── Typography.xaml
    ├── Spacing.xaml
    ├── Controls.xaml
    └── Reader.xaml
```

---

# Estado do desenvolvimento

## Fase 1 — Concluída ✅

Base funcional:

* importação;
* biblioteca;
* capas;
* leitura;
* progresso;
* retomada;
* zoom;
* armazenamento local.

## Fase 2 — Em desenvolvimento 🚧

Concluído:

* fundação técnica;
* design system;
* Biblioteca redesenhada;
* navegação principal;
* busca;
* filtros;
* ordenação;
* detalhes da obra;
* leitor CBR/CBZ/PDF redesenhado;
* leitor EPUB redesenhado;
* personalização EPUB;
* Configurações;
* Coleções;
* reforço de segurança;
* reorganização estrutural;
* ampliação dos testes.

Ainda planejado para o fechamento da Fase 2:

* Histórico;
* “Abrir com Quadra” no Android;
* refinamento do UX do EPUB;
* polimento visual;
* logo/ícone definitivo;
* integração da fonte Inter;
* melhorias na responsividade da grade;
* rodada final de testes e smoke tests Android.

---

# Roadmap futuro

Após a Fase 2, algumas possibilidades são:

* subcoleções;
* organização por séries e universos;
* histórico detalhado por sessões;
* estatísticas de leitura;
* leitura vertical/webtoon;
* direção de leitura RTL;
* renderização progressiva de PDF;
* metadados automáticos;
* sincronização opcional;
* servidor privado;
* backup;
* sincronização entre dispositivos;
* recursos avançados de personalização.

A leitura offline e o controle local dos arquivos continuarão sendo o núcleo do Quadra.
