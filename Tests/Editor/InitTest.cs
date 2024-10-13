using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Yanmonet.Git.Tests
{
    public class InitTest : MonoBehaviour
    {
        GitRepository git;
        string initRepoPath = "Temp/test_git_init";
        string cloneRepoPath = "Temp/test_git_clone";

        [SetUp]
        public void SetUp()
        {
            

        }


        [TearDown]
        public void TearDown()
        {
       
        }

        [Test]
        public void Init()
        {
            using (var git = new GitRepository(initRepoPath))
            {
                git.Initialize();
                Assert.IsTrue(Directory.Exists(Path.Combine(git.Path, GitUtility.GitFolderName)));
                Directory.Delete(git.Path, true);
            }
        }

        [Test]
        public void Clone()
        {
            using (var git = new GitRepository(cloneRepoPath))
            {
                string repoUrl = "git@gitlab.yanmonet.com:cicd/testnpm.git";
                git.Clone(repoUrl);
                Assert.IsTrue(Directory.Exists(Path.Combine(git.Path, GitUtility.GitFolderName)));
                Directory.Delete(git.Path, true);
            }
        }

        [Test]
        public void AddRemote()
        {
            string remote = "remote1";
            string url = "git@xxx.com/xxx.git";
            git.AddRemote(remote, url);

            Assert.Contains(remote, git.GetRemotes());
        }


        [Test]
        public void GetRemotes()
        {
            foreach (var remote in git.GetRemotes())
            {
                Debug.Log(remote);
            }
        }

    }
}