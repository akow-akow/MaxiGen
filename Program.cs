using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using Aspose.BarCode.Generation;

// Alias dla uniknięcia konfliktów nazw
using WinPadding = System.Windows.Forms.Padding;

namespace MaxiGen
{
    public class MainForm : Form
    {
        private TextBox txtInput;
        private NumericUpDown numPaperW, numPaperH, numCodeSize, numFontSize;
        private CheckBox chkShowText;
        private ComboBox cmbPrinters;
        private Button btnGenerate, btnPrint;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "MaxiGen Pro v2.0 - Zebra Tool";
            this.Size = new Size(700, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel boczny z ustawieniami
            Panel pnlSettings = new Panel { Dock = DockStyle.Right, Width = 240, Padding = new WinPadding(10), BackColor = Color.FromArgb(240, 240, 240) };
            
            // --- Sekcja Drukarki ---
            pnlSettings.Controls.Add(new Label { Text = "Drukarka:", Dock = DockStyle.Top });
            cmbPrinters = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadPrinters();
            pnlSettings.Controls.Add(cmbPrinters);

            // --- Sekcja Rozmiarów Papieru ---
            pnlSettings.Controls.Add(new Label { Text = "Szer. etykiety (mm):", Dock = DockStyle.Top, Margin = new WinPadding(0, 15, 0, 0) });
            numPaperW = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 100 };
            pnlSettings.Controls.Add(numPaperW);
            
            pnlSettings.Controls.Add(new Label { Text = "Wys. etykiety (mm):", Dock = DockStyle.Top, Margin = new WinPadding(0, 5, 0, 0) });
            numPaperH = new NumericUpDown { Dock = DockStyle.Top, Minimum = 10, Maximum = 500, Value = 70 };
            pnlSettings.Controls.Add(numPaperH);

            // --- Sekcja MaxiCode ---
            pnlSettings.Controls.Add(new Label { Text = "Skala MaxiCode (Pixels):", Dock = DockStyle.Top, Margin = new WinPadding(0, 15, 0, 0) });
            numCodeSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 1, Maximum = 100, Value = 10 };
            pnlSettings.Controls.Add(numCodeSize);

            // --- NOWE: Sekcja Napisu pod kodem ---
            pnlSettings.Controls.Add(new Label { Text = "Rozmiar czcionki tekstu:", Dock = DockStyle.Top, Margin = new WinPadding(0, 15, 0, 0) });
            numFontSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 4, Maximum = 72, Value = 14 }; // Domyślnie 14, żeby był czytelny
            pnlSettings.Controls.Add(numFontSize);

            chkShowText = new CheckBox { Text = "Pokaż tekst pod kodem", Checked = true, Dock = DockStyle.Top, Margin = new WinPadding(0, 5, 0, 0) };
            pnlSettings.Controls.Add(chkShowText);

            // --- Przyciski Akcji ---
            btnGenerate = new Button { Text = "ZAPISZ PNG", Dock = DockStyle.Bottom, Height = 45, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnPrint = new Button { Text = "DRUKUJ", Dock = DockStyle.Bottom, Height = 65, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, Font = new Font(this.Font, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            
            btnGenerate.Click += (s, e) => ProcessCodes(false);
            btnPrint.Click += (s, e) => ProcessCodes(true);

            pnlSettings.Controls.Add(btnGenerate);
            pnlSettings.Controls.Add(new Control { Height = 10, Dock = DockStyle.Bottom });
            pnlSettings.Controls.Add(btnPrint);

            // Pole tekstowe na dane
            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10), Text = "TEST123456\nMAXICODE_V2" };
            lblStatus = new Label { Text = "Gotowy.", Dock = DockStyle.Bottom, Height = 25, BackColor = Color.White };

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
                        // Ustawienia MaxiCode
                        generator.Parameters.Barcode.MaxiCode.MaxiCodeMode = MaxiCodeMode.Mode4;
                        generator.Parameters.Barcode.XDimension.Pixels = (float)numCodeSize.Value;
                        
                        // Konfiguracja napisu pod kodem
                        if (chkShowText.Checked)
                        {
                            generator.Parameters.Barcode.CodeTextParameters.Location = CodeLocation.Below;
                            generator.Parameters.Barcode.CodeTextParameters.Font.Size.Point = (float)numFontSize.Value;
                            generator.Parameters.Barcode.CodeTextParameters.Font.FamilyName = "Arial";
                        }
                        else
                        {
                            generator.Parameters.Barcode.CodeTextParameters.Location = CodeLocation.None;
                        }

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
                    MessageBox.Show("Błąd: " + ex.Message); 
                }
            }
            lblStatus.Text = "Zakończono generowanie!";
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
