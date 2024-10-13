using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


namespace Yanmonet.Git.Tests
{
    public class GitTest
    {
        public static string repoPath = "test2";
        public static string testDir = "git_test";
        GitRepository git;
        public static string file2 = "git_test/file2.txt";

        [SetUp]
        public void SetUp()
        {
            git = new GitRepository(repoPath);

        }


        [TearDown]
        public void TearDown()
        {
            git?.Dispose();
            git = null;
        }

        [Test]
        public void IsInsideRepositoryDir()
        {
            Assert.IsTrue(GitUtility.IsInsideRepositoryDir(repoPath));
            Assert.IsTrue(GitUtility.IsInsideRepositoryDir(Path.Combine(repoPath, testDir)));
        }
        [Test]
        public void GetRepositoryRoot()
        {
            string fullRepoPath = Path.GetFullPath(repoPath);
            fullRepoPath=GitUtility.NormalPath(fullRepoPath);
            string root = GitUtility.GetRepositoryRoot(repoPath);
            root = GitUtility.NormalPath(root);
            Debug.Log(root);
            Assert.AreEqual(fullRepoPath, root);

            root = GitUtility.GetRepositoryRoot(Path.Combine(repoPath, testDir));
            root = GitUtility.NormalPath(root);
            Debug.Log(root);
            Assert.AreEqual(fullRepoPath, root);
        }

        [Test]
        public void IsInsideGitDir()
        {
            Assert.IsFalse(GitUtility.IsInsideGitDir(Path.Combine(repoPath)));
            Assert.IsTrue(GitUtility.IsInsideGitDir(Path.Combine(repoPath, ".git")));
        }

        [Test]
        public void Fetch()
        {

            git.Fetch();
        }

        [Test]
        public void FetchAll()
        {
            git.FetchAll();
        }


        [Test]
        public void Diff()
        {
            git.GetDiff(out var modifieds, out var deleteds, out var untrackeds);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Modified:");
            foreach (var modified in modifieds)
            {
                builder.AppendLine($"    {modified}");
            }
            builder.AppendLine($"Deleted:");
            foreach (var deleted in deleteds)
            {
                builder.AppendLine($"    {deleted}");
            }
            builder.AppendLine($"Untracked:");
            foreach (var untracked in untrackeds)
            {
                builder.AppendLine($"    {untracked}");
            }
            Debug.Log(builder.ToString());
        }


    }
}