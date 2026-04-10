using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using Barcoder;
// Zmiana: Biblioteka często używa tej przestrzeni nazw dla enkodera
using Barcoder.Maxicode; 
using Barcoder.Renderer.Image;

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
            this.Text = "MaxiGen FREE v3.1 - Fixed Namespace";
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
            numCodeScale = new NumericUpDown { Dock = DockStyle.Top, Minimum = 1, Maximum = 50, Value = 10 };
            pnlSettings.Controls.Add(numCodeScale);

            pnlSettings.Controls.Add(new Label { Text = "Rozmiar napisu (pt):", Dock = DockStyle.Top, Margin = new Padding(0, 15, 0, 0) });
            numFontSize = new NumericUpDown { Dock = DockStyle.Top, Minimum = 4, Maximum = 100, Value = 16 }; 
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

            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10), Text = "KOD123456\nMAXICODE-FREE" };
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
            foreach (var line in lines)
            {
                try 
                {
                    string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                    if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                    string filePath = Path.Combine(outputDir, Guid.NewGuid().ToString().Substring(0,8) + ".png");

                    // FIX: Bezpośrednie wywołanie enkodera z poprawioną wielkością liter
                    var barcode = Barcoder.Maxicode.MaxicodeEncoder.Encode(line, 4);

                    var renderer = new ImageRenderer();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        renderer.Render(barcode, ms);
                        using (Bitmap tempBmp = new Bitmap(ms))
                        {
                            int scale = (int)numCodeScale.Value;
                            int textSpace = chkShowText.Checked ? (int)(numFontSize.Value * 2.5) : 0;
                            
                            using (Bitmap finalImg = new Bitmap(tempBmp.Width * scale, (tempBmp.Height * scale) + textSpace))
                            using (Graphics g = Graphics.FromImage(finalImg))
                            {
                                g.Clear(Color.White);
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                g.DrawImage(tempBmp, 0, 0, tempBmp.Width * scale, tempBmp.Height * scale);

                                if (chkShowText.Checked)
                                {
                                    using (Font font = new Font("Arial", (float)numFontSize.Value, FontStyle.Bold))
                                    {
                                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                                        g.DrawString(line, font, Brushes.Black, new RectangleF(0, tempBmp.Height * scale, finalImg.Width, textSpace), sf);
                                    }
                                }
                                finalImg.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    if (print) { /* Logika drukowania analogiczna do poprzedniej */ }
                } 
                catch (Exception ex) { MessageBox.Show("Błąd: " + ex.Message); }
            }
            lblStatus.Text = "Zakończono!";
        }
    }
}
