using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using leetreveil.AutoUpdate.Framework;
using Ninject;
using ZuneSocialTagger.Core.ZuneDatabase;
using ZuneSocialTagger.GUIV2.Models;
using ZuneSocialTagger.GUIV2.Properties;
using ZuneSocialTagger.GUIV2.Views;

namespace ZuneSocialTagger.GUIV2.ViewModels
{
    public class ApplicationViewModel : ViewModelBase
    {
        #region DbLoadResult enum

        public enum DbLoadResult
        {
            Success,
            Failed
        }

        #endregion

        private readonly IKernel _container;
        private readonly IZuneWizardModel _model;
        private readonly CachedZuneDatabaseReader _cache;
        private readonly IZuneDatabaseReader _dbReader;
        private ViewModelBase _currentPage;
        private bool _updateAvailable;
        private ErrorMessage _dbErrorMessage;

        public ApplicationViewModel(IZuneWizardModel model, CachedZuneDatabaseReader cache,IZuneDatabaseReader dbReader, IKernel container)
        {
            _model = model;
            _cache = cache;
            _dbReader = dbReader;
            _container = container;

            CheckForUpdates();
            SetupCommandBindings();

            //register for changes to the current view model so we can switch between views
            Messenger.Default.Register<Type>(this, SwitchToView);

            //register for database switch messages
            Messenger.Default.Register<string>(this, SwitchToDatabase);

            //register for error messages to be displayed
            Messenger.Default.Register<ErrorMessage>(this, DisplayErrorMessage);

            var webAlbumListViewModel = _container.Get<IFirstPage>();
            webAlbumListViewModel.FinishedLoading += FirstViewHasFinishedLoading;
            this.CurrentPage = (ViewModelBase)webAlbumListViewModel;
            SetupView();
        }

        void FirstViewHasFinishedLoading()
        {
            //We can only show error messages on the ApplicationView once all other views have loaded
            if (_dbErrorMessage != null)
                this.DisplayErrorMessage(_dbErrorMessage);
        }

        public RelayCommand UpdateCommand { get; private set; }
        public RelayCommand ShowAboutSettingsCommand { get; private set; }
        public InlineZuneMessageViewModel InlineZuneMessage { get { return _container.Get<InlineZuneMessageViewModel>(); } }

        public bool UpdateAvailable
        {
            get { return _updateAvailable; }
            set
            {
                _updateAvailable = value;
                RaisePropertyChanged("UpdateAvailable");
            }
        }

        public ViewModelBase CurrentPage
        {
            get { return _currentPage; }
            private set
            {
                _currentPage = value;
                RaisePropertyChanged("CurrentPage");
            }
        }

        private void SetupView()
        {
            if (this.CurrentPage.GetType() == typeof (SelectAudioFilesViewModel))
            {
                _container.Rebind<IFirstPage>().To<SelectAudioFilesViewModel>().InSingletonScope();
                Settings.Default.FirstViewToLoad = FirstViews.SelectAudioFilesViewModel;
            }

            //anytime the view gets switched we want to rebind and load the database
            if (this.CurrentPage.GetType() == typeof(WebAlbumListViewModel))
            {
                //_container.Rebind<IFirstPage>().To<WebAlbumListViewModel>().InSingletonScope();

                if (_model.AlbumsFromDatabase.Count == 0)
                {
                    DbLoadResult result = InitializeDatabase();

                    //if we cannot load the database then switch to the other view
                    if (result == DbLoadResult.Failed)
                    {
                        var selectAudioFilesViewModel = _container.Get<SelectAudioFilesViewModel>();
                        selectAudioFilesViewModel.CanSwitchToNewMode = false;
                        selectAudioFilesViewModel.FinishedLoading += FirstViewHasFinishedLoading;
                        this.CurrentPage = selectAudioFilesViewModel;
                        Settings.Default.FirstViewToLoad = FirstViews.SelectAudioFilesViewModel;
                    }
                    else
                    {
                        var webAlbumListViewModel = _container.Get<WebAlbumListViewModel>();
                        webAlbumListViewModel.FinishedLoading += FirstViewHasFinishedLoading;
                        this.CurrentPage = webAlbumListViewModel;
                        Settings.Default.FirstViewToLoad = FirstViews.WebAlbumListViewModel;
                        ReadDatabase();
                        CheckIfZuneSoftwareIsRunning();
                        WatchForProcessStartAndStop();
                    }
                }
            }
        }

        private void CheckIfZuneSoftwareIsRunning()
        {
            if (Process.GetProcessesByName("Zune").Length == 0)
                _dbErrorMessage = new ErrorMessage(ErrorMode.Warning,
                    "Any albums you link / delink will not show their changes until the zune software is running.");
        }

        private void WatchForProcessStartAndStop()
        {
            var processWatcher = new ProcessWatcher("Zune.exe");
            processWatcher.ProcessEnded += processWatcher_ProcessEnded;
            processWatcher.WatchForProcessEnd();
        }

