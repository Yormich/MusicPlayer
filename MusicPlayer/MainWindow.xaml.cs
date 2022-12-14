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
using System.Media;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Reflection.Emit;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace MusicPlayer
{

    public partial class MainWindow : Window
    {
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private SongFileManager _songFileManager = new SongFileManager();
        
        private const string _baseImagePath = "/StaticImages";

        private bool isRepeatMode = false;

        private bool isMixedMode = false;

        private ObservableCollection<string> songNames = new ObservableCollection<string>();

        //если заморочиться можно хранить в файлах и удалять через время простоя
        private List<string> history = new List<string>();

        private Dictionary<int, int> mixedCollection;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = _mediaPlayer;

            songNames = _songFileManager.GetSongNames();
            Songs_ListBox.ItemsSource = songNames;
            DisableSongRelatedElements();
            _songFileManager
                .SetImageSource(PlayPauseBtnState_Image, System.IO.Path.Combine(_baseImagePath, "Play.png"));

            Volume_Slider.Minimum = 0.0;
            Volume_Slider.Maximum = 1.0;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void CloseApp_Btn_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void FullScreen_Btn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal
                : WindowState.Maximized;
            string ImagePath = System.IO.Path.Combine(_baseImagePath, this.WindowState == WindowState.Maximized ?
                "ToWindow.png" : "FullScreen.png");
            _songFileManager
                .SetImageSource(WindowMode_Image, System.IO.Path.Combine(_baseImagePath, ImagePath));
        }

        private void Collapse_Btn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void AddSongs_Btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Songs(*.mp3;*.mp4;*.wav;*.aac)|*.mp3;*.mp4;*.wav;*.aac";
            fileDialog.FilterIndex = 0;
            fileDialog.Multiselect = true;
            //add interaction with added songs in file
            if(fileDialog.ShowDialog() ?? false)
            {
                string[] paths = fileDialog.FileNames;
                    foreach (string path in paths)
                    {
                        string Name = System.IO.Path.GetFileName(path);
                        if (!songNames.Contains(Name))
                        {
                            songNames.Add(Name);
                            _songFileManager.AddSongEntry(path);
                        }
                    }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if(_mediaPlayer.Source != null)
            {
                if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    //change labels 
                    CurrentSongTime_Lbl.Content = _mediaPlayer.Position.ToString(@"mm\:ss");
                    SongDuration_Lbl.Content = _mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");

                    //and progress bar
                    SongProgress_PgBar.Maximum = (int)_mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    SongProgress_PgBar.Value = (int)_mediaPlayer.Position.TotalSeconds;

                    if (_mediaPlayer.Position == _mediaPlayer.NaturalDuration.TimeSpan)
                    {
                        LoadNextSong();
                    }
                }
            }
        }

        private void Songs_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string songName = Songs_ListBox.SelectedValue as string;
                if (songNames.Contains(songName))
                {
                    string path = _songFileManager.GetSongPath(songName);
                    EnableSongRelatedElements();
                    _mediaPlayer.Open(new Uri(path, UriKind.Absolute));
                    _mediaPlayer.Play();
                    SongName_Lbl.Content = songName;
                    //setImage
                    var file = TagLib.File.Create(path);

                    if(file.Tag.Pictures.Any())
                    {
                        var bin = (byte[])(file.Tag.Pictures[0].Data.Data);

                        _songFileManager.SetImageSource(SongAlbum_Image, bin);
                    }
                    else
                    {
                        _songFileManager.SetImageSource(SongAlbum_Image, 
                            System.IO.Path.Combine(_baseImagePath, "NoImage.png"));
                    }

                    _songFileManager
                        .SetImageSource(PlayPauseBtnState_Image, System.IO.Path.Combine(_baseImagePath, "Pause.png"));
                }
                else if (Songs_ListBox.SelectedIndex != -1)
                    throw new ArgumentNullException("Wrong song name");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PlayPause_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string imageName = System.IO.Path.GetFileName(
                    (PlayPauseBtnState_Image.Source as BitmapImage).UriSource.ToString());

                if (imageName == "Pause.png")
                {
                    _mediaPlayer.Pause();
                    _songFileManager
                        .SetImageSource(PlayPauseBtnState_Image, System.IO.Path.Combine(_baseImagePath, "Play.png"));
                    PlayPause_Btn.ToolTip = "Play";
                }
                else if (imageName == "Play.png")
                {
                    _mediaPlayer.Play();
                    _songFileManager
                        .SetImageSource(PlayPauseBtnState_Image, System.IO.Path.Combine(_baseImagePath, "Pause.png"));
                    PlayPause_Btn.ToolTip = "Pause";
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        //использовать на историю прослушиваний вместо перемотки на единицу в списке
        private void Prev_Btn_Click(object sender, RoutedEventArgs e)
        {
            if(Songs_ListBox.SelectedIndex != -1)
            {
                if ((_mediaPlayer.Position.TotalSeconds/_mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds) <= 0.05)
                {
                    Songs_ListBox.SelectedIndex = Songs_ListBox.SelectedIndex > 0 ? (Songs_ListBox.SelectedIndex - 1) :
                        (Songs_ListBox.Items.Count - 1);
                }
                else
                    _mediaPlayer.Position = TimeSpan.Zero;
            }
        }

        private void Repeat_Btn_Click(object sender, RoutedEventArgs e)
        {
            isRepeatMode = !isRepeatMode;

            _songFileManager.SetImageSource(RepeatMode_Image, System.IO.Path.Combine(_baseImagePath,
                isRepeatMode ? "RepeatGreen.png" : "Repeat.png"));

            Repeat_Btn.ToolTip = isRepeatMode ? "Disable Repeat Mode" : "Enable Repeat Mode";
        }

        private void Mix_Btn_Click(object sender, RoutedEventArgs e)
        {
            isMixedMode = !isMixedMode;
            if(isMixedMode)
            {
                int songsCount = Songs_ListBox.Items.Count;
                int ignoreIndex = Songs_ListBox.SelectedIndex;
                Random rand = new Random();
                mixedCollection = new Dictionary<int, int>();
                for (int i = 0; i < songsCount; i++)
                {
                    if(i != ignoreIndex)
                        mixedCollection.Add(i,i);
                }
            }
            _songFileManager.SetImageSource(MixMode_Image, System.IO.Path.Combine(_baseImagePath,
                isMixedMode ? "MixGreen.png" : "Mix.png"));

            Mix_Btn.ToolTip = isMixedMode ? "Disable Mix" : "Mix";
        }

        private void Next_Btn_Click(object sender, RoutedEventArgs e)
        {
            if(Songs_ListBox.SelectedIndex != -1)
            {
                LoadNextSong();
            }
        }

        private void LoadNextSong()
        {
            if(isRepeatMode)
            {
                _mediaPlayer.Open(_mediaPlayer.Source);
            }
            else if(isMixedMode)
            {
                int songIndex = -1;
                Random rand = new Random();
                while(!mixedCollection.ContainsKey(songIndex) || songIndex == Songs_ListBox.SelectedIndex)
                {
                    songIndex = rand.Next(0, mixedCollection.Keys.Max() + 1);
                }
                Songs_ListBox.SelectedIndex = songIndex;
                history.Add(Songs_ListBox.SelectedItem as string);
            }
            else
            {
                history.Add(Songs_ListBox.SelectedItem as string);
                Songs_ListBox.SelectedIndex = Songs_ListBox.SelectedIndex == (Songs_ListBox.Items.Count - 1) ? 0 :
                    (Songs_ListBox.SelectedIndex + 1);
            }
        }


        private void DisableSongRelatedElements()
        {
            this.PlayPause_Btn.IsEnabled = false;
            this.Next_Btn.IsEnabled = false;
            this.Prev_Btn.IsEnabled = false;
            this.Mix_Btn.IsEnabled = false;
            this.Repeat_Btn.IsEnabled = false;
            this.SongProgress_PgBar.IsEnabled = false;
            this.Volume_Btn.IsEnabled = false;
            this.Volume_Slider.IsEnabled = false;
        }

        private void EnableSongRelatedElements()
        {
            this.PlayPause_Btn.IsEnabled = true;
            this.Next_Btn.IsEnabled = true;
            this.Prev_Btn.IsEnabled = true;
            this.Mix_Btn.IsEnabled = true;
            this.Repeat_Btn.IsEnabled = true;
            this.SongProgress_PgBar.IsEnabled = true;
            this.Volume_Btn.IsEnabled = true;
            this.Volume_Slider.IsEnabled = true;
        }


        private void Volume_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = Volume_Slider.Value / 4;
            if (Volume_Slider.Value == 0)
                _songFileManager.SetImageSource(Volume_Image, System.IO.Path.Combine(_baseImagePath, "VolumeMuted.png"));
            else
                _songFileManager.SetImageSource(Volume_Image, System.IO.Path.Combine(_baseImagePath, "Volume.png"));

        }

        private void SongProgress_Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mediaPlayer.Position = new TimeSpan(0, 0, (int)SongProgress_PgBar.Value);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchSongs_TxtBox.Text.Length != 0)
            {

                Regex songNameRegex = new Regex("^" + SearchSongs_TxtBox.Text.ToLower());
                var matchedSongs = songNames.Where(songName => songNameRegex.IsMatch(songName.ToLower()));
                Songs_ListBox.ItemsSource = matchedSongs;
            }
            else
                Songs_ListBox.ItemsSource = songNames;
        }   

        private void SongProgress_PgBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var rootVisual = Application.Current.MainWindow;
            Point relativePoint = SongProgress_PgBar.TransformToAncestor(rootVisual)
                .Transform(new Point(0, 0));
            
            double pgStartX = relativePoint.X, pgEndX = relativePoint.X + SongProgress_PgBar.ActualWidth;
            double cordsPerValue = (pgEndX - pgStartX) / (SongProgress_PgBar.Maximum - SongProgress_PgBar.Minimum);
            Point mouseRel = Mouse.GetPosition(this);

            double pgBarValue = (mouseRel.X - pgStartX) / cordsPerValue;
            SongProgress_PgBar.Value = pgBarValue;

            int newSeconds = (int)Math.Ceiling(pgBarValue);

            _mediaPlayer.Position = new TimeSpan(0, 0,newSeconds);
        }

        private void DeleteSong_Btn_Click(object sender, RoutedEventArgs e)
        {
            if(Songs_ListBox.SelectedIndex != -1)
            {
                _mediaPlayer.Pause();
                string songName = Songs_ListBox.SelectedItem as string;
                _songFileManager.RemoveSongEntry(songName);
                if (mixedCollection.ContainsKey(Songs_ListBox.SelectedIndex))
                    mixedCollection.Remove(Songs_ListBox.SelectedIndex);
                Songs_ListBox.SelectedIndex = -1;
                songNames.Remove(songName);

            }
        }
    }
}
