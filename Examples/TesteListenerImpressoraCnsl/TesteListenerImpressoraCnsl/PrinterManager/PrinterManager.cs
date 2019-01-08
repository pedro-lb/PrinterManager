/*
 * Printer Manager v1.0 - Classe para monitoramento e gerenciamento de serviços de impressão.
 * Pedro Luis Calesco Bini - 16/01/2018.
 */

using System.Management;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace UtilPrinterManager
{
    /// <summary>
    /// Permite monitoramento e gerenciamento de serviços de impressão.
    /// </summary>
    public static class PrinterManager
    {
        /// <summary>
        /// Listeners para eventos de serviços de impressão.
        /// </summary>
        private static PrinterManagerListeners _PrinterManagerListeners = new PrinterManagerListeners();

        /// <summary>
        /// Timer para callback de listeners.
        /// </summary>
        private static Timer _ListenerTimer = new Timer(_TimerCallback, null, 0, 100);

        /// <summary>
        /// Lista de callbacks de serviços de impressão que estão sendo executados no momento.
        /// </summary>
        private static List<PrintJob> _Callbacks = new List<PrintJob>();

        /// <summary>
        /// Vetor para executar split em propriedades de impressão.
        /// </summary>
        private static readonly char[] splitArr = new char[1] { ',' };

        /// <summary>
        /// Máximo de callbacks mantidos em memória.
        /// </summary>
        private const int MAX_CALLBACK_MEMORY = 500;

        /// <summary>
        /// Intervalo para callback de listeners.
        /// </summary>
        private static int _ListenerInterval = 100;

        /// <summary>
        /// Define se o serviço de monitoramento está sendo executado
        /// </summary>
        private static bool _IsRunning = true;

        /// <summary>
        /// Intervalo para callback de monitoramento.
        /// </summary>
        public static int ListenerInterval
        {
            get
            {
                return _ListenerInterval;
            }
            set
            {
                _ListenerInterval = value;
            }
        }

        /// <summary>
        /// Define se o serviço de monitoramento está sendo executado
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                return _IsRunning;
            }
        }

        /// <summary>
        /// Pausa o serviço de monitoramento de eventos de impressão.
        /// </summary>
        public static void Pause()
        {
            _ListenerTimer = null;
            _IsRunning = false;
        }

        /// <summary>
        /// Resume o serviço de monitoramento de eventos de impressão.
        /// </summary>
        public static void Resume()
        {
            _ListenerTimer = new Timer(_TimerCallback, null, 0, 100);
            _IsRunning = true;
        }

        /// <summary>
        /// Retorna uma lista de impressoras do sistema.
        /// </summary>
        public static List<Printer> GetPrinters()
        {
            var printerNameCollection = new List<Printer>();

            foreach (var printer in GetRawPrintersCollection())
            {
                printerNameCollection.Add(new Printer(printer.Properties["Name"].Value.ToString()));
            }

            return printerNameCollection;
        }

        /// <summary>
        /// Retorna uma lista de serviços de impressão de uma impressora.
        /// </summary>
        /// <param name="printer">Impressora</param>
        public static List<PrintJob> GetPrintJobs(this Printer printer)
        {
            var printJobCollection = new List<PrintJob>();

            foreach (ManagementObject prntJob in GetRawPrintJobsCollection())
            {
                var documentName = prntJob.Properties["Document"].Value.ToString();
                var jobID = Convert.ToInt32(prntJob.Properties["JobID"].Value);

                if (String.Compare(GetPrinterName(prntJob), printer.Name, true) == 0)
                {
                    printJobCollection.Add(new PrintJob(printer.Name, documentName, jobID));
                }
            }

            return printJobCollection;
        }

        /// <summary>
        /// Monitora novos serviços de impressão do sistema, de todas as impressoras.
        /// </summary>
        /// <param name="onJobFound">Ação a ser executada ao encontrar um novo serviço, com o parâmetro PrintJob</param>
        public static void ListenForAllPrintJobs(Action<PrintJob> onJobFound)
        {
            foreach (var printer in GetPrinters())
            {
                printer.AddPrintJobListener(onJobFound);
            }
        }

        /// <summary>
        /// Adiciona novo monitoramento de serviços de impressão para uma impressora.
        /// </summary>
        /// <param name="printer">Impressora</param>
        /// <param name="onJobFound">Ação a ser executada ao encontrar um novo serviço, com o parâmetro PrintJob</param>
        public static void AddPrintJobListener(this Printer printer, Action<PrintJob> onJobFound)
        {
            if (!_PrinterManagerListeners.Listeners.Any(l => l.Printer.Name == printer.Name))
            {
                _PrinterManagerListeners.Listeners.Add(new PrinterManagerListener(printer, onJobFound));
            }
        }

        /// <summary>
        /// Remove um monitoramento de serviços de impressão para uma impressora, se existir.
        /// </summary>
        /// <param name="printer">Impressora</param>
        public static void RemovePrintJobListener(this Printer printer)
        {
            var Listener = _PrinterManagerListeners.Listeners.FirstOrDefault(l => l.Printer.Name == printer.Name);
            if (Listener != null)
            {
                _PrinterManagerListeners.Listeners.Remove(Listener);
            }
        }

        /// <summary>
        /// Retorna a lista de monitoramentos atual.
        /// </summary>
        public static List<PrinterManagerListener> GetCurrentListeners()
        {
            return _PrinterManagerListeners.Listeners;
        }

        /// <summary>
        /// Pausa um serviço de impressão.
        /// </summary>
        /// <param name="printJob">Serviço de impressão</param>
        public static bool PausePrintJob(this PrintJob printJob)
        {
            return InvokePrintMethod("Pause", printJob);
        }

        /// <summary>
        /// Resume um serviço de impressão.
        /// </summary>
        /// <param name="printJob">Serviço de impressão</param>
        public static bool ResumePrintJob(this PrintJob printJob)
        {
            return InvokePrintMethod("Resume", printJob);
        }

        /// <summary>
        /// Cancela um serviço de impressão.
        /// </summary>
        /// <param name="printJob">Serviço de impressão</param>
        public static bool CancelPrintJob(this PrintJob printJob)
        {
            foreach (ManagementObject prntJob in GetRawPrintJobsCollection())
            {
                var jobName = prntJob.Properties["Name"].Value.ToString();
                int prntJobID = Convert.ToInt32(jobName.Split(splitArr)[1]);

                if (String.Compare(GetPrinterName(prntJob), printJob.PrinterName, true) == 0 && prntJobID == printJob.JobID)
                {
                    prntJob.Delete();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calback de função para timer de monitoramento.
        /// </summary>
        /// <param name="o">Estado do objeto</param>
        private static void _TimerCallback(dynamic o)
        {
            if (IsRunning && _ListenerTimer != null)
            {
                foreach (var Listener in _PrinterManagerListeners.Listeners)
                {
                    var CheckTask = Task.Factory.StartNew(() => Listen(Listener));
                }
            }
        }

        /// <summary>
        /// Procura serviços de impressão de uma impressora, e executa ação em cada um.
        /// </summary>
        /// <param name="printer">Impressora</param>
        /// <param name="onJobFound">Ação a ser executada ao encontrar um novo serviço, com o parâmetro PrintJob</param>
        private static void Listen(PrinterManagerListener listener)
        {
            foreach (var printJob in listener.Printer.GetPrintJobs())
            {
                var CallbackTask = Task.Factory.StartNew(() => ListenCallback(printJob, listener.OnJobFound));
            }
        }

        /// <summary>
        /// Realiza o callback de ação no serviço de impressão.
        /// </summary>
        /// <param name="printJob">Serviço de impressão</param>
        /// <param name="onJobFound">Ação a ser executada ao encontrar um novo serviço, com o parâmetro PrintJob</param>
        private static void ListenCallback(PrintJob printJob, Action<PrintJob> onJobFound)
        {
            lock (_Callbacks)
            {
                if (_Callbacks.Any(c => c.JobID == printJob.JobID && c.PrinterName == printJob.PrinterName && c.DocumentName == printJob.DocumentName))
                {
                    return;
                }

                _Callbacks.Add(printJob);
            }

            onJobFound(printJob);

            if (_Callbacks.Count > MAX_CALLBACK_MEMORY)
            {
                ClearCallbackMemory();
            }
        }

        /// <summary>
        /// Limpa a memória de callbacks executados.
        /// </summary>
        private static void ClearCallbackMemory()
        {
            lock (_Callbacks)
            {
                var ToRemove = _Callbacks.OrderBy(c => c.JobID).Take(_Callbacks.Count - 10);
                foreach (var Callback in ToRemove)
                {
                    _Callbacks.Remove(Callback);
                }
            }
        }

        /// <summary>
        /// Retorna uma coleção de serviços de impressão, em ManagementObjectCollection.
        /// </summary>
        private static ManagementObjectCollection GetRawPrintJobsCollection()
        {
            var printJobCollection = new List<string>();
            string searchQuery = "SELECT * FROM Win32_PrintJob";
            return new ManagementObjectSearcher(searchQuery).Get();
        }

        /// <summary>
        /// Retorna uma coleção de impressoras, em ManagementObjectCollection.
        /// </summary>
        private static ManagementObjectCollection GetRawPrintersCollection()
        {
            var printerNameCollection = new List<string>();
            var searchQuery = "SELECT * FROM Win32_Printer";
            return new ManagementObjectSearcher(searchQuery).Get();
        }

        /// <summary>
        /// Extrai o nome de um impressora de um serviço de impressão.
        /// </summary>
        /// <param name="prntJob">Serviço de impressão</param>
        private static string GetPrinterName(ManagementObject prntJob)
        {
            var jobName = prntJob.Properties["Name"].Value.ToString();
            return jobName.Split(splitArr)[0];
        }

        /// <summary>
        /// Invoca e executa um método em um serviço de impressão.
        /// </summary>
        /// <param name="method">Nome do método</param>
        /// <param name="printJob">Serviço de impressão</param>
        private static bool InvokePrintMethod(string method, PrintJob printJob)
        {
            foreach (ManagementObject prntJob in GetRawPrintJobsCollection())
            {
                var jobName = prntJob.Properties["Name"].Value.ToString();
                int prntJobID = Convert.ToInt32(jobName.Split(splitArr)[1]);

                if (String.Compare(GetPrinterName(prntJob), printJob.PrinterName, true) == 0 && prntJobID == printJob.JobID)
                {
                    prntJob.InvokeMethod(method, null);
                    return true;
                }
            }

            return false;
        }
    }
}