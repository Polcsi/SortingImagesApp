using System;
using System.Threading;
using System.IO;
using System.Timers;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace SortingImagesApp
{
    public class Change
    {
        private string _path;
        private FileSystemWatcher watcher;
        private static System.Timers.Timer _timer;
        private static int elapsedSeconds = 1;
        private static readonly int elapsedMinimumSec = 1;
        private static readonly int waitTimeInSec = 5;
        private static readonly string logFileName = "log.txt";
        private string Path
        {
            get { return _path; }
            set
            {
                if (!Directory.Exists(value))
                {
                    string newPath = @"C:\Image Sorting";
                    Directory.CreateDirectory(newPath);
                    _path = newPath;
                }
                else
                {
                    _path = value;
                }
            }
        }
        public Change(string path)
        {
            Path = path;
            watcher = new FileSystemWatcher(Path);
            setTimer();
            start();
        }
        public void Stop()
        {
            logMessage("Directory watching is stopped.");
            watcher.Dispose();
            _timer.Dispose();
        }
        private void setTimer()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, Path);
            _timer.AutoReset = true;
        }

        private static void OnTimedEvent(object sender, ElapsedEventArgs e, string path)
        {
            if (elapsedSeconds == waitTimeInSec)
            {
                _timer.Stop();
                _timer.Close();
                Thread startThread = new Thread(() =>
                {
                    SortingCreatedImages(path);
                });
                startThread.Start();
            }
            logMessage($"{elapsedSeconds}");
            elapsedSeconds++;
        }

        private void start()
        {
            logMessage($"Directory watching started... here: {Path}");

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Deleted += OnDelete;
            watcher.Created += OnCreated;

            watcher.Filter = "";
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;
        }
        public static void logMessage(string message)
        {
            DateTime now = DateTime.Now;
            string strToLog = $"{now.ToString("yyyy/MM/dd HH:mm:ss")} - {message}";
            Console.WriteLine(strToLog);
            try
            {
                using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\" + logFileName, true))
                {
                    writer.WriteLine(strToLog);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{now.ToString("yyyy/MM/dd HH:mm:ss")} - {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            logMessage($"Created: {e.FullPath}");

            elapsedSeconds = elapsedMinimumSec;
            _timer.Start();
        }

        private void OnDelete(object sender, FileSystemEventArgs e)
        {
            _timer.Stop();
            logMessage($"Deleted: {e.FullPath}");
            elapsedSeconds = elapsedMinimumSec;
            _timer.Start();
        }
        private static void SortingCreatedImages(string path)
        {
            string supportedExtensions = "*.jpg,*.gif,*.png,*.bmp,*.jpe,*.jpeg,*.tiff,*.psd,*.pdf,*.eps,*.ai,*.raw,*.ico,*.svg,*.webp,*.mp3,*.mp4,*.m4p,*.m4v,*.mov,*.mkv,*.avim,*.mpg,*.mpeg,*.m4v,*.wmv,*.doc,*.docm,*.docx,*.dot,*.dotm,*dotx,*.rtf,*.txt,*.wps,*.xml,*.csv,*.dpf,*.dif,*.html,*.xla,*.xlam,*.xls,*.xlsb,*.xlsm,*.xlsx,*.xlt,*.xltm,*.xltx,*.xps,*.bmp,*.emf,*.odp,*.pot,*.potm,*.potx,*.ppa,*.ppam,*.pps,*.ppsm,*.ppsx,*.ppt,*.pptm,*.pptx,*.thmx,*.wmf,*.xps,*.zip,*.rar,*.exe,*.msi,*.css,*.js";
            IEnumerable<string> filenames = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s)));

            foreach (string file in filenames)
            {
                FileInfo fi = null;
                try
                {
                    fi = new FileInfo(file);
                }
                catch (FileNotFoundException e)
                {
                    logMessage(e.Message);
                    continue;
                }

                string date = fi.LastWriteTime.ToString("yyyy-MM-dd");
                string newPath = $"{path}\\{date}";
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }

                try
                {
                    string destinationPath = $"{newPath}\\{fi.Name}";
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                    File.Move(file, destinationPath);
                }
                catch (IOException ex)
                {
                    logMessage(ex.Message);
                }
                catch (Exception ex)
                {
                    logMessage(ex.ToString());
                }
            }
        }
    }
}
