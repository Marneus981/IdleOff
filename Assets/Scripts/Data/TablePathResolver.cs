using System;
using System.IO;
using UnityEngine;

namespace IdleOff.Data
{
    public static class TablePathResolver
    {
        public static string Resolve(string tablePath)
        {
            if (string.IsNullOrWhiteSpace(tablePath))
            {
                throw new ArgumentException("Table path cannot be empty.", nameof(tablePath));
            }

            if (Path.IsPathRooted(tablePath))
            {
                return tablePath;
            }

            var currentDirectoryPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), tablePath));
            if (File.Exists(currentDirectoryPath))
            {
                return currentDirectoryPath;
            }

            var playerRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", tablePath));
            if (File.Exists(playerRootPath))
            {
                return playerRootPath;
            }

            return currentDirectoryPath;
        }
    }
}
