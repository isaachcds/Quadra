# Quadra

Quadra é um aplicativo de leitura e organização de quadrinhos desenvolvido com .NET MAUI.

O projeto nasceu com o objetivo de oferecer uma experiência simples, rápida e personalizada para importar, organizar e ler arquivos CBR, CBZ e PDF diretamente no celular.

> Status atual: MVP em desenvolvimento — v0.1.0-alpha

---

## Sobre o projeto

O Quadra está sendo desenvolvido inicialmente para Android, com foco em uma biblioteca local e funcionamento offline.

A proposta é permitir que o usuário:

- importe arquivos CBR, CBZ e PDF;
- organize suas obras em uma biblioteca local;
- visualize capas automaticamente;
- acompanhe o progresso de leitura;
- continue a leitura de onde parou;
- leia diretamente dentro do aplicativo.

Futuramente, o projeto poderá receber sincronização opcional com um servidor privado.

---

## Funcionalidades atuais

- Importação de arquivos CBR, CBZ e PDF
- Cópia segura para o armazenamento interno do aplicativo
- Biblioteca persistente com SQLite
- Exibição das obras em grade
- Geração automática de capas para arquivos CBR
- Tela de detalhes da obra
- Exclusão individual sem apagar o arquivo original
- Suporte a tema claro e escuro
- Arquitetura MVVM
- Injeção de dependência

---

## Em desenvolvimento

- Leitor nativo de CBR
- Contagem total de páginas
- Navegação entre páginas
- Salvamento do progresso
- Continuação automática da leitura
- Zoom e gestos
- Leitura vertical
- Suporte completo a CBZ e PDF
- Pesquisa local
- Favoritos e coleções

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
- Shell Navigation
- Dependency Injection

---

## Estrutura do projeto

```text
Quadra.App
├── Data
├── Models
├── Pages
├── Services
├── ViewModels
├── Platforms
└── Resources
