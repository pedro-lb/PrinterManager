using System;
using UtilPrinterManager;

namespace TesteListenerImpressoraCnsl
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Monitorando serviço de impressão..." + Environment.NewLine);

            PrinterManager.ListenForAllPrintJobs((job) =>
            {
                Console.WriteLine("Serviço " + job.DocumentName + " detectado.");
                job.PausePrintJob();
                var file = job.GetPrintFile();
                Console.WriteLine("Arquivo " + job.DocumentName + " obtido.");
                job.CancelPrintJob();
                Console.WriteLine("Serviço " + job.DocumentName + " cancelado com sucesso." + Environment.NewLine);
            });

            Console.ReadLine();
        }
    }
}