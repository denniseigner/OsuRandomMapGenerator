using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using System.Xml;

namespace OsuRandomMapGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Action _cancelWork;
        String mapURL;
        bool autoOpen;
        int rating, difficulty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void bSearch_Click(object sender, RoutedEventArgs e)
        {
            bSearch.IsEnabled = false;
            bStop.IsEnabled = true;
            autoOpen = (bool)cbOpen.IsChecked;

            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                _cancelWork = () =>
                {
                    bStop.IsEnabled = false;
                    cancellationTokenSource.Cancel();
                };

                var limit = 10;

                var progressReport = new Progress<int>(
                    (i) =>
                            lProgress.Content = "URLs tested: " + i.ToString()
                    );

                var token = cancellationTokenSource.Token;

                await Task.Run(() =>
                    DoWork(limit, token, progressReport),
                    token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            bSearch.IsEnabled = true;
            bStop.IsEnabled = false;
            bOpen.IsEnabled = true;
            _cancelWork = null;
        }

        private int DoWork(int limit, CancellationToken token, IProgress<int> progressReport)
        {
            bool loop = false;
            string baseURL = "https://osu.ppy.sh/s/";

            Random r = new Random();

            var progress = 0;


            using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
            {
                while (!loop)
                {
                    int rand = r.Next(10000, 1000000);
                    string doUrl = baseURL + rand.ToString();

                    string htmlCode = client.DownloadString(doUrl);

                    if (checkFilter(htmlCode))
                    {
                        mapURL = doUrl;
                        if (autoOpen)
                            System.Diagnostics.Process.Start(doUrl);
                        loop = true;
                    }

                    progress++;
                    progressReport.Report(progress++);

                    if (token.IsCancellationRequested)
                    {
                        return limit;
                    }
                }
            }

            return limit;
        }

        private bool checkFilter(string htmlCode)
        {
            bool returnBool = false;
            int cnt = 0;
            int limit = 1;

            if (htmlCode.IndexOf("Ranked:") != -1)
            {
                cnt++;
            }

            if (rating != 0 && cnt > limit - 1)
            {
                limit++;
                string useString = htmlCode.Substring(htmlCode.IndexOf("User Rating:") + htmlCode.Substring(htmlCode.IndexOf("User Rating:")).IndexOf("<td"),
                    htmlCode.IndexOf("Success Rate:") - (htmlCode.IndexOf("User Rating:") + htmlCode.Substring(htmlCode.IndexOf("User Rating:")).IndexOf("<td")));
                using (XmlReader reader = XmlReader.Create(new StringReader(useString)))
                {
                    double neg = 0;
                    double pos = 0;

                    reader.ReadToFollowing("td");
                    reader.ReadToFollowing("td");

                    neg = Double.Parse(reader.ReadElementContentAsString().Replace(",", ""));

                    reader.ReadToFollowing("td");

                    pos = Double.Parse(reader.ReadElementContentAsString().Replace(",", ""));

                    double rat = (pos / (neg + pos)) * 100;

                    if (rat >= rating)
                    {
                        cnt++;
                    }
                }
            }

            if (difficulty != 0 && cnt > limit - 1)
            {
                limit++;
                string useString = htmlCode.Substring(htmlCode.IndexOf("Star Difficulty") + htmlCode.Substring(htmlCode.IndexOf("Star Difficulty")).IndexOf("<td"),
                    htmlCode.IndexOf("Creator:") - (htmlCode.IndexOf("Star Difficulty") + htmlCode.Substring(htmlCode.IndexOf("Star Difficulty")).IndexOf("<td")));

                string difficultyOfBeatmap = useString.Substring(IndexOfSecond(useString, "</div>"),
                    useString.IndexOf("</td>") - useString.IndexOf("</div>"));

                difficultyOfBeatmap = difficultyOfBeatmap.Replace("</div> (", "");
                difficultyOfBeatmap = difficultyOfBeatmap.Replace(")</td>", "");

                difficultyOfBeatmap = difficultyOfBeatmap.Substring(0, difficultyOfBeatmap.IndexOf(".")); ;

                int difficultyOfBeatmapInt = Int32.Parse(difficultyOfBeatmap);

                if (difficultyOfBeatmapInt >= difficulty)
                {
                    cnt++;
                }
            }

            if (cnt == limit)
            {
                returnBool = true;
            }

            return returnBool;
        }

        private int IndexOfSecond(string theString, string toFind)
        {
            int first = theString.IndexOf(toFind);

            if (first == -1) return -1;

            // Find the "next" occurrence by starting just past the first
            return theString.IndexOf(toFind, first + 1);
        }

        private void bStop_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelWork != null)
                _cancelWork();
        }

        private void bOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(mapURL);
        }

        private void sDifficulty_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = sDifficulty.Value;
            lDifficulty.Content = value.ToString("0");
            difficulty = (int)value;
        }

        private void sRating_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = sRating.Value;
            lRating.Content = value.ToString("0");
            rating = (int)value;
        }
    }
}
