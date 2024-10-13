using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

[assembly: InternalsVisibleTo("Git.Tests.Editor")]

namespace Unity.Git
{

    //查看帮助
    //git log -help
    //git help log 
    public class GitUtility
    {
        public const string GitFolderName = ".git";
        private const string LogPrefix = "[Git] ";

        internal const string ISO_DATE_FORMAT = "--date=iso";

        internal const int DefaultTimeoutMS = 10 * 1000;


        internal const string HEAD_REFS_PREFIX = "refs/heads/";
        internal const string REMOTE_REFS_PREFIX = "refs/remotes/";

        public const string ARGUMENT_CONFIG_SCOPE_GLOBAL = "--global";
        public const string ARGUMENT_CONFIG_SCOPE_SYSTEM = "--system";
        public const string ARGUMENT_CONFIG_SCOPE_LOCAL = "--local";
        public const string ARGUMENT_CONFIG_SCOPE_WORKTREE = "--worktree";

        public const string HEAD_BRANCH = "HEAD";//@, HEAD~0
        public const string HEAD1_BRANCH = "HEAD~1";//HEAD~1



        #region UTC Time

        /// <summary>
        /// UTC 1970/1/1 00:00:00
        /// </summary>
        internal static readonly DateTime UtcTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 当前时间戳
        /// </summary>
        internal static long NowTimestamp()
        {
            return ToTimestamp(DateTime.UtcNow);
        }

        /// <summary>
        /// 时间戳，1970/1/1 00:00:00 UTC 毫秒数，java: System.currentTimeMillis()
        /// </summary>
        internal static long ToTimestamp(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            return (long)dt.Subtract(UtcTimestamp).TotalMilliseconds;
        }
        /// <summary>
        /// 时间戳，1970/1/1 00:00:00 UTC 毫秒数
        /// </summary> 
        internal static DateTime FromTimestamp(long timestamp)
        {
            return UtcTimestamp.Add(TimeSpan.FromMilliseconds(timestamp));
        }

        #endregion


        [Conditional("GIT_DEBUG")]
        internal static void Log(string msg)
        {
            Debug.Log(LogPrefix + msg);
        }

        [Conditional("GIT_DEBUG")]
        internal static void LogError(string msg)
        {
            Debug.LogError(LogPrefix + msg);
        }

        public static string NormalPath(string path)
        {
            path = path.Replace('\\', '/');
            return path;
        }

