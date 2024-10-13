using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Yanmonet.Git.Tests
{
    public class ResetTest
    {
        GitRepository git;
        public string file2 => GitTest.file2;
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
        public void ResetFile()
        {
            string fullPath = Path.Combine(git.Path, file2);

            string text = File.ReadAllText(fullPath, Encoding.UTF8);
            text += "\ntest";
            File.WriteAllText(fullPath, text, Encoding.UTF8);

            Assert.IsTrue(git.GetChangedFiles().Contains(file2));
            git.ResetFile(file2);
            Assert.IsFalse(git.GetChangedFiles().Contains(file2));
        }

    }
}
