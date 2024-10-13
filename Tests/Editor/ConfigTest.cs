using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Yanmonet.Git.Tests
{
    public class ConfigTest  
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
        public void List()
        {
            StringBuilder builder = new StringBuilder();
            int total = 0;
            foreach (var item in git.GetConfigs())
            {
                builder.AppendLine($"[{item.Key}] = [{item.Value}]");
                total++;
            }

            Debug.Log($"Total: {total}");
            Debug.Log(builder.ToString());

        }

        [Test]
        public void List_Local()
        {
            StringBuilder builder = new StringBuilder();
            int total = 0;
            foreach (var item in git.GetConfigs(GitConfigScope.Local))
            {
                builder.AppendLine($"[{item.Key}] = [{item.Value}]");
                total++;
            }

            Debug.Log($"Total: {total}");
            Debug.Log(builder.ToString());
        }

        [Test]
        public void List_Global()
        {
            StringBuilder builder = new StringBuilder();
            int total = 0;
            foreach (var item in git.GetConfigs(GitConfigScope.Global))
            {
                builder.AppendLine($"[{item.Key}] = [{item.Value}]");
                total++;
            }

            Debug.Log($"Total: {total}");
            Debug.Log(builder.ToString());
        }

        [Test]
        public void List_System()
        {
            StringBuilder builder = new StringBuilder();
            int total = 0;
            foreach (var item in git.GetConfigs(GitConfigScope.System))
            {
                builder.AppendLine($"[{item.Key}] = [{item.Value}]");
                total++;
            }

            Debug.Log($"Total: {total}");
            Debug.Log(builder.ToString());
        }


        [Test]
        public void List_Worktree()
        {
            StringBuilder builder = new StringBuilder();
            int total = 0;
            foreach (var item in git.GetConfigs(GitConfigScope.Worktree))
            {
                builder.AppendLine($"[{item.Key}] = [{item.Value}]");
                total++;
            }

            Debug.Log($"Total: {total}");
            Debug.Log(builder.ToString());
        }


        [Test]
        public void HasKey()
        {
            string key = "user.name";
            Assert.IsTrue(git.HasConfig(key));
        }

        [Test]
        public void NotHasKey()
        {
            string key = "test.not-has-key";
            Assert.IsFalse(git.HasConfig(key));
        }


        [Test]
        public void Get_Scope()
        {
            Assert.IsTrue(git.TryGetConfigScope("user.name", out var scope));
            Assert.AreEqual(GitConfigScope.Global, scope);
        }


        [Test]
        public void Get_User_Name()
        {
            string value = git.GetString("user.name");
            Debug.Log($"[user.name] = [{value}]");
        }



        [Test]
        public void Set_String()
        {
            string key = "test.string-local";
            string value = "string-value";
            git.SetString(key, value);
            string value2 = git.GetString(key);
            Debug.Log($"[{key}] = [{value2}]");
            Assert.AreEqual(value, value2);
        }


        [Test]
        public void Set_String_Array()
        {
            string key = "test.array-string";
            string[] values = new string[] { "value1", "value2" };
            git.DeleteConfig(key);

            foreach (var value in values)
            {
                git.AddString(key, value);
            }

            string[] value2 = git.GetArray(key);
            Debug.Log($"[{key}] = [{string.Join(";", value2)}]");

            CollectionAssert.AreEqual(values, value2);
        }

        [Test]
        public void Get_NotKey_String_Array()
        {
            string key = "test.not-key-string-array";
            git.DeleteConfig(key);

            string[] value = git.GetArray(key);
            CollectionAssert.IsEmpty(value);
        }

        [Test]
        public void Delete_Local()
        {
            string key = "test.delete";
            string value = "abc";
            git.SetString(GitConfigScope.Local, key, value);
            string value2 = git.GetString(key);
            Debug.Log($"[{key}] = [{value2}]");
            Assert.AreEqual(value, value2);
            git.DeleteConfig(key);

            Assert.IsFalse(git.HasConfig(key));
        }

        [Test]
        public void Delete_Global()
        {
            string key = "test.delete-global";
            string value = "abc";
            git.SetString(GitConfigScope.Global, key, value);
            string value2 = git.GetString(key);
            Debug.Log($"[{key}] = [{value2}]");
            Assert.AreEqual(value, value2);
            git.DeleteConfig(GitConfigScope.Local, key);
            Assert.IsTrue(git.HasConfig(key));

            git.DeleteConfig(key);
            Assert.IsFalse(git.HasConfig(key));
        }

        [Test]
        public void Delete_Not_Key()
        {
            string key = "test.not-key";
            git.DeleteConfig(key);
        }

    }
}