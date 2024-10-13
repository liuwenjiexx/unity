using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Yanmonet.Git.Tests
{
    public class BranchTest
    {
        GitRepository git;
        string branch1 = "branch1";
        string branchTrack = "track_branch";
        string branchDelete = "delete_branch";
        string remoteBranch = "master";


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
        public void CurrentBranch()
        {
            string branch = git.GetCurrentBranch();
            Debug.Log(branch);
        }

        [Test]
        public void CreateBranch()
        {
            git.CreateBranch(branch1);
            Assert.Contains(branch1, git.GetBranchs());
        }
        [Test]
        public void CreateTrackBranch()
        {
            string remote = git.GetRemotes()[0];
            git.CreateBranch(branchTrack, trackedBranch: GitUtility.Combine(remote, remoteBranch));
        }
        [Test]
        public void TrackBranch()
        {
            string remote = git.GetRemotes()[0];
            git.TrackBranch(branch1, trackedBranch: GitUtility.Combine(remote, remoteBranch));
        }
        [Test]
        public void CheckoutRemote()
        {
            string remote = git.GetRemotes()[0];
            git.Checkout(GitUtility.Combine(remote, remoteBranch));
        }

        [Test]
        public void CheckoutLocal()
        {
            git.Checkout(branch1);
        }

        [Test]
        public void DeleteBranch()
        {
            if (!git.HasBranch(branchDelete))
            {
                git.CreateBranch(branchDelete);
            }
            git.DeleteBranch(branchDelete, true);
            Assert.IsFalse(git.HasBranch(branchDelete));
        }

        [Test]
        public void GetBranchs()
        {
            foreach (var branch in git.GetBranchs())
            {
                Debug.Log(branch);
            }
        }

        [Test]
        public void GetRemoteBranchs()
        {
            foreach (var branch in git.GetRemoteBranchs())
            {
                Debug.Log(branch);
            }
        }
        [Test]
        public void GetAllRefBranchs()
        {
            foreach (var branch in git.GetAllRefBranchs())
            {
                Debug.Log(branch);
            }
        }

        //[Test]
        //public void GetRemoteBranchs()
        //{
        //    foreach (var branch in git.GetRemoteBranchs())
        //    {
        //        Debug.Log(branch);
        //    }
        //}

        [Test]
        public void GetRemoteRefBranchs()
        {
            foreach (var branch in git.GetRemoteRefBranchs())
            {
                Debug.Log(branch);
            }
        }

    }
}
