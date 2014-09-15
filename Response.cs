using System;
using System.Collections;
using System.Runtime.InteropServices;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    public class Response
    {
        [StructLayout(LayoutKind.Explicit)]
        struct ResponseData
        {
            [FieldOffset(0)]
            public int[] Integers;
            [FieldOffset(0)]
            public string[] Strings;
            [FieldOffset(0)]
            public double[] Doubles;
            [FieldOffset(0)]
            public Guid[] UUIDs;
            [FieldOffset(0)]
            public Usage Usage;
            [FieldOffset(4)]
            public JsonObject Result;
            [FieldOffset(4)]
            public string ErrorMessage;
            [FieldOffset(8)]
            public int ErrorCode;
        }

        ResponseData data;

        internal Response(int errorCode, string errorMessage) {
            this.data.ErrorCode = errorCode;
            this.data.ErrorMessage = errorMessage;
            this.DataType = RandomOrgDataType.Error;
        }

        internal Response(JsonObject jObject, RandomOrgDataType dataType) {
            this.Id = (int)jObject["id"];

            this.DataType = dataType;

            jObject = jObject["result"] as JsonObject;
            if(jObject == null) {
                this.DataType = RandomOrgDataType.Error;
                return;
            }
            this.BitsLeft = (int)jObject["bitsLeft"];
            this.RequestsLeft = (int)jObject["requestsLeft"];

            if(dataType == RandomOrgDataType.Usage) {
                this.data.Usage = new Usage(jObject);
                return;
            }

            this.AdvisoryDelay = (int)jObject["advisoryDelay"];
            this.BitsUsed = (int)jObject["bitsUsed"];
            this.data.Result = jObject;

            jObject = jObject["random"] as JsonObject;
            if(jObject == null) {
                this.DataType = RandomOrgDataType.Error;
                return;
            }
            CompletionTime = DateTime.Parse(jObject["completionTime"] as string);

            switch(dataType) {
            case RandomOrgDataType.Integer:
                this.data.Integers = extractData<int>(jObject);
                break;
            case RandomOrgDataType.String:
            case RandomOrgDataType.Blob:
                this.data.Strings = extractData<string>(jObject);
                break;
            case RandomOrgDataType.Decimal:
            case RandomOrgDataType.Gaussian:
                this.data.Doubles = extractData<double>(jObject);
                break;
            case RandomOrgDataType.UUID:
                string[] rawUUIDs = extractData<string>(jObject);
                Guid[] uuids = new Guid[rawUUIDs.Length];
                for(int i = 0; i < uuids.Length; i++)
                    uuids[i] = new Guid(rawUUIDs[i]);
                this.data.UUIDs = uuids;
                break;
            default:
                break;
            }
        }
        T[] extractData<T>(JsonObject random) {
            var data = random["data"] as IList;
            var result = new T[data.Count];
            for(int i = 0; i < result.Length; i++)
                result[i] = (T)data[i];

            return result;
        }

        public int[] GetIntegers() {
            if(DataType == RandomOrgDataType.Integer)
                return data.Integers;
            else
                throw new InvalidOperationException();
        }
        public string[] GetStrings() {
            if(DataType == RandomOrgDataType.String)
                return data.Strings;
            else
                throw new InvalidOperationException();
        }
        public string[] GetBlobs() {
            if(DataType == RandomOrgDataType.Blob)
                return data.Strings;
            else
                throw new InvalidOperationException();
        }
        public double[] GetDoubles() {
            if(DataType == RandomOrgDataType.Decimal)
                return data.Doubles;
            else
                throw new InvalidOperationException();
        }
        public double[] GetGaussians() {
            if(DataType == RandomOrgDataType.Gaussian)
                return data.Doubles;
            else
                throw new InvalidOperationException();
        }
        public Guid[] GetUUIDs() {
            if(DataType == RandomOrgDataType.UUID)
                return data.UUIDs;
            else
                throw new InvalidOperationException();
        }
        public Usage GetUsage() {
            if(DataType == RandomOrgDataType.Usage)
                return data.Usage;
            else
                throw new InvalidOperationException();
        }
        public JsonObject GetRawResult() {
            if(DataType != RandomOrgDataType.Error && DataType != RandomOrgDataType.Usage)
                return data.Result;
            else
                throw new InvalidOperationException();
        }

        public bool HasError(out int errorCode, out string errorMessage) {
            if(DataType == RandomOrgDataType.Error) {
                errorCode = data.ErrorCode;
                errorMessage = data.ErrorMessage;
                return true;
            } else {
                errorCode = -1;
                errorMessage = null;
                return false;
            }
        }
        public bool HasError(out int errorCode) {
            if(DataType == RandomOrgDataType.Error) {
                errorCode = data.ErrorCode;
                return true;
            } else {
                errorCode = -1;
                return false;
            }
        }
        public bool HasError() {
            return DataType == RandomOrgDataType.Error;
        }

        public int BitsUsed { get; private set; }
        public int BitsLeft { get; private set; }
        public int RequestsLeft { get; private set; }
        public int Id { get; private set; }
        public int AdvisoryDelay { get; private set; }
        public DateTime CompletionTime { get; private set; }
        public RandomOrgDataType DataType { get; private set; }

    }

    public enum RandomOrgDataType
    {
        Integer,
        String,
        Decimal,
        Gaussian,
        UUID,
        Blob,
        Usage,
        Error
    }
}
