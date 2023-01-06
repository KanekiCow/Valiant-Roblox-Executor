using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Valiant.Classes
{
    internal static class ExceptionHandler
    {
        private static readonly string LoggingDirectory = Path.GetFullPath("log");
        private static readonly string FileName = $"{DateTime.Now:MM-dd-yyyy_HH-mm-ss}.txt";

        public static void Start()
        {
            Directory.CreateDirectory(LoggingDirectory);
            File.WriteAllText(Path.Combine(LoggingDirectory, "latest.txt"), "");
            File.WriteAllText(Path.Combine(LoggingDirectory, FileName), "");

            AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;
        }

        public static void Stop() => AppDomain.CurrentDomain.FirstChanceException -= FirstChanceException;

        private static void FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            File.AppendAllText(Path.Combine(LoggingDirectory, "latest.txt"), $"{e.Exception}\n");
            File.AppendAllText(Path.Combine(LoggingDirectory, FileName), $"{e.Exception}\n");
        }
    }
}
