﻿using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ZuneSocialTagger.GUI.Converters;
using ZuneSocialTagger.GUI.ViewModels;

namespace ZuneSocialTagger.GUI.Controls
{
    /// <summary>
    /// Interaction logic for InlineZuneMessage.xaml
    /// </summary>
    public partial class InlineZuneMessage : UserControl
    {
        private readonly Timer _timer;

        public InlineZuneMessage()
        {
            InitializeComponent();

            _timer = new Timer();
            _timer.Elapsed += delegate 
            {
                this.Dispatcher.Invoke(new Action(() => this.SetValue(ShowMessageProperty, false)));
            };

            //set default image
            var imageSource =
                (string) new ErrorModeToImageSourceConverter().Convert(ErrorMode.Error, typeof (ErrorMode), null, null);

            this.imgError.Source = new BitmapImage(new Uri(imageSource, UriKind.RelativeOrAbsolute));
        }

        #region dp's

        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register("MessageText", typeof(string), typeof(InlineZuneMessage),
                                        new PropertyMetadata(MessageTextChanged));


        public static readonly DependencyProperty ErrorModeProperty =
            DependencyProperty.Register("ErrorMode", typeof (ErrorMode), typeof (InlineZuneMessage),
                                        new PropertyMetadata(ErrorModeChanged));

        public static readonly DependencyProperty ShowMessageProperty =
            DependencyProperty.Register("ShowMessage", typeof(bool), typeof(InlineZuneMessage),
                                        new PropertyMetadata(ShowMessageChanged));

        #endregion

        public ErrorMode ErrorMode
        {
            get { return (ErrorMode) GetValue(ErrorModeProperty); }
            set { SetValue(ErrorModeProperty, value); }
        }

        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        public bool ShowMessage
        {
            get { return (bool)GetValue(ShowMessageProperty); }
            set { SetValue(ShowMessageProperty, value); }
        }

        private static void MessageTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (InlineZuneMessage) d;
            view.tbMessage.Text = (string) e.NewValue;
        }

        private static void ErrorModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (InlineZuneMessage)d;

            var imageSource =
                (string)new ErrorModeToImageSourceConverter().Convert(e.NewValue, typeof(ErrorMode), null, null);

            view.imgError.Source = new BitmapImage(new Uri(imageSource, UriKind.RelativeOrAbsolute));
        }

        private static void ShowMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (InlineZuneMessage)d;

            bool result = (bool) e.NewValue;
            view.Visibility = result ? Visibility.Visible : Visibility.Collapsed;

            if (result)
            {
                view._timer.Interval = 20000;
                view._timer.Start();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.SetValue(ShowMessageProperty,false);
        }
    }
}