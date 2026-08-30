# Fase 2 — Abrir com Quadra

O Android registra `ACTION_VIEW` para `content` e `file` com MIME types PDF, EPUB/ZIP e octet-stream. `MainActivity` encaminha intents recebidos em `OnCreate` e `OnNewIntent` ao serviço Android. O serviço resolve nome por `IOpenableColumns`, abre `content://` com `ContentResolver.OpenInputStream` e delega ao pipeline reutilizável de importação; após sucesso navega aos detalhes. URIs repetidas são deduplicadas por ação e URI. A validação final continua baseada na extensão suportada.
