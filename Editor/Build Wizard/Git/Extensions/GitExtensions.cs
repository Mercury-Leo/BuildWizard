/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

using System;
using System.Diagnostics;
using Build_Wizard.Process.Extensions;
using UnityEngine;

namespace Build_Wizard.Git.Extensions
{
    public static class GitExtensions
    {
        public static string CommitHash => Run(@"rev-parse --short HEAD");
        public static string FullCommitHash => Run(@"rev-parse HEAD");
        public static string Branch => Run(@"rev-parse --abbrev-ref HEAD");
        public static string NumberOfCommits => Run(@"rev-list --count ..HEAD");
        public static string NumberOfCommitsSinceTag => Run(@"rev-list --count " + LatestTag + "..HEAD");
        public static string LatestTag => Run(@"describe --abbrev=0 --tags");

        public static string GetNumberOfCommits()
        {
            string number;
            try
            {
                var tag = LatestTag;
                number = NumberOfCommitsSinceTag;
            }
            catch (GitException)
            {
                number = NumberOfCommits;
            }

            return number;
        }

        public static string Run(string arguments)
        {
            using var process = new System.Diagnostics.Process();
            var exitCode = process.Run(@"git", arguments, Application.dataPath, out string output, out string errors);
            if (exitCode == 0)
            {
                return output;
            }

            throw new GitException(exitCode, errors);
        }
    }

    public class GitException : InvalidOperationException
    {
        public readonly int ExitCode;

        public GitException(int exitCode, string errors) : base(errors)
        {
            ExitCode = exitCode;
        }
    }
}
