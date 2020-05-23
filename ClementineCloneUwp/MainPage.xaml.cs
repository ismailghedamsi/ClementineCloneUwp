﻿
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ClementineCloneUwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {

        private ObservableCollection<Song> PlaylistsongsMetaData;
        private ObservableCollection<Song> MusicLibrarySongsMetaData;
        private FolderPicker picker;
        private ObservableCollection<StorageFile> musicLibrary;
        private ObservableCollection<StorageFile> playlistTracks;
        private StorageFolder folder = KnownFolders.MusicLibrary;
        private MediaPlayer player;
        private static int currentPlayingSongMusicLibraryIndex;
        private static int currentPlayingSongPlaylistIndex;
        private Timer timer;
        private PlayingMode playingMode;




        public MainPage()
        {
            this.InitializeComponent();
            musicLibrary = new ObservableCollection<StorageFile>();
            PlaylistsongsMetaData = new ObservableCollection<Song>();
            MusicLibrarySongsMetaData = new ObservableCollection<Song>();
            playlistTracks = new ObservableCollection<StorageFile>();
            player = new MediaPlayer();
            player.MediaEnded += PlayNewSong_MediaEnded;
            volumeSlider.Value = player.Volume * 100;
            currentPlayingSongMusicLibraryIndex = 0;
            currentPlayingSongPlaylistIndex = 0;
            playingMode = PlayingMode.PLAYLIST;


        }

        private  void OpenCloseSplitView_Click(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private async void Continue_playing(object sender,RoutedEventArgs e)
        {
            UpdateTimelineSlider();
            await MusicPlayerController.PlayAsync(player);
        }


        private async void PlaySongFromGrid_DoubleClick(object sender, DoubleTappedRoutedEventArgs ev)
        {

            string paths = ((Song)dataGrid.SelectedItem).Path;
            StorageFile file = await StorageFile.GetFileFromPathAsync(paths);
            player.Dispose();
            player = new MediaPlayer();
            player.MediaEnded += PlayNewSong_MediaEnded;
       

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High,
             () =>
             {
                     player.SetFileSource(file);
                     player.Play();
             }
             );

            if(playingMode == PlayingMode.PLAYLIST)
            {
                currentPlayingSongPlaylistIndex = dataGrid.SelectedIndex;
            }
            else
            {
                currentPlayingSongMusicLibraryIndex = dataGrid.SelectedIndex;
            }
        
            seekPositionSlider.Value = 0;
            seekPositionSlider.ManipulationCompleted += SeekPositionSlider_ManipulationCompleted;
            UpdateTimelineSlider();
        }

        private void UpdateTimelineSlider()
        {
        
            timer = new System.Threading.Timer(async (e) =>
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {

                       double increaseRate = 100.0 / player.PlaybackSession.NaturalDuration.TotalSeconds;
                       if (player != null && !Double.IsInfinity(increaseRate))
                       {
                           seekPositionSlider.Value += (99.0 / player.PlaybackSession.NaturalDuration.TotalSeconds);
                       }
                       else
                       {
                           increaseRate = 1;
                       }

                   }
                 );
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }



        private void SeekPositionSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var seekPosition = seekPositionSlider.Value / 100;
            var playFrom = player.PlaybackSession.NaturalDuration * seekPosition;
            player.PlaybackSession.Position = playFrom;
         
        }


        private void dataGrid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "drop a sound";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;

        }

 

        private async void dataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Any())
                {
                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    for (int i = 0; i < items.Count; i++)
                    {
                        var storageFile = items[i] as StorageFile;
                        var contentType = storageFile.ContentType;
                        StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                        MusicProperties metaData = await newFile.Properties.GetMusicPropertiesAsync();
                        playlistTracks.Add(newFile);
                        PlaylistsongsMetaData.Add(new Song(metaData.Title, metaData.Artist, metaData.Album, AudioFileRetriever.FormatTrackDuration(metaData.Duration.TotalMinutes), metaData.Genre.Count == 0 ? "" : metaData.Genre[0], newFile.Path));
                    }
                    
                   
                    dataGrid.ItemsSource = null;
                    dataGrid.Columns.Clear();
                    dataGrid.ItemsSource = PlaylistsongsMetaData;


                }

            }
        }

        private async void PlayNewSong_MediaEnded(MediaPlayer sender, object args)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            bool isPlayingPlaylistTrack = playingMode == PlayingMode.PLAYLIST;
            Console.WriteLine("a new song with be played");
            player.Dispose();
            player = new MediaPlayer();

            player.MediaEnded += PlayNewSong_MediaEnded;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            if (isPlayingPlaylistTrack)
            {
                currentPlayingSongPlaylistIndex++;
                SetCurrentPlayingSong(playlistTracks, currentPlayingSongPlaylistIndex);
            }
            else
            {
                currentPlayingSongMusicLibraryIndex++;
                SetCurrentPlayingSong(musicLibrary,currentPlayingSongMusicLibraryIndex);
            }
            
          

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                dataGrid.SelectedIndex = isPlayingPlaylistTrack ? currentPlayingSongPlaylistIndex : currentPlayingSongMusicLibraryIndex;
                seekPositionSlider.Value = 0;
              
          
            }
            );

            if (player != null)
            {
                player.Play();
            }
            UpdateTimelineSlider();

        }

        private void SetCurrentPlayingSong(ObservableCollection<StorageFile> tracks,int currentPlayingSong)
        {
            if (tracks.Count > currentPlayingSong)
            {
                player.SetFileSource(tracks[currentPlayingSong]);
            }
        }

        public void GenerateColumnHeaderManually()
        {

            DataGridTextColumn idCol = new DataGridTextColumn();
            idCol.Header = "Id";

            DataGridTextColumn titleCol = new DataGridTextColumn();
            titleCol.Header = "Title";

            DataGridTextColumn artistCol = new DataGridTextColumn();
            artistCol.Header = "Artist";

            DataGridTextColumn albumCol = new DataGridTextColumn();
            albumCol.Header = "Album";

            DataGridTextColumn durationCol = new DataGridTextColumn();
            durationCol.Header = "Duration";

            DataGridTextColumn genreCol = new DataGridTextColumn();
            durationCol.Header = "Genre";

            DataGridTextColumn pathCol = new DataGridTextColumn();
            durationCol.Header = "Path";

            dataGrid.Columns.Add(idCol);
            dataGrid.Columns.Add(titleCol);
            dataGrid.Columns.Add(artistCol);
            dataGrid.Columns.Add(albumCol);
            dataGrid.Columns.Add(durationCol);
            dataGrid.Columns.Add(genreCol);
            dataGrid.Columns.Add(pathCol);
        }

        private  void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateColumnHeaderManually();
        }

        private void DG1_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {

            e.Column.Visibility = Visibility.Visible;
        }

        private void ButtonPlaylist_Click(object sender,RoutedEventArgs e)
        {
            playingMode = PlayingMode.PLAYLIST;
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();
            //GenerateColumnHeaderManually();
            dataGrid.ItemsSource = PlaylistsongsMetaData;
            dataGrid.SelectedIndex = currentPlayingSongPlaylistIndex;
        }

        private void Button_Click_Stop(object sender, RoutedEventArgs e)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            MusicPlayerController.PausePlayer(player);
        }

        private  async void Button_Click_Library(object sender, RoutedEventArgs e)
        {
            playingMode = PlayingMode.MUSIC_LIBRARY;
            await AudioFileRetriever.RetreiveFilesInFolders(musicLibrary, folder);
            await AudioFileRetriever.RetrieveSongMetadata(musicLibrary, MusicLibrarySongsMetaData);
            if(MusicLibrarySongsMetaData.Count == 0 || dataGrid.ItemsSource == null)
            {
                dataGrid.Columns.Clear();
            }
            dataGrid.ItemsSource = MusicLibrarySongsMetaData;
            dataGrid.SelectedIndex = currentPlayingSongMusicLibraryIndex;
        }

        private void volumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            player.Volume = volumeSlider.Value / 100;
            player.MediaEnded += PlayNewSong_MediaEnded;
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<StorageFile> folderFiles = new ObservableCollection<StorageFile>();
            ObservableCollection<Song> songs = new ObservableCollection<Song>();
            picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            await AudioFileRetriever.RetreiveFilesInFolders(playlistTracks, folder);
            await AudioFileRetriever.RetrieveSongMetadata(playlistTracks, PlaylistsongsMetaData);
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();
            dataGrid.ItemsSource = PlaylistsongsMetaData;
        }


        private void PlayNextSongButton_Click(object sender, RoutedEventArgs e)
        {

            player.Dispose();
            player = new MediaPlayer();
            player.MediaEnded += PlayNewSong_MediaEnded;
        
            if(++currentPlayingSongPlaylistIndex< playlistTracks.Count && playingMode == PlayingMode.PLAYLIST)
            {
                player.SetFileSource(playlistTracks[currentPlayingSongPlaylistIndex]);
                dataGrid.SelectedIndex = currentPlayingSongPlaylistIndex;
            }
            else if(++currentPlayingSongMusicLibraryIndex < musicLibrary.Count)
            {
                player.SetFileSource(musicLibrary[currentPlayingSongMusicLibraryIndex]);
                dataGrid.SelectedIndex = currentPlayingSongMusicLibraryIndex;
            }
         
           
            player.Play();
        }

        private void splitView_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {

        }
    }
}
