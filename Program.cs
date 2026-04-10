using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using Aspose.BarCode.Generation;

// Rozwiązanie konfliktu nazw:
using WinPadding = System.Windows.Forms.Padding;

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

            // Używamy WinPadding zamiast Padding
            Panel pnlSettings = new Panel { Dock = DockStyle.Right, Width = 220, Padding = new WinPadding(10), BackColor = Color.FromArgb(245, 245, 245) };
            
            pnlSettings.Controls.Add(new Label { Text = "Wybierz drukarkę (Printer):", Dock = DockStyle.Top });
            cmbPrinters = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadPrinters();
            pnlSettings.Controls.Add(cmbPrinters);

            pnlSettings.Controls.Add(new Label { Text = "Szer. papieru / Width (mm):", Dock = DockStyle.Top, Margin = new WinPadding(0, 15, 0, 0) });
            numPaperW = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 100 };
            pnlSettings.Controls.Add(numPaperW);
            
            pnlSettings.Controls.Add(new Label { Text = "Wys. papieru / Height (mm):", Dock = DockStyle.Top, Margin = new WinPadding(0, 10, 0, 0) });
            numPaperH = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 70 };
            pnlSettings.Controls.Add(numPaperH);

            pnlSettings.Controls.Add(new Label { Text = "Skala MaxiCode (Pixels):", Dock = DockStyle.Top, Margin = new WinPadding(0, 15, 0, 0) });
            numCodeSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 1, Maximum = 100, Value = 10 };
            pnlSettings.Controls.Add(numCodeSize);

            btnGenerate = new Button { Text = "GENERUJ PLIKI (PNG)", Dock = DockStyle.Bottom, Height = 45, BackColor = Color.LightSkyBlue, FlatStyle = FlatStyle.Flat };
            btnPrint = new Button { Text = "DRUKUJ (PRINT)", Dock = DockStyle.Bottom, Height = 65, BackColor = Color.LimeGreen, Font = new Font(this.Font, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            
            btnGenerate.Click += (s, e) => ProcessCodes(false);
            btnPrint.Click += (s, e) => ProcessCodes(true);

            pnlSettings.Controls.Add(btnGenerate);
            pnlSettings.Controls.Add(new Control { Height = 10, Dock = DockStyle.Bottom });
            pnlSettings.Controls.Add(btnPrint);

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 11), Text = "KOD123\nKOD456" };
            lblStatus = new Label { Text = "Gotowy (Ready).", Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Padding = new WinPadding(5) };

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

            foreach (var line in lines)
            {
                try 
                {
                    string safeName = string.Join("_", line.Split(Path.GetInvalidFileNameChars()));
                    string filePath = Path.Combine(outputDir, safeName + ".png");

                    using (var generator = new BarcodeGenerator(EncodeTypes.MaxiCode, line))
                    {
                        generator.Parameters.Barcode.MaxiCode.MaxiCodeMode = MaxiCodeMode.Mode4;
                        generator.Parameters.Barcode.XDimension.Pixels = (float)numCodeSize.Value;
                        generator.Parameters.Barcode.CodeTextParameters.Location = CodeLocation.Below;
                        
                        generator.Save(filePath, BarCodeImageFormat.Png);
                    }

                    if (print && cmbPrinters.SelectedItem != null) 
                    {
                        using (Bitmap bmp = new Bitmap(filePath))
                        {
                            PrintImage(bmp, cmbPrinters.SelectedItem.ToString());
                        }
                    }
                } 
                catch (Exception ex) 
                { 
                    MessageBox.Show("Blad: " + ex.Message); 
                }
            }
            lblStatus.Text = "Operacja zakonczona!";
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
                    ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
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

        [STAThread] static void Main() 
        { 
            Application.EnableVisualStyles(); 
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm()); 
        }
    }
}
