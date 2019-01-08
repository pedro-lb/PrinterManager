/*
 * Printer Manager v1.0 - Classe para monitoramento e gerenciamento de serviços de impressão.
 * Pedro Luis Calesco Bini - 16/01/2018.
 */

using System;

namespace UtilPrinterManager
{
    public class PrinterManagerListener
    {
        public PrinterManagerListener(Printer printer, Action<PrintJob> onJobFound)
        {
            Printer = printer ?? throw new ArgumentNullException(nameof(printer));
            this.OnJobFound = onJobFound ?? throw new ArgumentNullException(nameof(onJobFound));
        }

        public Printer Printer { get; set; }
        public Action<PrintJob> OnJobFound { get; set; }
    }
}