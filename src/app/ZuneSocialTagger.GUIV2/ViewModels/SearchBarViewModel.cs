using System;
using System.Collections.Generic;
using System.Threading;
using ZuneSocialTagger.Core;
using ZuneSocialTagger.Core.ZuneWebsite;
using ZuneSocialTagger.GUIV2.Models;

namespace ZuneSocialTagger.GUIV2.ViewModels
{
    public class SearchBarViewModel : NotifyPropertyChangedImpl
    {
        private bool _canSearch;
        private bool _isSearching;
        private string _searchText;

        public SearchBarViewModel()
        {
            SearchResults = new AsyncObservableCollection<Album>();
        }

        public AsyncObservableCollection<Album> SearchResults { get; set; }

        public event Action StartedSearching = delegate { };

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                IsAbleToSearch = !String.IsNullOrEmpty(_searchText);
                NotifyOfPropertyChange(() => SearchText);
            }
        }

        public bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                _isSearching = value;
                IsAbleToSearch = !value;
                NotifyOfPropertyChange(() => IsSearching);
            }
        }

        public bool IsAbleToSearch
        {
            get { return _canSearch; }
            set
            {
                _canSearch = value;
                NotifyOfPropertyChange(() => IsAbleToSearch);
            }
        }

        /// <summary>
        /// Way to search without having to click on the magnifying glass
        /// </summary>
        public void Search()
        {
            StartedSearching.Invoke();
            SearchFor(this.SearchText);
        }

        private void SearchFor(string searchString)
        {
            SearchResults.Clear();
            IsSearching = true;

            ThreadPool.QueueUserWorkItem(_ =>
                 {
                     IEnumerable<Album> results = AlbumSearch.SearchFor(searchString);

                     foreach (Album result in results)
                         SearchResults.Add(result);

                     IsSearching = false;
                 });
        }
    }
}