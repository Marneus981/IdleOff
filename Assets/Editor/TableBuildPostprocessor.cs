using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace IdleOff.Editor
{
    public sealed class TableBuildPostprocessor : IPostprocessBuildWithReport
    {
        private const string SourceTablesPath = "Assets/Tables";
        private const string SourceArtPath = "Assets/Art";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null || string.IsNullOrWhiteSpace(report.summary.outputPath) || !Directory.Exists(SourceTablesPath))
            {
                return;
            }

            var outputDirectory = File.Exists(report.summary.outputPath)
                ? Path.GetDirectoryName(report.summary.outputPath)
                : report.summary.outputPath;
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return;
            }

            CopyJsonTables(outputDirectory);
            CopyArtPngs(outputDirectory);
        }

        private static void CopyJsonTables(string outputDirectory)
        {
            var targetTablesPath = Path.Combine(outputDirectory, SourceTablesPath);
            Directory.CreateDirectory(targetTablesPath);

            foreach (var sourcePath in Directory.GetFiles(SourceTablesPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(sourcePath);
                File.Copy(sourcePath, Path.Combine(targetTablesPath, fileName), true);
            }
        }

        private static void CopyArtPngs(string outputDirectory)
        {
            if (!Directory.Exists(SourceArtPath))
            {
                return;
            }

            foreach (var sourcePath in Directory.GetFiles(SourceArtPath, "*.png", SearchOption.AllDirectories))
            {
                var relativePath = sourcePath.Replace('\\', '/');
                var targetPath = Path.Combine(outputDirectory, relativePath);
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath, true);
            }
        }
    }
}
