/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

using System.Diagnostics;
using System.Text;

namespace Editor.Build_Wizard.Process.Extensions
{
    public static class ProcessExtensions
    {
        public static int Run(this System.Diagnostics.Process process, string application, string arguments,
            string workingDirectory,
            out string output, out string errors)
        {
            CreateProcess(process, application, arguments, workingDirectory, true, false);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
            process.ErrorDataReceived += (_, args) => errorBuilder.AppendLine(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            output = outputBuilder.ToString().TrimEnd();
            errors = errorBuilder.ToString().TrimEnd();
            return process.ExitCode;
        }

        public static void Run(this System.Diagnostics.Process process, string application, string arguments,
            string workingDirectory)
        {
            CreateProcess(process, application, arguments, workingDirectory, false, false);
            process.Start();
        }

        private static void CreateProcess(this System.Diagnostics.Process process, string application,
            string arguments,
            string workingDirectory, bool awaitCompletion, bool useShell)
        {
            process.EnableRaisingEvents = true;

            process.StartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = useShell,
                RedirectStandardError = awaitCompletion,
                RedirectStandardOutput = awaitCompletion,
                FileName = application,
                Arguments = arguments,
                WorkingDirectory = workingDirectory
            };
        }
    }
}