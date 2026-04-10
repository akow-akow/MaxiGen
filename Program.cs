using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Linq;
using ZXing;
using ZXing.Common;

namespace MaxiGen
{
    public class MainForm : Form
    {
        private TextBox txtInput;
        private NumericUpDown numCodeScale;
        private Button btnPrint;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "MaxiGen v3.8 - Forced Engine";
            this.Size = new Size(500, 600);
            
            txtInput = new TextBox { Multiline = true, Dock = DockStyle.Fill, Text = "TEST12345" };
            numCodeScale = new NumericUpDown { Value = 10, Dock = DockStyle.Top };
            btnPrint = new Button { Text = "GENERUJ I DRUKUJ", Dock = DockStyle.Bottom, Height = 50 };
            lblStatus = new Label { Text = "Status: Oczekiwanie...", Dock = DockStyle.Bottom };

            btnPrint.Click += (s, e) => GenerateMaxiCode();

            this.Controls.Add(txtInput);
            this.Controls.Add(new Label { Text = "Skala kodu:", Dock = DockStyle.Top });
            this.Controls.Add(numCodeScale);
            this.Controls.Add(btnPrint);
            this.Controls.Add(lblStatus);
        }

        private void GenerateMaxiCode()
        {
            try
            {
                string text = txtInput.Text.Split('\n')[0].Trim();
                
                // ROZWIĄZANIE PROBLEMU: 
                // Zamiast MultiFormatWriter, używamy bezpośrednio konkretnego enkodera.
                // W ZXing.Net dla .NET 4.8 często trzeba to zainicjować tak:
                var writer = new ZXing.Maxicode.MaxicodeWriter(); 
                
                // Jeśli powyższe sypie błędem o braku klasy, spróbuj zamienić na:
                // var writer = new ZXing.MaxiCode.MaxiCodeWriter(); 

                var matrix = writer.encode(text, BarcodeFormat.MAXICODE, 0, 0);

                int scale = (int)numCodeScale.Value;
                using (Bitmap bmp = new Bitmap(matrix.Width * scale, matrix.Height * scale))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    for (int y = 0; y < matrix.Height; y++)
                    {
                        for (int x = 0; x < matrix.Width; x++)
                        {
                            if (matrix[x, y])
                                g.FillRectangle(Brushes.Black, x * scale, y * scale, scale, scale);
                        }
                    }
                    
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.png");
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    lblStatus.Text = "Wygenerowano pomyślnie!";
                    System.Diagnostics.Process.Start(path);
                }
            }
            catch (Exception ex)
            {
                // Wyświetlamy pełny błąd, żeby wiedzieć czy to brak klasy czy brak enkodera
                MessageBox.Show($"BŁĄD KRYTYCZNY:\n{ex.Message}\n\nTyp błędu: {ex.GetType().Name}");
            }
        }

        [STAThread] static void Main() { Application.Run(new MainForm()); }
    }
}
