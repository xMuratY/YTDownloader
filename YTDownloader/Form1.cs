using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;

namespace YTDownloader
{
    public partial class Form1 : Form
    {
        private static string playListIdentifier = "list=";
        private static YoutubeClient youtube = new YoutubeClient();
        private static List<Tuple<string, string>> mDownloadList = new List<Tuple<string, string>>();
        private static Queue<string> downloader_queue = new Queue<string>();

        private async Task GetList(string listID)
        {
            var playlist = await youtube.Playlists.GetAsync(listID);
            var title = playlist.Title;

            await foreach (var video in youtube.Playlists.GetVideosAsync(playlist.Id))
            {
                var videoTitle = video.Title;

                checkedListBox1.Items.Add($"{videoTitle}");
                mDownloadList.Add(new Tuple<string, string>(videoTitle, video.Id));
            }
        }
        private async Task GetVideo( string videoID )
        {
            var video = await youtube.Videos.GetAsync(videoID);
            checkedListBox1.Items.Add($"{video.Title}");
            mDownloadList.Add(new Tuple<string, string>(video.Title, video.Id));
        }

        private bool DownloadAndSave( string title )
        {
            try
            {
                var videoID = mDownloadList.Find(x => x.Item1 == title)?.Item2;
                if (String.IsNullOrEmpty(videoID)) return false;

                var streamManifest = youtube.Videos.Streams.GetManifestAsync(videoID).GetAwaiter().GetResult();
                var streamInfo = streamManifest.GetAudioOnly().FirstOrDefault();
                var kekwaitstream = youtube.Videos.Streams.GetAsync(streamInfo).GetAwaiter().GetResult();

                var filename = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));

                using (var reader = new AudioFileReader(streamInfo.Url))
                using (var writer = new LameMP3FileWriter($"{AppDomain.CurrentDomain.BaseDirectory}Output\\{filename}.mp3", reader.WaveFormat, 128))
                    reader.CopyTo(writer);

                return true;
            }
            catch
            {
                Thread.Sleep(300);
                return DownloadAndSave(title);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string yturi = textBox1.Text;
            var isPlayList = yturi.Contains(playListIdentifier);

            if (isPlayList)
                await GetList(yturi);
            else
                await GetVideo(yturi);
        }
        
        private async void button2_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Output")) Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}Output");

            foreach (var video_title in checkedListBox1.CheckedItems)
                downloader_queue.Enqueue(video_title.ToString());

            //10 Thread Downloader
            for (int i = 0; i < 10; i++)
            {
                new Thread(new ThreadStart(() => {
                    while (downloader_queue.Count > 0)
                    {
                        DownloadAndSave(downloader_queue.Dequeue());
                    }
                })).Start();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < checkedListBox1.Items.Count; i++)
                checkedListBox1.SetItemChecked(i, true);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            for (; checkedListBox1.CheckedIndices.Count > 0;)
                checkedListBox1.SetItemChecked(checkedListBox1.CheckedIndices[0], false);
        }
    }
}
