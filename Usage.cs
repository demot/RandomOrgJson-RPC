using System;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    /// <summary>
    /// Represents the usage of a specific random.org Api key.
    /// </summary>
    public class Usage
    {
        internal Usage(JsonObject jObject) {
            switch(jObject["status"] as string) {
            case "running":
                Status = UsageStatus.Running;
                break;
            case "stopped":
                Status = UsageStatus.Stopped;
                break;
            case "paused":
                Status = UsageStatus.Paused;
                break;
            }
            CreationTime = DateTime.Parse(jObject["creationTime"] as string);
            TotalBits = JsonHelper.CastValue<long>(jObject["totalBits"]);
            TotalRequests = JsonHelper.CastValue<long>(jObject["totalRequests"]);
        }

        /// <summary>
        /// Indicates the Api key's current status.
        /// </summary>
        public UsageStatus Status { get; private set; }
        /// <summary>
        /// The timestamp at which the Api key was created.
        /// </summary>
        public DateTime CreationTime { get; private set; }
        /// <summary>
        /// Contains the total number of bits used.
        /// </summary>
        public long TotalBits { get; private set; }
        /// <summary>
        /// Contains the total number of requests.
        /// </summary>
        public long TotalRequests { get; private set; }
    }

    public enum UsageStatus
    {
        Stopped,
        Paused,
        Running
    }
}
