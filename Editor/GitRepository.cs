using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace Unity.Git
{
    // git status

    public class GitRepository : IDisposable
    {
        CommitInfo? lastCommit;

        static string VersionTagPattern = "v*";
        public GitRepository(string repositoryPath)
        {
            if (repositoryPath == null) throw new ArgumentNullException(nameof(repositoryPath));

            Path = System.IO.Path.GetFullPath(repositoryPath);
        }

        public string Path { get; private set; }

        public CommitInfo LastCommit
        {
            get
            {
                if (lastCommit == null)
                {
                    lastCommit = GetCommit();
                }
                return lastCommit;
            }
        }
        public string WorkDir { get; private set; }

        public void Initialize()
        {
            string dir = Path;

            if (File.Exists(dir))
                throw new IOException($"Initialize repository failed, Already exists file '{dir}'");

            if (Directory.Exists(dir))
                throw new IOException($"Initialize repository failed, Already exists directory '{dir}'");

            Directory.CreateDirectory(dir);

            Run(new string[] {
                "init",
                dir
            });

            if (!Directory.Exists(System.IO.Path.Combine(dir, ".git")))
            {
                throw new Exception("Initialize repository failed");
            }
        }

        public void Clone(string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            string dir = Path;

            if (File.Exists(dir))
                throw new IOException($"Initialize repository failed, Already exists file '{dir}'");

            if (Directory.Exists(dir))
                throw new IOException($"Initialize repository failed, Already exists directory '{dir}'");

            Directory.CreateDirectory(dir);

            Run(new string[] {
                "clone",
                url,
                dir
            });
        }

        public string Combine(params string[] paths)
        {
            return GitUtility.Combine(Path, GitUtility.Combine(paths));
        }

        #region Config


        public Dictionary<string, string> GetConfigs()
        {
            return _GetConfigs(GitConfigScope.None);
        }
        public Dictionary<string, string> GetConfigs(GitConfigScope scope)
        {
            return _GetConfigs(scope);
        }

        public static void SetScopeArgument(List<string> args, GitConfigScope scope)
        {
            string s = GitUtility.GetScopeArgumentName(scope);
            if (!string.IsNullOrEmpty(s))
            {
                args.Add(s);
            }
        }

        private Dictionary<string, string> _GetConfigs(GitConfigScope scope)
        {
            List<string> args = new List<string>
            {
                "config",
                "--list"
            };

            SetScopeArgument(args, scope);

            string s = Run(args);
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (var line in GitUtility.Split(s))
            {
                int index = line.IndexOf("=");
                if (index >= 0)
                {
                    string name = line.Substring(0, index);
                    string value = line.Substring(index + 1);
                    values[name] = value;
                }
            }
            return values;
        }

        public bool HasConfig(string key)
        {
            return HasConfig(GitConfigScope.None, key);
        }

        public bool HasConfig(GitConfigScope scope, string key)
        {
            List<string> args = new List<string>
            {
                "config",
                "--name-only"
            };

            SetScopeArgument(args, scope);

            args.Add("--get-regexp");
            args.Add(key);
            string s;
            try
            {
                s = Run(args);
            }
            catch
            {
                return false;
            }
            foreach (var line in GitUtility.Split(s))
            {
                if (line == key)
                    return true;
            }
            return false;
        }

        public bool TryGetConfigScope(string key, out GitConfigScope scope)
        {
            List<string> args = new List<string>
            {
                "config",
                "--name-only",
                "--show-scope",
                "--get-regexp",
                key
            };

            string s;
            try
            {
                s = Run(args);
            }
            catch
            {
                scope = GitConfigScope.None;
                return false;
            }
            Regex regex = new Regex("^(?<scope>\\S+)\\s+(?<key>.+)$");
            foreach (var line in GitUtility.Split(s))
            {
                var m = regex.Match(line);
                if (m.Success)
                {
                    if (m.Groups["key"].Value == key)
                    {
                        string scopeName = m.Groups["scope"].Value;
                        switch (scopeName)
                        {
                            case "local":
                                scope = GitConfigScope.Local;
                                return true;
                            case "global":
                                scope = GitConfigScope.Global;
                                return true;
                            case "system":
                                scope = GitConfigScope.System;
                                return true;
                        }
                    }
                }
            }

            scope = GitConfigScope.None;
            return false;
        }


        public void SetString(string key, string value)
        {
            _SetString(GitConfigScope.None, key, value);
        }

        public void SetString(GitConfigScope scope, string key, string value)
        {
            _SetString(scope, key, value);
        }

        private void _SetString(GitConfigScope scope, string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            List<string> args = new List<string>
            {
                "config"
            };

            SetScopeArgument(args, scope);

            args.Add(key);
            args.Add(value);

            Run(args);
        }

        public void AddString(string key, string value)
        {
            AddString(GitConfigScope.None, key, value);
        }

        public void AddString(GitConfigScope scope, string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            List<string> args = new List<string>
            {
                "config",
                "--add"
            };

            SetScopeArgument(args, scope);

            args.Add(key);
            args.Add(value);

            Run(args);
        }


        public string GetString(string key)
        {
            return _GetString(GitConfigScope.None, key, null);
        }

        public string GetString(GitConfigScope scope, string key)
        {
            return _GetString(scope, key, null);
        }

        public string GetString(string key, string defaultValue)
        {
            return _GetString(GitConfigScope.None, key, defaultValue);
        }

        public string GetString(GitConfigScope scope, string key, string defaultValue)
        {
            return _GetString(scope, key, defaultValue);
        }

        private string _GetString(GitConfigScope scope, string key, string defaultValue, string valuePattern = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            List<string> args = new List<string>
            {
                "config",
                "--get"
            };

            SetScopeArgument(args, scope);

            if (!string.IsNullOrEmpty(defaultValue))
            {
                args.Add("--default");
                args.Add(defaultValue);
            }
            else
            {
                args.Add("--default");
                args.Add(string.Empty);
            }

            args.Add(key);
            if (!string.IsNullOrEmpty(valuePattern))
            {
                args.Add(valuePattern);
            }

            string s = Run(args);
            return s;
        }

        public string[] GetArray(string key)
        {
            return GetArray(GitConfigScope.None, key);
        }

        public string[] GetArray(GitConfigScope scope, string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (!HasConfig(scope, key))
                return new string[0];

            List<string> args = new List<string>
            {
                "config",
                "--get-all"
            };

            SetScopeArgument(args, scope);

            args.Add(key);
            string s = Run(args);
            string[] result;
            result = GitUtility.Split(s);
            return result;
        }

        public void DeleteConfig(string key)
        {
            if (!TryGetConfigScope(key, out var scope))
            {
                return;
            }
            DeleteConfig(scope, key);
        }

        public void DeleteConfig(GitConfigScope scope, string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (scope == GitConfigScope.None)
                scope = GitConfigScope.Local;

            if (!HasConfig(scope, key))
                return;

            List<string> args = new List<string>
            {
                "config",
                "--unset-all"
            };

            SetScopeArgument(args, scope);

            args.Add(key);
            string s = Run(args);
        }
        #endregion


        #region Branch

        public void CreateBranch(string branch, string trackedBranch = null)
        {
            List<string> args = new()
            {
                "branch"
            };

            if (!string.IsNullOrEmpty(trackedBranch))
            {
                args.Add("--track");
                args.Add(branch);
                args.Add(trackedBranch);
            }
            else
            {
                args.Add(branch);
            }

            Run(args);
        }

        public void Checkout(string branch, bool createBranch = false)
        {
            List<string> args = new List<string>
            {
                "checkout"
            };

            if (createBranch)
            {
                args.Add("-b");
            }
            args.Add(branch);
            Run(args);
        }
        public void CheckoutRemote(string branch, string remoteBranch)
        {
            List<string> args = new List<string>
            {
                "checkout",
                "-b",
                branch,
                remoteBranch
            };
            Run(args);
        }

        public void TrackBranch(string branch, string trackedBranch)
        {
            List<string> args = new()
            {
                "branch",
                "--track",
                branch,
                trackedBranch
            };
            Run(args);
        }


        public void DeleteBranch(string branch, bool force = false)
        {
            List<string> args = new()
            {
                "branch"
            };

            if (force)
            {
                args.Add("-D");
            }
            else
            {
                args.Add("-d");
            }
            args.Add(branch);
            Run(args);
        }

        public void DeleteRemoteBranch(string remoteBranch)
        {
            List<string> args = new List<string>();
            //Run(new string[] { "push", remote, "--delete", remoteBranch });
            args.Add("branch");
            args.Add("-dr");
            args.Add(remoteBranch);
            Run(args);
        }

        public void DeleteRemoteBranch(string remote, string remoteBranch)
        {
            DeleteRemoteBranch($"{remote}/{remoteBranch}");
        }

        public void SwitchBranch(string branch)
        {
            List<string> args = new List<string>();
            args.Add("switch");
            args.Add(branch);
            Run(args);
        }

        public bool HasBranch(string branch)
        {
            string[] branchs = GetBranchs();
            return branchs.Contains(branch);
        }

        public string GetCurrentBranch()
        {
            string s;
            s = Run($"branch");
            string branch = null;
            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string line2 = line.Trim();
                    if (line2.StartsWith("*"))
                    {
                        line2 = line2.Substring(1);
                        line2 = line2.Trim();
                        if (line2.StartsWith("(HEAD"))
                            continue;

                        branch = line2;
                        break;
                    }
                }
            }
            return branch;
        }

        public string[] GetBranchs()
        {
            List<string> branchs = new();
            string s;
            s = Run($"branch");
            //all
            //s = Cmd($"branch -a");

            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string line2 = line;
                    line2 = line2.Trim();
                    bool active = false;
                    if (line2.StartsWith("*"))
                    {
                        active = true;
                        line2 = line2.Substring(1);
                        if (line2.StartsWith("* (HEAD"))
                            continue;
                    }
                    line2 = line2.Trim();
                    var parts = line2.Split(new char[] { ' ' });
                    if (parts.Length > 0)
                    {
                        string branch = parts[0];
                        if (!string.IsNullOrEmpty(branch))
                        {
                            branchs.Add(branch);
                        }
                    }
                }
            }
            return branchs.ToArray();
        }

        /*public Branch[] GetRemoteBranchs()
        {
            List<Branch> commitIds = new();
            string s = Cmd("ls-remote");
            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    if (Branch.TryParseCommitIdAndBranch(line, out var branch))
                    {
                        commitIds.Add(branch);
                    }
                }
            }
            return commitIds.ToArray();
        }*/

        public string[] GetRemoteBranchs()
        {
            List<string> branchs = new();
            string s;
            s = Run($"branch -r");

            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string line2 = line;
                    bool active = false;
                    if (line2.StartsWith("*"))
                    {
                        active = true;
                        line2 = line2.Substring(1);
                    }
                    line2 = line2.Trim();
                    var parts = line2.Split(new char[] { ' ' });
                    if (parts.Length > 0)
                    {
                        string branch = parts[0];
                        if (!string.IsNullOrEmpty(branch))
                        {
                            branchs.Add(branch);
                        }
                    }
                }
            }
            return branchs.ToArray();
        }

        public Branch[] GetAllRefBranchs()
        {
            return GetRefBranchs(null);
        }

        public Branch[] GetRefBranchs(string branch)
        {
            List<string> args = new();
            args.Add("show-ref");
            if (!string.IsNullOrEmpty(branch))
                args.Add(branch);

            string s;
            s = Run(args);

            List<Branch> branchs = new();
            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    var parts = line.Trim().Split(' ');
                    if (parts.Length == 2)
                    {
                        Branch refBranch = new Branch();
                        refBranch.commitId = parts[0];
                        refBranch.branch = parts[1];
                        branchs.Add(refBranch);
                    }
                }
            }
            return branchs.ToArray();
        }


        public Branch GetRemoteRefBranch(string branch)
        {
            Branch[] branchs = GetRefBranchs(branch);
            Branch findBranch = null;
            foreach (var item in branchs)
            {
                findBranch = item;
                if (GitUtility.IsRemoteBranch(item.branch))
                {
                    findBranch = item;
                    break;
                }
            }
            return findBranch;
        }

        #endregion

        #region Commit

        public void AddToCommit(params string[] files)
        {
            List<string> args = new List<string>();
            args.Add("add");
            args.AddRange(files);
            Run(args);
        }
        //git restore --staged .
        public void RemoveFromCommit(params string[] files)
        {
            List<string> args = new List<string>();
            args.Add("rm");
            args.AddRange(files);
            Run(args);
        }

        public void RenameToCommit(string source, string dest)
        {
            List<string> args = new List<string>();
            args.Add("mv");
            args.Add(source);
            args.Add(dest);
            Run(args);
        }

        public void Commit(string message)
        {
            List<string> args = new List<string>();
            args.Add("commit");
            if (!string.IsNullOrEmpty(message))
            {
                args.Add("-m");
                args.Add(message);
            }

            Run(args);
        }

        public void CommitChanged(string message)
        {
            List<string> args = new List<string>();
            args.Add("commit");
            args.Add("-a");
            if (!string.IsNullOrEmpty(message))
            {
                args.Add("-m");
                args.Add(message);
            }

            Run(args);
        }

        public void AmendPreviousCommit(string message)
        {
            List<string> args = new List<string>();
            args.Add("commit");
            args.Add("--amend");
            if (!string.IsNullOrEmpty(message))
            {
                args.Add("-m");
                args.Add(message);
            }

            Run(args);
        }

        public bool HasChanged()
        {
            GetDiff(out var modifieds, out var deleteds, out var untrackeds);
            return (modifieds.Length + deleteds.Length) > 0;
        }

        public string[] GetChangedFiles()
        {
            GetDiff(out var modifieds, out var deleteds, out var untrackeds);
            List<string> files = new List<string>();
            files.AddRange(modifieds);
            files.AddRange(deleteds);
            return files.ToArray();
        }

        #endregion

        #region Pull


        public void Fetch(string remote = null)
        {
            List<string> args = new();
            //await GitCmdAsync("fetch origin --progress -v");
            args.Add("fetch");
            if (!string.IsNullOrEmpty(remote))
            {
                args.Add(remote);
            }
            args.Add("--progress");
            args.Add("-v");
            Run(args);
            //commit = GetCommit();
            //return commit;
        }



        public void FetchAll()
        {
            List<string> args = new();
            //await GitCmdAsync("fetch origin --all --progress -v");
            args.Add("fetch");
            args.Add("--all");
            args.Add("--progress");
            args.Add("-v");

            Run(args);
            //await GitCmdAsync("fetch");
        }



        public CommitInfo Pull()
        {
            List<string> args = new();
            args.Add("pull");
            args.Add("--progress");
            args.Add("-v");
            args.Add("--no-rebase");
            Run(args);
            lastCommit = GetCommit();
            return lastCommit;
        }

        //�����첽�ӿڣ��첽������ Test ����
        //public async Task<CommitInfo> PullAsync()
        //{
        //    await new SubThread();
        //    var result = Pull();
        //    await new MainThread();
        //    return result;
        //}

        #endregion


        #region Merge

        public void Merge(string branch)
        {
            List<string> args = new()
            {
                "merge",
                branch
            };

            Run(args);
        }

        public void MergeCommit(string commit)
        {
            List<string> args = new()
            {
                "cherry-pick",
                commit
            };
            Run(args);
        }

        #endregion


        public void Rebase(string branch)
        {
            List<string> args = new()
            {
                "rebase",
                branch
            };
            Run(args);
        }

        public void Reset(bool force)
        {
            List<string> args = new()
            {
                "reset"
            };

            if (force)
            {
                args.Add("--hard");
            }

            Run(args);
        }

        public void Reset(string commitOrBranch, bool force)
        {
            List<string> args = new()
            {
                "reset"
            };
            if (force)
            {
                args.Add("--hard");
            }
            args.Add(commitOrBranch);
            Run(args);
        }

        public void ResetFile(string file)
        {
            ResetFile(new string[] { file });
        }

        public void ResetFile(IEnumerable<string> files)
        {
            ResetFile(GitUtility.HEAD_BRANCH, files);
        }

        public void ResetFile(string commitOrBranch, string file)
        {
            ResetFile(commitOrBranch, new string[] { file });
        }

        public void ResetFile(string commitOrBranch, IEnumerable<string> files)
        {
            if (string.IsNullOrEmpty(commitOrBranch))
                commitOrBranch = "--";//区分分支和文件名

            List<string> args = new()
            {
                "checkout",
                commitOrBranch
            };

            args.AddRange(files);
            Run(args);
        }

        public void Clean(bool force)
        {
            List<string> args = new()
            {
                "clean"
            };
            if (force)
            {
                args.Add("-f");
            }
            Run(args);
        }

        public CommitInfo GetCommit()
        {
            string commitId = GetCommitId();
            return GetCommit(commitId);
        }

        //public CommitInfo GetCommit(string branch)
        //{
        //    string commitId = GetCommitId(branch);
        //    return GetCommitById(commitId);
        //}




        public CommitInfo GetCommit(string commitId)
        {
            List<string> args = new();
            args.Add("show");
            args.Add(commitId);
            args.Add(GitUtility.ISO_DATE_FORMAT);
            args.Add("--format=%H\n%cd\n%cn\n%ce\n%s");

            string s = Run(args);

            if (string.IsNullOrEmpty(s))
                return null;


            var lines = GitUtility.Split(s);
            if (lines.Length < 5)
            {
                return null;
            }
            CommitInfo commit = new CommitInfo();
            commit.id = lines[0];
            if (DateTime.TryParse(lines[1], out var d))
            {
                commit.time = d;
            }
            commit.authorName = lines[2];
            commit.anthorEmail = lines[3];
            commit.message = lines[4];

            return commit;
        }

        public string GetCommitId()
        {
            return GetCommitId(GitUtility.HEAD_BRANCH);
        }

        private string GetCommitId(string branch)
        {
            List<string> args = new()
            {
                "rev-parse",
                branch
            };
            string commitId = Run(args);
            commitId = commitId.Trim();
            return commitId;
        }



        public DateTime GetCommitTime(string commitId)
        {
            List<string> args = new()
            {
                "show",
                "-s",
                "--format=%ci",
                commitId
            };
            string str = Run(args);
            if (string.IsNullOrEmpty(str))
                return DateTime.MinValue;
            DateTime.TryParse(str, out var dt);
            return dt.ToUniversalTime();
        }

        public long GetCommitTimestamp(string commitId)
        {
            List<string> args = new()
            {
                "show",
                "-s",
                "--format=%ct",
                commitId
            };
            string str = Run(args);
            return long.Parse(str);
        }


        public Branch[] GetRemoteRefBranchs()
        {
            List<string> args = new()
            {
                "ls-remote",
                "--refs"
            };

            string s = Run(args);
            List<Branch> commitIds = new();
            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    if (Branch.TryParseCommitIdAndBranch(line, out var branch))
                    {
                        commitIds.Add(branch);
                    }
                }
            }
            return commitIds.ToArray();
        }

        public int GetCommitCount()
        {
            return GetCommitCount(GitUtility.HEAD_BRANCH);
        }

        public int GetCommitCount(string branch)
        {
            List<string> args = new()
            {
                "rev-list",
                branch,
                "--count"
            };
            string result = Run(args)?.Trim();
            int.TryParse(result, out var n);
            return n;
        }

        public int GetCommitDiffCount(string branch)
        {
            string refBranch = GetRemoteRefBranch(branch)?.branch;
            if (string.IsNullOrEmpty(refBranch))
                throw new Exception("Ref branch null");
            return GetCommitCount(refBranch) - GetCommitCount(branch);
        }

        #region Tag

        //git show-ref --tags
        public string[] GetTags()
        {
            return GetTags(null);
        }

        /// <summary>
        /// pattern: v*
        /// </summary>
        public string[] GetVersionTags(string sortField = null, bool sortAsc = true, int? limit = null)
        {
            string sortKey = null;

            if (!string.IsNullOrEmpty(sortField))
            {
                sortKey = sortField;
                if (!sortAsc)
                {
                    sortKey = "-" + sortKey;
                }
            }

            return GetTags(pattern: VersionTagPattern, limit: limit, sortKey: sortKey);
        }

        /// <param name="pattern">Version tag v1.0.0: v*</param>
        public string[] GetTags(string pattern, int? limit = null, string sortKey = null)
        {
            List<string> args = new()
            {
                "tag",
            };
            args.Add("-l");


            if (sortKey != null)
            {
                args.Add($"--sort={sortKey}");
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                args.Add(pattern);
            }


            //if (limit > 0)
            //{
            //    args.Add("|");
            //    args.Add("head");
            //    args.Add("-n");
            //    args.Add(limit.ToString());
            //}

            string s = Run(args);

            List<string> tags = new List<string>();
            Regex regex = new Regex("^\\s*(?<tag>\\S+)\\s*");
            foreach (var line in GitUtility.Split(s))
            {
                var m = regex.Match(line);
                if (m.Success)
                {
                    string tag = m.Groups["tag"].Value;
                    tags.Add(tag);
                    if (limit.HasValue && tags.Count >= limit.Value)
                        break;
                }
            }
            return tags.ToArray();
        }

        public string GetLatestTag(string pattern)
        {
            //始终加long，不加 --long 有时会返回long
            List<string> args = new()
            {
                "describe",
                "--tags",
                "--long"
            };

            if (!string.IsNullOrEmpty(pattern))
            {
                args.Add("--match");
                args.Add(pattern);
            }

            if (!TryRun(args, out var s))
                return null;

            string tag;
            if (!TryParseDescribeTagResult(s, out tag, out var diff, out var commitId))
                return null;
            return tag;
        }

        public string GetLatestVersionTag()
        {
            return GetLatestTag(VersionTagPattern);
        }

        public bool ExistsTag(string tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            List<string> args = new()
            {
                "describe",
                "--tags",
                tag
            };

            if (!TryRun(args, out var s))
                return false;
            if (s != tag)
                return false;
            return true;
        }

        private bool TryParseDescribeTagResult(string s, out string tag, out int diff, out string commitId)
        {
            tag = null;
            diff = 0;
            commitId = null;
            if (string.IsNullOrEmpty(s))
                return false;

            int index = s.LastIndexOf("-");
            if (index < 0)
                return false;
            commitId = s.Substring(index + 1);
            s = s.Substring(0, index);
            index = s.LastIndexOf("-");
            if (index < 0)
                return false;
            if (!int.TryParse(s.Substring(index + 1), out diff))
                return false;
            tag = s.Substring(0, index);
            return true;
        }

        public int DiffTag(string pattern)
        {
            List<string> args = new()
            {
                "describe",
                "--tags",
                "--long"
            };

            if (!string.IsNullOrEmpty(pattern))
            {
                args.Add("--match");
                args.Add(pattern);
            }
            //空标签报错
            //fatal: No names found, cannot describe anything.
            if (!TryRun(args, out var s))
                return -1;
            if (!TryParseDescribeTagResult(s, out var tag, out var diff, out var commitId))
                return -1;
            return diff;
        }

        public int DiffVersionTag()
        {
            return DiffTag(VersionTagPattern);
        }

        public TagInfo GetTagInfo(string tag)
        {
            string s;
            List<string> args = new()
            {
                "show",
                tag
            };
            try
            {
                s = Run(args);
            }
            catch
            {
                return null;
            }
            if (TagInfo.TryParse(s, out var tagInfo))
            {
                tagInfo.tag = tag;
            }
            return tagInfo;
        }

        public bool HasTag(string tag)
        {
            return GetTagInfo(tag) != null;
        }

        public void CreateTag(string tag, string commit = null)
        {
            List<string> args = new()
            {
                "tag",
                tag
            };
            if (!string.IsNullOrEmpty(commit))
            {
                args.Add(commit);
            }
            Run(args);
        }

        //public void CreateRemoteTag(string remote, string tag)
        //{
        //    Cmd($"push {remote} :refs/tags/{tag}");
        //}

        public void PushTag(string remote, string tag)
        {
            List<string> args = new()
            {
                "push",
                remote,
                tag
            };
            Run(args);
        }

        public void PushTags(string remote)
        {
            List<string> args = new()
            {
                "push",
                remote,
                "--tags"
            };
            Run(args);
        }

        public void DeleteTag(string tag)
        {
            List<string> args = new()
            {
                "tag",
                "-d",
                tag
            };
            Run(args);
        }


        public void CheckoutTag(string tag)
        {
            List<string> args = new()
            {
                "checkout",
                tag
            };
            Run(args);
        }

        public void CheckoutTag(string tag, string newBranch)
        {
            List<string> args = new()
            {
                "checkout",
                "-b",
                newBranch,
                tag
            };
            Run(args);
        }
        #endregion


        #region Remote

        public string GetDefaultRemote()
        {
            string defaultRemote = GetString("remote.pushdefault", null);
            if (string.IsNullOrEmpty(defaultRemote))
            {
                string[] remotes = GetRemotes();
                if (remotes.Length == 1)
                {
                    defaultRemote = remotes[0];
                }
            }
            return defaultRemote;
        }

        public void AddRemote(string remote, string url)
        {
            List<string> args = new()
            {
                "remote",
                "add",
                remote,
                url
            };
            string s;
            s = Run(args);

        }

        public string[] GetRemotes()
        {
            List<string> args = new()
            {
                "remote",
                "show",
            };
            string s;
            s = Run(args);
            List<string> remotes = new();
            //origin, github, gitlab

            if (!string.IsNullOrEmpty(s))
            {
                foreach (var line in s.Split('\n'))
                {
                    string line2 = line.Trim();
                    if (string.IsNullOrEmpty(line2))
                        continue;
                    remotes.Add(line2);
                }
            }
            return remotes.ToArray();
        }

        #endregion



        public int GetDiff(out string[] modifieds, out string[] deleteds, out string[] untrackeds)
        {
            List<string> args = new()
            {
                "status"
            };

            if (!string.IsNullOrEmpty(WorkDir))
            {
                args.Add(WorkDir);
            }

            string s = Run(args);

            List<string> modified2 = new(), deleted2 = new(), untracked2 = new();
            Regex modifiedRegex = new Regex("^\\s*modified:\\s*(?<file>(\".+\")|(.+))\\s*$");
            Regex deletedRegex = new Regex("^\\s*deleted:\\s*(?<file>(\".+\")|(.+))\\s*$");
            Regex untrackedRegex = new Regex("^\\s*(?<file>(\".+\")|(.+))\\s*$");


            var lines = new Queue<string>(GitUtility.Split(s)
                .Select(o => o.Trim()));

            while (lines.Count > 0)
            {
                string line = lines.Dequeue();
                if (line.StartsWith("Changes not staged for commit:") || line.StartsWith("Changes to be committed:"))
                {
                    while (lines.Count > 0)
                    {
                        line = lines.Peek();
                        if (line.StartsWith("("))
                            lines.Dequeue();
                        else
                            break;
                    }

                    while (lines.Count > 0)
                    {
                        line = lines.Dequeue();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }

                        string file = null;
                        var m = modifiedRegex.Match(line);
                        if (m.Success)
                        {
                            file = m.Groups["file"].Value;
                            if (file.StartsWith("\""))
                            {
                                file = file.Substring(1, file.Length - 2);
                            }
                            modified2.Add(file);
                        }
                        else
                        {
                            m = deletedRegex.Match(line);
                            if (m.Success)
                            {
                                file = m.Groups["file"].Value;
                                if (file.StartsWith("\""))
                                {
                                    file = file.Substring(1, file.Length - 2);
                                }
                                deleted2.Add(file);
                            }
                        }

                    }
                }
                else if (line.StartsWith("Untracked"))
                {
                    while (lines.Count > 0)
                    {
                        line = lines.Peek();
                        if (line.StartsWith("("))
                            lines.Dequeue();
                        else
                            break;
                    }

                    while (lines.Count > 0)
                    {
                        line = lines.Dequeue();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        string file = null;
                        var m = untrackedRegex.Match(line);
                        if (m.Success)
                        {
                            file = m.Groups["file"].Value;
                            if (file.StartsWith("\""))
                            {
                                file = file.Substring(1, file.Length - 2);
                            }
                            untracked2.Add(file);
                        }
                    }
                }
            }

            modifieds = modified2.ToArray();
            deleteds = deleted2.ToArray();
            untrackeds = untracked2.ToArray();

            return modifieds.Length + deleteds.Length;
        }

        #region Logs
        //public CommitInfo[] GetCommitsAfter(DateTime after, int limit = 10, int skip = 0)
        //{
        //    return GetCommits(after: after, limit: limit, skip: skip);
        //}

        //public CommitInfo[] GetCommits(int limit = 10, int skip = 0)
        //{
        //    return GetCommits(limit: limit, skip: skip);
        //}

        public CommitInfo[] GetCommits(
            string filter = null,
            DateTime? before = null,
            DateTime? after = null,
            string author = null,
            string tag = null,
            string branch = null,
            int limit = 10,
            int skip = 0)
        {
            List<string> args = new()
            {
                "log",
                GitUtility.ISO_DATE_FORMAT
            };

            //Timestamp Seconds
            //--date=raw
            if (!string.IsNullOrEmpty(filter))
            {
                args.Add($"--grep={filter}");
            }

            if (!string.IsNullOrEmpty(author))
            {
                args.Add($"--author={author}");
            }

            if (skip > 0)
            {
                args.Add($"--skip={skip}");
            }

            if (limit > 0)
            {
                //args.Add("-n");
                //args.Add(limit.ToString());
                args.Add($"--max-count={limit}");
            }

            if (before.HasValue)
            {
                args.Add($"--before={GitUtility.ToDateString(before.Value)}");
            }

            if (after.HasValue)
            {
                args.Add($"--after={GitUtility.ToDateString(after.Value)}");
            }

            if (!string.IsNullOrEmpty(tag))
            {
                args.Add($"--tags={tag}");
            }
            if (!string.IsNullOrEmpty(branch))
            {
                args.Add($"--branchs={branch}");
            }

            if (!string.IsNullOrEmpty(WorkDir))
            {
                args.Add(WorkDir);
            }

            string s = Run(args);
            List<CommitInfo> list = new List<CommitInfo>();

            Regex commitRegex = new Regex("^commit\\s+(?<commit>\\S+)(\\s*\\(?(?<branch>\\S+?)\\)?)?\\s*$");
            var lines = new Queue<string>(GitUtility.Split(s));
            string line;
            while (lines.Count > 0)
            {
                line = lines.Dequeue();
                if (string.IsNullOrEmpty(line))
                    continue;
                var m = commitRegex.Match(line);
                if (m.Success)
                {
                    CommitInfo commit = new CommitInfo();
                    commit.id = m.Groups["commit"].Value;
                    bool isCommit = false;
                    while (lines.Count > 0)
                    {
                        line = lines.Dequeue();

                        if (string.IsNullOrEmpty(line))
                        {
                            if (isCommit)
                                break;
                            isCommit = true;
                        }
                        else if (isCommit)
                        {
                            if (commit.message == null)
                                commit.message = line.TrimStart();
                            else
                                commit.message += $"\n{line.TrimStart()}";
                        }
                        else
                        {
                            if (line.StartsWith("Author: "))
                            {
                                commit.authorName = line.Substring("Author: ".Length);
                            }
                            else if (line.StartsWith("Date: "))
                            {
                                if (DateTime.TryParse(line.Substring("Date: ".Length), out var d))
                                {
                                    commit.time = d;
                                }
                                //if (GitUtility.TryParseDate(line.Substring("Date: ".Length), out var d))
                                //{
                                //    commit.time = d;
                                //}
                            }
                        }



                    }
                    list.Add(commit);
                }
            }
            return list.ToArray();
        }

        #endregion


        public int GetUserTotalCommit(string author)
        {
            if (GetUserTotalCommit().TryGetValue(author, out int count))
            {
                return count;
            }
            return 0;
        }

        public Dictionary<string, int> GetUserTotalCommit()
        {
            List<string> args = new();
            args.Add("shortlog");
            args.Add("-sn");
            string s = Run(args);
            Dictionary<string, int> dic = new();
            foreach (var line in GitUtility.Split(s).Select(o => o.Trim()))
            {
                string[] parts = line.Split(' ');
                if (parts.Length < 2)
                    continue;
                if (int.TryParse(parts[0], out var n))
                {
                    dic[parts[1]] = n;
                }
            }
            return dic;
        }

        public string[] GetFileCommits(string file)
        {
            List<string> args = new();
            args.Add("blame");
            args.Add(file);
            string s = Run(args);
            List<string> commits = new();
            foreach (var line in GitUtility.Split(s).Select(o => o.Trim()))
            {
                string[] parts = line.Split(new char[] { ' ' }, 2);
                if (parts.Length < 2)
                    continue;
                string commitId = parts[0];
                commits.Add(commitId);
            }
            return commits.ToArray();
        }

        public string Run(string args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            string result = null;
            try
            {
                result = GitUtility.RunWithResult("git", args, workDir: Path);
            }
            catch (Exception ex)
            {
                GitUtility.LogError($"{Path}\n{ex.Message}\nArguments: \n{string.Join("\n", args)}");
                throw ex;
            }
            finally
            {
                sw.Stop();
                GitUtility.Log($"{Path} ({sw.ElapsedMilliseconds} ms)\nArguments: \n{string.Join("\n", args)}\nResult:\n{result}");
            }
            return result;
        }

        public bool TryRun(string args, out string result)
        {
            return TryRun(args, out result, out var ex);
        }

        public bool TryRun(string args, out string result, out Exception ex)
        {
            Stopwatch sw = Stopwatch.StartNew();
            result = null;
            ex = null;
            try
            {
                result = GitUtility.RunWithResult("git", args, workDir: Path);
            }
            catch (Exception _)
            {
                ex = _;
                return false;
            }
            finally
            {
                sw.Stop();
                GitUtility.Log($"{Path} ({sw.ElapsedMilliseconds} ms)\nArguments: \n{string.Join("\n", args)}\nResult:\n{result}");
            }
            return true;
        }

        public string Run(IEnumerable<string> args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            string result = null;
            try
            {
                result = GitUtility.RunWithResult("git", args, workDir: Path);
            }
            catch (Exception ex)
            {
                GitUtility.LogError($"{Path}\n{ex.Message}\nArguments: \n{string.Join("\n", args)}");
                throw ex;
            }
            finally
            {
                sw.Stop();
                GitUtility.Log($"{Path} ({sw.ElapsedMilliseconds} ms)\nArguments: \n{string.Join("\n", args)}\nResult:\n{result}");
            }
            return result;
        }

        public bool TryRun(IEnumerable<string> args, out string result, out Exception ex)
        {
            Stopwatch sw = Stopwatch.StartNew();
            result = null;
            ex = null;
            try
            {
                result = GitUtility.RunWithResult("git", args, workDir: Path);

            }
            catch (Exception _)
            {
                ex = _;
                return false;
            }
            finally
            {
                sw.Stop();
                GitUtility.Log($"{Path} ({sw.ElapsedMilliseconds} ms)\nArguments: \n{string.Join("\n", args)}\nResult:\n{result}");
            }
            return true;
        }

        public bool TryRun(IEnumerable<string> args, out string result)
        {
            return TryRun(args, out result, out var ex);
        }

        public void Dispose()
        {

        }

        public IDisposable BeginDirectory(string dir)
        {
            return new WorkDirScope(this, dir);
        }


        class WorkDirScope : IDisposable
        {
            GitRepository repo;
            private string oldDir;
            private bool disposed;

            public WorkDirScope(GitRepository repo, string dir)
            {
                this.repo = repo;
                //this.oldDir = Environment.CurrentDirectory;
                //Environment.CurrentDirectory = dir;
                this.oldDir = repo.WorkDir;
                repo.WorkDir = dir;

            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                repo.WorkDir = oldDir;
                //Environment.CurrentDirectory = oldDir;
            }
        }

    }



    public class Branch
    {
        public string branch;
        public string commitId;


        public static bool TryParseCommitIdAndBranch(string line, out Branch branch)
        {
            branch = null;

            if (string.IsNullOrEmpty(line))
                return false;
            var parts = line.Trim().Split('\t');
            if (parts.Length == 2)
            {
                var tmp = new Branch();
                tmp.commitId = parts[0].Trim();
                if (string.IsNullOrEmpty(tmp.commitId))
                    return false;
                tmp.branch = parts[1].Trim();
                if (string.IsNullOrEmpty(tmp.branch))
                    return false;
                branch = tmp;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"[{branch}] {commitId}";
        }
    }


}