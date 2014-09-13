using System;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    public class Usage
    {
        internal Usage(JsonObject jObject) {
            Id = (int)jObject["id"];

            jObject = jObject["result"] as JsonObject;
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
            default:
                throw new FormatException("Could not resolve status");
            }
            CreationTime = DateTime.Parse(jObject["creationTime"] as string);
            BitsLeft = (int)jObject["bitsLeft"];
            RequestsLeft = (int)jObject["requestsLeft"];
            TotalBits = JsonHelper.CastValue<long>(jObject["totalBits"]);
            TotalRequests = JsonHelper.CastValue<long>(jObject["totalRequests"]);
        }

        public UsageStatus Status { get; private set; }
        public DateTime CreationTime { get; private set; }
        public int BitsLeft { get; private set; }
        public int RequestsLeft { get; private set; }
        public long TotalBits { get; private set; }
        public long TotalRequests { get; private set; }
        public int Id { get; private set; }
    }

    public enum UsageStatus
    {
        Stopped,
        Paused,
        Running
    }
}
