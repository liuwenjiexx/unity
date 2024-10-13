using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Yanmonet.Git.Tests
{
    public class CommitTest
    {
        GitRepository git;
        string testFile = "git_test/git_test.txt";

        [SetUp]
        public void SetUp()
        {
            git = new GitRepository(GitTest.repoPath);
        }


        [TearDown]
        public void TearDown()
        {
            git?.Dispose();
            git = null;
        }

        [Test]
        public void HasChanged()
        {
            Debug.Log(git.HasChanged());
        }

        [Test]
        public void CommitChagned()
        {
            git.CommitChanged("Commit Chagned");
        }

        [Test]
        public void Commit()
        {
            string fullPath = Path.Combine(git.Path, testFile);

            string text = File.ReadAllText(fullPath);
            text += "\n1";
            File.WriteAllText(fullPath, text);

            git.AddToCommit(testFile);
            git.Commit($"Commit '{testFile}'");
        }

        [Test]
        public void AmendPreviousCommit()
        {
            git.AmendPreviousCommit("Amend Previous Commit");
        }




        [Test]
        public void GetCommit()
        {
            var commit = git.GetCommit();
            Debug.Log(commit);
        }
        [Test]
        public void GetCommitId()
        {
            var commit = git.GetCommitId();
            Debug.Log(commit);
        }


        [Test]
        public void GetCommitInfo_ShortId()
        {
            var commit = git.GetCommit("ce3462f");
            Debug.Log(commit);
        }

        [Test]
        public void GetCommits_Limit3()
        {
            var commits = git.GetCommits(limit: 3);
            foreach (var commit in commits)
            {
                Debug.Log(commit);
                Debug.Log(GitUtility.ToTimestamp(commit.time));
            }
            Assert.LessOrEqual(commits.Length, 3);
        }
        [Test]
        public void GetCommits_NowDay()
        {
            var now = DateTime.Now;
            var after = new DateTime(now.Year, now.Month, now.Day);
            Debug.Log("Date: " + GitUtility.ToDateString(after));
            var commits = git.GetCommits(after: after);
            foreach (var commit in commits)
            {
                Debug.Log(commit);
                Assert.AreEqual(after.Year, commit.time.Year);
                Assert.AreEqual(after.Month, commit.time.Month);
                Assert.AreEqual(after.Day, commit.time.Day);
            }
        }



    }
}
