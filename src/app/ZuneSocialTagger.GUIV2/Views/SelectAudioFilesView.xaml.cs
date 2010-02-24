﻿using System.Windows.Controls;
using ZuneSocialTagger.GUIV2.Models;
using ZuneSocialTagger.GUIV2.ViewModels;

namespace ZuneSocialTagger.GUIV2.Views
{
    /// <summary>
    /// Interaction logic for SelectAudioFilesView.xaml
    /// </summary>
    public partial class SelectAudioFilesView : UserControl
    {
        private SelectAudioFilesViewModel _model;
        public SelectAudioFilesView()
        {
            this.InitializeComponent();
			
            // Insert code required on object creation below this point.

            this.DataContextChanged += delegate { _model = (SelectAudioFilesViewModel) this.DataContext; };
        }

        private void LinkAlbum_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _model.LinkAlbum((Album) lvAlbums.SelectedItem);
        }

        private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _model.RefreshAlbum((Album)lvAlbums.SelectedItem);
        }

        private void DelinkAlbum_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _model.DelinkAlbum((Album) lvAlbums.SelectedItem);
        }
    }
}