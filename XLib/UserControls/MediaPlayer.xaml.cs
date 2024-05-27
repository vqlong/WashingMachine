﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XLib.UserControls
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>
    public partial class MediaPlayer : Window, INotifyPropertyChanged
    {
        public MediaPlayer()
        {
            InitializeComponent();

            var uri = new Uri("pack://application:,,,/PresentationFramework.Royale;component/themes/Royale.NormalColor.xaml");
            var dictionary = new ResourceDictionary { Source = uri };
            foreach (var key in dictionary.Keys)
            {
                //Lấy ra style cho scrollbar và thêm vào resource của app
                if (dictionary[key] is Style style && style.TargetType == typeof(ScrollBar))
                {
                    //style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                    Resources.Add(key, style);
                    break;
                }
            }

            if (Directory.Exists(txbDefaultPath.Text))
            {
                var files = Directory.EnumerateFiles(txbDefaultPath.Text, "*.*", SearchOption.AllDirectories)
                                    .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase));
                foreach (var file in files)
                {
                    Filenames.Add(file);
                }
            }

            //lấy ra cái canvas chứa title trong template
            //template được áp dụng sau khi ContentRendered
            ContentRendered += (s, e) => titleSongName = (Canvas)Template.FindName("canvasTitle", this);

            //unzip file .gif từ resource
            using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/XLib;component/Resources/Images/Equalizer.zip", UriKind.Absolute)).Stream;
            var zipArchive = new ZipArchive(stream);
            var gifPath = @$"{Environment.CurrentDirectory}\ResourceEqualizer";
            zipArchive.ExtractToDirectory(gifPath, true);
            GifFiles.AddRange(Directory.GetFiles(gifPath));

            //bổ sung thêm file .gif từ thư mục ngoài
            var gifPath2 = @$"{Environment.CurrentDirectory}\Equalizer";
            Directory.CreateDirectory(gifPath2);
            GifFiles.AddRange(Directory.GetFiles(gifPath2, "*.gif"));
        }
        //Storyboard điều khiển animation cho equalizer của window table manager
        public Storyboard? StoryboardEqualizer { get; set; }
        string _textPosition = "00:00:00";
        public string TextPosition { get => _textPosition; set => OnPropertyChanged(ref _textPosition, value); }
        MediaPlayerStatus _status;
        public MediaPlayerStatus Status { get => _status; set => OnPropertyChanged(ref _status, value); }
        ClockState _clockState;
        public ClockState ClockState { get => _clockState; set => OnPropertyChanged(ref _clockState, value); }
        public XSlider SeekBar => sliderSeekBar;
        public bool ShouldClose { get; set; }
        public string AppName { get; } = "Media Player 2023";
        public bool IsRepeat { get => btnRepeat != null ? btnRepeat.IsChecked.GetValueOrDefault(false) : false; set => btnRepeat.IsChecked = value; }
        public bool IsShuffle { get => btnShuffle.IsChecked.GetValueOrDefault(false); set => btnShuffle.IsChecked = value; }

        FrameworkElement? titleSongName;
        ClockController? titleClockController;

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        ObservableCollection<string> _filenames = new ObservableCollection<string>();
        public ObservableCollection<string> Filenames { get => _filenames; set => OnPropertyChanged(ref _filenames, value); } 

        public List<string> GifFiles { get; } = new List<string>();
        private void media_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (media.NaturalDuration != Duration.Automatic)
                sliderSeekBar.Maximum = media.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        bool isSeekingByMedia;
        private void MediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (media.HasVideo)
            {
                gifPlayer.Visibility = Visibility.Collapsed;
                equalizer.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (menuEqualizer.IsChecked)
                {
                    if (isPlayingGif)
                    {
                        gifPlayer.Visibility = Visibility.Visible;
                        equalizer.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        gifPlayer.Visibility = Visibility.Collapsed;
                        equalizer.Visibility = Visibility.Visible;
                    }
                    
                }
                else
                {
                    gifPlayer.Visibility = Visibility.Collapsed;
                    equalizer.Visibility = Visibility.Collapsed;
                }
            }

            if (sliderSeekBar.IsDragging == false)
            {
                isSeekingByMedia = true;
                //Đánh dấu sliderSeekBar đang được set value bởi media element để event ValueChanged không seek nữa 
                sliderSeekBar.Value = media.Position.TotalMilliseconds;
                isSeekingByMedia = false;
            }

            var totalDuration = media.NaturalDuration.ToString();
            if (media.NaturalDuration.HasTimeSpan)
            {
                totalDuration = media.NaturalDuration.TimeSpan.ToString("mm\\:ss");
            }

            txblPosition.Text = $"{media.Position.ToString("mm\\:ss")} / {totalDuration}";

            ClockState = media.Clock.CurrentState;

            //Khi play hết bài (nếu RepeatBehavior = Forever thì sẽ không bao giờ Filling)
            if (media.Clock.CurrentState == ClockState.Filling)
            {
                gifPlayer.Stop();
                Status = MediaPlayerStatus.End;
                if (IsRepeat == false)
                {
                    //Nếu không repeat thì next sang bài tiếp (nếu repeat thì play liên tục 1 bài)
                    btnNext_Click(sender, new RoutedEventArgs(ButtonBase.ClickEvent, btnNext));
                    return;
                }
            }
        }

        private void sliderSeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //ValueChanged xảy ra khi thumb dragging hoặc click chuột lên track (IsMoveToPointEnabled="True")
            if (sender is XSlider slider)
            {
                if (isSeekingByMedia == false)
                {
                    seekMedia.Offset = new TimeSpan(0, 0, 0, 0, (int)slider.Value);
                    btnSeek.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnSeek));
                }
            }

        }

        private void btnRepeat_Checked(object sender, RoutedEventArgs e)
        {
            mediaTimeline.RepeatBehavior = RepeatBehavior.Forever;

            if (media.Position != TimeSpan.Zero)
            {
                //play lại để mediatimeline nhận gía trị mới của RepeatBehavior
                seekMedia.Offset = new TimeSpan(0, 0, 0, 0, (int)sliderSeekBar.Value);
                btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
                btnSeek.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnSeek));
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRepeat"));
        }

        private void btnRepeat_Unchecked(object sender, RoutedEventArgs e)
        {
            mediaTimeline.RepeatBehavior = new RepeatBehavior(1);

            if (media.Position != TimeSpan.Zero)
            {
                seekMedia.Offset = new TimeSpan(0, 0, 0, 0, (int)sliderSeekBar.Value);
                btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
                btnSeek.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnSeek));
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRepeat"));
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Mp3 files |*.mp3|Media files |*.mp3;*.mp4;*.wav;*.flv;*.avi;*.m4a|All files |*.*";
            if (Directory.Exists(txbDefaultPath.Text)) openFileDialog.InitialDirectory = txbDefaultPath.Text;
            if (openFileDialog.ShowDialog() == true)
            {
                //Nếu mở từ button btnBrowse sẽ thêm 1 danh sách mới, nếu mở từ contextmenu của listview thì chỉ thêm vào danh sách hiện tại
                if (sender is Button) Filenames.Clear();
                foreach (var item in openFileDialog.FileNames) Filenames.Add(item);

                //play ngay sau khi mở danh sách mới
                if (sender is Button) btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
            }
        }

        void Button_ControlMedia(object sender, RoutedEventArgs e)
        {

            if (sender is Button button)
            {
                var songName = "Playlist is empty";
                if (lsvFilenames.Items.Count > 0)
                {
                    if (lsvFilenames.SelectedItem == null) lsvFilenames.SelectedIndex = 0;
                    songName = lsvFilenames.SelectedItem.ToString();
                }

                if (button.Name == "btnPlay")
                {
                    Title = songName;

                    titleClockController?.Remove();

                    UpdateLayout();
                    TextBlock textBlock = (TextBlock)Template.FindName("txblTitle", this);
                    DoubleAnimation titleAnimation = new DoubleAnimation { From = 1, To = -1 * titleSongName.ActualWidth - 35, Duration = TimeSpan.FromMilliseconds(12000), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
                    var clock = titleAnimation.CreateClock();
                    titleClockController = clock.Controller;
                    textBlock.ApplyAnimationClock(Canvas.LeftProperty, clock);
                    titleSongName.HorizontalAlignment = HorizontalAlignment.Left;

                    if (lsvFilenames.Items.Count <= 0)
                    {
                        DialogBox.Show("Playlist is empty.", "ERROR", DialogBoxButton.OK, DialogBoxIcon.Error);
                        return;
                    }

                    gifPlayer.Play();

                    Status = MediaPlayerStatus.Play;

                    StoryboardEqualizer?.Begin();

                    MenuItem_NextEqualizer(sender, e);
                }
                if (button.Name == "btnPause")
                {
                    titleClockController?.Stop();
                    Title = AppName;
                    titleSongName.HorizontalAlignment = HorizontalAlignment.Center;

                    gifPlayer.Pause();

                    if (Status == MediaPlayerStatus.Play || Status == MediaPlayerStatus.Resume) Status = MediaPlayerStatus.Pause;

                    StoryboardEqualizer?.Pause();
                }
                if (button.Name == "btnResume")
                {
                    titleClockController?.Begin();
                    Title = songName;
                    titleSongName.HorizontalAlignment = HorizontalAlignment.Left;

                    gifPlayer.Play();

                    if (Status == MediaPlayerStatus.Pause) Status = MediaPlayerStatus.Resume;

                    StoryboardEqualizer?.Resume();
                }
                if (button.Name == "btnStop")
                {
                    titleClockController?.Stop();
                    Title = AppName;
                    if(titleSongName != null) titleSongName.HorizontalAlignment = HorizontalAlignment.Center;

                    gifPlayer.Stop();

                    Status = MediaPlayerStatus.Stop;

                    StoryboardEqualizer?.Stop();
                }
            }
        }

        private void gifPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            gifPlayer.Position = TimeSpan.FromSeconds(1);
            gifPlayer.Play();
        }
        bool isPlayingGif;
        private void MenuItem_NextEqualizer(object sender, RoutedEventArgs e)
        {
            if (media.HasVideo) return;
            if (GifFiles.Count > 0)
            {
                Random random = new Random(new Random().Next(9999));
                var index = random.Next(GifFiles.Count + 5);
                if(index < GifFiles.Count)
                {
                    isPlayingGif = true;
                    gifPlayer.Visibility = Visibility.Visible;
                    equalizer.Visibility = Visibility.Collapsed;
                    gifPlayer.Source = new Uri(GifFiles[index]);
                    gifPlayer.Play();
                }
                else
                {
                    isPlayingGif = false;
                    gifPlayer.Stop();
                    gifPlayer.Visibility = Visibility.Collapsed;
                    equalizer.Visibility = Visibility.Visible;
                    if (index == GifFiles.Count + 0) equalizer.Mode = 0;
                    if (index == GifFiles.Count + 1) equalizer.Mode = 1;
                    if (index == GifFiles.Count + 2) equalizer.Mode = 2;
                    if (index == GifFiles.Count + 3) equalizer.Mode = 3;
                    if (index == GifFiles.Count + 4) equalizer.Mode = 4;
                }

            }

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            //Nếu không phải là bài hát cuối list thì có thể next bằng cách thay đổi SelectedIndex và begin lại storyboard
            if (lsvFilenames.SelectedIndex < lsvFilenames.Items.Count - 1)
            {
                if (btnShuffle.IsChecked == true) lsvFilenames.SelectedIndex = new Random(new Random().Next(999)).Next(lsvFilenames.Items.Count);
                else lsvFilenames.SelectedIndex += 1;

                //Scroll đến giữa scrollviewer
                //var offset = (lsvFilenames.SelectedIndex * 1.0 / lsvFilenames.Items.Count) * svFilenames.ExtentHeight - svFilenames.ActualHeight/2;
                //svFilenames.ScrollToVerticalOffset(offset);
                ScrollToSelectedItem(lsvFilenames);

                btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
            }
        }

        //scroll đến SelectedItem của 1 selector nếu nó được bao bởi scrollviewer, SelectedItem được scroll đến giữa scrollviewer
        void ScrollToSelectedItem(Selector selector)
        {
            if (selector.Parent is ScrollViewer scrollViewer)
            {
                //Scroll đến giữa scrollviewer
                var offset = (selector.SelectedIndex * 1.0 / selector.Items.Count) * scrollViewer.ExtentHeight - scrollViewer.ActualHeight / 2;
                scrollViewer.ScrollToVerticalOffset(offset);
            }

        }

        private void btnShuffle_Checked(object sender, RoutedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsShuffle"));
        }

        private void btnShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsShuffle"));
        }

        private void lsvFilenames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
            MenuItem_NextEqualizer(sender, e);
        }

        private void lsvFilenames_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files) Filenames.Add(file);
        }

        private void MenuItem_ControlMedia(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                if (item.Header.ToString() == "Play")
                    btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
                if (item.Header.ToString() == "Pause")
                    btnPause.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPause));
                if (item.Header.ToString() == "Resume")
                    btnResume.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnResume));
                if (item.Header.ToString() == "Stop")
                    btnStop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnStop));
                if (item.Header.ToString() == "Next")
                    btnNext.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnNext));
            }
        }

        private void svFilenames_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //child element trong scroll viewer bị chặn event MouseWheel, dùng PreviewMouseWheel để tunnel ngược về  
            if (sender is ScrollViewer scrollViewer) scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (string.IsNullOrEmpty(textBox.Text)) return;
                var filename = Filenames.Where(fn => fn.ToUnsigned().ToLower().Contains(textBox.Text.ToUnsigned().ToLower())).FirstOrDefault();
                //lsvFilenames.ScrollIntoView(filename);
                if (string.IsNullOrEmpty(filename)) return;
                lsvFilenames.SelectedIndex = Filenames.IndexOf(filename);
                ScrollToSelectedItem(lsvFilenames);
            }
        }

        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "Search from playlist") textBox.Clear();
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "") textBox.Text = "Search from playlist";
            }
        }

        private void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {

            DialogBox.Show(e.ErrorException.Message);
            return;
        }

        private void MenuItem_RemoveFromPlaylist(object sender, RoutedEventArgs e)
        {
            if (Filenames.Count > 0 && lsvFilenames.SelectedItem != null) Filenames.Remove((string)lsvFilenames.SelectedItem);
            var view = CollectionViewSource.GetDefaultView(lsvFilenames.ItemsSource);
            view.Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ShouldClose)
            {
                var gifPath = @$"{Environment.CurrentDirectory}\ResourceEqualizer";
                if (Directory.Exists(gifPath)) Directory.Delete(gifPath, true);
            }
            else
            {
                e.Cancel = true;
                Hide();
            }
        }

        public bool IsClosed { get; private set; }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            btnStop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnStop));
            IsClosed = true;
        }
    }
}
