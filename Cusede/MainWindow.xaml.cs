using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using NAudio.Wave;
using Cusede.helpers;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Threading;
namespace Cusede
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public event EventHandler<double> VolumeChanged;
        private readonly YoutubeClient yt_client;
        private WaveOutEvent wave_o_event;
        private MediaFoundationReader reader;
        private DispatcherTimer timerClock;
        private int currentIndex;
        public MainWindow()
        {
            InitializeComponent();
            yt_client = new YoutubeClient();
            wave_o_event = new WaveOutEvent();
            timerClock = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timerClock.Tick += Timer_Tick;

        }


        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchBox.Text;
            var videos = yt_client.Search.GetVideosAsync(query);
            ResultsList.Items.Clear();
            int contador = 0;
            await foreach (var video in videos)
            {
                ResultsList.Items.Add(new VideoItem { titulo = video.Title, url = video.Url, videoId = video.Id });
                contador++;
                if (contador > 30)
                {
                    break;
                }
                currentIndex = 0;
            }
        }

        private async void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is VideoItem videoItem)
            {
                currentIndex = ResultsList.SelectedIndex;
                await PlayAudioAsync(videoItem.url);
            }
        }

        private async Task PlayAudioAsync(string videoId)
        {
            var streamManifest = await yt_client.Videos.Streams.GetManifestAsync(videoId);
            var audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();
            if (audioStreamInfo != null)
            {
                var stream = await yt_client.Videos.Streams.GetAsync(audioStreamInfo);
                WindowStarting(1, null);
                reader = new MediaFoundationReader(audioStreamInfo.Url);
                wave_o_event.Init(reader);
                wave_o_event.Play();
                timerClock.Start();

            }
        }

        private void WindowStarting(object sender, System.ComponentModel.CancelEventArgs e = null)
        {
            if (Convert.ToInt32(sender) == 1)
            {

                wave_o_event?.Stop();
                reader?.Dispose();
                wave_o_event?.Dispose();
            }
            if (Convert.ToInt32(sender) == 2)
            {
                wave_o_event?.Play();
                reader?.Dispose();
                wave_o_event?.Dispose();
            }
        }
        private async void CahngeMusic(object sender, RoutedEventArgs e = null)
        {
            switch (sender)
            {
                case 1:
                    {
                        if (currentIndex > 0)
                        {
                            currentIndex--;
                            ResultsList.SelectedIndex = currentIndex;
                            if (ResultsList.SelectedItem is VideoItem previousVideo)
                            {
                                await PlayAudioAsync(previousVideo.videoId);
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        if (currentIndex < ResultsList.Items.Count - 1)
                        {
                            currentIndex++;
                            ResultsList.SelectedIndex = currentIndex;
                            if (ResultsList.SelectedItem is VideoItem nextVideo)
                            {
                                await PlayAudioAsync(nextVideo.videoId);
                            }
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (reader != null && wave_o_event.PlaybackState == PlaybackState.Playing)
            {
                CurrentTimeText.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"mm\:ss");
            }
        }

        private void ProgressSlider_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (reader != null)
            {
                reader.CurrentTime = TimeSpan.FromSeconds(ProgressSlider.Value);
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (reader != null && wave_o_event.PlaybackState == PlaybackState.Playing)
            {
                ProgressSlider.Value = reader.CurrentTime.TotalSeconds;
                CurrentTimeText.Text = reader.CurrentTime.ToString(@"mm\:ss");
            }
        }

        private void SearchButton_Start_Click(object sender, RoutedEventArgs e)
        {
            WindowStarting(2, null);
        }


        private void SearchButton_Pause_Click(object sender, RoutedEventArgs e)
        {
            WindowStarting(1, null);
        }



        private void MusicaAtras_Click(object sender, RoutedEventArgs e)
        {
            CahngeMusic(1, null);
        }

        private void MusicaAdelante_Click(object sender, RoutedEventArgs e)
        {
            CahngeMusic(2, null);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (wave_o_event != null)
            {
                wave_o_event.Volume = (float)e.NewValue;
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(null, null);
            }
        }

        private void VolumeDial_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(VolumePath);
                double centerX = VolumePath.ActualWidth / 2;
                double centerY = VolumePath.ActualHeight / 2;
        
                double angle = Math.Atan2(position.Y - centerY, position.X - centerX) * 180 / Math.PI;
                angle = (angle + 360) % 360; // Normaliza el ángulo
         
                double volume = angle / 360;
                wave_o_event.Volume = (float)volume;
                double radius = VolumePath.ActualWidth / 2 - VolumeKnob.Width / 2;
                double radians = angle * Math.PI / 180;
                double knobX = centerX + radius * Math.Cos(radians);
                double knobY = centerY + radius * Math.Sin(radians);
                Canvas.SetLeft(VolumeKnob, knobX - VolumeKnob.Width / 2);
                Canvas.SetTop(VolumeKnob, knobY - VolumeKnob.Height / 2);
                RotateTransform rotate = new RotateTransform(angle, VolumeKnob.Width / 2, VolumeKnob.Height / 2);
                VolumeKnob.RenderTransform = rotate;

                VolumeChanged?.Invoke(this, volume);
            }
        }
    }
}