using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using NetBarcode;
using SkiaSharp;

namespace MaxiGen
{
    public class MainForm : Form
    {
        private TextBox txtInput;
        private NumericUpDown numPaperW, numPaperH;
        private ComboBox cmbPrinters;
        private Button btnGenerate, btnPrint;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "MaxiGen - Zebra Tool";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            Panel pnlSettings = new Panel { Dock = DockStyle.Right, Width = 200, Padding = new Padding(10), BackColor = Color.WhiteSmoke };
            
            pnlSettings.Controls.Add(new Label { Text = "Drukarka:", Dock = DockStyle.Top });
            cmbPrinters = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (string p in PrinterSettings.InstalledPrinters) cmbPrinters.Items.Add(p);
            if (cmbPrinters.Items.Count > 0) cmbPrinters.SelectedIndex = 0;
            pnlSettings.Controls.Add(cmbPrinters);

            pnlSettings.Controls.Add(new Label { Text = "Szer. (mm):", Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) });
            numPaperW = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 200, Value = 100 };
            pnlSettings.Controls.Add(numPaperW);

            pnlSettings.Controls.Add(new Label { Text = "Wys. (mm):", Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) });
            numPaperH = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 200, Value = 70 };
            pnlSettings.Controls.Add(numPaperH);

            btnGenerate = new Button { Text = "ZAPISZ PNG", Dock = DockStyle.Bottom, Height = 40 };
            btnPrint = new Button { Text = "DRUKUJ", Dock = DockStyle.Bottom, Height = 60, BackColor = Color.LightGreen };
            
            btnGenerate.Click += (s, e) => Process(false);
            btnPrint.Click += (s, e) => Process(true);

            pnlSettings.Controls.Add(btnGenerate);
            pnlSettings.Controls.Add(btnPrint);

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, Text = "KOD123" };
            lblStatus = new Label { Text = "Gotowy", Dock = DockStyle.Bottom, Height = 25 };

            this.Controls.Add(txtInput);
            this.Controls.Add(pnlSettings);
            this.Controls.Add(lblStatus);
        }

        private void Process(bool print)
        {
            var lines = txtInput.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            foreach (var line in lines)
            {
                // Generujemy MaxiCode za pomocą NetBarcode
                var barcode = new Barcode(line, NetBarcode.Type.MaxiCode);
                byte[] imgData = barcode.GetByteArray();
                
                using (var ms = new MemoryStream(imgData))
                using (var skBmp = SKBitmap.Decode(ms))
                {
                    // Dodajemy margines na tekst
                    var info = new SKImageInfo(skBmp.Width, skBmp.Height + 40);
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.White);
                        canvas.DrawBitmap(skBmp, 0, 0);
                        
                        using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 24, TextAlign = SKTextAlign.Center, IsAntialias = true })
                        {
                            canvas.DrawText(line, info.Width / 2, info.Height - 10, paint);
                        }

                        using (var image = surface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        using (var finalBmp = new Bitmap(data.AsStream()))
                        {
                            finalBmp.Save(Path.Combine(path, line + ".png"));
                            if (print) PrintImg(finalBmp);
                        }
                    }
                }
            }
            lblStatus.Text = "Zrobione!";
        }

        private void PrintImg(Bitmap bmp)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = cmbPrinters.SelectedItem.ToString();
                pd.DefaultPageSettings.PaperSize = new PaperSize("Label", (int)(numPaperW.Value / 25.4m * 100), (int)(numPaperH.Value / 25.4m * 100));
                pd.PrintPage += (s, e) => {
                    e.Graphics.DrawImage(bmp, 0, 0, e.PageBounds.Width, e.PageBounds.Height);
                };
                pd.Print();
            }
        }

        [STAThread] static void Main() { Application.EnableVisualStyles(); Application.Run(new MainForm()); }
    }
}
