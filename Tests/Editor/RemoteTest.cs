using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yanmonet.Git.Tests
{
    public class RemoteTest
    {
        GitRepository git;
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
        public void Tags()
        {
            var tags = git.GetTags();
            Debug.Log(string.Join("\n", tags));
        }

        [Test]
        public void GetDefaultRemote()
        {
            string remote = git.GetDefaultRemote();
            Assert.IsNotNull(remote);
            Debug.Log($"Default Remote: {remote}");
        }

        [Test]
        public void GetRemotes()
        {
            string[] remotes = git.GetRemotes();
            Debug.Log("Remotes:\n" + string.Join("\n", remotes));
        }

    }
}
