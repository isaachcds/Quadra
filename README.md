# Quadra

Quadra é um aplicativo de leitura e organização de quadrinhos, livros e documentos desenvolvido com .NET MAUI.

O projeto nasceu com o objetivo de oferecer uma experiência simples, rápida e personalizada para importar, organizar e ler arquivos CBR, CBZ, PDF e EPUB diretamente no celular.

> Status atual: Fase 1 concluída — v0.1.0-alpha

---

## Sobre o projeto

O Quadra está sendo desenvolvido inicialmente para Android, com foco em uma biblioteca local e funcionamento totalmente offline.

A proposta é permitir que o usuário:

- importe arquivos CBR, CBZ, PDF e EPUB;
- organize suas obras em uma biblioteca local;
- visualize capas geradas automaticamente;
- leia diretamente dentro do aplicativo;
- acompanhe o progresso de leitura;
- continue exatamente de onde parou;
- conclua uma obra e possa iniciar a leitura novamente;
- escolha como deseja navegar entre as páginas.

Futuramente, o projeto poderá receber sincronização opcional com um servidor privado, mantendo a leitura local como parte principal da experiência.

---

## Funcionalidades atuais

### Biblioteca

- Importação de arquivos CBR, CBZ, PDF e EPUB
- Cópia segura para o armazenamento interno do aplicativo
- Biblioteca persistente com SQLite
- Exibição das obras em grade
- Identificação visual do formato de cada arquivo
- Tela de detalhes da obra
- Estado vazio para biblioteca sem itens
- Exclusão individual sem apagar o arquivo original do usuário

### Capas

- Geração automática de capas para CBR
- Geração automática de capas para CBZ
- Renderização da primeira página como capa de PDF
- Extração da capa embutida de EPUB
- Suporte a EPUB 2 e EPUB 3
- Fallback para arquivos EPUB sem metadados de capa corretamente definidos

### Leitura de CBR e CBZ

- Leitor nativo baseado em páginas
- Extração das imagens para cache local
- Ordenação natural das páginas
- Navegação horizontal por swipe
- Navegação opcional por toque nas laterais
- Toque central para mostrar ou ocultar os controles
- Contador de páginas
- Salvamento automático do progresso
- Continuação da leitura de onde parou
- Identificação de leitura concluída
- Opção de ler novamente

### Leitura de PDF

- Leitura offline de arquivos PDF
- Renderização das páginas localmente no Android
- Cache das páginas renderizadas
- Integração com o mesmo leitor utilizado por CBR e CBZ
- Navegação por swipe e toque
- Salvamento e retomada do progresso
- Identificação de leitura concluída
- Opção de ler novamente

### Leitura de EPUB

- Extração local do conteúdo do EPUB
- Leitura offline por capítulos
- Exibição dos capítulos em WebView
- Suporte a HTML, CSS e imagens internas
- Navegação entre capítulos
- Contador de capítulos
- Salvamento e retomada do progresso
- Identificação de leitura concluída
- Opção de ler novamente
- Estilo de leitura padronizado para melhorar margens, fonte e espaçamento
- Ajuste automático de imagens e tabelas para a tela

### Zoom e gestos

- Zoom por toque duplo em CBR, CBZ e PDF
- Movimentação da imagem ampliada
- Bloqueio da troca de página durante o zoom
- Restauração da imagem com novo toque duplo
- Liberação automática do swipe ao voltar para a escala normal

### Preferências

- Modo “Apenas deslizar”
- Modo “Deslizar e tocar nas laterais”
- Preferência de navegação salva localmente
- Restauração automática da preferência ao reabrir o aplicativo

### Armazenamento e limpeza

- Persistência local com SQLite
- Armazenamento interno dos arquivos importados
- Cache separado para quadrinhos, PDFs e EPUBs
- Remoção da capa ao excluir uma obra
- Limpeza das páginas extraídas de CBR e CBZ
- Limpeza das páginas renderizadas de PDF
- Limpeza do conteúdo extraído de EPUB
- Preservação do arquivo original do usuário

---

## Formatos suportados

| Formato | Importação | Capa | Leitura | Progresso |
|---|---:|---:|---:|---:|
| CBR | ✅ | ✅ | ✅ | ✅ |
| CBZ | ✅ | ✅ | ✅ | ✅ |
| PDF | ✅ | ✅ | ✅ | ✅ |
| EPUB | ✅ | ✅ | ✅ | ✅ |

---

## Tecnologias utilizadas

- .NET 10
- .NET MAUI
- C#
- XAML
- CommunityToolkit.Mvvm
- SQLite
- sqlite-net-pcl
- SharpCompress
- VersOne.Epub
- Android PdfRenderer
- WebView
- Shell Navigation
- Dependency Injection

---

## Arquitetura

O projeto utiliza o padrão MVVM para separar interface, regras de apresentação, serviços e persistência.

A leitura de arquivos de páginas fixas é centralizada pelo `ComicReaderService`:

```text
CBR
└── SharpCompress

CBZ
└── System.IO.Compression

PDF
└── IPdfReaderService
    └── PdfRenderer no Android
