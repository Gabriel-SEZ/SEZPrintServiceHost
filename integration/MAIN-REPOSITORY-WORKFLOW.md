# Integrar ao repositório principal SEZPrintService

Este repositório `SEZPrintServiceHost` compila e publica o executável. O Inno Setup continua no repositório principal `Gabriel-SEZ/SEZPrintService`.

## Publicar a primeira versão do host

1. Envie os arquivos deste repositório para a branch `main`.
2. Abra **Actions** e execute **Build SEZPrintServiceHost.exe**.
3. Confirme que o workflow terminou com sucesso e gerou o artefato `SEZPrintServiceHost-Windows-x64`.
4. Crie e envie uma tag, por exemplo:

```bash
git tag v1.0.0
git push origin v1.0.0
```

5. O workflow compilará novamente e publicará `SEZPrintServiceHost.exe` e o checksum na seção **Releases**.

## Baixar o host no workflow do repositório principal

No workflow que compila o instalador do repositório `SEZPrintService`, adicione esta etapa logo depois do Checkout e antes de chamar o Inno Setup:

```yaml
      - name: Download SEZPrintServiceHost.exe release
        shell: pwsh
        run: |
          $url = 'https://github.com/Gabriel-SEZ/SEZPrintServiceHost/releases/latest/download/SEZPrintServiceHost.exe'
          New-Item -ItemType Directory -Path 'service-host' -Force | Out-Null
          Invoke-WebRequest -Uri $url -OutFile 'service-host/SEZPrintServiceHost.exe'
          $file = Get-Item 'service-host/SEZPrintServiceHost.exe'
          if ($file.Length -lt 10000) { throw 'SEZPrintServiceHost.exe ausente ou incompleto.' }
          Write-Host "Host baixado: $($file.Length) bytes"
```

O `setup.iss` do repositório principal deve continuar contendo:

```ini
Source: "service-host\\SEZPrintServiceHost.exe"; DestDir: "{app}"; Flags: ignoreversion
```

O script `install-service.ps1` deve registrar `{app}\\SEZPrintServiceHost.exe` como serviço `SEZPrintService`, com início automático e recuperação configurada.

## Se o repositório do host for privado

O exemplo acima usa download público de Release. Se `SEZPrintServiceHost` for privado, o workflow do repositório principal precisará de um token com permissão de leitura de conteúdo nesse repositório (por exemplo, secret `SEZ_PRINT_HOST_TOKEN`) e baixar o asset autenticado pela API do GitHub.

## Importante

- Não coloque o EXE compilado no controle de versão; ele é produzido pelo workflow.
- O host deve ser instalado na mesma pasta do `server.js`, pois usa a pasta do próprio executável como diretório de trabalho.
- O Inno Setup não compila C#. Ele somente empacota o EXE previamente gerado e baixado.
- Para iniciar antes do login, o instalador deve registrar o host com o Windows Service Control Manager, não por tarefa agendada de usuário.
