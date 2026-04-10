using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using ZXing;
using ZXing.Maxicode;

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
            this.Text = "MaxiGen v3.5 - ZXing Engine";
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

            pnlSettings.Controls.Add(new Label { Text = "Skala MaxiCode:", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numCodeScale = new NumericUpDown { Dock = DockStyle.Top, Minimum = 1, Maximum = 50, Value = 8 };
            pnlSettings.Controls.Add(numCodeScale);

            pnlSettings.Controls.Add(new Label { Text = "Rozmiar napisu (pt):", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numFontSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 4, Maximum = 100, Value = 14 }; 
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

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10), Text = "KOD123456\nMAXICODE-STABLE" };
            lblStatus = new Label { Text = "Silnik: ZXing.Net", Dock = DockStyle.Bottom, Height = 25, BackColor = Color.White };

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
            var writer = new MaxicodeWriter();

            foreach (var line in lines)
            {
                try 
                {
                    string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                    if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                    string filePath = Path.Combine(outputDir, Guid.NewGuid().ToString().Substring(0,8) + ".png");

                    // Generowanie macierzy bitowej (MaxiCode ma stały rozmiar 30x33)
                    var matrix = writer.encode(line, BarcodeFormat.MAXICODE, 0, 0);
                    
                    int scale = (int)numCodeScale.Value;
                    int textSpace = chkShowText.Checked ? (int)(numFontSize.Value * 3) : 0;
                    
                    int bmpW = matrix.Width * scale;
                    int bmpH = matrix.Height * scale;

                    using (Bitmap bmp = new Bitmap(bmpW, bmpH + textSpace))
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        
                        // Rysowanie MaxiCode (czarne kropki/piksele)
                        using (Brush brush = new SolidBrush(Color.Black))
                        {
                            for (int y = 0; y < matrix.Height; y++)
                            {
                                for (int x = 0; x < matrix.Width; x++)
                                {
                                    if (matrix[x, y])
                                        g.FillRectangle(brush, x * scale, y * scale, scale, scale);
                                }
                            }
                        }

                        // Dodawanie napisu
                        if (chkShowText.Checked)
                        {
                            using (Font font = new Font("Arial", (float)numFontSize.Value, FontStyle.Bold))
                            {
                                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                                g.DrawString(line, font, Brushes.Black, new RectangleF(0, bmpH, bmpW, textSpace), sf);
                            }
                        }
                        bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    if (print && cmbPrinters.SelectedItem != null)
                    {
                        PrintFile(filePath, cmbPrinters.SelectedItem.ToString());
                    }
                } 
                catch (Exception ex) { MessageBox.Show("Błąd: " + ex.Message); }
            }
            lblStatus.Text = "Gotowe!";
        }

        private void PrintFile(string path, string printerName)
        {
            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrinterSettings.PrinterName = printerName;
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                pd.PrintPage += (s, ev) => {
                    using (Image img = Image.FromFile(path))
                    {
                        float scale = Math.Min((float)ev.PageBounds.Width / img.Width, (float)ev.PageBounds.Height / img.Height);
                        ev.Graphics.DrawImage(img, 0, 0, img.Width * scale, img.Height * scale);
                    }
                };
                pd.Print();
            }
        }

        [STAThread] static void Main() { 
            Application.EnableVisualStyles(); 
            Application.Run(new MainForm()); 
        }
    }
}
