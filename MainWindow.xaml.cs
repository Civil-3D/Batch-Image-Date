using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

// საჭირო ბიბლიოთეკები
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats.Jpeg;

using ISColor = SixLabors.ImageSharp.Color;
using ISImage = SixLabors.ImageSharp.Image;
using ISPointF = SixLabors.ImageSharp.PointF;
using Size = SixLabors.ImageSharp.Size;



namespace BatchImage_Studio
{
    public partial class MainWindow : Window
    {
        private readonly string[] validExtensions = [".jpg", ".jpeg", ".png", ".heic", ".webp", ".bmp"];
        private bool isBatchUpdating = false;

        // Pan ფუნქციის ცვლადები
        private System.Windows.Point _panStart;
        private System.Windows.Point _panOrigin;

        // 🔴 ფოტოების როტაციის მეხსიერება 🔴
        private Dictionary<string, int> _imageRotations = new();

        public MainWindow()
        {
            InitializeComponent();
            QuestPDF.Settings.License = LicenseType.Community;
            this.StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.Margin = new Thickness(7); // აცილებს ფანჯარას ამოჭრას ეკრანის კიდეებზე
            }
            else
            {
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.Margin = new Thickness(0);
            }
        }

        #region Save & Load Settings
        private static string GetSettingsFilePath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatchImageStudio");
            if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.json");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null)
                    {
                        ChkUseExif.IsChecked = s.UseExif;
                        TxtCustomDate.Text = s.CustomDate;
                        ComboFont.SelectedIndex = s.FontIndex;
                        BtnBold.IsChecked = s.IsBold;
                        BtnItalic.IsChecked = s.IsItalic;
                        SizeSlider.Value = s.SizeLevel;
                        ComboPosition.SelectedIndex = s.PositionIndex;

                        if (!string.IsNullOrEmpty(s.TextColor))
                            TextColorBlock.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.TextColor));

                        StrokeCheck.IsChecked = s.EnableStroke;
                        if (!string.IsNullOrEmpty(s.StrokeColor))
                            StrokeColorBlock.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.StrokeColor));

                        StrokeThicknessSlider.Value = s.StrokeThickness;

                        ChkConvertJpeg.IsChecked = s.ConvertJpeg;
                        ChkResize.IsChecked = s.DoResize;
                        TxtMaxSize.Text = s.MaxSizeMb;
                        CompressCheck.IsChecked = s.DoCompress;
                        QualitySlider.Value = s.JpegQuality;

                        FormatImage.IsChecked = s.IsFormatImage;
                        FormatSinglePdf.IsChecked = s.IsFormatSinglePdf;
                        FormatMultiPdf.IsChecked = s.IsFormatMultiPdf;

                        RadioReplace.IsChecked = s.IsReplace;
                        RadioOriginalFolder.IsChecked = s.IsOriginalFolder;
                        RadioCustomDir.IsChecked = s.IsCustomDir;
                        TxtPdfFileName.Text = s.PdfFileName;

                        // 🛑 ფოლდერების შემოწმება
                        string defaultDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                        if (!string.IsNullOrEmpty(s.CustomDirPath) && System.IO.Directory.Exists(s.CustomDirPath))
                            TxtOutputDir.Text = s.CustomDirPath;
                        else
                            TxtOutputDir.Text = defaultDesktop;

                        if (!string.IsNullOrEmpty(s.InputFolderPath) && System.IO.Directory.Exists(s.InputFolderPath))
                            TxtInputFolder.Text = s.InputFolderPath;
                    }
                }
            }
            catch { /* იგნორირება */ }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var s = new AppSettings
                {
                    UseExif = ChkUseExif.IsChecked == true,
                    CustomDate = TxtCustomDate.Text,
                    FontIndex = ComboFont.SelectedIndex,
                    IsBold = BtnBold.IsChecked == true,
                    IsItalic = BtnItalic.IsChecked == true,
                    SizeLevel = SizeSlider.Value,
                    PositionIndex = ComboPosition.SelectedIndex,
                    TextColor = (TextColorBlock.Background as SolidColorBrush)?.Color.ToString() ?? "",
                    EnableStroke = StrokeCheck.IsChecked == true,
                    StrokeColor = (StrokeColorBlock.Background as SolidColorBrush)?.Color.ToString() ?? "",
                    StrokeThickness = StrokeThicknessSlider.Value,
                    ConvertJpeg = ChkConvertJpeg.IsChecked == true,
                    DoResize = ChkResize.IsChecked == true,
                    MaxSizeMb = TxtMaxSize.Text,
                    DoCompress = CompressCheck.IsChecked == true,
                    JpegQuality = QualitySlider.Value,
                    IsFormatImage = FormatImage.IsChecked == true,
                    IsFormatSinglePdf = FormatSinglePdf.IsChecked == true,
                    IsFormatMultiPdf = FormatMultiPdf.IsChecked == true,
                    IsReplace = RadioReplace.IsChecked == true,
                    IsOriginalFolder = RadioOriginalFolder.IsChecked == true,
                    IsCustomDir = RadioCustomDir.IsChecked == true,

                    InputFolderPath = TxtInputFolder.Text,
                    CustomDirPath = TxtOutputDir.Text,
                    PdfFileName = TxtPdfFileName.Text
                };
                File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(s));
            }
            catch { }
        }
        #endregion

        #region Window Controls
        private void TopBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e);
                return;
            }
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.Top = e.GetPosition(this).Y - 15;
                    double percentHorizontal = e.GetPosition(this).X / this.ActualWidth;
                    double targetHorizontal = this.RestoreBounds.Width * percentHorizontal;
                    this.Left = e.GetPosition(this).X - targetHorizontal;
                    this.WindowState = WindowState.Normal;
                }
                this.DragMove();
            }
        }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal;
            else
            {
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.MaxWidth = SystemParameters.WorkArea.Width;
                this.WindowState = WindowState.Maximized;
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        #endregion

        #region File Import Logic
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Input Folder" };
            if (System.IO.Directory.Exists(TxtInputFolder.Text))
                dialog.InitialDirectory = TxtInputFolder.Text;

            if (dialog.ShowDialog() == true)
                TxtInputFolder.Text = dialog.FolderName;
        }

        private void TxtInputFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (System.IO.Directory.Exists(TxtInputFolder.Text))
                ProcessImportedItems([TxtInputFolder.Text], ChkDeepSearch.IsChecked == true);
        }

        private void ListBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] droppedItems)
                {
                    ProcessImportedItems(droppedItems, ChkDeepSearch.IsChecked == true);
                }
            }
        }

        private void ProcessImportedItems(string[] paths, bool deepSearch)
        {
            var searchOpt = deepSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var path in paths)
            {
                if (System.IO.Directory.Exists(path))
                {
                    try
                    {
                        var files = System.IO.Directory.GetFiles(path, "*.*", searchOpt)
                                             .Where(f => validExtensions.Contains(Path.GetExtension(f).ToLower()));
                        foreach (var f in files) AddUniqueToReadyList(f);
                    }
                    catch (UnauthorizedAccessException) { }
                }
                else if (File.Exists(path) && validExtensions.Contains(Path.GetExtension(path).ToLower()))
                {
                    AddUniqueToReadyList(path);
                }
            }
            UpdateReadyCount();

            if (ReadyFilesList.Items.Count > 0 && ReadyFilesList.SelectedIndex == -1)
            {
                ReadyFilesList.SelectedIndex = 0;
            }
        }

        private void AddUniqueToReadyList(string filePath)
        {
            if (!ReadyFilesList.Items.Contains(filePath) && !ExcludedFilesList.Items.Contains(filePath))
                ReadyFilesList.Items.Add(filePath);
        }
        #endregion

        #region List Operations & UI Logic
        private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            if (ExcludedFilesList.SelectedItems.Count == 0) return;
            isBatchUpdating = true;

            var lastSelectedItem = ExcludedFilesList.SelectedItems[ExcludedFilesList.SelectedItems.Count - 1]?.ToString();
            var selectedItems = ExcludedFilesList.SelectedItems.Cast<string>().ToList();

            foreach (var item in selectedItems)
            {
                ReadyFilesList.Items.Add(item);
                ExcludedFilesList.Items.Remove(item);
            }
            isBatchUpdating = false;
            UpdateReadyCount();

            if (lastSelectedItem != null)
            {
                ReadyFilesList.SelectedItem = lastSelectedItem;
                ReadyFilesList.ScrollIntoView(lastSelectedItem);
            }
        }

        private void BtnRemoveFiles_Click(object sender, RoutedEventArgs e)
        {
            if (ReadyFilesList.SelectedItems.Count == 0) return;
            isBatchUpdating = true;

            var lastSelectedItem = ReadyFilesList.SelectedItems[ReadyFilesList.SelectedItems.Count - 1]?.ToString();
            var selectedItems = ReadyFilesList.SelectedItems.Cast<string>().ToList();

            foreach (var item in selectedItems)
            {
                ExcludedFilesList.Items.Add(item);
                ReadyFilesList.Items.Remove(item);
            }
            isBatchUpdating = false;
            UpdateReadyCount();

            if (lastSelectedItem != null)
            {
                ExcludedFilesList.SelectedItem = lastSelectedItem;
                ExcludedFilesList.ScrollIntoView(lastSelectedItem);
            }
        }

        private void ListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && sender is System.Windows.Controls.ListBox listBox)
            {
                isBatchUpdating = true;
                var selectedItems = listBox.SelectedItems.Cast<string>().ToList();
                foreach (var item in selectedItems) listBox.Items.Remove(item);
                isBatchUpdating = false;
                UpdateReadyCount();
                ClearPreview();
            }
        }

        private void UpdateReadyCount() => TxtReadyCount.Text = $"Ready to Process ({ReadyFilesList.Items.Count})";

        private void ExportFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (FormatImage.IsChecked == false)
            {
                if (RadioReplace.IsChecked == true)
                {
                    RadioOriginalFolder.IsChecked = true;
                }
            }
        }

        private void BtnCustomOutputDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Output Folder" };
            if (System.IO.Directory.Exists(TxtOutputDir.Text))
                dialog.InitialDirectory = TxtOutputDir.Text;

            if (dialog.ShowDialog() == true)
                TxtOutputDir.Text = dialog.FolderName;
        }
        #endregion

        #region Zoom & Pan Logic
        private void BtnZoomExtend_Click(object sender, RoutedEventArgs e) => ResetZoom();

        private void ResetZoom()
        {
            ZoomSlider.Value = 1;
            PanTransform.X = 0;
            PanTransform.Y = 0;
        }

        private void BtnRotate_Click(object sender, RoutedEventArgs e)
        {
            string? activePath = ReadyFilesList.SelectedItem?.ToString() ?? ExcludedFilesList.SelectedItem?.ToString();
            if (activePath == null) return;

            // ვამატებთ 90 გრადუსს ყოველ დაჭერაზე
            int currentAngle = _imageRotations.TryGetValue(activePath, out int a) ? a : 0;
            _imageRotations[activePath] = (currentAngle + 90) % 360;

            // ვაახლებთ პრევიუს
            bool isExcluded = ExcludedFilesList.SelectedItem != null;
            ShowImageInPreview(activePath, isExcluded);
        }

        private void ZoomContainer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0) ZoomSlider.Value = Math.Min(10, ZoomSlider.Value + 0.5);
            else ZoomSlider.Value = Math.Max(1, ZoomSlider.Value - 0.5);
        }

        private void ZoomContainer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ZoomSlider.Value > 1)
            {
                _panStart = e.GetPosition(PreviewClipContainer);
                _panOrigin = new System.Windows.Point(PanTransform.X, PanTransform.Y);
                ZoomContainer.CaptureMouse();
            }
        }

        private void ZoomContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ZoomContainer.IsMouseCaptured)
            {
                var v = e.GetPosition(PreviewClipContainer) - _panStart;
                PanTransform.X = _panOrigin.X + v.X;
                PanTransform.Y = _panOrigin.Y + v.Y;
            }
        }

        private void ZoomContainer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ZoomContainer.ReleaseMouseCapture();
        }
        #endregion

        #region Live Preview & Colors
        private void ReadyFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isBatchUpdating) return;
            if (ReadyFilesList.SelectedItem != null)
            {
                ExcludedFilesList.UnselectAll();
                ShowImageInPreview(ReadyFilesList.SelectedItem.ToString(), isExcluded: false);
            }
            else if (ExcludedFilesList.SelectedItem == null) ClearPreview();
        }

        private void ExcludedFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isBatchUpdating) return;
            if (ExcludedFilesList.SelectedItem != null)
            {
                ReadyFilesList.UnselectAll();
                ShowImageInPreview(ExcludedFilesList.SelectedItem.ToString(), isExcluded: true);
            }
            else if (ReadyFilesList.SelectedItem == null) ClearPreview();
        }

        private void ShowImageInPreview(string path, bool isExcluded)
        {
            ResetZoom();
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);

                // 🔴 ვამოწმებთ თუ გვაქვს მითითებული როტაცია ამ ფოტოსთვის 🔴
                int angle = _imageRotations.TryGetValue(path, out int a) ? a : 0;
                if (angle == 90) bitmap.Rotation = Rotation.Rotate90;
                else if (angle == 180) bitmap.Rotation = Rotation.Rotate180;
                else if (angle == 270) bitmap.Rotation = Rotation.Rotate270;

                bitmap.EndInit();

                InternalImageCanvas.Width = bitmap.PixelWidth;
                InternalImageCanvas.Height = bitmap.PixelHeight;

                ImgPreview.Source = bitmap;
                TxtPreviewHint.Visibility = Visibility.Collapsed;
                ZoomContainer.Visibility = Visibility.Visible;
                ExcludedWarningOverlay.Visibility = isExcluded ? Visibility.Visible : Visibility.Collapsed;

                UpdateLivePreview();
            }
            catch { ClearPreview(); }
        }

        private void ClearPreview()
        {
            ImgPreview.Source = null;
            ZoomContainer.Visibility = Visibility.Collapsed;
            TxtPreviewHint.Visibility = Visibility.Visible;
        }

        private void LivePreview_Trigger(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) UpdateLivePreview();
        }

        private void UpdateLivePreview()
        {
            if (ImgPreview.Source == null || WatermarkGrid == null || InternalImageCanvas == null) return;
            WatermarkGrid.Children.Clear();

            string textToDraw = TxtCustomDate.Text;
            if (ChkUseExif.IsChecked == true)
            {
                string? activePath = ReadyFilesList.SelectedItem?.ToString() ?? ExcludedFilesList.SelectedItem?.ToString();
                if (activePath != null && File.Exists(activePath)) textToDraw = ExtractExifDate(activePath);
            }

            string selectedFont = (ComboFont.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Arial";
            System.Windows.Media.FontFamily fontFamily = new(selectedFont);
            System.Windows.FontWeight fontWeight = BtnBold.IsChecked == true ? FontWeights.Bold : FontWeights.Normal;
            System.Windows.FontStyle fontStyle = BtnItalic.IsChecked == true ? FontStyles.Italic : FontStyles.Normal;

            double baseDimension = Math.Max(InternalImageCanvas.Width, InternalImageCanvas.Height);
            double level = SizeSlider.Value;
            double baseSize = baseDimension * (level * 0.015);

            string? position = (ComboPosition.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (position)
            {
                case "Top Left":
                    WatermarkGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    WatermarkGrid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    break;
                case "Top Right":
                    WatermarkGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    WatermarkGrid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    break;
                case "Bottom Left":
                    WatermarkGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    WatermarkGrid.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    break;
                case "Bottom Right":
                    WatermarkGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    WatermarkGrid.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    break;
                case "Center":
                    WatermarkGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    WatermarkGrid.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    break;
            }

            double marginSize = baseDimension * 0.02;
            WatermarkGrid.Margin = new Thickness(marginSize);

            System.Windows.Media.Brush textBrush = TextColorBlock.Background is SolidColorBrush tb ? tb : System.Windows.Media.Brushes.White;
            System.Windows.Media.Brush strokeBrush = StrokeColorBlock.Background is SolidColorBrush sb ? sb : System.Windows.Media.Brushes.Black;
            double strokeThick = StrokeThicknessSlider.Value;

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            FormattedText formattedText = new(
                textToDraw,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface(fontFamily, fontStyle, fontWeight, FontStretches.Normal),
                baseSize,
                textBrush,
                pixelsPerDip);

            Geometry textGeometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));

            System.Windows.Shapes.Path textPath = new()
            {
                Data = textGeometry,
                Fill = textBrush,
                Stretch = Stretch.None
            };

            if (StrokeCheck.IsChecked == true)
            {
                double dynamicStroke = strokeThick * (baseDimension / 1500.0);
                textPath.Stroke = strokeBrush;
                textPath.StrokeThickness = dynamicStroke;
                textPath.StrokeLineJoin = PenLineJoin.Round;
            }

            WatermarkGrid.Children.Add(textPath);
        }

        private void ColorBlock_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                using var dialog = new System.Windows.Forms.ColorDialog();
                if (border.Background is SolidColorBrush solidBrush)
                    dialog.Color = System.Drawing.Color.FromArgb(solidBrush.Color.A, solidBrush.Color.R, solidBrush.Color.G, solidBrush.Color.B);

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B));
                    UpdateLivePreview();
                }
            }
        }
        #endregion

        #region Custom Dark MessageBox
        private void ShowDarkMessage(string message, string title)
        {
            var msgWin = new Window
            {
                Title = title,
                Width = 380,
                MinHeight = 180,
                SizeToContent = SizeToContent.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(77, 168, 218)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            var grid = new Grid
            {
                RowDefinitions = {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
            };

            var titleTxt = new TextBlock { Text = title, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(77, 168, 218)), FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15) };
            Grid.SetRow(titleTxt, 0);

            var msgTxt = new TextBlock
            {
                Text = message,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(msgTxt, 1);

            var btn = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(77, 168, 218)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            btn.Click += (s, ev) => msgWin.Close();
            Grid.SetRow(btn, 2);

            grid.Children.Add(titleTxt); grid.Children.Add(msgTxt); grid.Children.Add(btn);
            border.Child = grid; msgWin.Content = border;
            msgWin.ShowDialog();
        }
        #endregion

        #region Batch Processing
        private static string ExtractExifDate(string imagePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var dateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

                if (!string.IsNullOrEmpty(dateTime))
                {
                    if (DateTime.TryParseExact(dateTime, "yyyy:MM:dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                        return dt.ToString("yyyy/MM/dd");
                }
            }
            catch { }
            return File.GetCreationTime(imagePath).ToString("yyyy/MM/dd");
        }

        private async void BtnStartProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ReadyFilesList.Items.Count == 0)
            {
                ShowDarkMessage("Please add files to the 'Ready to Process' list first!", "No Files");
                return;
            }

            var filesToProcess = ReadyFilesList.Items.Cast<string>().ToList();
            bool useExif = ChkUseExif.IsChecked == true;
            string customDateTxt = TxtCustomDate.Text;

            string fontName = (ComboFont.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Arial";
            bool isBold = BtnBold.IsChecked == true;
            bool isItalic = BtnItalic.IsChecked == true;
            int sizeLevel = (int)SizeSlider.Value;
            string positionStr = (ComboPosition.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Center";

            var wpfTxtColor = (TextColorBlock.Background as SolidColorBrush)?.Color ?? System.Windows.Media.Color.FromRgb(255, 255, 255);
            ISColor txtColor = ISColor.FromRgba(wpfTxtColor.R, wpfTxtColor.G, wpfTxtColor.B, wpfTxtColor.A);

            var wpfStrColor = (StrokeColorBlock.Background as SolidColorBrush)?.Color ?? System.Windows.Media.Color.FromRgb(0, 0, 0);
            ISColor strColor = ISColor.FromRgba(wpfStrColor.R, wpfStrColor.G, wpfStrColor.B, wpfStrColor.A);

            bool enableStroke = StrokeCheck.IsChecked == true;
            float strThickness = (float)StrokeThicknessSlider.Value;

            bool convertJpeg = ChkConvertJpeg.IsChecked == true;
            bool doResize = ChkResize.IsChecked == true;

            if (!double.TryParse(TxtMaxSize.Text, out double maxSizeMb)) maxSizeMb = 5.0;
            long maxSizeBytes = (long)(maxSizeMb * 1024 * 1024);

            bool doCompress = CompressCheck.IsChecked == true;
            int jpegQuality = (int)QualitySlider.Value;

            bool isSinglePdf = FormatSinglePdf.IsChecked == true;
            bool isMultiPdf = FormatMultiPdf.IsChecked == true;

            bool isReplace = RadioReplace.IsChecked == true;
            bool isCustomDir = RadioCustomDir.IsChecked == true;
            string customDirPath = TxtOutputDir.Text;

            string pdfFileName = string.IsNullOrWhiteSpace(TxtPdfFileName.Text) ? "BatchExport_Multi" : TxtPdfFileName.Text;

            BtnStartProcess.IsEnabled = false;
            ProgressBarIndicator.IsIndeterminate = false;
            ProgressBarIndicator.Value = 0;
            ProgressBarIndicator.Maximum = filesToProcess.Count;

            int successCount = 0;
            List<string> failedFiles = [];

            await Task.Run(async () =>
            {
                List<string> processedImagePathsForPdf = [];

                for (int i = 0; i < filesToProcess.Count; i++)
                {
                    string originalPath = filesToProcess[i];
                    string finalOutputPath = originalPath;

                    try
                    {
                        using (var img = await ISImage.LoadAsync(originalPath))
                        {
                            // 🔴 დაამატე ზუსტად ეს ერთი ხაზი აქ! (ეს გაასწორებს ამოტრიალებულ ფოტოებს) 🔴
                            img.Mutate(x => x.AutoOrient());

                            // 🔴 ექსპორტის დროსაც ვატრიალებთ ზუსტად ისე, როგორც პრევიუში ავარჩიეთ 🔴
                            if (_imageRotations.TryGetValue(originalPath, out int angle) && angle != 0)
                            {
                                img.Mutate(x => x.Rotate((float)angle));
                            }

                            string finalWatermarkText = useExif ? ExtractExifDate(originalPath) : customDateTxt;

                            var fileInfo = new FileInfo(originalPath);
                            if (doResize && fileInfo.Length > maxSizeBytes)
                            {
                                img.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(1920, 1080), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max }));
                            }

                            var family = SixLabors.Fonts.SystemFonts.TryGet(fontName, out var f) ? f : SixLabors.Fonts.SystemFonts.Families.First();
                            var style = SixLabors.Fonts.FontStyle.Regular;
                            if (isBold && isItalic) style = SixLabors.Fonts.FontStyle.BoldItalic;
                            else if (isBold) style = SixLabors.Fonts.FontStyle.Bold;
                            else if (isItalic) style = SixLabors.Fonts.FontStyle.Italic;

                            float baseDim = Math.Max(img.Width, img.Height);
                            float fontSize = baseDim * ((float)sizeLevel * 0.015f);
                            var font = family.CreateFont(fontSize, style);

                            var textMeasureOptions = new SixLabors.Fonts.TextOptions(font);
                            var textBounds = TextMeasurer.MeasureBounds(finalWatermarkText, textMeasureOptions);

                            float marginSize = baseDim * 0.02f;
                            float xPos = 0, yPos = 0;

                            switch (positionStr)
                            {
                                case "Top Left": xPos = marginSize; yPos = marginSize; break;
                                case "Top Right": xPos = img.Width - textBounds.Width - marginSize; yPos = marginSize; break;
                                case "Bottom Left": xPos = marginSize; yPos = img.Height - textBounds.Height - marginSize; break;
                                case "Bottom Right": xPos = img.Width - textBounds.Width - marginSize; yPos = img.Height - textBounds.Height - marginSize; break;
                                case "Center": xPos = (img.Width - textBounds.Width) / 2; yPos = (img.Height - textBounds.Height) / 2; break;
                            }

                            var richTextOptions = new RichTextOptions(font)
                            {
                                Origin = new ISPointF(xPos, yPos),
                                HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment.Left,
                                VerticalAlignment = SixLabors.Fonts.VerticalAlignment.Top
                            };

                            var sBrush = SixLabors.ImageSharp.Drawing.Processing.Brushes.Solid(strColor);
                            var tBrush = SixLabors.ImageSharp.Drawing.Processing.Brushes.Solid(txtColor);

                            if (enableStroke)
                            {
                                float dynamicStroke = strThickness * (baseDim / 1500f);
                                var pen = SixLabors.ImageSharp.Drawing.Processing.Pens.Solid(strColor, dynamicStroke);
                                img.Mutate(ctx => ctx.DrawText(richTextOptions, finalWatermarkText, tBrush, pen));
                            }
                            else
                            {
                                img.Mutate(ctx => ctx.DrawText(richTextOptions, finalWatermarkText, tBrush));
                            }

                            string fileName = Path.GetFileNameWithoutExtension(originalPath);
                            string extension = convertJpeg ? ".jpg" : Path.GetExtension(originalPath);

                            if (isSinglePdf || isMultiPdf)
                            {
                                extension = ".jpg";
                                finalOutputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
                            }
                            else
                            {
                                string targetFolder = Path.GetDirectoryName(originalPath) ?? string.Empty;
                                if (isCustomDir && System.IO.Directory.Exists(customDirPath)) targetFolder = customDirPath;

                                string newFileName = isReplace && !isCustomDir ? Path.GetFileName(originalPath) : $"{fileName}_wm{extension}";
                                finalOutputPath = Path.Combine(targetFolder, newFileName);
                            }

                            if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                            {
                                var encoder = new JpegEncoder { Quality = doCompress ? jpegQuality : 100 };
                                await img.SaveAsJpegAsync(finalOutputPath, encoder);
                            }
                            else
                            {
                                await img.SaveAsync(finalOutputPath);
                            }

                            processedImagePathsForPdf.Add(finalOutputPath);
                        }

                        if (isSinglePdf)
                        {
                            string targetDir = isCustomDir ? customDirPath : Path.GetDirectoryName(originalPath) ?? string.Empty;
                            string pdfOutPath = Path.Combine(targetDir, $"{Path.GetFileNameWithoutExtension(originalPath)}.pdf");

                            var imgInfo = ISImage.Identify(finalOutputPath);

                            Document.Create(container => {
                                container.Page(page => {
                                    page.Size(imgInfo.Width, imgInfo.Height);
                                    page.Margin(0);
                                    page.Content().Image(finalOutputPath);
                                });
                            }).GeneratePdf(pdfOutPath);

                            File.Delete(finalOutputPath);
                        }

                        successCount++;
                    }
                    catch (Exception)
                    {
                        failedFiles.Add(Path.GetFileName(originalPath));
                    }

                    int currentProgress = i + 1;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressBarIndicator.Value = currentProgress;
                        TxtProgressText.Text = $"Processing {currentProgress} of {filesToProcess.Count}...";
                    });
                }

                if (isMultiPdf && processedImagePathsForPdf.Count > 0)
                {
                    string targetDir = isCustomDir ? customDirPath : Path.GetDirectoryName(filesToProcess[0]) ?? string.Empty;
                    string pdfOutPath = Path.Combine(targetDir, $"{pdfFileName}.pdf");

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressBarIndicator.IsIndeterminate = true;
                        TxtProgressText.Text = $"Creating PDF file: {Path.GetFileName(pdfOutPath)}... Please wait.";
                    });

                    try
                    {
                        Document.Create(container => {
                            foreach (var imgPath in processedImagePathsForPdf)
                            {
                                var imgInfo = ISImage.Identify(imgPath);
                                container.Page(page => {
                                    page.Size(imgInfo.Width, imgInfo.Height);
                                    page.Margin(0);
                                    page.Content().Image(imgPath);
                                });
                            }
                        }).GeneratePdf(pdfOutPath);
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"PDF Error: {ex.Message}");
                    }
                    finally
                    {
                        foreach (var imgPath in processedImagePathsForPdf)
                        {
                            if (File.Exists(imgPath)) File.Delete(imgPath);
                        }
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressBarIndicator.IsIndeterminate = false;
                    });
                }
            });

            string resultMessage = $"Batch processing completed!\n\nSuccessfully processed: {successCount}";
            if (failedFiles.Count > 0)
            {
                resultMessage += $"\n\nFailed Files ({failedFiles.Count}):\n" + string.Join("\n", failedFiles.Take(5));
                if (failedFiles.Count > 5) resultMessage += "\n...and more.";
            }

            ShowDarkMessage(resultMessage, "Process Report");

            BtnStartProcess.IsEnabled = true;
            TxtProgressText.Text = "Ready to process!";
            ProgressBarIndicator.Value = 0;
            ProgressBarIndicator.IsIndeterminate = false;
        }
        #endregion

    }



    public class AppSettings
    {
        public bool UseExif { get; set; }
        public string CustomDate { get; set; } = string.Empty;
        public int FontIndex { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public double SizeLevel { get; set; }
        public int PositionIndex { get; set; }
        public string TextColor { get; set; } = string.Empty;
        public bool EnableStroke { get; set; }
        public string StrokeColor { get; set; } = string.Empty;
        public double StrokeThickness { get; set; }
        public bool ConvertJpeg { get; set; }
        public bool DoResize { get; set; }
        public string MaxSizeMb { get; set; } = string.Empty;
        public bool DoCompress { get; set; }
        public double JpegQuality { get; set; }
        public bool IsFormatImage { get; set; }
        public bool IsFormatSinglePdf { get; set; }
        public bool IsFormatMultiPdf { get; set; }
        public bool IsReplace { get; set; }
        public bool IsOriginalFolder { get; set; }
        public bool IsCustomDir { get; set; }
        public string InputFolderPath { get; set; } = string.Empty;
        public string CustomDirPath { get; set; } = string.Empty;
        public string PdfFileName { get; set; } = string.Empty;
    }
}