using Serilog;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.IO.Compression;

namespace Kinderworx.Utilities.BuildUtilities
{
    public static class BuildUtils
    {
        /// <summary>
        /// Nuke build utilities.
        /// </summary>
        /// <returns></returns>
        public static string GetProjectName()
        {

            // Get the entry assembly (the assembly that contains the Main method)
            Assembly assembly = Assembly.GetEntryAssembly();

            // Get the full path of the assembly (including the file name)
            string assemblyPath = assembly.Location;

            // Get the file name (including the extension)
            string assemblyFileName = Path.GetFileName(assemblyPath);

            return assemblyFileName;
        }

        /// <summary>
        /// Replaces . for - to make a Sonarqube-compatible project key.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static string Helper(string projectName) => projectName.Replace(".", "-");


        /// <summary>
        /// Zips a specified path.
        /// </summary>
        public static void ZipDirectory(string directory, string zipPath)
        {
            if (Directory.Exists(directory))
            {
                ZipFile.CreateFromDirectory(directory, zipPath);
            }

            else
            {
                Directory.CreateDirectory(directory);
                throw new DirectoryNotFoundException();
            }


        }

        /// <summary>
        /// Returns the assembly informational version from Nerdbank.
        /// </summary>
        /// <param name="stdOutBuffer"></param>
        /// <param name="stdErrBuffer"></param>
        /// <returns></returns>
        public static string ExtractVersion(string stdOut)
        {

            var withoutSpeechMarks = stdOut.Replace("\"", "");

            // Remove square brackets
            var withoutSquareBrackets = withoutSpeechMarks.Replace("[", "")
                .Replace("]", "");

            var lines = withoutSquareBrackets.Split(',');

            string s = lines[2];

            string modifiedString = s.Replace("AssemblyInformationalVersion: ", string.Empty).Trim();

            return modifiedString;

        }
    }
}