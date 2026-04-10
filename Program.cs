using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using Barcoder;
using SkiaSharp;

namespace MaxiGen
{
    public class MainForm : Form
    {
        private TextBox txtInput;
        private NumericUpDown numPaperW, numPaperH, numCodeSize;
        private ComboBox cmbPrinters;
        private Button btnGenerate, btnPrint;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "MaxiGen Pro - Zebra Edition";
            this.Size = new Size(650, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            Panel pnlSettings = new Panel { Dock = DockStyle.Right, Width = 220, Padding = new Padding(10), BackColor = Color.FromArgb(245, 245, 245) };
            
            pnlSettings.Controls.Add(new Label { Text = "Wybierz drukarkę:", Dock = DockStyle.Top });
            cmbPrinters = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadPrinters();
            pnlSettings.Controls.Add(cmbPrinters);

            pnlSettings.Controls.Add(new Label { Text = "Szer. papieru (mm):", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numPaperW = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 100 };
            
            pnlSettings.Controls.Add(new Label { Text = "Wys. papieru (mm):", Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) });
            numPaperH = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 70 };

            pnlSettings.Controls.Add(new Label { Text = "Skala MaxiCode:", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numCodeSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 5, Maximum = 100, Value = 15 };

            btnGenerate = new Button { Text = "GENERUJ PLIKI / KOPIUJ", Dock = DockStyle.Bottom, Height = 45, BackColor = Color.LightSkyBlue, FlatStyle = FlatStyle.Flat };
            btnPrint = new Button { Text = "DRUKUJ NA WYBRANEJ", Dock = DockStyle.Bottom, Height = 65, BackColor = Color.LimeGreen, Font = new Font(this.Font, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            
            btnGenerate.Click += (s, e) => ProcessCodes(false);
            btnPrint.Click += (s, e) => ProcessCodes(true);

            pnlSettings.Controls.Add(btnGenerate);
            pnlSettings.Controls.Add(new Control { Height = 10, Dock = DockStyle.Bottom });
            pnlSettings.Controls.Add(btnPrint);

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 11), Text = "KOD123" };
            lblStatus = new Label { Text = "Gotowy.", Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };

            this.Controls.Add(txtInput);
            this.Controls.Add(pnlSettings);
            this.Controls.Add(lblStatus);
        }

        private void LoadPrinters()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters) cmbPrinters.Items.Add(printer);
            if (cmbPrinters.Items.Count > 0) cmbPrinters.SelectedIndex = 0;
        }

        private void ProcessCodes(bool print)
        {
            var lines = txtInput.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length == 0) return;

            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            foreach (var line in lines)
            {
                try {
                    Bitmap bmp = GenerateMaxiCode(line, (int)numCodeSize.Value);
                    if (lines.Length == 1 && !print) Clipboard.SetImage(bmp);
                    string safeName = string.Join("_", line.Split(Path.GetInvalidFileNameChars()));
                    bmp.Save(Path.Combine(outputDir, safeName + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                    if (print) PrintImage(bmp, cmbPrinters.SelectedItem.ToString());
                } catch (Exception ex) { MessageBox.Show("Błąd: " + ex.Message); }
            }
            lblStatus.Text = "Zakończono operację.";
        }

        private Bitmap GenerateMaxiCode(string text, int pixelSize)
        {
            // ZMIANA: Używamy Barcoder.Maxicode.MaxicodeEncoder bezpośrednio
            var maxicode = Barcoder.Maxicode.MaxicodeEncoder.Encode(text);
            
            // ZMIANA: Renderer jest w Barcoder.Renderer.Image.ImageRenderer
            var renderer = new Barcoder.Renderer.Image.ImageRenderer(pixelSize: pixelSize);
            
            using (var ms = new MemoryStream())
            {
                renderer.Render(maxicode, ms);
                ms.Position = 0;

                using (var skBitmap = SKBitmap.Decode(ms))
                {
                    int textSpace = (int)(skBitmap.Height * 0.25);
                    var info = new SKImageInfo(skBitmap.Width, skBitmap.Height + textSpace);
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.White);
                        canvas.DrawBitmap(skBitmap, 0, 0);
                        using (var paint = new SKPaint { 
                            Color = SKColors.Black, 
                            TextSize = (float)(pixelSize * 2.5), 
                            IsAntialias = true, 
                            TextAlign = SKTextAlign.Center,
                            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                        })
                        {
                            canvas.DrawText(text, info.Width / 2, info.Height - (textSpace / 4), paint);
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

        private void PrintImage(Bitmap bmp, string printerName)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = printerName;
                int w = (int)((double)numPaperW.Value / 25.4 * 100);
                int h = (int)((double)numPaperH.Value / 25.4 * 100);
                pd.DefaultPageSettings.PaperSize = new PaperSize("Custom", w, h);
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                pd.PrintPage += (s, ev) => {
                    if (bmp.Width > ev.PageBounds.Width || bmp.Height > ev.PageBounds.Height) {
                        float scale = Math.Min((float)ev.PageBounds.Width / bmp.Width, (float)ev.PageBounds.Height / bmp.Height);
                        ev.Graphics.DrawImage(bmp, (ev.PageBounds.Width - bmp.Width * scale) / 2, (ev.PageBounds.Height - bmp.Height * scale) / 2, bmp.Width * scale, bmp.Height * scale);
                    } else {
                        ev.Graphics.DrawImage(bmp, (ev.PageBounds.Width - bmp.Width) / 2, (ev.PageBounds.Height - bmp.Height) / 2);
                    }
                };
                pd.Print();
            }
        }

        [STAThread] static void Main() { Application.EnableVisualStyles(); Application.Run(new MainForm()); }
    }
}
