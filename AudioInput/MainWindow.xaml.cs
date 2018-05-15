using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;


namespace AudioInput
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        WaveIn wavein;
        WaveFormat formato;
        AudioFileReader reader;
        WaveOutEvent waveOut;

        string[] notas = { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };
        double pasoNota = Math.Pow(2, 1.0 / 12.0);
        double frecuenciaLaBase = 110.0;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {
            wavein = new WaveIn();
            wavein.WaveFormat = new WaveFormat(44100, 16, 1);
            formato = wavein.WaveFormat;

            wavein.DataAvailable += OnDataAvailable;
            wavein.BufferMilliseconds = 500;

            wavein.StartRecording();
        }
        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;

            double acumulador = 0;

            double nummuestras = bytesGrabados / 2;
            int exponente = 1;
            int numeroMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;
            } while (bitsMaximos < nummuestras);

            exponente -= 2;
            numeroMuestrasComplejas = bitsMaximos / 2;

            Complex[] muestrasComplejas =
                new Complex[numeroMuestrasComplejas];

            for (int i = 0; i < bytesGrabados; i += 2)
            {
                short muestra = (short)(buffer[i + 1] << 8 | buffer[i]);

                float muestra32bits = (float)muestra / 32768.0f;

                acumulador += Math.Abs(muestra32bits);
                if (i / 2 < numeroMuestrasComplejas)
                {
                    muestrasComplejas[i / 2].X = muestra32bits;
                }


            }
            double promedio = acumulador / ((double)bytesGrabados / 2.0);

            if (promedio > 0)
            {
                FastFourierTransform.FFT(true, exponente, muestrasComplejas);
                float[] valoresAbsolutos =
                    new float[muestrasComplejas.Length];

                for (int i = 0; i < muestrasComplejas.Length; i++)
                {
                    valoresAbsolutos[i] = (float)
                        Math.Sqrt((muestrasComplejas[i].X * muestrasComplejas[i].X) +
                        (muestrasComplejas[i].Y * muestrasComplejas[i].Y));
                }

                int indiceMaximo =
                    valoresAbsolutos.ToList().IndexOf(
                        valoresAbsolutos.Max());

                float frecFundamental = (float)(indiceMaximo * wavein.WaveFormat.SampleRate) / (float)valoresAbsolutos.Length;

                //frecFundamental, promedio




                lblFrecuencia.Text = frecFundamental.ToString("n2");

                int octava = 0;
                int indiceTono = (int)Math.Round(Math.Log10(frecFundamental / frecuenciaLaBase) / Math.Log10(pasoNota));
                if (indiceTono < 0)
                {
                    do
                    {
                        indiceTono += 12;
                        octava--;
                    } while (indiceTono < 0);
                }
                else if (indiceTono > 11)
                {
                    do
                    {
                        octava++;
                        indiceTono -= 12;
                    } while (indiceTono > 11);
                }
                lblTono.Text = notas[indiceTono];
                //lblTono.Text = octava.ToString();
                double frecTono = frecuenciaLaBase;
                for (int i = 0; i < Math.Abs(octava); i++)
                {
                    if (octava > 0)
                    {
                        frecTono *= 2.0;
                    }
                    else if (octava < 0)
                    {
                        frecTono /= 2.0;
                    }
                }

                for (int i = 0; i < indiceTono; i++)
                {
                    frecTono *= Math.Pow(2, 1.0 / 12.0);
                }
                lblFrecTono.Text = frecTono.ToString();


                double proxTono = frecTono * Math.Pow(2, 1.0 / 12.0);
                double antTono = frecTono / Math.Pow(2, 1.0 / 12.0);
                double rango = proxTono - antTono;
                double frecNormalizada = (frecFundamental - antTono) / rango;

                elipse.Margin = new Thickness(0, ((frecNormalizada * 200) - 100) * -1, 0, 0);
                Grid.SetRow(elipseTono, indiceTono);
               

            } else
            {
                lblFrecTono.Text = "0";
                lblFrecuencia.Text = "0";
                lblTono.Text = "-";

            }

           
            
        }

        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            wavein.StopRecording();
        }
    }
}