        internal static string RunWithResult(string file, IEnumerable<string> args, string workDir = null, Action<StreamWriter> onInput = null, Action<string> onOutput = null, Action<string> onError = null, int timeoutMS = 0)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(workDir))
                startInfo.WorkingDirectory = Path.GetFullPath(workDir);
            startInfo.FileName = file;
            if (args != null)
            {
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }
            return RunWithResult(startInfo, onInput: onInput, onOutput: onOutput, onError: onError, timeoutMS: timeoutMS);
        }


        //internal static async Task<string> RunWithResultAsync(string file, IEnumerable<string> args, string workDir = null, Action<StreamWriter> onInput = null, Action<string> onOutput = null, Action<string> onError = null, int timeoutMS = 0)
        //{
        //    await new SubThread();

        //    string result = RunWithResult(file, args, workDir: workDir, onInput: onInput, onOutput: onOutput, onError: onError, timeoutMS: timeoutMS);

        //    await new MainThread();

        //    return result;
        //}

        internal static string RunWithResult(string file, string args, string workDir = null, Action<StreamWriter> onInput = null, Action<string> onOutput = null, Action<string> onError = null, int timeoutMS = 0)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(workDir))
                startInfo.WorkingDirectory = Path.GetFullPath(workDir);
            startInfo.FileName = file;
            startInfo.Arguments = args;
            return RunWithResult(startInfo, onInput: onInput, onOutput: onOutput, onError: onError, timeoutMS: timeoutMS);
        }

        //internal static async Task<string> RunWithResultAsync(string file, string args, string workDir = null, Action<StreamWriter> onInput = null, Action<string> onOutput = null, Action<string> onError = null, int timeoutMS = 0)
        //{
        //    await new SubThread();

        //    string result = RunWithResult(file, args, workDir: workDir, onInput: onInput, onOutput: onOutput, onError: onError, timeoutMS: timeoutMS);

        //    await new MainThread();

        //    return result;
        //}

        private static string RunWithResult(ProcessStartInfo startInfo, Action<StreamWriter> onInput = null, Action<string> onOutput = null, Action<string> onError = null, int timeoutMS = 0)
        {
            StringBuilder errorBuilder = null;
            StringBuilder dataBuilder = new StringBuilder();

            int exitCode = _Run(startInfo,
                onOutput: o =>
                {
                    onOutput?.Invoke(o);
                    if (dataBuilder.Length > 0)
                        dataBuilder.Append("\n");
                    dataBuilder.Append(o);

                },
                onError: o =>
                {
                    if (errorBuilder == null)
                        errorBuilder = new StringBuilder();
                    onError?.Invoke(o);
                    if (errorBuilder.Length > 0)
                        errorBuilder.Append("\n");
                    errorBuilder.Append(o);
                },
                onInput: onInput,
                timeoutMS: timeoutMS);

            if (exitCode != 0)
            {
                string error;

                if (errorBuilder != null)
                {
                    error = errorBuilder.ToString();
                }
                else
                {
                    error = $"Process Error ({exitCode})";
                }
                throw new Exception(error);
            }

            return dataBuilder.ToString();
        }

        internal static int Run(string file, IEnumerable<string> args, Action<string> onOutput, Action<string> onError, string workDir = null, Action<StreamWriter> onInput = null, int timeoutMS = 0)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(workDir))
                startInfo.WorkingDirectory = Path.GetFullPath(workDir);
            startInfo.FileName = file;
            if (args != null)
            {
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            return _Run(startInfo, onOutput, onError, onInput: onInput, timeoutMS: timeoutMS);
        }

        //internal static async Task<int> RunAsync(string file, IEnumerable<string> args, Action<string> onOutput, Action<string> onError, string workDir = null, Action<StreamWriter> onInput = null, int timeoutMS = 0)
        //{
        //    await new SubThread();
        //    int result = Run(file, args, onOutput, onError, workDir: workDir, onInput: onInput, timeoutMS: timeoutMS);
        //    await new MainThread();
        //    return result;
        //}

        internal static int Run(string file, string args, Action<string> onOutput, Action<string> onError, string workDir = null, Action<StreamWriter> onInput = null, int timeoutMS = 0)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(workDir))
                startInfo.WorkingDirectory = Path.GetFullPath(workDir);
            startInfo.FileName = file;
            startInfo.Arguments = args;
            return _Run(startInfo, onOutput, onError, onInput: onInput, timeoutMS: timeoutMS);
        }

        //internal static async Task<int> RunAsync(string file, string args, Action<string> onOutput, Action<string> onError, string workDir = null, Action<StreamWriter> onInput = null, int timeoutMS = 0)
        //{
        //    await new SubThread();
        //    int result = Run(file, args, onOutput, onError, workDir: workDir, onInput: onInput, timeoutMS: timeoutMS);
        //    await new MainThread();
        //    return result;
        //}

        static Encoding runOutputEncoding;


        public static Encoding RunOutputEncoding
        {
            get
            {
                return runOutputEncoding;
            }
            set
            {
                runOutputEncoding = value;
            }
        }

        private static int _Run(ProcessStartInfo startInfo, Action<string> onOutput, Action<string> onError, Action<StreamWriter> onInput = null, int timeoutMS = 0)
        {
            if (startInfo == null) throw new ArgumentNullException(nameof(startInfo));

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            //startInfo.EnvironmentVariables["LESSCHARSET"] = "utf-8";

            var outputEncoding = RunOutputEncoding;

            //            if (outputEncoding != null)
            //            {
            //#if UNITY_STANDALONE_WIN
            //                outputEncoding = Encoding.GetEncoding("gb2312");
            //#endif
            //            }

            if (outputEncoding != null)
            {
                startInfo.StandardOutputEncoding = outputEncoding;
                startInfo.StandardErrorEncoding = outputEncoding;
            }

            if (onInput != null)
            {
                startInfo.RedirectStandardInput = true;
                //startInfo.StandardInputEncoding = Encoding.UTF8;
            }

            using (var proc = new Process())
            {
                proc.StartInfo = startInfo;

                proc.OutputDataReceived += (o, e) =>
                {
                    if (e.Data != null)
                    {
                        //string text = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));
                        //string text = e.Data;

                        onOutput?.Invoke(e.Data);
                    }
                };
                proc.ErrorDataReceived += (o, e) =>
                {
                    if (e.Data != null)
                    {
                        //string text = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));

                        onError?.Invoke(e.Data);
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                if (onInput != null)
                {
                    onInput(proc.StandardInput);
                }

                if (timeoutMS > 0)
                {
                    proc.WaitForExit(timeoutMS);
                }
                else
                {
                    proc.WaitForExit();
                }
                try
                {
                    if (proc.ExitCode == 0)
                    {
                        string end = proc.StandardOutput.ReadToEnd();
                        if (!string.IsNullOrEmpty(end))
                            onOutput?.Invoke(end);
                    }
                    else
                    {
                        string end = proc.StandardError.ReadToEnd();
                        if (!string.IsNullOrEmpty(end))
                            onError?.Invoke(end);
                    }
                }
                catch
                {
                }

                if (!proc.HasExited)
                {
                    proc.Kill();
                }

                return proc.ExitCode;
            }
        }


        public static string[] Split(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];

            return input.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        const string DATE_FORMAT = "ddd MMM d HH:mm:ss yyyy K";

        public static bool TryParseDate(string s, out DateTime date)
        {
            if (s != null)
                s = s.Trim();
            if (string.IsNullOrEmpty(s))
            {
                date = default;
                return false;
            }
            if (DateTime.TryParseExact(s, DATE_FORMAT,
                                             CultureInfo.InvariantCulture,
                                             DateTimeStyles.None, out date))
            {

                return true;
            }
            return false;
        }

        public static string ToDateString(DateTime date)
        {

            return date.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        public static bool IsHeadBranch(string branch)
        {
            if (string.IsNullOrEmpty(branch)) return false;
            if (branch.StartsWith(HEAD_REFS_PREFIX))
                return true;
            return false;
        }

        public static bool IsRemoteBranch(string branch)
        {
            if (string.IsNullOrEmpty(branch)) return false;
            if (branch.StartsWith(REMOTE_REFS_PREFIX))
                return true;
            return false;
        }

        public static string Combine(params string[] paths)
        {
            string str = null;
            /* foreach (var path in paths)
             {
                 if (string.IsNullOrEmpty(path))
                     continue;
                 if (str != null)
                 {
                     if (str.EndsWith("/") || path.StartsWith("/"))
                         str += path;
                     else
                         str += "/" + path;
                 }
                 else
                 {
                     str = path;
                 }
             }

             return str ?? string.Empty;*/
            str = Path.Combine(paths);
            str = str.Replace('\\', '/');
            return str;
        }

        public static string GetScopeArgumentName(GitConfigScope scope)
        {
            switch (scope)
            {
                case GitConfigScope.Local:
                    return ARGUMENT_CONFIG_SCOPE_LOCAL;
                case GitConfigScope.Global:
                    return ARGUMENT_CONFIG_SCOPE_GLOBAL;
                case GitConfigScope.System:
                    return ARGUMENT_CONFIG_SCOPE_SYSTEM;
                case GitConfigScope.Worktree:
                    return ARGUMENT_CONFIG_SCOPE_WORKTREE;
            }
            return null;
        }


        public static bool IsInsideRepositoryDir(string dir)
        {
            if (!Directory.Exists(dir))
                return false;

            string result = RunWithResult("git", "rev-parse --is-inside-work-tree", workDir: dir).Trim();
            if (result == "true")
                return true;
            return false;
        }

        public static string GetRepositoryRoot(string dir)
        {
            if (!Directory.Exists(dir))
                return null;
            string result = RunWithResult("git", "rev-parse --show-toplevel", workDir: dir).Trim();
            return result;
        }

        //.git 目录
        public static bool IsInsideGitDir(string dir)
        {
            if (!Directory.Exists(dir))
                return false;

            string result = RunWithResult("git", "rev-parse --is-inside-git-dir", workDir: dir).Trim();
            if (result == "true")
                return true;
            return false;
        }


    }

    public enum GitConfigScope
    {
        None,
        Local,
        Global,
        System,
        Worktree
    }
}