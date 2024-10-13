using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Git
{
    public class GitFields
    {
        public const string RefName = "refname";
        public const string Subject = "subject";
        public const string Body = "body";
        public const string Committerdate = "committerdate";
        public const string HEAD = "HEAD";
        public const string CreatorDate = "creatordate";
        public const string TaggerDate = "taggerdate";
        public const string VersionRefName = "version:refname";
        public const string ObjectType = "objecttype";
        public const string ObjectSize = "objectsize";
        public const string ObjectNAME = "objectname";
        public const string AuthorName = "authorname";
        public const string AuthorEmail = "authoremail";
        public const string AuthorDate = "authordate";
        public const string UpStream = "upstream";
        public const string Push = "push";

        public static string Short(string field)
        {
            return $"{field}:short";
        }
        public static string Short(string field, int length)
        {
            return $"{field}:short={length}";
        }

        public static string Strip(string field, int length)
        {
            return $"{field}:strip={length}";
        }
        public static string StripRight(string field, int length)
        {
            return $"{field}:rstrip={length}";
        }

        public static string Track(string field)
        {
            return $"{field}:track";
        }

        public static string RemoteName(string field)
        {
            return $"{field}:remotename";
        }

        public static string RemoteRef(string field)
        {
            return $"{field}:remoteref";
        }
        public static string Disk(string field)
        {
            return $"{field}:disk";
        }
    }


}