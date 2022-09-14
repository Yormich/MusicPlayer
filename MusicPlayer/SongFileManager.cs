using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace MusicPlayer
{
    internal class SongFileManager
    {
        private readonly string _addedSongsFilePath;
        private readonly string _listeningHistoryFilePath = "History.txt";

        public string AddedSongsPath
        { 
            get
            {
                return _addedSongsFilePath;
            }
        }

        public string ListeningHistoryPath
        {
            get
            {
                return _listeningHistoryFilePath;
            }
        }

        public SongFileManager(string addedSongsFilePath = "Songs.txt",string ListeningHistoryFilePath = "History.txt")
        {
            _addedSongsFilePath = addedSongsFilePath;
            _listeningHistoryFilePath = ListeningHistoryFilePath;
            if(!File.Exists(_addedSongsFilePath))
            {
                File.Create(_addedSongsFilePath);
            }
            if(!File.Exists(_listeningHistoryFilePath))
            {
                File.Create(_listeningHistoryFilePath);
            }
        }
        public void SetImageSource(Image image, string ImagePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(ImagePath, UriKind.Relative);
            bitmapImage.EndInit();
            image.Source = bitmapImage;
        }

        public void SetImageSource(Image image, byte[] picture)
        {
            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream ms = new MemoryStream(picture))
            {
                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = null;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
            
            }
            image.Source = bitmapImage;
        }

        public Dictionary<string, string> GetAddedSongsPaths()
        {
            Dictionary<string, string> namePath = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(_addedSongsFilePath))
            {
                string path;

                while ((path = sr.ReadLine()) != null)
                {
                    namePath.Add(Path.GetFileName(path), path);
                }
            }
            return namePath;
        }
    }
}
