
using Microsoft.Toolkit.Uwp.UI.Controls;
using Syncfusion.Data.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
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

        private List<Song> playlistsongsMetaData;
        private List<Song> musicLibrarySongsMetaData;
        private FolderPicker picker;
        private List<StorageFile> musicLibrary;
        private List<StorageFile> playlistTracks;
        private StorageFolder folder = KnownFolders.MusicLibrary;
        private MediaPlayer player;
        private static int currentPlayingSongMusicLibraryIndex;
        private static int currentPlayingSongPlaylistIndex;
        private Timer timer;
        private PlayingMode playingMode;


        public MainPage()
        {
            this.InitializeComponent();
            musicLibrary = new List<StorageFile>();
            playlistsongsMetaData = new List<Song>();
            musicLibrarySongsMetaData = new List<Song>();
            playlistTracks = new List<StorageFile>();
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
            MusicPlayerController.ReinitiatePlayer(ref player);
            player.MediaEnded += PlayNewSong_MediaEnded;
            MusicPlayerController.SelectNewSource(player, file);
            await MusicPlayerController.PlayAsync(player);
            SetCurrentPlayingSongIndex();
            timelineSlider.Value = 0;
            timelineSlider.ManipulationCompleted += SeekPositionSlider_ManipulationCompleted;
            UpdateTimelineSlider();
        }

        private void SetCurrentPlayingSongIndex()
        {
            if (playingMode == PlayingMode.PLAYLIST)
            {
                currentPlayingSongPlaylistIndex = dataGrid.SelectedIndex;
            }
            else
            {
                currentPlayingSongMusicLibraryIndex = dataGrid.SelectedIndex;
            }
        }

        private void UpdateTimelineSlider()
        {
        
            timer = new Timer(async (e) =>
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                   () =>
                   {

                       double increaseRate = 100.0 / player.PlaybackSession.NaturalDuration.TotalSeconds;
                       if (player != null && !Double.IsInfinity(increaseRate))
                       {
                           timelineSlider.Value += (100.0 / player.PlaybackSession.NaturalDuration.TotalSeconds);
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
            var seekPosition = timelineSlider.Value / 100;
            var playFrom = player.PlaybackSession.NaturalDuration * seekPosition;
            player.PlaybackSession.Position = playFrom;
         
        }


        private void dataGrid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "drop a sound";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
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
                        StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                        MusicProperties metaData = await newFile.Properties.GetMusicPropertiesAsync();
                        playlistTracks.Add(newFile);
                        playlistsongsMetaData.Add(new Song(metaData.Title, metaData.Artist, metaData.Album, AudioFileRetriever.FormatTrackDuration(metaData.Duration.TotalMinutes), metaData.Genre.Count == 0 ? "" : metaData.Genre[0], newFile.Path));
                    }
                    SetNewGridItemSource(playlistsongsMetaData);
                }

            }
        }

        private void SetNewGridItemSource(List<Song> songsMetaData)
        {
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();
            dataGrid.ItemsSource = songsMetaData;
        }

        private async void PlayNewSong_MediaEnded(MediaPlayer sender, object args)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            bool isPlayingPlaylistTrack = playingMode == PlayingMode.PLAYLIST;
            MusicPlayerController.ReinitiatePlayer(ref player);
            player.MediaEnded += PlayNewSong_MediaEnded;
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
                timelineSlider.Value = 0;
            }
            );

            await MusicPlayerController.PlayAsync(player);
            UpdateTimelineSlider();

        }

        private void SetCurrentPlayingSong(List<StorageFile> tracks,int currentPlayingSong)
        {
            if (tracks.Count > currentPlayingSong)
            {
                MusicPlayerController.SelectNewSource(player, tracks[currentPlayingSong]);
            }
        }

     
        private void ButtonPlaylist_Click(object sender,RoutedEventArgs e)
        {
            playingMode = PlayingMode.PLAYLIST;
            SetNewGridItemSource(playlistsongsMetaData);
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
            await AudioFileRetriever.RetrieveSongMetadata(musicLibrary, musicLibrarySongsMetaData);
            SetNewGridItemSource(musicLibrarySongsMetaData);
            dataGrid.SelectedIndex = currentPlayingSongMusicLibraryIndex;
        }

        private void volumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            player.Volume = volumeSlider.Value / 100;
            player.MediaEnded += PlayNewSong_MediaEnded;
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            playlistTracks.Clear();
            playlistsongsMetaData.Clear();
            picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".mp3");
            var folder = await picker.PickSingleFolderAsync();
            await AudioFileRetriever.RetreiveFilesInFolders(playlistTracks, folder);
            await AudioFileRetriever.RetrieveSongMetadata(playlistTracks, playlistsongsMetaData);
            SetNewGridItemSource(playlistsongsMetaData);
        }

        [Obsolete]
        private void PlayNextSongButton_Click(object sender, RoutedEventArgs e)
        {

            MusicPlayerController.ReinitiatePlayer(ref player);
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
            timelineSlider.Value = 0;
            player.Play();
        }

        [Obsolete]
        private void PlayPreviousSongButton_Click(object sender, RoutedEventArgs e)
        {

            MusicPlayerController.ReinitiatePlayer(ref player);
            player.MediaEnded += PlayNewSong_MediaEnded;

            if (--currentPlayingSongPlaylistIndex > 0 && playingMode == PlayingMode.PLAYLIST)
            {
                player.SetFileSource(playlistTracks[currentPlayingSongPlaylistIndex]);
                dataGrid.SelectedIndex = currentPlayingSongPlaylistIndex;
           
            }
            else if (--currentPlayingSongMusicLibraryIndex > 0)
            {
                player.SetFileSource(musicLibrary[currentPlayingSongMusicLibraryIndex]);
                dataGrid.SelectedIndex = currentPlayingSongMusicLibraryIndex;
            }
            timelineSlider.Value = 0;
          
            player.Play();
        }

        private void DeleteRowKeyUp_Click(object sender, KeyRoutedEventArgs e)
        {
            if (VirtualKey.Delete == e.Key)
            {
               var selectedItems = dataGrid.SelectedItems;

                if (playingMode == PlayingMode.MUSIC_LIBRARY)
                {
                    RemoveSongFromSongToPlay(selectedItems,musicLibrary, musicLibrarySongsMetaData);
                    SetNewGridItemSource(musicLibrarySongsMetaData);
                }
                else
                {
                    RemoveSongFromSongToPlay(selectedItems, playlistTracks, playlistsongsMetaData);
                    SetNewGridItemSource(playlistsongsMetaData);
                }
               
            }
           
        }

        private void RemoveSongFromSongToPlay(IList selectedItems,List<StorageFile> musicList, List<Song> metaDataList)
        {
            for (int i = 0; i < selectedItems.Count; i++)
            {
                Song item = selectedItems[i] as Song;
                metaDataList.Remove(metaDataList.Where(elem2 => ((Song)item).Path == elem2.Path).First());
                musicList.Remove(musicList.Where(elem2 => (item).Path == elem2.Path).First());
            }
        }

    }
}
