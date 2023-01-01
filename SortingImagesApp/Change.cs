using System;
using System.Threading;
using System.IO;
using System.Timers;

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
                if(!Directory.Exists(value))
                {
                    string newPath = @"C:\Image Sorting";
                    Directory.CreateDirectory(newPath);
                    _path = newPath;
                } else
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
        private void setTimer()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, Path);
            _timer.AutoReset= true;
        }

        private static void OnTimedEvent(object sender, ElapsedEventArgs e, string path)
        {
            if(elapsedSeconds == waitTimeInSec)
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

        private void start ()
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
                using (StreamWriter writer = new StreamWriter(logFileName, true))
                {
                    writer.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{now.ToString("yyyy/MM/dd HH:mm:ss")} - {ex.Message}");
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
            string[] filenames = Directory.GetFiles(path);

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
                if(!Directory.Exists(newPath))
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
