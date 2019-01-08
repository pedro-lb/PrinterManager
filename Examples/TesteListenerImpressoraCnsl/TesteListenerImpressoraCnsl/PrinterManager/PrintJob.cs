/*
 * Printer Manager v1.0 - Classe para monitoramento e gerenciamento de serviços de impressão.
 * Pedro Luis Calesco Bini - 16/01/2018.
 */

using System;
using System.IO;
using System.Threading;

namespace UtilPrinterManager
{
    /// <summary>
    /// Representa um serviço de impressão.
    /// </summary>
    public class PrintJob
    {
        public PrintJob(string printerName, string documentName, int jobID)
        {
            PrinterName = printerName ?? throw new ArgumentNullException(nameof(printerName));
            DocumentName = documentName ?? throw new ArgumentNullException(nameof(documentName));
            JobID = jobID;
        }

        /// <summary>
        /// Define o nome da impressora responsável pela impressão.
        /// </summary>
        public string PrinterName { get; set; }

        /// <summary>
        /// Define o nome do documento a ser impresso.
        /// </summary>
        public string DocumentName { get; set; }

        /// <summary>
        /// Define o ID do serviço de impressão.
        /// </summary>
        public int JobID { get; set; }

        /// <summary>
        /// Retorna o arquivo a ser impresso, se existir, em formato SPL.
        /// Retorna null se o arquivo não existir.
        /// </summary>
        public byte[] GetPrintFile()
        {
            var fileSpoolPath = Path.Combine(@"C:\Windows\System32\spool\PRINTERS", JobID.ToString().PadLeft(5, '0')) + ".SPL";

            if (File.Exists(fileSpoolPath))
            {
                while (true)
                {
                    try
                    {
                        var FileStream = new FileStream(fileSpoolPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var MemStream = new MemoryStream();
                        FileStream.CopyTo(MemStream);
                        return MemStream.ToArray();
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is IOException))
                        {
                            break;
                        }

                        Thread.Sleep(250);
                    }
                }
            }

            return null;
        }
    }
}