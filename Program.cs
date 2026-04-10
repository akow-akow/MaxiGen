using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using ZXing;
using ZXing.Maxicode;
using ZXing.Common;

namespace MaxiGen
{
    public class MainForm : Form
    {
        private TextBox txtInput;
        private NumericUpDown numPaperW, numPaperH, numCodeScale, numFontSize;
        private CheckBox chkShowText;
        private ComboBox cmbPrinters;
        private Button btnGenerate, btnPrint;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "MaxiGen FREE (No Watermark) - Zebra Edition";
            this.Size = new Size(700, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            Panel pnlSettings = new Panel { Dock = DockStyle.Right, Width = 250, Padding = new Padding(10), BackColor = Color.FromArgb(240, 240, 240) };
            
            pnlSettings.Controls.Add(new Label { Text = "Drukarka:", Dock = DockStyle.Top });
            cmbPrinters = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadPrinters();
            pnlSettings.Controls.Add(cmbPrinters);

            pnlSettings.Controls.Add(new Label { Text = "Szer. etykiety (mm):", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numPaperW = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 100 };
            pnlSettings.Controls.Add(numPaperW);
            
            pnlSettings.Controls.Add(new Label { Text = "Wys. etykiety (mm):", Dock = DockStyle.Top, Margin = new Padding(0, 5, 0, 0) });
            numPaperH = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 70 };
            pnlSettings.Controls.Add(numPaperH);

            pnlSettings.Controls.Add(new Label { Text = "Skala MaxiCode (Zoom):", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numCodeScale = new NumericUpDown { Dock = DockStyle.Top, Minimum = 1, Maximum = 50, Value = 10 };
            pnlSettings.Controls.Add(numCodeScale);

            pnlSettings.Controls.Add(new Label { Text = "Rozmiar napisu (pt):", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numFontSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 4, Maximum = 100, Value = 12 }; 
            pnlSettings.Controls.Add(numFontSize);

            chkShowText = new CheckBox { Text = "Pokaż tekst pod kodem", Checked = true, Dock = DockStyle.Top, Margin = new Padding(0, 5, 0, 0) };
            pnlSettings.Controls.Add(chkShowText);

            btnGenerate = new Button { Text = "ZAPISZ PNG", Dock = DockStyle.Bottom, Height = 45, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnPrint = new Button { Text = "DRUKUJ", Dock = DockStyle.Bottom, Height = 65, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, Font = new Font(this.Font, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            
            btnGenerate.Click += (s, e) => ProcessCodes(false);
            btnPrint.Click += (s, e) => ProcessCodes(true);

            pnlSettings.Controls.Add(btnGenerate);
            pnlSettings.Controls.Add(new Control { Height = 10, Dock = DockStyle.Bottom });
            pnlSettings.Controls.Add(btnPrint);

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10), Text = "FREE-CODE-123\nTEST-NO-WATERMARK" };
            lblStatus = new Label { Text = "Gotowy (Wersja darmowa).", Dock = DockStyle.Bottom, Height = 25, BackColor = Color.White };

            this.Controls.Add(txtInput);
            this.Controls.Add(pnlSettings);
            this.Controls.Add(lblStatus);
        }

        private void LoadPrinters()
        {
            try {
                foreach (string printer in PrinterSettings.InstalledPrinters) cmbPrinters.Items.Add(printer);
                if (cmbPrinters.Items.Count > 0) cmbPrinters.SelectedIndex = 0;
            } catch { }
        }

        private void ProcessCodes(bool print)
        {
            var lines = txtInput.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length == 0) return;

            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // Inicjalizacja writera ZXing
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.MAXICODE,
                Options = new EncodingOptions { Height = 300, Width = 300, Margin = 1 }
            };

            foreach (var line in lines)
            {
                try 
                {
                    string safeName = string.Join("_", line.Split(Path.GetInvalidFileNameChars()));
                    string filePath = Path.Combine(outputDir, safeName + ".png");

                    // 1. Generuj surowy obraz kodu
                    var pixelData = writer.Write(line);
                    using (var tempBmp = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                    {
                        var bitmapData = tempBmp.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                        try { System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length); }
                        finally { tempBmp.UnlockBits(bitmapData); }

                        // 2. Skalowanie i dodawanie tekstu (Ręczne rysowanie)
                        int scale = (int)numCodeScale.Value;
                        int textSpace = chkShowText.Checked ? (int)numFontSize.Value * 2 : 0;
                        
                        int finalW = tempBmp.Width * scale;
                        int finalH = (tempBmp.Height * scale) + textSpace;

                        using (Bitmap finalImg = new Bitmap(finalW, finalH))
                        using (Graphics g = Graphics.FromImage(finalImg))
                        {
                            g.Clear(Color.White);
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            
                            // Rysuj kod
                            g.DrawImage(tempBmp, 0, 0, tempBmp.Width * scale, tempBmp.Height * scale);

                            // Rysuj tekst
                            if (chkShowText.Checked)
                            {
                                using (Font font = new Font("Arial", (float)numFontSize.Value, FontStyle.Bold))
                                {
                                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                                    g.DrawString(line, font, Brushes.Black, new RectangleF(0, tempBmp.Height * scale, finalW, textSpace), sf);
                                }
                            }

                            finalImg.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }

                    if (print && cmbPrinters.SelectedItem != null) 
                    {
                        using (Bitmap bmpToPrint = new Bitmap(filePath)) { PrintImage(bmpToPrint, cmbPrinters.SelectedItem.ToString()); }
                    }
                } 
                catch (Exception ex) { MessageBox.Show("Błąd: " + ex.Message); }
            }
            lblStatus.Text = "Gotowe! Kody bez znaku wodnego zapisane.";
        }

        private void PrintImage(Bitmap bmp, string printerName)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = printerName;
                int w = (int)((double)numPaperW.Value / 25.4 * 100);
                int h = (int)((double)numPaperH.Value / 25.4 * 100);
                pd.DefaultPageSettings.PaperSize = new PaperSize("Label", w, h);
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                
                pd.PrintPage += (s, ev) => {
                    ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    float scale = Math.Min((float)ev.PageBounds.Width / bmp.Width, (float)ev.PageBounds.Height / bmp.Height);
                    float drawW = bmp.Width * scale;
                    float drawH = bmp.Height * scale;
                    ev.Graphics.DrawImage(bmp, (ev.PageBounds.Width - drawW) / 2, (ev.PageBounds.Height - drawH) / 2, drawW, drawH);
                };
                pd.Print();
            }
        }

        [STAThread] static void Main() { 
            Application.EnableVisualStyles(); 
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm()); 
        }
    }
}
