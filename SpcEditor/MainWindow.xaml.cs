using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using V3Lib.Spc;

namespace SpcEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentSPCFilename;
        private SpcFile currentSPC;

        public MainWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SpcEditorCommands.Open, Open));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.Save, Save, CanInteractWithFile));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.SaveAs, SaveAs, CanInteractWithFile));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.Insert, Insert, CanInteractWithFile));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.Extract, Extract, CanExtract));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.ExtractAll, ExtractAll, CanExtractAll));
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".SPC",
                Filter = "SPC Archive (*.SPC)|*.SPC|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            currentSPC = new SpcFile();
            currentSPCFilename = dialog.FileName;
            currentSPC.Load(currentSPCFilename);
            SubFileListView.ItemsSource = currentSPC.Subfiles;
            statusText.Text = $"{currentSPCFilename} loaded.";
        }

        private void CanInteractWithFile(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = currentSPC != null;

        private void Save(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(currentSPCFilename))
            {
                SaveAs(sender, e);
            }
            else
            {
                currentSPC.Save(currentSPCFilename);
                statusText.Text = $"{currentSPCFilename} saved.";
            }
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".SPC",
                Filter = "SPC Archive (*.SPC)|*.SPC|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            currentSPCFilename = dialog.FileName;
            currentSPC.Save(currentSPCFilename);
            statusText.Text = $"File saved to {currentSPCFilename}.";
        }

        private void Insert(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".SPC",
                Filter = "SPC Archive (*.SPC)|*.SPC|All Files (*.*)|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            statusText.Text = $"Inserting {dialog.FileNames.Length} files...";
            foreach (var filename in dialog.FileNames)
            {
                currentSPC.InsertSubfile(filename);
            }
            statusText.Text = $"Sucessfully inserted {dialog.FileNames.Length} files.";
        }

        private void CanExtract(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = currentSPC != null && currentSPC.Subfiles.Count > 0 && SubFileListView.SelectedItems.Count > 0;

        private void Extract(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Extract Location...",
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }

            statusText.Text = $"Extracting {SubFileListView.SelectedItems.Count} files...";
            foreach (var item in SubFileListView.SelectedItems)
            {
                if (item is SpcSubfile file)
                {
                    currentSPC.ExtractSubfile(file.Name, dialog.FileName);
                }
            }
            statusText.Text = $"Successfully extracted {SubFileListView.SelectedItems.Count} files to {dialog.FileName}.";
        }

        private void CanExtractAll(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = currentSPC != null && currentSPC.Subfiles.Count > 0;

        private void ExtractAll(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Extract Location...",
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }

            statusText.Text = $"Extracting {currentSPC.Subfiles.Count} files...";
            foreach (var file in currentSPC.Subfiles)
            {
                currentSPC.ExtractSubfile(file.Name, dialog.FileName);
            }
            statusText.Text = $"Successfully extracted {currentSPC.Subfiles.Count} files to {dialog.FileName}.";
        }
        
        /**
         * <summary>
         * The ListView object overlaps with the ScrollViewer and steals its mouse scroll event.
         * This event captures the mouse scroll event on the ListView so we can apply the scroll
         * effect manually.
         * </summary>
         */
        private void SubFileListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            SubFileListScrollViewer.ScrollToVerticalOffset(SubFileListScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ReplaceFileListContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is currently not implemented. :(");
        }

        private void OpenInEditorFileListContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is currently not implemented. :(");
        }
    }

    public static class SpcEditorCommands
    {
        public static RoutedCommand Open = new RoutedCommand("Open", typeof(SpcEditorCommands), new InputGestureCollection {
            new KeyGesture(Key.O, ModifierKeys.Control)
        });
        public static RoutedCommand Save = new RoutedCommand("Save", typeof(SpcEditorCommands), new InputGestureCollection {
            new KeyGesture(Key.S, ModifierKeys.Control)
        });
        public static RoutedCommand SaveAs = new RoutedCommand("Save As", typeof(SpcEditorCommands), new InputGestureCollection {
            new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
        });
        public static RoutedCommand Insert = new RoutedCommand("Insert", typeof(SpcEditorCommands));
        public static RoutedCommand Extract = new RoutedCommand("Extract", typeof(SpcEditorCommands));
        public static RoutedCommand ExtractAll = new RoutedCommand("Extract All", typeof(SpcEditorCommands));
    }

    public sealed class CompressionStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (short)value switch
            {
                1 => "Compressed",
                2 => "Uncompressed",
                3 => "N/A (External)",
                short x => $"N/A (Unknown: {x})"
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException("This is a compression state -> string is a one-way operation!");
    }
}
