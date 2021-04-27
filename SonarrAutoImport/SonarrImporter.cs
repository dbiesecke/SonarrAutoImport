﻿using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using RestSharp;
using System;
using System.Threading;

namespace SonarrAuto
{
    public class Importer
    {
        private static string[] movieExtensions = {
                ".mkv", ".avi", ".wmv", ".mov", ".amv",
                ".mp4", ".m4a", ".m4v", ".f4v", ".f4a", ".m4b", ".m4r", ".f4b",
                ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv" , ".ts"
            };

        [DataContract(Name = "payload")]
        public class PayLoad
        {
            public string path;
            public string name; // API command name
            public string importMode = "Move";
            public string downloadClientId = "Generic";
        }

        [DataContract(Name = "transform")]
        public class SonarrTransform
        {
            public string search;
            public string replace;
        }

        private void Log(string fmt, params object[] args)
        {
            Logging.LogHandler.Log(fmt, args);
        }

        private void LogError(string fmt, params object[] args)
        {
            Logging.LogHandler.LogError(fmt, args);
        }

        private string TransformFileName(List<Transform> transforms, string path, bool verbose)
        {
            string fName = Path.GetFileName(path);
            string newfName = fName;

            if (transforms != null && transforms.Any())
            {
                Log($" Running {transforms.Count()} transforms on {path}...");

                foreach (var transform in transforms)
                {
                    newfName = Regex.Replace(newfName, transform.search, transform.replace);
                    Logging.LogHandler.LogVerbose(" - Transform {0} => {1}", fName, newfName);
                }

                if (string.Compare(fName, newfName, StringComparison.OrdinalIgnoreCase) != 0)
                    Log("Filename transformed: {0} => {1}", fName, newfName);
            }
            else
                Log("No transforms configured.");
            return newfName;
        }

        private string MoveFile(string fullPathName, string newFileName)
        {
            string folder = Path.GetDirectoryName(fullPathName);
            string oldFileName = Path.GetFileName(fullPathName);

            if (string.Compare(oldFileName, newFileName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string newPath = Path.Combine(folder, newFileName);
                Log("Transforming file '{0}' to '{1}'", oldFileName, newFileName);
                try
                {
                    File.Move(fullPathName, newPath, false);
                    return newPath;
                }
                catch (Exception ex)
                {
                    LogError("Unable to rename file {0}: {1}", fullPathName, ex.Message);
                }
            }

            return fullPathName;
        }

        public void ProcessService( ServiceSettings settings, bool dryRun, bool verbose, string apiCommand, bool import_mymode, int timeout)
        {
            DirectoryInfo baseDir = new DirectoryInfo(settings.downloadsFolder);

            Log("Starting video processing for: {0}", baseDir);
            if (verbose)
            {
                Log(" Base Url:   {0}", settings.url);
                Log(" API Key:    {0}", settings.apiKey);
                Log(" Mapping:    {0}", settings.mappingPath);
                Log(" Dry Run:    {0}", dryRun);
            }

            if (baseDir.Exists)
            {
                var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).ToList();
                var movieFiles = allFiles.Where(x => movieExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase))
                                        .Where(x => !x.Name.Contains(".partial.", StringComparison.OrdinalIgnoreCase))
                                        .ToList();

                if (movieFiles.Any())
                {
                    Log("Processing {0} video files...", movieFiles.Count());

                    foreach (var file in movieFiles)
                    {
                        string videoFullPath = file.FullName;

                        string newFileName = TransformFileName(settings.transforms, videoFullPath, verbose);
                        
                        if (!dryRun)
                        {
                            videoFullPath = MoveFile(file.FullName, newFileName);
                        }
                        
                        if (!dryRun)
                        {
                            videoFullPath = MoveFile(file.FullName, newFileName);
                        }

                        string path = TranslatePath(settings.downloadsFolder, videoFullPath, settings.mappingPath);

                        if (!dryRun)
                        {
                            QuickImport(path, settings, verbose, apiCommand,import_mymode,timeout);
                        }
                        else
                            Log(" => {0}", path);
                    }

                    Log("All processing complete.");
                }
                else
                    Log("No videos found. Nothing to do!");
            }
            else
                Log($"Folder {baseDir} was not found. Check configuration.");
        }

        private string TranslatePath(string baseFolder, string fullName, string mapFolder)
        {
            string path = Path.GetFullPath(fullName);
            string localPath = path.Remove(0, baseFolder.Length);
            if (localPath.StartsWith(Path.DirectorySeparatorChar))
                localPath = localPath.Remove(0, 1);
            return Path.Combine(mapFolder, localPath);
        }

        private void QuickImport(string remotePath, ServiceSettings service, bool verbose, string apiCommand , bool mymode , int timeout)
        {
            Log($"Sleeping some seconds...");
            Thread.Sleep(timeout);
            try
            {
                RestClient client = new RestClient(service.url);
//                 
                    var payload = new PayLoad { path = remotePath, name = apiCommand};
                    if (mymode) {
                        payload = new PayLoad { path = remotePath, name = apiCommand , importMode = "Copy"};
                    }
//                 }else {
//                     var payload = new PayLoad { path = remotePath, name = apiCommand , importMode = "Copy"};
                    
//                 }

                var request = new RestRequest(Method.POST);

                request.Resource = "api/command";
                request.RequestFormat = DataFormat.Json;
                request.AddJsonBody(payload);
                request.AddHeader("User-Agent", "generic");
                request.AddHeader("X-Api-Key", service.apiKey);

                var response = client.Execute(request);
                Log(" - Executed Service command for {0}", remotePath);
                if (verbose)
                    Log(response.Content);
            }
            catch (Exception e)
            {
                LogError("Exception: {0}", e);
            }
        }
    }
}
