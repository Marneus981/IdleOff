using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace IdleOff.Editor
{
    public sealed class TableBuildPostprocessor : IPostprocessBuildWithReport
    {
        private const string SourceTablesPath = "Assets/Tables";

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

            var targetTablesPath = Path.Combine(outputDirectory, SourceTablesPath);
            Directory.CreateDirectory(targetTablesPath);

            foreach (var sourcePath in Directory.GetFiles(SourceTablesPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(sourcePath);
                File.Copy(sourcePath, Path.Combine(targetTablesPath, fileName), true);
            }
        }
    }
}
