using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private static readonly string TEMP_DIR = Path.Combine(Path.GetTempPath(), "SpcEditor");

        // These are only temporarily null, and all null checks are made
        // before any logic on them can execute (with the CanXXXX functions)
        private string currentSPCFilename = null!;
        private SpcFile currentSPC = null!;

        private FileSystemWatcher fsWatch = null!;
        private string tmpFilesDir = null!;

        public MainWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, Open));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, Save, CanInteractWithFile));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.SaveAs, SaveAs, CanInteractWithFile));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.Insert, Insert, CanInteractWithFile));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.Extract, Extract, CanExtract));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.ExtractAll, ExtractAll, CanExtractAll));
            CommandBindings.Add(new CommandBinding(SpcEditorCommands.OpenInEditor, OpenInEditor, CanExtract));

            Closed += MainWindow_Closed;
        }

        public MainWindow(string spcPath) : this()
        {
            LoadSpc(spcPath);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // Let's clean up our temp directory...
            if (Directory.Exists(TEMP_DIR))
            {
                Directory.Delete(TEMP_DIR, true);
            }
        }

        private void RefreshListViewItems()
        {
            if (currentSPC != null)
            {
                // We want to defer refreshing the view until we're on the main thread.
                Application.Current.Dispatcher.BeginInvoke((Action)SubFileListView.Items.Refresh);
            }
        }

        private void InitFileSystemWatcher()
        {
            if (fsWatch != null)
            {
                fsWatch.Dispose();
            }
            tmpFilesDir = Path.Combine(TEMP_DIR, Path.GetFileNameWithoutExtension(currentSPCFilename));
            Directory.CreateDirectory(tmpFilesDir);
            // We want to see when our temporary files have changed
            // So we can automatically update them in the SPC.
            fsWatch = new FileSystemWatcher(tmpFilesDir);
            fsWatch.Changed += async (sender, e) =>
            {
                if (currentSPC.Subfiles.Select(x => x.Name).Contains(e.Name))
                {
                    // Wait for file to become unlocked...
                    while (true)
                    {
                        try
                        {
                            fsWatch.EnableRaisingEvents = false;
                            await currentSPC.InsertSubfileAsync(e.FullPath);
                            if (!Directory.Exists(tmpFilesDir))
                            {
                                return;
                            }
                            fsWatch.EnableRaisingEvents = true;
                        }
                        catch (IOException)
                        {
                            await Task.Delay(50);
                        }
                    }
                }
            };
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

            LoadSpc(dialog.FileName);
        }

        private void LoadSpc(string path)
        {
            currentSPC = new SpcFile();
            currentSPCFilename = path;
            currentSPC.Load(currentSPCFilename);
            statusText.Text = $"{currentSPCFilename} loaded.";

            SubFileListView.ItemsSource = currentSPC.Subfiles;
            InitFileSystemWatcher();
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
            RefreshListViewItems();
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

        private void OpenInEditor(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("test");
            if (SubFileListView.SelectedItems.Count > 1 &&
                MessageBox.Show(
                    "You're trying to open multiple files in editors. This could cause a large number of windows to be created. Are you sure?",
                    "Open Files in Editor", MessageBoxButton.YesNo, MessageBoxImage.None) != MessageBoxResult.Yes)
            {
                return;
            }

            fsWatch.EnableRaisingEvents = false;

            foreach (var subfile in SubFileListView.SelectedItems.Cast<SpcSubfile>())
            {
                string ext = Path.GetExtension(subfile.Name).ToLower();
                if (!new[] { ".sfl", ".wrd" }.Contains(ext))
                {
                    MessageBox.Show($"{subfile.Name} does not currently have a supporting editor.");
                    continue;
                }
                // We extract the file and store it in a temporary location so we can
                // pass it to the appropriate editor.
                try
                {
                    currentSPC.ExtractSubfile(subfile.Name, tmpFilesDir);
                }
                catch (IOException)
                {
                    MessageBox.Show("An IOException occurred. Try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                string tmpFilePath = Path.Combine(tmpFilesDir, subfile.Name);

                try
                {
                    // Ideally, there could be some kind of config for where the other editors are located.
                    // Requiring them to be in the same directory is not really ideal.
                    Process process = ext switch
                    {
                        ".sfl" => Process.Start("SflEditor.exe", tmpFilePath),
                        ".wrd" => Process.Start("WrdEditor.exe", tmpFilePath),
                        _ => null! // This should never be reached!
                    };
                    // But in the rare event that the editor extension list doesn't match up with the switch expression,
                    // let's add a fallback just in case.
                    if (process == null)
                    {
                        MessageBox.Show($"This application isn't properly configured to use the {ext} editor. Please contact a developer.",
                            ":(", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        Task.Delay(10).ContinueWith(_ =>
                        {
                            if (process.HasExited)
                            {
                                // Some files are not present.
                                MessageBox.Show($"The editor for {subfile.Name} is present, but it's missing some necessary files.");
                            }
                        });
                    }
                }
                catch (Win32Exception)
                {
                    // If the desired editor isn't present, catch that and open a messagebox
                    MessageBox.Show($"An editor exists for {subfile.Name}, but this application can't find its executable.");
                }
            }

            fsWatch.EnableRaisingEvents = true;
        }
    }

    public static class SpcEditorCommands
    {
        public static RoutedCommand SaveAs = new RoutedCommand("Save As", typeof(SpcEditorCommands), new InputGestureCollection {
            new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
        });
        public static RoutedCommand Insert = new RoutedCommand("Insert", typeof(SpcEditorCommands));
        public static RoutedCommand Extract = new RoutedCommand("Extract", typeof(SpcEditorCommands));
        public static RoutedCommand ExtractAll = new RoutedCommand("Extract All", typeof(SpcEditorCommands));
        public static RoutedCommand OpenInEditor = new RoutedCommand("Open in Editor", typeof(SpcEditorCommands));
    }

    public sealed class CompressionStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (short)value switch
            {
                1 => "Uncompressed",
                2 => "Compressed",
                3 => "N/A (External)", // I don't know if this is still accurate :P
                short x => $"N/A (Unknown: {x})"
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException("This is a compression state -> string is a one-way operation!");
    }
}
