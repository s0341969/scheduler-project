using System;
using System.IO;
using Tesseract;

var baseDir = @"G:\codex_pg\DrawingTagVs2022_UI完整版\publish_output";
var tessDir = Path.Combine(baseDir, "tessdata");
var parentDir = Directory.GetParent(tessDir)?.FullName;
Console.WriteLine($"baseDir={baseDir}");
Console.WriteLine($"tessDir={tessDir}");
Console.WriteLine($"parentDir={parentDir}");
Console.WriteLine($"engExists={File.Exists(Path.Combine(tessDir, "eng.traineddata"))}");

void TryInit(string label, string path)
{
    try
    {
        Console.WriteLine($"TRY {label}: path={path ?? "<null>"}");
        using var engine = new TesseractEngine(path, "eng", EngineMode.Default);
        engine.SetVariable("tessedit_char_whitelist", "0123456789");
        engine.SetVariable("classify_bln_numeric_mode", "1");
        Console.WriteLine($"OK {label}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {label}: {ex.GetType().FullName}: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"INNER {label}: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
        }
    }
}

TryInit("tessDir", tessDir);
TryInit("parentDir", parentDir);
TryInit("baseDir", baseDir);
