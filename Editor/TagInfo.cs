using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.Git;

namespace Unity.Git
{

    public class TagInfo
    {
        public string tag;
        public string commitId;
        public string author;
        public DateTime time;

        public static bool TryParse(string input, out TagInfo tagInfo)
        {
            tagInfo = null;
            if (string.IsNullOrEmpty(input)) return false;

            Regex commitIdRegex = new Regex("^\\s*commit (?<commit_id>\\w+)");
            TagInfo tmp = new TagInfo();
            foreach (var line in input.Split("\n"))
            {
                string line2 = line.Trim();
                var m = commitIdRegex.Match(line);
                if (m.Success)
                {
                    tmp.commitId = m.Groups["commit_id"].Value;
                }

                if (line2.StartsWith("Author: "))
                {
                    tmp.author = line2.Substring("Author: ".Length);
                }
                if (line2.StartsWith("Date: "))
                {
                    string dateStr = line2.Substring("Date: ".Length).Trim();
                    DateTime time;
                    if (GitUtility.TryParseDate(dateStr, out time))
                    {
                        tmp.time = time;
                    }
                }

            }

            if (!string.IsNullOrEmpty(tmp.commitId))
            {
                tagInfo = tmp;
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"Tag: {tag}\nCommitId: {commitId}\nAuthor: {author}\nDate: {time}";
        }
    }
}
