using System;
using System.Threading;
using System.IO;
using System.Timers;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SortingImagesApp
{
    public class Change
    {
        public Settings Settings { get; private set; }
        private FileSystemWatcher watcher;
        private static System.Timers.Timer _timer;
        private static int elapsedSeconds = 1;
        private static readonly int elapsedMinimumSec = 1;
        private static readonly int waitTimeInSec = 5;
        private static string logFileName;
        private readonly string defaultPath;
        private readonly string selectedPath;
        private readonly bool includeSubdirectories;
        private static string[] supportedExtensions;
        private string _path;
        private string Path
        {
            get { return _path; }
            set
            {
                if (!Directory.Exists(value))
                {
                    Directory.CreateDirectory(defaultPath);
                    _path = defaultPath;
                }
                else
                {
                    _path = value;
                }
            }
        }
        public Change()
        {
            Settings = LoadJson("settings.json");
            selectedPath = Settings.selectedPath;
            defaultPath = Settings.defaultPath;
            logFileName = Settings.logFileName;
            includeSubdirectories = Settings.includeSubdirectories;
            supportedExtensions = Settings.supportedExtensions;
            for (int i = 0; i < supportedExtensions.Length; ++i)
            {
                supportedExtensions[i] = $"*{supportedExtensions[i]}";
            }

            Path = selectedPath;
            watcher = new FileSystemWatcher(Path);
            setTimer();
            Start();
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

        private void Start()
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
            watcher.IncludeSubdirectories = includeSubdirectories;
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
                Console.WriteLine($"{now.ToString("yyyy/MM/dd HH:mm:ss")} - {ex.Message}");
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            elapsedSeconds = elapsedMinimumSec;
            _timer.Stop();
            logMessage($"Created: {e.FullPath}");

            elapsedSeconds = elapsedMinimumSec;
            FileInfo fileInfo = new FileInfo(e.FullPath);
            while(IsFileLocked(fileInfo))
            {
                Thread.Sleep(500);
            }
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
            string extensionsString = "";
            foreach (string ext in supportedExtensions)
            {
                extensionsString += $"{ext},";
            }
            extensionsString = extensionsString.Substring(0, extensionsString.Length - 1);
            IEnumerable<string> filenames = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(s => extensionsString.Contains(System.IO.Path.GetExtension(s)));

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
        static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                /*return true;*/
                logMessage(ex.Message);
            }
            finally
            {
                if(stream != null)
                {
                    stream.Close();
                }
            }
            return false;
        }
        private Settings LoadJson(string path)
        {
            List<Settings> settings = new List<Settings>();
            using (StreamReader reader = new StreamReader(path))
            {
                string json = reader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<List<Settings>>(json);
            }

            return settings[0];
        }
    }
}
