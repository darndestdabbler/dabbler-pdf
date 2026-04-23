using System.Diagnostics;
using DabblerPdf.Samples.Samples;

namespace DabblerPdf.Samples;

public class Program
{
    public static void Main(string[] args)
    {
        var outputDir = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "Output");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("DabblerPdf Visual Samples");
        Console.WriteLine($"Output: {outputDir}");
        Console.WriteLine(new string('\u2500', 60));

        var samples = new SampleBase[]
        {
            new S01_BasicText(),
            new S02_TextWrapping(),
            new S03_LinesAndShapes(),
            new S04_Colors(),
            new S05_Images(),
            new S06_TableLayout(),
            new S07_MultiPage(),
            new S08_Padding(),
            new S09_InvoiceSample(),
            new S10_StressTest(),
            new S11_OrdersStyleReport(),
        };

        var total = Stopwatch.StartNew();
        int passed = 0, failed = 0;

        foreach (var sample in samples)
        {
            try
            {
                sample.Generate(outputDir);
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  \u2717 {sample.Name} \u2014 FAILED: {ex.Message}");
                Console.WriteLine($"    {ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}");
                failed++;
            }
        }

        total.Stop();
        Console.WriteLine(new string('\u2500', 60));
        Console.WriteLine($"Done: {passed} passed, {failed} failed, {total.ElapsedMilliseconds} ms total");
        Console.WriteLine($"\nOpen the Output folder to inspect the generated PDFs.");
    }
}
