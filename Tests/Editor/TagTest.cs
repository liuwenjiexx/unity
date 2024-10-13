using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Yanmonet.Git.Tests
{
    public class TagTest
    {
        GitRepository git;
        public string file2 => GitTest.file2;
        string testCreateTag = "test-tag-create";
        string testDeleteTag = "test-tag-delete";

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
        public void GetTags()
        {
            var tags = git.GetTags();
            Assert.Greater(tags.Length, 0);
            Debug.Log(string.Join("\n", tags));
        }

        [Test]
        public void GetTagInfo()
        {
            var tag = git.GetTags().FirstOrDefault();

            var tagInfo = git.GetTagInfo(tag);
            Debug.Log(tagInfo);
        }

        [Test]
        public void CreateTag()
        {
            Assert.IsFalse(git.HasTag(testCreateTag));
            git.CreateTag(testCreateTag);
            Assert.IsTrue(git.HasTag(testCreateTag));
            git.DeleteTag(testCreateTag);
        }

        [Test]
        public void DeleteTag()
        {
            git.CreateTag(testDeleteTag);
            Assert.IsTrue(git.HasTag(testDeleteTag));
            git.DeleteTag(testDeleteTag);
            Assert.IsFalse(git.HasTag(testDeleteTag));
        }


        [Test]
        public void LatestVersion()
        {
            var tag = git.GetTags("v*").FirstOrDefault();
            Assert.IsNotNull(tag);
            Debug.Log($"Tag: {tag}");
            Assert.IsTrue(Version.TryParse(tag.Substring(1), out var version));
            Debug.Log($"Version: {version}");
        }


        [Test]
        public void CreateVersionTag()
        {
            var tag = git.GetTags("v*").FirstOrDefault();
            Version.TryParse(tag.Substring(1), out var version);
            Debug.Log($"Old Tag: {tag}");

            var newVersion = new Version(version.Major, version.Minor, version.Build + 1);

            string newTag = "v" + newVersion.ToString();
            Debug.Log($"New Tag: {newTag}");

            git.CreateTag(newTag);

            Assert.IsNotNull(git.GetTagInfo(newTag));
        }

    }
}
