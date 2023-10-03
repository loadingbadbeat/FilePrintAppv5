using System;
using System.IO;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        // List available printers and number them
        var availablePrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        Console.WriteLine("Available Printers:");
        for (int i = 0; i < availablePrinters.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {availablePrinters[i]}");
        }

        Console.Write("Enter the number of the printer to use: ");
        if (int.TryParse(Console.ReadLine(), out int selectedPrinterNumber) && selectedPrinterNumber >= 1 && selectedPrinterNumber <= availablePrinters.Count)
        {
            string selectedPrinter = availablePrinters[selectedPrinterNumber - 1];
            Console.WriteLine($"Selected printer: {selectedPrinter}");

            string folderToMonitor = @"C:\temp\print"; // Specify the folder to monitor
            Console.WriteLine($"Monitoring folder '{folderToMonitor}' for new files.");

            using (FileSystemWatcher watcher = new FileSystemWatcher(folderToMonitor))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Filter = "*.*"; // You can specify a particular file type if needed
                watcher.Created += (sender, e) => OnFileCreated(e.FullPath, selectedPrinter);

                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        else
        {
            Console.WriteLine("Invalid printer number. Exiting.");
        }
    }

    static void OnFileCreated(string filePath, string selectedPrinter)
    {
        // Retry mechanism with a delay to check if the file is available for printing
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    fileStream.Close();
                }

                // If the file is available, proceed with printing
                PrintFile(filePath, selectedPrinter);
                return;
            }
            catch (IOException)
            {
                // File is still in use, retry after a delay
                Thread.Sleep(1000); // Wait for 1 second before retrying
            }
        }

        Console.WriteLine($"Error printing {filePath}: File is still in use.");
    }

    static void PrintFile(string filePath, string selectedPrinter)
    {
        try
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = selectedPrinter;
                pd.PrintPage += (sender, e) =>
                {
                    using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);
                        e.Graphics.DrawImageUnscaledAndClipped(System.Drawing.Image.FromStream(new MemoryStream(bytes)), e.PageBounds);
                    }
                };

                // Print the document
                pd.Print();

                // After successful print, delete the file
                File.Delete(filePath);

                Console.WriteLine($"Printed and deleted {filePath} from {selectedPrinter}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error printing {filePath}: {ex.Message}");
        }
    }
}

