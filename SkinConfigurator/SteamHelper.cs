﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

#pragma warning disable CA1416 // Validate platform compatibility

namespace SkinConfigurator
{
    /// <summary>
    /// Based on: https://stackoverflow.com/questions/54767662/finding-game-launcher-executables-in-directory-c-sharp
    /// </summary>
    internal static class SteamHelper
    {
        private static readonly string[] _registryKeys = new[] { "SOFTWARE\\Wow6432Node\\Valve\\", "SOFTWARE\\VALVE\\" };
        private static readonly List<string> _steamGameDirs;

        static SteamHelper()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _steamGameDirs = new List<string>();
                return;
            }

            _steamGameDirs = _registryKeys
                .Select(v => Registry.LocalMachine.OpenSubKey(v))
                .Where(registryKey => registryKey != null)
                .SelectMany(
                    registryKey =>
                    {
                        using (registryKey)
                        {
                            return GetDirectories(registryKey!).ToArray();
                        }
                    }
                )
                .Distinct()
                .ToList();
        }

        private static IEnumerable<string> GetDirectories(RegistryKey registryKey)
        {
            foreach (var subKeyName in registryKey.GetSubKeyNames())
            {
                using var subKey = registryKey.OpenSubKey(subKeyName);
                if (subKey == null)
                {
                    continue;
                }

                var installPath = subKey.GetValue("InstallPath");
                if (installPath == null)
                {
                    continue;
                }

                var steamPath = installPath.ToString();
                var configPath = $"{steamPath}/steamapps/libraryfolders.vdf";
                const string driveRegex = @"[A-Z]:\\";
                if (!File.Exists(configPath))
                {
                    continue;
                }

                var configLines = File.ReadAllLines(configPath);
                foreach (var item in configLines)
                {
                    var match = Regex.Match(item, driveRegex);
                    if (item == string.Empty || !match.Success)
                    {
                        continue;
                    }

                    var matched = match.ToString();
                    var item2 = item.Substring(item.IndexOf(matched, StringComparison.Ordinal));
                    item2 = item2.Replace("\\\\", "\\");
                    item2 = item2.Replace("\"", "\\steamapps\\common\\");
                    yield return item2;
                }

                yield return $"{steamPath}\\steamapps\\common\\";
            }
        }

        private const string DV_FOLDER_NAME = "Derail Valley";
        private const string DV_MOD_FOLDER_NAME = "Mods";

        public static string? GetModsDirectory()
        {
            string? dvDir = _steamGameDirs
                .Select(v => Path.Combine(v, DV_FOLDER_NAME))
                .Where(v => Directory.Exists(v))
                .FirstOrDefault();

            if (dvDir != null)
            {
                string modDir = Path.Combine(dvDir, DV_MOD_FOLDER_NAME);
                if (Directory.Exists(modDir))
                {
                    return modDir;
                }
            }
            return dvDir;
        }
    }

#pragma warning restore CA1416 // Validate platform compatibility
}
