﻿
using System;
using System.IO;
using CommandLine;
using SonarrAuto.Logging;

namespace SonarrAuto
{
    class Program
    {

        public class Options
        {
            [Option('v', "verbose", HelpText = "Run logging in Verbose Mode")]
            public bool Verbose { get; set; }
            
            [Option('c', "copy", Required = false, Default = false, HelpText = "Move or copy")]
            public bool ImportMode { get; set; }
            
            [Option('d', "dry-run", Required = false, Default = false, HelpText = "Dry run - change nothing.")]
            public bool DryRun { get; set; }

            [Option('t', "timeout", Required = false, Default = 1000, HelpText = "Timeout between requests")]
            public int Timeout { get; set; }            

            [Value(0, MetaName = "Settings Path", HelpText = "Path to settings JSON file (default = app dir)", Required = false)]
            public string SettingsPath { get; set; } = "Settings.json";
        };

        static void Main(string[] args)
        {
            LogHandler.InitLogs();

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed( o => { RunProcess(o); });
        }

        private static void RunProcess(Options o)
        {
            var settings = Settings.Read(o.SettingsPath);

            if (settings != null)
            {
                var importer = new Importer();

                if (settings.sonarr != null)
                {
                    Logging.LogHandler.Log("Processing videos for Sonarr...");
                    importer.ProcessService(settings.sonarr, o.DryRun, o.Verbose, "DownloadedEpisodesScan", o.ImportMode, o.Timeout);
                }
                if (settings.radarr != null)
                {
                    Logging.LogHandler.Log("Processing videos for Radarr...");
                    importer.ProcessService(settings.radarr, o.DryRun, o.Verbose, "DownloadedMoviesScan",o.ImportMode, o.Timeout);
                }
            }
            else
                Logging.LogHandler.LogError($"Settings not found: {o.SettingsPath}");
        }
    }
}
