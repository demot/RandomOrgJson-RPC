using System;
using System.Collections;
using System.Runtime.InteropServices;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    /// <summary>
    /// Represents a response from a request to the random.org Api.
    /// </summary>
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
            [FieldOffset(0)]
            public string ErrorMessage;
            [FieldOffset(4)]
            public JsonObject Result;
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
            this.data.Result = jObject;

            if(dataType == RandomOrgDataType.Usage) {
                this.data.Usage = new Usage(jObject);
                return;
            }

            this.AdvisoryDelay = (int)jObject["advisoryDelay"];
            this.BitsUsed = (int)jObject["bitsUsed"];

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

        /// <summary>
        /// Gets the generated integers.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public int[] Integers {
            get {
                if(DataType == RandomOrgDataType.Integer || DataType == RandomOrgDataType.SignedInteger)
                    return data.Integers;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Gets the generated strings.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public string[] Strings {
            get {
                if(DataType == RandomOrgDataType.String || DataType == RandomOrgDataType.SignedString)
                    return data.Strings;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Gets the generated Binary Large OBjects (BLOBs).
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public string[] Blobs {
            get {
                if(DataType == RandomOrgDataType.Blob || DataType == RandomOrgDataType.SignedBlob)
                    return data.Strings;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Gets the generated decimal fractions.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public double[] Decimals {
            get {
                if(DataType == RandomOrgDataType.Decimal || DataType == RandomOrgDataType.SignedDecimal)
                    return data.Doubles;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Gets the generated gaussians.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public double[] Gaussians {
            get {
                if(DataType == RandomOrgDataType.Gaussian || DataType == RandomOrgDataType.SignedGaussian)
                    return data.Doubles;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Gets the generated Universally Unique IDentifiers (UUIDs)s
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Guid[] UUIDs {
            get {
                if(DataType == RandomOrgDataType.UUID || DataType == RandomOrgDataType.SignedUUID)
                    return data.UUIDs;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Gets the usage.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Usage Usage {
            get {
                if(DataType == RandomOrgDataType.Usage)
                    return data.Usage;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Contains a representation of the random object.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public JsonObject RandomObject {
            get {
                if(DataType != RandomOrgDataType.Error)
                    return data.Result;
                else
                    throw new InvalidOperationException();
            }
        }
        /// <summary>
        /// Contains a base64-encoded signature of the response signed with random.org's public key.
        /// (Only if the signed flag in the request was true)
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public string Signature {
            get {
                if(IsSigned)
                    return data.Result["signature"] as string;
                else
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets if the response is signed.
        /// </summary>
        public bool IsSigned {
            get {
                return (int)DataType % 2 == 1;
            }
        }

        /// <summary>
        /// Contains the number of true random bits used by this request.
        /// </summary>
        public int BitsUsed { get; private set; }
        /// <summary>
        /// Contains the number of bits available to the client.
        /// </summary>
        public int BitsLeft { get; private set; }
        /// <summary>
        /// Contains the number of requests left.
        /// </summary>
        public int RequestsLeft { get; private set; }
        /// <summary>
        /// The id, which was also included in the request.
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// Contains the recommended number of milliseconds that the client should delay before
        /// the next request.
        /// </summary>
        public int AdvisoryDelay { get; private set; }
        /// <summary>
        /// The timestamp at which the request was completed.
        /// </summary>
        public DateTime CompletionTime { get; private set; }
        /// <summary>
        /// Contains the type of data of this response.
        /// </summary>
        public RandomOrgDataType DataType { get; private set; }

        /// <summary>
        /// Returns true if the response from random.org contains an error.
        /// </summary>
        /// <param name="errorCode">Numberic representation of the error.</param>
        /// <param name="errorMessage">Error message explaining the error.</param>
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
        /// <summary>
        /// Returns true if the response from random.org contains an error.
        /// </summary>
        /// <param name="errorCode">Numberic representation of the error.</param>
        public bool HasError(out int errorCode) {
            if(DataType == RandomOrgDataType.Error) {
                errorCode = data.ErrorCode;
                return true;
            } else {
                errorCode = -1;
                return false;
            }
        }
        /// <summary>
        /// Returns true if the response from random.org contains an error.
        /// </summary>
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
