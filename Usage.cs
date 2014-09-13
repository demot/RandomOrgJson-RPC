using System;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    public class Usage
    {
        int errorCode;
        string errorMessage;

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

        public UsageStatus Status { get; private set; }
        public DateTime CreationTime { get; private set; }
        public long TotalBits { get; private set; }
        public long TotalRequests { get; private set; }
    }

    public enum UsageStatus
    {
        Stopped,
        Paused,
        Running
    }
}
