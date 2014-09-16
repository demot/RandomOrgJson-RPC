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
            case RandomOrgDataType.SignedInteger:
                this.data.Integers = extractData<int>(jObject);
                break;
            case RandomOrgDataType.String:
            case RandomOrgDataType.SignedString:
            case RandomOrgDataType.Blob:
            case RandomOrgDataType.SignedBlob:
                this.data.Strings = extractData<string>(jObject);
                break;
            case RandomOrgDataType.Decimal:
            case RandomOrgDataType.SignedDecimal:
            case RandomOrgDataType.Gaussian:
            case RandomOrgDataType.SignedGaussian:
                this.data.Doubles = extractData<double>(jObject);
                break;
            case RandomOrgDataType.UUID:
            case RandomOrgDataType.SignedUUID:
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

        public int[] Integers {
            get {
                if(DataType == RandomOrgDataType.Integer || DataType == RandomOrgDataType.SignedInteger)
                    return data.Integers;
                else
                    throw new InvalidOperationException();
            }
        }
        public string[] Strings {
            get {
                if(DataType == RandomOrgDataType.String || DataType == RandomOrgDataType.SignedString)
                    return data.Strings;
                else
                    throw new InvalidOperationException();
            }
        }
        public string[] Blobs {
            get {
                if(DataType == RandomOrgDataType.Blob || DataType == RandomOrgDataType.SignedBlob)
                    return data.Strings;
                else
                    throw new InvalidOperationException();
            }
        }
        public double[] Decimals {
            get {
                if(DataType == RandomOrgDataType.Decimal || DataType == RandomOrgDataType.SignedDecimal)
                    return data.Doubles;
                else
                    throw new InvalidOperationException();
            }
        }
        public double[] Gaussians {
            get {
                if(DataType == RandomOrgDataType.Gaussian || DataType == RandomOrgDataType.SignedGaussian)
                    return data.Doubles;
                else
                    throw new InvalidOperationException();
            }
        }
        public Guid[] UUIDs {
            get {
                if(DataType == RandomOrgDataType.UUID || DataType == RandomOrgDataType.SignedUUID)
                    return data.UUIDs;
                else
                    throw new InvalidOperationException();
            }
        }
        public Usage Usage {
            get {
                if(DataType == RandomOrgDataType.Usage)
                    return data.Usage;
                else
                    throw new InvalidOperationException();
            }
        }
        public JsonObject RawResult {
            get {
                if(DataType != RandomOrgDataType.Error && DataType != RandomOrgDataType.Usage)
                    return data.Result;
                else
                    throw new InvalidOperationException();
            }
        }
        public string Signature {
            get {
                if(IsSigned)
                    return data.Result["signature"] as string;
                else
                    throw new InvalidOperationException();
            }
        }

        public bool IsSigned {
            get {
                return (int)DataType % 2 == 1;
            }
        }

        public int BitsUsed { get; private set; }
        public int BitsLeft { get; private set; }
        public int RequestsLeft { get; private set; }
        public int Id { get; private set; }
        public int AdvisoryDelay { get; private set; }
        public DateTime CompletionTime { get; private set; }
        public RandomOrgDataType DataType { get; private set; }

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
    }

    public enum RandomOrgDataType
    {
        // asign signed types to odd numbers 
        Integer = 0,
        SignedInteger,
        String,
        SignedString,
        Decimal,
        SignedDecimal,
        Gaussian,
        SignedGaussian,
        UUID,
        SignedUUID,
        Blob,
        SignedBlob,
        Usage = 12,
        Error = 14
    }
}
