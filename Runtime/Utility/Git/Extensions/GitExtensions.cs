using System;
using Utility.Process.Extensions;
using UnityEngine;

namespace Utility.Git.Extensions
{
    public static class GitExtensions
    {
        public static string CommitHash => Run(@"rev-parse --short HEAD");
        public static string FullCommitHash => Run(@"rev-parse HEAD");
        public static string Branch => Run(@"rev-parse --abbrev-ref HEAD");
        public static string NumberOfCommits => Run(@"rev-list --count ..HEAD");
        public static string NumberOfCommitsSinceTag => Run(@"rev-list --count " + LatestTag + "..HEAD");
        public static string LatestTag => Run(@"describe --abbrev=0 --tags");

        public static string GetFullCommitHash()
        {
            string hash;
            try
            {
                hash = FullCommitHash;
            }
            catch (GitException)
            {
                hash = CommitHash;
            }

            return hash;
        }

        public static string GetBranchName()
        {
            string name;
            try
            {
                name = Branch;
            }
            catch (GitException)
            {
                name = "Default";
            }

            return name;
        }

        public static string GetCommitHash()
        {
            string hash;
            try
            {
                hash = CommitHash;
            }
            catch (GitException)
            {
                hash = string.Empty;
            }

            return hash;
        }

        public static string GetNumberOfCommits()
        {
            string number;
            try
            {
                number = NumberOfCommitsSinceTag;
            }
            catch (GitException)
            {
                try
                {
                    number = NumberOfCommits;
                }
                catch (GitException)
                {
                    number = "0";
                }
            }

            return number;
        }

        public static string Run(string arguments)
        {
            using var process = new System.Diagnostics.Process();
            var exitCode = process.Run(@"git", arguments, Application.dataPath, out var output, out var errors);
            if (exitCode == 0)
            {
                return output;
            }

            throw new GitException(exitCode, errors);
        }
    }

    public class GitException : InvalidOperationException
    {
        public readonly int exitCode;

        public GitException(int exitCode, string errors) : base(errors)
        {
            this.exitCode = exitCode;
        }
    }
}
