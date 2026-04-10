using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using Barcoder;
using Barcoder.Maxicode;
using Barcoder.Renderer.Image;
using SkiaSharp;

namespace MaxiGen
{
    public class MainForm : Form
    {
        private TextBox txtInput;
        private Button btnGenerate, btnPrint;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "MaxiCode Generator (Zebra Ready)";
            this.Size = new Size(400, 500);

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Top, Height = 300, ScrollBars = ScrollBars.Vertical };
            btnGenerate = new Button { Text = "Generuj i zapisz / Kopiuj", Dock = DockStyle.Top, Height = 50 };
            btnPrint = new Button { Text = "Drukuj na Zebra (Domyślna)", Dock = DockStyle.Top, Height = 50 };
            lblStatus = new Label { Text = "Wpisz kody (1 na linię)", Dock = DockStyle.Bottom };

            btnGenerate.Click += (s, e) => ProcessCodes(false);
            btnPrint.Click += (s, e) => ProcessCodes(true);

            this.Controls.Add(btnPrint);
            this.Controls.Add(btnGenerate);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblStatus);
        }

        private void ProcessCodes(bool print)
        {
            var lines = txtInput.Lines;
            if (lines.Length == 0) return;

            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            Directory.CreateDirectory(outputDir);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                Bitmap bmp = GenerateMaxiCode(line);
                
                if (lines.Length == 1 && !print) {
                    Clipboard.SetImage(bmp);
                    lblStatus.Text = "Skopiowano do schowka!";
                }

                string path = Path.Combine(outputDir, $"{line.Replace(" ", "_")}.png");
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                if (print) PrintImage(bmp);
            }
            if (!print) lblStatus.Text = $"Zapisano {lines.Length} plików w /Output";
        }

        private Bitmap GenerateMaxiCode(string text)
        {
            var maxicode = Encoders.Maxicode.Encode(text, mode: 4);
            var renderer = new ImageRenderer(pixelSize: 10);
            
            using (var ms = new MemoryStream())
            {
                renderer.Render(maxicode, ms);
                ms.Position = 0;

                using (var skBitmap = SKBitmap.Decode(ms))
                {
                    var info = new SKImageInfo(skBitmap.Width, skBitmap.Height + 50);
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.White);
                        canvas.DrawBitmap(skBitmap, 0, 0);

                        using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 25, IsAntialias = true, TextAlign = SKTextAlign.Center })
                        {
                            canvas.DrawText(text, info.Width / 2, info.Height - 15, paint);
                        }

                        using (var image = surface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            return new Bitmap(data.AsStream());
                        }
                    }
                }
            }
        }

        private void PrintImage(Bitmap bmp)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (s, ev) => {
                // Centrowanie na małej etykiecie Zebry
                ev.Graphics.DrawImage(bmp, 0, 0);
            };
            pd.Print();
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }
}
