/*
 * Printer Manager v1.0 - Classe para monitoramento e gerenciamento de serviços de impressão.
 * Pedro Luis Calesco Bini - 16/01/2018.
 */

using System.Collections.Generic;

namespace UtilPrinterManager
{
    internal class PrinterManagerListeners
    {
        public PrinterManagerListeners()
        {
            Listeners = new List<PrinterManagerListener>();
        }

        public List<PrinterManagerListener> Listeners { get; set; }
    }
}