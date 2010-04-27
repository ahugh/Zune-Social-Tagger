using System;
using System.Collections.Specialized;
using System.Threading;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.WindowsAPICodePack.Taskbar;
using ZuneSocialTagger.GUI.Controls;
using ZuneSocialTagger.GUI.Models;
using System.Linq;
using ZuneSocialTagger.GUI.Properties;
using ZuneSocialTagger.Core.ZuneDatabase;

namespace ZuneSocialTagger.GUI.ViewModels
{
    public class WebAlbumListViewModel : ViewModelBaseExtended
    {
        private readonly IZuneDatabaseReader _dbReader;
        private bool _canShowScanAllButton;
        private int _loadingProgress;
        private string _scanAllText;
        private AlbumDownloaderWithProgressReporting _downloader;
        private bool _canShowProgressBar;
        private bool _canShowReloadButton;
        private readonly bool _isTaskbarSupported;
        private SortOrder _sortOrder;
        private ZuneObservableCollection<AlbumDetailsViewModel> _albums;

        public WebAlbumListViewModel(IZuneDatabaseReader dbReader)
        {
            _dbReader = dbReader;
            _dbReader.FinishedReadingAlbums += DbAdapterFinishedReadingAlbums;
            _dbReader.ProgressChanged += DbAdapterProgressChanged;
            _dbReader.StartedReadingAlbums += _dbAdapter_StartedReadingAlbums;

            this.ScanAllText = "SCAN ALL";
            _isTaskbarSupported = TaskbarManager.IsPlatformSupported;

            SetupCommandBindings();

            this.CanShowReloadButton = true;
            this.CanShowScanAllButton = true;

            this.SortOrder = Settings.Default.SortOrder;

            Messenger.Default.Register<string>(this, HandleMessages);
        }

        private void HandleMessages(string message)
        {
            if (message == "SORT")
            {
                SortData(Settings.Default.SortOrder);
            }
        }

        private void SetupCommandBindings()
        {
            this.LoadDatabaseCommand = new RelayCommand(RefreshDatabase);
            this.LoadFromZuneWebsiteCommand = new RelayCommand(LoadFromZuneWebsite);
            this.CancelDownloadingCommand = new RelayCommand(CancelDownloading);
            this.SwitchToClassicModeCommand = new RelayCommand(SwitchToClassicMode);
            this.SortCommand = new RelayCommand(Sort);
        }

        #region View Binding Properties

        public bool IsCurrentAlbumLinkable
        {
            get
            {
                return true;
            }
        }

        public int SelectedIndex { get; set; }

        public ZuneObservableCollection<AlbumDetailsViewModel> Albums
        {
            get { return _albums; }
            set
            {
                _albums = value;
                _albums.NeedsUpdating += UpdateLinkTotals;
                _albums.CollectionChanged += (sender, args) => {
                    if (args.Action == NotifyCollectionChangedAction.Add || 
                        args.Action ==  NotifyCollectionChangedAction.Remove)
                    {
                        UpdateLinkTotals();
                    }
                };
            }
        }

        public bool CanShowProgressBar
        {
            get { return _canShowProgressBar; }
            set
            {
                _canShowProgressBar = value;
                this.ScanAllText = CanShowProgressBar ? "STOP" : "SCAN ALL";
                RaisePropertyChanged(() => this.CanShowProgressBar);
            }
        }

        public bool CanShowReloadButton
        {
            get { return _canShowReloadButton; }
            set
            {
                _canShowReloadButton = value;
                RaisePropertyChanged(() => this.CanShowReloadButton);
            }
        }

        public SortOrder SortOrder
        {
            get { return _sortOrder; }
            set
            {
                _sortOrder = value; 
                RaisePropertyChanged(() => this.SortOrder);
            }
        }

        public bool CanShowScanAllButton
        {
            get { return _canShowScanAllButton; }
            set
            {
                _canShowScanAllButton = value;
                RaisePropertyChanged(() => this.CanShowScanAllButton);
            }
        }

        public int LoadingProgress
        {
            get { return _loadingProgress; }
            set
            {
                _loadingProgress = value;
                RaisePropertyChanged(() => this.LoadingProgress);
            }
        }

        public string ScanAllText
        {
            get { return _scanAllText; }
            set
            {
                _scanAllText = value;
                RaisePropertyChanged(() => this.ScanAllText);
            }
        }

        public int LinkedTotal
        {
            get { return this.Albums.Where(x => x.LinkStatus == LinkStatus.Linked).Count(); }
        }

        public int UnlinkedTotal
        {
            get
            {
                return this.Albums.Where(x => x.LinkStatus == LinkStatus.Unlinked
                                              || x.LinkStatus == LinkStatus.Unavailable).Count();
            }
        }

