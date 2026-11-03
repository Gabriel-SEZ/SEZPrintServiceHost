# SEZPrintServiceHost

Código-fonte e pipeline de compilação do host nativo do **SEZ Print Service** para Windows.

O `SEZPrintServiceHost.exe` registra-se como serviço do Windows por meio do Service Control Manager (SCM). Ele inicia o `server.js` instalado na mesma pasta, sem janela de console, supervisiona o processo Node.js e tenta reiniciá-lo após encerramento inesperado.

## O que este repositório faz

- Mantém o código-fonte C# do serviço Windows.
- Compila `SEZPrintServiceHost.exe` no GitHub Actions usando um runner Windows.
- Publica o executável como artefato de CI em cada build.
- Publica o executável em uma GitHub Release quando uma tag `v*` é enviada.

O Inno Setup permanece no repositório principal `Gabriel-SEZ/SEZPrintService`. Ele baixa o executável da Release e o inclui no instalador.

## Gerar o EXE no GitHub

1. Envie o conteúdo deste repositório para a branch `main`.
2. Abra **Actions**.
3. Selecione **Build SEZPrintServiceHost.exe**.
4. Clique em **Run workflow**.
5. Baixe o artefato `SEZPrintServiceHost-Windows-x64`.

Para publicar uma versão consumível pelo instalador principal, crie e envie uma tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

O workflow compila e anexa `SEZPrintServiceHost.exe` à Release dessa tag. O instalador pode baixar a versão mais recente no endereço:

`https://github.com/Gabriel-SEZ/SEZPrintServiceHost/releases/latest/download/SEZPrintServiceHost.exe`

## Gerar localmente no Windows

Requer Windows com o compilador C# do .NET Framework 4.x disponível.

No PowerShell, na raiz do repositório:

```powershell
.\scripts\compile-host.ps1
```

Ou execute `scripts\compile-host.cmd`. O resultado será salvo em `dist\SEZPrintServiceHost.exe`, acompanhado de `dist\SEZPrintServiceHost.sha256`.

## Comportamento em execução

- Nome interno do serviço: `SEZPrintService`.
- Nome exibido: `SEZ Print Service`.
- Diretório de execução: pasta onde o host foi instalado; deve conter `server.js`.
- Processo supervisionado: `node.exe server.js`.
- Execução do processo Node sem janela visível.
- Logs em `%ProgramData%\SmartEZ\SEZ Print Service\logs`.

O instalador do repositório principal deve configurar o serviço como automático, executar a conta `LocalSystem` e definir ações de recuperação no Windows. Para impressão antes do login, as impressoras precisam estar instaladas em nível de computador e disponíveis para a conta de serviço.

## Estrutura

```text
SEZPrintServiceHost/
├── .github/workflows/build-host.yml
├── integration/MAIN-REPOSITORY-WORKFLOW.md
├── scripts/compile-host.ps1
├── scripts/compile-host.cmd
└── src/SezPrintServiceHost.cs
```
