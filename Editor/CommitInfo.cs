using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Git;

namespace Unity.Git
{
    public class CommitInfo : ISerializationCallbackReceiver
    {
        public string id;
        public string message;
        public long timestamp;
        public DateTime time;
        public string authorName;
        public string anthorEmail;

        public void OnAfterDeserialize()
        {
            time = GitUtility.FromTimestamp(timestamp);
        }

        public void OnBeforeSerialize()
        {
            timestamp = GitUtility.ToTimestamp(time);
        }
        public override string ToString()
        {
            return $"CommitId: {id}\nDate: {time}\nAuthor: {authorName}\nEmail: {anthorEmail}\nMessage: {message}";
        }

    }
}
