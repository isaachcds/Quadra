# Arquitetura

O Quadra é um aplicativo .NET MAUI, inicialmente direcionado ao Android, para organizar e ler arquivos CBR, CBZ, PDF e EPUB de forma local.

## Organização

```text
Quadra.App/
├── Data/          SQLite e inicialização do banco
├── Models/        Obras, coleções, capítulos e páginas
├── Pages/         Telas XAML e comportamento de apresentação
├── ViewModels/    Estado e comandos com CommunityToolkit.Mvvm
├── Services/      Importação, capas, leitores, armazenamento e limpeza
├── Controls/      Componentes reutilizáveis de interface e zoom
├── Platforms/     Integrações específicas de cada plataforma
└── Resources/     Estilos, tokens, strings, imagens e fontes
```

As Pages mantêm detalhes de interação visual e ciclo de vida; os ViewModels concentram estado, comandos e navegação. Serviços encapsulam I/O, processamento de formatos e persistência. A composição é feita em `MauiProgram`, com serviços de longa duração registrados como singleton e Pages/ViewModels como transient.

## Persistência

`QuadraDatabase` gerencia o SQLite local. `ObraBiblioteca` representa uma obra importada e registra, entre outros dados, formato, cópia interna, capa e posição de leitura.

Coleções usam as tabelas `Collections` e `CollectionBooks` em uma relação N:N. A criação é idempotente e não substitui a biblioteca existente. Excluir uma coleção não exclui obras; excluir uma obra remove seus vínculos antes da limpeza de seus arquivos internos.

## Importação e armazenamento

`ImportacaoBibliotecaService` é o ponto comum da importação feita pela Biblioteca e pelo Android. O fluxo valida o formato, verifica espaço, copia o conteúdo para o armazenamento interno, gera a capa e persiste a obra. Arquivos temporários usam a extensão `.partial` e são limpos em falhas ou cancelamentos.

`ArmazenamentoBibliotecaService` mantém as cópias das obras; `CapaService` gera capas; `LimpezaBibliotecaService` remove cópias, capas e caches regeneráveis sem tocar no arquivo original escolhido pelo usuário. Caches de quadrinhos, PDF e EPUB são separados e podem ser reconstruídos se forem removidos pelo sistema.

## Leitores

- **CBR e CBZ:** `LeitorQuadrinhosService` extrai páginas com validação e cache local.
- **PDF:** `ILeitorPdfService` usa o `PdfRenderer` no Android para renderizar páginas em cache.
- **EPUB:** `ILeitorEpubService` extrai capítulos e recursos locais para um WebView protegido.

CBR, CBZ e PDF compartilham `LeitorPage`, com `CarouselView`, zoom por toque duplo, pan, progresso e controles em overlay. EPUB usa `LeitorEpubPage`, rolagem vertical por capítulo e preferências de aparência persistidas. O progresso é salvo com debounce durante a leitura e recebe flush ao encerrar o leitor.

## Segurança de arquivos

O processamento limita entradas, tamanhos e páginas; valida formatos antes de finalizar a cópia; e evita que arquivos compactados escapem das raízes de cache. No EPUB, caminhos e recursos são normalizados, scripts são desabilitados e navegação externa requer confirmação.

## Integração Android

`MainActivity` encaminha intents `ACTION_VIEW` para `AbrirComAndroidService`. O serviço lê `content://` por `ContentResolver`, resolve o nome quando disponível e delega para o mesmo pipeline de importação da Biblioteca. O app não depende de um caminho físico externo e não modifica o arquivo original.

Os serviços Android também fornecem renderização de PDF, geração de capa de PDF e diagnóstico de espaço disponível. Implementações de fallback permitem manter o projeto compilável em outras plataformas.

## Design system

Os recursos compartilhados ficam em `Resources/Styles`: `Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml` e `Leitor.xaml`. `Strings.xaml` concentra textos reutilizáveis. O objetivo é manter a identidade Digital Codex consistente, com suporte a tema claro e escuro, sem acoplar regras de domínio ao XAML.
