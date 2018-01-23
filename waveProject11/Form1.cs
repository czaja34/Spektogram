using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using NAudio.Wave;
using Accord.Math;
using System.Numerics;

namespace waveProject11
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Wave File (*.wav)|*.wav";
            if (open.ShowDialog() != DialogResult.OK) return;
         

            customWaveViewer1.WaveStream = new NAudio.Wave.WaveFileReader(open.FileName);
            customWaveViewer1.FitToScreen();

            NAudio.Wave.WaveChannel32 wave = new NAudio.Wave.WaveChannel32(new NAudio.Wave.WaveFileReader(open.FileName));

            byte[] buffer = new byte[16384];
            int read = 0;

            double point = 0;
            double x = 0;

            while (wave.Position < wave.Length)
            {
                read = wave.Read(buffer, 0, 16384);

                for (int i = 0; i < read / 4; i++)
                {

                    point = BitConverter.ToSingle(buffer, (i * 4));
                    x = x + (0.00002267573 / 2);

                    chart2.Series["wave"].Points.AddXY(x, point);
                }
}
            WaveFileReader file = new WaveFileReader(open.FileName);
            
            ComputeGraph(file);


     }

        public void plotHeatMap(double totalSeconds, double[,] RoboczeYs, int counterOfPieces, int lengthOfOnePiece)
        {
            chart1.Series.Dispose();
            chart1.ChartAreas.Clear();
            ChartArea CA = chart1.ChartAreas.Add("CA");
            CA.AxisX.Title = "czas";
            CA.AxisX.TitleAlignment = StringAlignment.Far;
            CA.AxisX.TextOrientation = TextOrientation.Horizontal;
            CA.AxisX.TitleFont = new Font("Times New Roman", 12.0f);
            CA.AxisY.Title = "częstotliwość";
            CA.AxisY.TitleAlignment = StringAlignment.Far;
            CA.AxisY.TextOrientation = TextOrientation.Rotated270;
            CA.AxisY.TitleFont = new Font("Times New Roman", 12.0f);
            chart1.Series.Clear();
            Series S1 = chart1.Series.Add("S1");
            chart1.Legends.Clear();
            S1.ChartType = SeriesChartType.Point;
            Size sz = chart1.ClientSize;
            double maximus1 = 0;
            for (int i = 0; i < counterOfPieces; i++)
            {
                for (int j = 0; j < lengthOfOnePiece; j++)
                {
                    if (maximus1 < RoboczeYs[i, j]) maximus1 = RoboczeYs[i, j];
                }
            }
            List<double> maximus = new List<double>();
            for (int i = 9; i >= 0; i--)
            {
                maximus.Add(Math.Pow(0.1, 0.3 * i) * maximus1);
            }
            maximus.Add(maximus1);
            for (int i = 0; i < counterOfPieces; i++)
            {
                for (int j = 0; j < lengthOfOnePiece / 2; j++)
                {
                    int pt = S1.Points.AddXY(((double)i / counterOfPieces) * totalSeconds, ((double)j / lengthOfOnePiece) * 44100.0);
                    double pointPower = RoboczeYs[i, j];
                    if (pointPower < maximus[0]) S1.Points[pt].MarkerColor = Color.White;
                    else if (pointPower >= maximus[0] && pointPower < maximus[1]) S1.Points[pt].MarkerColor = Color.LightGreen;
                    else if (pointPower >= maximus[1] && pointPower < maximus[2]) S1.Points[pt].MarkerColor = Color.Green;
                    else if (pointPower >= maximus[2] && pointPower < maximus[3]) S1.Points[pt].MarkerColor = Color.LightSkyBlue;
                    else if (pointPower >= maximus[3] && pointPower < maximus[4]) S1.Points[pt].MarkerColor = Color.LightBlue; 
                    else if (pointPower >= maximus[4] && pointPower < maximus[5]) S1.Points[pt].MarkerColor = Color.Blue;
                    else if (pointPower >= maximus[5] && pointPower < maximus[6]) S1.Points[pt].MarkerColor = Color.LightYellow;
                    else if (pointPower >= maximus[6] && pointPower < maximus[7]) S1.Points[pt].MarkerColor = Color.LightGoldenrodYellow;
                    else if (pointPower >= maximus[7] && pointPower < maximus[8]) S1.Points[pt].MarkerColor = Color.Yellow; 
                    else if (pointPower >= maximus[8] && pointPower <= maximus[9]) S1.Points[pt].MarkerColor = Color.Orange;

                }
            }
            maximus.Clear();
            this.ActiveControl = chart1;
            //Try to do that with file - RAM use probably will be less and maybe not so many computation?
        }


        public void ComputeGraph(WaveFileReader fileReader)
        {
            byte[] buffer = new byte[fileReader.Length]; //To jest bufor na cały dźwięk
            int read = fileReader.Read(buffer, 0, buffer.Length); //To jest po prostu przeniesienie dźwięku z pliku do tego bufora
            Int16[] buffer2 = new Int16[fileReader.Length + 50]; // Już niepotrzebne
            Int32[] vals = new Int32[(buffer.Length - 2) / 2]; //Nowa tablica z wartościami z bufora.
            int YsLength = 2;                                   //Dostosowanie długości YsLength, bo algorytm FFT wymaga, żeby była to potęga 2. Ma być większy niż bufor, żeby się wszystko zmieściło.
            while (YsLength < ((buffer.Length - 2) / 2)) YsLength = YsLength * 2;
            double[] Ys = new double[YsLength]; //To jest właśnie ta tablica z wartościami do FFT
            for (int i = 0; i < YsLength; i++) Ys[i] = 0; //Zapełnienie zerami
            double[] Xs = new double[YsLength]; //Takiej samej długości tablica na oś X
            for (int i = 0; i < (buffer.Length - 2) / 2; i++)
            {
                //buffer2[i] = BitConverter.ToInt16(buffer, i);
                byte hbyte = buffer[i * 2 + 1];     //To skopiowałem z internetu. Dzięki temu się uzyskuje amplitudę. Ale co tu się dzieje, nie mam pojęcia, o co chodzi  przesunięciami bitowymi
                byte lbyte = buffer[i * 2 + 0];
                vals[i] = (int)(short)((hbyte << 8) | lbyte);
                Ys[i] = vals[i]; //To są wartości amplitudy
                Xs[i] = ((double)i / ((buffer.Length - 2) / 2)) * fileReader.TotalTime.TotalSeconds;    //Tutaj do tablicy zapisuje po prostu kolejne wartości sekund, żeby nie wygladał wykres bardzo lipnie. To jest poo prostu kolejna iteracja, podzielona przez całą długość w sekundach ;)
            }
            //Tutaj nefralgiczny moment
            /*
             * FFT działa na próbkach o potędze dwójki
             * Ja wybrałem, że długość jednej próbki to będzie 1024 elementy.
             * Ponieważ wszystkie próbki mam w jednej tablicy, to po prostu muszę podzielić tę tablicę na dwa indeksy tak, żeby w każdym indeksie było po 1024 próbki
             * W tej rozbudowanej pętli for właśnie takie podzielenie tej tablicy się odbywa
             * Wynikiem działania jest tablica dwuwumiarowa RoboczeYs
             */
            int lengthOfOnePiece = 1024;
            int counterOfPieces = YsLength / lengthOfOnePiece;
            double[,] RoboczeYs = new double[counterOfPieces, lengthOfOnePiece];
            int counterOfCurrentIndexOfYs = 0;
            for (int i = 0; i < counterOfPieces; i++)
            {
                for (int j = 0; j < lengthOfOnePiece; j++)
                {
                    if (counterOfCurrentIndexOfYs < (counterOfPieces * lengthOfOnePiece))
                    {
                        RoboczeYs[i, j] = Ys[counterOfCurrentIndexOfYs];
                        counterOfCurrentIndexOfYs++;
                    }
                    else
                    {
                        RoboczeYs[i, j] = 0;
                        counterOfCurrentIndexOfYs++;
                    }
                }
            }
            counterOfCurrentIndexOfYs = 0;
            /*
             *Tutaj kolejny nefralgiczny moment - a mianowicie samo FFT.
             * Nazwą FFT2 się nie przejmuj, jest to po prostu FFT w mojej przerobionej wersji, żeby pasowało do moich danych
             * Tutaj skoczmy do definicji metody FFT2
            */
            double[,] RoboczyYs2 = FFT2(RoboczeYs, counterOfPieces, lengthOfOnePiece);
            int countOfZeros = 0;
            for (int i = counterOfPieces - 1; i >= 0; i--)
            {
                if (RoboczyYs2[i, 0] > 0)
                {
                    countOfZeros = i;
                    break;
                }
            }
            //Nagrany dźwięk tworzy ilość próbekm która nie odpowiada potędze dwójki. Powyższą pętlą i poniższą instrukcją
            //odcinam ciszę, czyli same zera. Tablica Ys3 to zapis już gotowych, przeliczonych po FFT próbek.
            //Ta tablica w jednej próbce zawiera POZIOMY AMPLITUDY na KONKRETNYCH POZIOMACH CZĘSTOTLIWOŚCI
            //Co to oznacza? To znaczy, że skoro mam 1024 elementowe próbki i nagrywałem dźwięk z wartościmi progowymi
            // 0 - 44000Hz, to teraz te zakresy częstotliwości zostały przypisane do konkretnej próbki.
            // Co to oznacza? To znaczy, że mając 1024-elementową próbkę
            // -element [0] tej tablicy to amplituda (siła głośności) na zakresie częstotliwości 0-42,96 Hz
            // -element [1] tej tablicy to amplituda (siła głośności) na zakresie częstotliwości 42,97-85,93 Hz
            // -element [3] tej tablicy to amplituda (siła głośności) na zakresie częstotliwości 85,93 - 128,91 Hz
            // .....
            // -element [1023] tej tablicy to amplituda (siła głośności) na zakresie częstotliwości 44057,04 - 44100 Hz
            // I właśnie to zawiera tablica Ys3
            double[,] Ys3 = new double[countOfZeros, lengthOfOnePiece / 2];
            for (int i = 0; i < countOfZeros; i++)
            {
                for (int j = 0; j < lengthOfOnePiece / 2; j++)
                {
                    Ys3[i, j] = RoboczyYs2[i, j];
                }
            }
            double[] Ys2 = new double[YsLength];
            double[] Xs3 = new double[YsLength];
            for (int i = 0; i < counterOfPieces; i++)
            {
                for (int j = 0; j < lengthOfOnePiece; j++)
                {
                    if (YsLength < (i * j)) break;
                    else
                    {
                        //Ys2[counterOfCurrentIndexOfYs] = RoboczyYs2[i, j];
                        Xs3[counterOfCurrentIndexOfYs] = (double)counterOfCurrentIndexOfYs / Ys.Length * (fileReader.WaveFormat.SampleRate) / 1000.0;
                        counterOfCurrentIndexOfYs++;
                    }
                }
            }
            int w1 = 0;
            for (int i = Ys2.Length - 1; i >= 0; i--)
            {
                if (Ys2[i] > 0)
                {
                    w1 = i;
                    break;
                }
            }

            int countOfXs = Xs.Length;
            int countOfYs = Ys.Length;
            int countOfXs3 = Xs3.Length;
            Invalidate(true);

           
            
            System.IO.StreamWriter writerData = new System.IO.StreamWriter("data.txt");

            writerData.Close();

            plotHeatMap(fileReader.TotalTime.TotalSeconds, Ys3, countOfZeros, lengthOfOnePiece / 2);

           
            this.ActiveControl = chart1;
        }

       
        public double[,] FFT2(double[,] data, int count, int lengthOfOnePiece)
        {
            /*
             *OK, skoro skoczyliśmy to wyjaśniam do FFT2
             * data - to jest ta przerobiona tablica
             * count - ponieważ w zależności od długości pliku liczba próbek będzie różna,
             * to po podzieleniu oryginalnej tablicy na 1024 elementy możemy otrzymać różną ilość 1024-elementowych próbek.
             * Count to właśnie zmienna, przechowująca ilość tych próbek
             * lengthOfOnePiece - to jest zmienna, przechowująca długość pojedynczej próbki, musi być potęgą 2
            */
            double[,] fft2 = new double[count, lengthOfOnePiece];
            for (int j = 0; j < (int)count; j++)
            {
                //Ten algorytm mam z internetu. Służył do przeliczania tylko jednej próbki o długość 4096
                //Ponieważ ja mam tych próbek miliony, to wsadziłem to w porcje 1024-elementowe.
                //Musi być to w porcjach, dlatego, że ten dolny wykres tworzy imitację całego obrazu,
                //ale tak naprawdę jest to bardzo dużo słupków ustawionych koło siebie o wysokości 1024
                //Jeśli nagrasz króciutki plik, to zobaczysz o co mi chodzi
                //Wynik tych pojedynczych slupków zapisuje do tablicy fft2 i zwracam ;)
                double[] fft = new double[lengthOfOnePiece]; // this is where we will store the output (fft)
                Complex[] fftComplex = new Complex[lengthOfOnePiece]; // the FFT function requires complex format
                for (int i = 0; i < lengthOfOnePiece; i++)
                {
                    fftComplex[i] = new Complex(data[j, i], 0.0); // make it complex format (imaginary = 0)
                }
            
                Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
                for (int i = 0; i < lengthOfOnePiece; i++)
                {
                    fft[i] = fftComplex[i].Magnitude; // back to double
                                                      //fft[i] = Math.Log10(fft[i]); // convert to dB
                }
                for (int i = 0; i < lengthOfOnePiece; i++)
                {
                    fft2[j, i] = fft[i];
                }
            }
            return fft2;
            //todo: this could be much faster by reusing variables
        
    }


        private void zamknijToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
