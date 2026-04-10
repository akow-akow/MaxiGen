using System;
using System.IO;
using System.Linq;
using Barcoder;
using Barcoder.Maxicode;
using Barcoder.Renderer.Image;
using SkiaSharp;

namespace MaxiGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataToEncode = new[] { "KOD12345", "TEST-6789", "MAXI-DATA-001" };
            string outputDir = "Output";
            
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            Console.WriteLine("🚀 Rozpoczynam generowanie MaxiCode (.NET 4.8)...");

            foreach (var text in dataToEncode)
            {
                try
                {
                    // 1. Generowanie MaxiCode
                    var maxicode = Encoders.Maxicode.Encode(text, mode: 4);
                    
                    // 2. Renderowanie do pamięci
                    var renderer = new ImageRenderer(pixelSize: 10);
                    using (var ms = new MemoryStream())
                    {
                        renderer.Render(maxicode, ms);
                        ms.Position = 0;

                        // 3. SkiaSharp: Dodawanie tekstu
                        using (var bitmap = SKBitmap.Decode(ms))
                        {
                            var surfaceSize = new SKImageInfo(bitmap.Width, bitmap.Height + 40);
                            using (var surface = SKSurface.Create(surfaceSize))
                            {
                                var canvas = surface.Canvas;
                                canvas.Clear(SKColors.White);
                                canvas.DrawBitmap(bitmap, 0, 0);

                                using (var paint = new SKPaint
                                {
                                    Color = SKColors.Black,
                                    IsAntialias = true,
                                    TextSize = 24,
                                    TextAlign = SKTextAlign.Center
                                })
                                {
                                    canvas.DrawText(text, surfaceSize.Width / 2, surfaceSize.Height - 10, paint);
                                }

                                // 4. Zapis
                                using (var image = surface.Snapshot())
                                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                                {
                                    string filePath = Path.Combine(outputDir, text + ".png");
                                    File.WriteAllBytes(filePath, data.ToArray());
                                }
                            }
                        }
                    }
                    Console.WriteLine("✅ Zapisano: " + text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Błąd przy " + text + ": " + ex.Message);
                }
            }

            Console.WriteLine("\n✨ Gotowe! Naciśnij dowolny klawisz...");
            Console.ReadKey();
        }
    }
}
