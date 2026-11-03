using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace SezPrintServiceHost
{
    internal sealed class PrintService : ServiceBase
    {
        private Process child;
        private Thread supervisor;
        private volatile bool stopping;
        private readonly string appDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SmartEZ", "SEZ Print Service", "logs");

        public PrintService()
        {
            ServiceName = "SEZPrintService";
            CanStop = true;
            CanShutdown = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            Directory.CreateDirectory(logDir);
            stopping = false;
            supervisor = new Thread(Supervise)
            {
                IsBackground = true,
                Name = "SEZ Print Service supervisor"
            };
            supervisor.Start();
        }

        private void Supervise()
        {
            while (!stopping)
            {
                try
                {
                    // Preferir runtime Node.js empacotado no próprio instalador.
                    // Isso evita depender de uma instalação global/perfil do usuário.
                    string node = Path.Combine(appDir, "runtime", "node.exe");

                    if (!File.Exists(node))
                    {
                        node = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            "nodejs", "node.exe");
                    }

                    if (!File.Exists(node))
                    {
                        node = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                            "nodejs", "node.exe");
                    }

                    if (!File.Exists(node))
                        throw new FileNotFoundException(
                            "Node.js não encontrado. O runtime embarcado está ausente em runtime\\node.exe.");

                    string server = Path.Combine(appDir, "server.js");
                    if (!File.Exists(server))
                        throw new FileNotFoundException("server.js não encontrado.", server);

                    var psi = new ProcessStartInfo(node, "\"" + server + "\"")
                    {
                        WorkingDirectory = appDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    child = new Process { StartInfo = psi, EnableRaisingEvents = true };
                    child.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null) Log(e.Data);
                    };
                    child.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) Log("ERR " + e.Data);
                    };

                    Log("Iniciando processo Node.js.");
                    child.Start();
                    child.BeginOutputReadLine();
                    child.BeginErrorReadLine();

                    while (!stopping && !child.WaitForExit(1000)) { }

                    if (!stopping)
                        Log("Processo Node.js encerrou. Reinício em 5 segundos.");
                }
                catch (Exception ex)
                {
                    Log("Falha: " + ex);
                }
                finally
                {
                    SafeKill();
                }

                if (!stopping)
                    Thread.Sleep(5000);
            }
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd") + ".log"),
                    DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
            }
            catch { }
        }

        private void SafeKill()
        {
            try
            {
                Process current = child;
                child = null;
                if (current != null)
                {
                    if (!current.HasExited) current.Kill();
                    current.Dispose();
                }
            }
            catch { }
        }

        protected override void OnStop()
        {
            stopping = true;
            SafeKill();
            if (supervisor != null && supervisor.IsAlive)
                supervisor.Join(7000);
        }

        protected override void OnShutdown()
        {
            OnStop();
            base.OnShutdown();
        }

        public static void Main()
        {
            ServiceBase.Run(new PrintService());
        }
    }
}