        void processWatcher_ProcessEnded(string obj)
        {
            DisplayErrorMessage(new ErrorMessage(ErrorMode.Warning,
                 "Any albums you link / delink will not show their changes until the zune software is running.")); 
        }

        private void DisplayErrorMessage(ErrorMessage message)
        {
            UIDispatcher.GetDispatcher().Invoke(new Action(() => InlineZuneMessage.ShowMessage(message.ErrorMode, message.Message)));
        }

        private void SwitchToDatabase(string message)
        {
            this.CurrentPage = _container.Get<WebAlbumListViewModel>();
            SetupView();
        }

        private void SwitchToView(Type viewType)
        {
            CurrentPage = (ViewModelBase) _container.Get(viewType);
        }

        private void SetupCommandBindings()
        {
            ShowAboutSettingsCommand = new RelayCommand(ShowAboutSettings);
            UpdateCommand = new RelayCommand(ShowUpdate);
        }

        private DbLoadResult InitializeDatabase()
        {
            try
            {
                if (_dbReader.CanInitialize)
                {
                    //since the adapter is initally set to the cache this should always be true if the cache exists
                    bool initalized = _dbReader.Initialize();

                    if (!initalized)
                    {
                        _dbErrorMessage = new ErrorMessage(ErrorMode.Error, "Error loading zune database");
                        return DbLoadResult.Failed;
                    }

                    return DbLoadResult.Success;
                }

                _dbErrorMessage = new ErrorMessage(ErrorMode.Error, "Failed to determine if the zune database can be loaded");
                return DbLoadResult.Failed;
            }
            catch (NotSupportedException e)
            {
                //if the version of the dll is not the version that this software supports
                //we should display an error but still attempt to load the database because it might work
                _dbErrorMessage = new ErrorMessage(ErrorMode.Error, e.Message);
                return DbLoadResult.Failed;
            }
            catch (FileNotFoundException e)
            {
                //if ZuneDBApi.dll cannot be found this will be thrown
                _dbErrorMessage = new ErrorMessage(ErrorMode.Error, e.Message);
                return DbLoadResult.Failed;
            }
            catch(Exception e)
            {
                _dbErrorMessage = new ErrorMessage(ErrorMode.Error,e.Message);
                return DbLoadResult.Failed;
            }
        }

        private void ReadDatabase()
        {
            //if we can read the cache then skip the database and just load that
            if (_cache.CanInitialize && _cache.Initialize())
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    foreach (AlbumDetails newAlbum in _cache.ReadAlbums())
                    {
                        AlbumDetails album = newAlbum;
                        UIDispatcher.GetDispatcher().Invoke(new Action(() => 
                            _model.AlbumsFromDatabase.Add(new AlbumDetailsViewModel(album))));
                    }
                });
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    foreach (Album newAlbum in _dbReader.ReadAlbums())
                    {
                        Album album = newAlbum;
                        UIDispatcher.GetDispatcher().Invoke(new Action(() => 
                            _model.AlbumsFromDatabase.Add(new AlbumDetailsViewModel(SharedMethods.ToAlbumDetails(album)))));
                    }
                });
            }
        }

        public void ShuttingDown()
        {
            WriteCacheToFile();
            Settings.Default.Save();
        }

        private void WriteCacheToFile()
        {
            List<AlbumDetails> albums = (from album in _model.AlbumsFromDatabase
                                         select new AlbumDetails
                                                    {
                                                        LinkStatus = album.LinkStatus,
                                                        WebAlbumMetaData = album.WebAlbumMetaData,
                                                        ZuneAlbumMetaData = album.ZuneAlbumMetaData,
                                                    }).ToList();

            if (albums.Count > 0)
            {
                try
                {
                    var xSer = new XmlSerializer(albums.GetType());

                    using (var fs = new FileStream(Path.Combine(Settings.Default.AppDataFolder, @"zunesoccache.xml"), FileMode.Create))
                        xSer.Serialize(fs, albums);
                }
                catch{}
            }
        }

        public void ShowUpdate()
        {
            new UpdateView().Show();
        }

        public void ShowAboutSettings()
        {
            new AboutView().Show();
        }

        private void CheckForUpdates()
        {
            string updaterPath = Path.Combine(Settings.Default.AppDataFolder,Settings.Default.UpdateExeName);

            if (Settings.Default.CheckForUpdates)
            {
                try
                {
                    //do update checking stuff here
                    UpdateManager updateManager = UpdateManager.Instance;

                    updateManager.UpdateExePath = updaterPath;
                    updateManager.AppFeedUrl = Settings.Default.UpdateFeedUrl;
                    updateManager.UpdateExe = Resources.socialtaggerupdater;

                    //always clean up at startup because we cant do it at the end
                    updateManager.CleanUp();

                    ThreadPool.QueueUserWorkItem(state =>
                                                     {
                                                         try
                                                         {
                                                             if (updateManager.CheckForUpdate())
                                                                 UpdateAvailable = true;
                                                         }
                                                         catch
                                                         {
                                                         }
                                                     });
                }
                catch
                {
                    //TODO: log that we could not check for updates
                }
            }
        }
    }
}