        public int AlbumOrArtistMismatchTotal
        {
            get { return this.Albums.Where(x => x.LinkStatus == LinkStatus.AlbumOrArtistMismatch).Count(); }
        }

        public RelayCommand LoadDatabaseCommand { get; private set; }
        public RelayCommand LoadFromZuneWebsiteCommand { get; private set; }
        public RelayCommand CancelDownloadingCommand { get; private set; }
        public RelayCommand SwitchToClassicModeCommand { get; private set; }
        public RelayCommand SortCommand { get; private set; }

        #endregion

        public void LoadFromZuneWebsite()
        {
            if (this.CanShowProgressBar)
                CancelDownloading();
            else
            {
                this.CanShowReloadButton = false;
                this.CanShowProgressBar = true;

                //check if we have already downloaded all the albums
                if (this.Albums.Where(x => x.LinkStatus != LinkStatus.Unlinked).
                    All(x => x.WebAlbumMetaData != null))
                {
                    foreach (var album in this.Albums.Where(album => album.LinkStatus != LinkStatus.Unlinked))
                    {
                        album.WebAlbumMetaData = null;
                        album.LinkStatus = LinkStatus.Unknown;
                    }
                }

                _downloader = new AlbumDownloaderWithProgressReporting(
                    this.Albums.Where(x => x.WebAlbumMetaData == null && x.LinkStatus != LinkStatus.Unlinked));

                _downloader.ProgressChanged += downloader_ProgressChanged;
                _downloader.FinishedDownloadingAlbums += downloader_FinishedDownloadingAlbums;
                _downloader.Start();
            }
        }

        public void CancelDownloading()
        {
            if (_downloader != null)
                _downloader.StopDownloading();
        }

        public void RefreshDatabase()
        {
            string msg = "Are you sure? All downloaded album details will be reset and the database will be reloaded.";

            ZuneMessageBox.Show(new ErrorMessage(ErrorMode.Warning, msg), () =>
                 {
                     this.SortOrder =SortOrder. NotSorted;
                     Messenger.Default.Send("SWITCHTODB");
                 });
        }

        public void SortData(SortOrder sortOrder)
        {
            Settings.Default.SortOrder = sortOrder;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                switch (sortOrder)
                {
                    case SortOrder.DateAdded:
                        this.Albums.SortDesc(x => x.ZuneAlbumMetaData.DateAdded);
                        break;
                    case SortOrder.Album:
                        this.Albums.Sort(x => x.ZuneAlbumMetaData.AlbumTitle);
                        break;
                    case SortOrder.Artist:
                        this.Albums.Sort(x => x.ZuneAlbumMetaData.AlbumArtist);
                        break;
                    case SortOrder.LinkStatus:
                        this.Albums.Sort(x => x.LinkStatus);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        public void SwitchToClassicMode()
        {
            Messenger.Default.Send(typeof(SelectAudioFilesViewModel));
        }

        private void _dbAdapter_StartedReadingAlbums()
        {
            this.CanShowReloadButton = false;
            this.CanShowProgressBar = true;
            this.CanShowScanAllButton = false;
        }

        private void DbAdapterProgressChanged(int arg1, int arg2)
        {
            ReportProgress(arg1, arg2);
        }

        private void DbAdapterFinishedReadingAlbums()
        {
            this.CanShowReloadButton = true;
            this.CanShowProgressBar = false;
            this.CanShowScanAllButton = true;
            ResetLoadingProgress();
            SortData(Settings.Default.SortOrder);
        }

        private void downloader_FinishedDownloadingAlbums()
        {
            this.CanShowProgressBar = false;
            this.CanShowReloadButton = true;
            ResetLoadingProgress();
        }

        private void downloader_ProgressChanged(int arg1, int arg2)
        {
            ReportProgress(arg1, arg2); //arg1 = current album ;arg2 = total albums
        }

        private void ResetLoadingProgress()
        {
            this.LoadingProgress = 0;

            if (_isTaskbarSupported)
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
        }

        private void ReportProgress(int current, int total)
        {
            this.LoadingProgress = current * 100 / total;

            if (_isTaskbarSupported)
                TaskbarManager.Instance.SetProgressValue(current, total);
        }

        private void Sort()
        {
            SortData(this.SortOrder);
        }

        private void UpdateLinkTotals()
        {
            this.RaisePropertyChanged(() => this.LinkedTotal);
            this.RaisePropertyChanged(() => this.UnlinkedTotal);
            this.RaisePropertyChanged(() => this.AlbumOrArtistMismatchTotal);
        }
    }
}