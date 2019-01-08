/*
 * Printer Manager v1.0 - Classe para monitoramento e gerenciamento de serviços de impressão.
 * Pedro Luis Calesco Bini - 16/01/2018.
 */

using System;

namespace UtilPrinterManager
{
    /// <summary>
    /// Representa uma impressora.
    /// </summary>
    public class Printer
    {
        public Printer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Nome da impressora.
        /// </summary>
        public string Name { get; set; }
    }
}