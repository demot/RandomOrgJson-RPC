using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    /// <summary>
    /// A wrapper for the Random.org API.
    /// </summary>
    public class RandomOrgApiClient
    {
        const string
            // HTTP header
            BaseUrl = "https://api.random.org/json-rpc/1/invoke",
            ContentType = "application/json",
            
            // basic API methods
            MethodInteger = "generateIntegers",
            MethodDecimalFraction = "generateDecimalFractions",
            MethodGaussian = "generateGaussians",
            MethodString = "generateStrings",
            MethodUUID = "generateUUIDs",
            MethodBlob = "generateBlobs",
            MethodGetUsage = "getUsage",
            // Signed API methods
            MethodSignedInteger = "generateSignedIntegers",
            MethodSignedDecimalFraction = "generateSignedDecimalFractions",
            MethodSignedGaussian = "generateSignedGaussians",
            MethodSignedString = "generateSignedStrings",
            MethodSignedUUID = "generateSignedUUIDs",
            MethodSignedBlobs = "generateSignedBlobs",
            MethodVerifySignature = "verifySignature",
            // Parameters
            ParameterApiKey = "apiKey",
            ParameterCount = "n",
            ParameterLength = "length",
            ParameterCharacters = "characters",
            ParameterReplacement = "replacement",
            ParameterJsonVersion = "jsonrpc",
            ParameterMethod = "method",
            ParameterParams = "params",
            ParameterId = "id",
            ParameterResult = "result",
            ParameterMin = "min",
            ParameterMax = "max",
            ParameterDecimalPlaces = "decimalPlaces",
            ParameterMean = "mean",
            ParameterStandartDeviation = "standartDeviation",
            ParameterSignificantDigits = "significantDigits",
            ParameterSize = "size",
            ParameterFormat = "format",

            ArgumentRangeException = "{0} must be within {1} and {2}",
            ProtocolException = "({0}) {1}",
            StringCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const int
            maxN = 10000,
            rangeMinMax = 1000000000,
            OneKiB = 1024 * 1024;

        // specifies if numbers should be picked with replacement
        // true: numbers contains duplicates
        // false: numbers are unique
        const bool DefaultReplacement = true;

        long maxBlockingTime,
             lastresponseRevieved,
             advisoryDelay;
        Random rand;
        string apiKey;

        /// <summary>
        /// Intializes a new instance of the RandomOrgApiClient.
        /// </summary>
        /// <param name="apiKey">API key, used to keep track of the true random bit usage.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public RandomOrgApiClient(string apiKey) {
            if(String.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException("apiKey should contain a valid key.");
            
            this.apiKey = apiKey;
            this.MaxBlockingTime = 3000;
            this.rand = new Random();
        }


        /// <summary>
        /// Generates true random strings.
        /// </summary>
        /// <param name="n">Number of strings to generate, must be within the [1, 1e4] range.</param>
        /// <param name="length">Length of each generated string, must be within the [1, 20] range.</param>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <param name="characters">Characters to occur in the random strings, maximal length: 80 UTF-8 encoded
        ///                          characters. If the value is null, the default value will be used</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateStrings(int n, int length, int id, string characters = StringCharacters, bool replacement = DefaultReplacement) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(!String.IsNullOrEmpty(characters) && characters.Length > 80)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "characters length", 1, 80));
            else
                characters = StringCharacters;

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           ParameterLength, length,
                                           ParameterCharacters, characters,
                                           ParameterReplacement, replacement);

            var response = sendRequest(MethodString, parameters, id);
            return createResponse(response, RandomOrgDataType.String);
        }
        /// <summary>
        /// Generates true random strings.
        /// </summary>
        /// <param name="n">Number of strings to generate, must be within the [1, 1e4] range.</param>
        /// <param name="length">Length of each generated string, must be within the [1, 20] range.</param>
        /// <param name="characters">Characters to occur in the random strings, maximal length: 80 UTF-8 encoded
        ///                          characters. If the value is null, the default value will be used</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateStrings(int n, int length, string characters = StringCharacters, bool replacement = DefaultReplacement) {
            return GenerateStrings(n, length, rand.Next(), characters, replacement);
        }

        /// <summary>
        /// Generates true random integers.
        /// </summary>
        /// <param name="n">Number of integers to generate, must be within the [1, 1e4] range.</param>
        /// <param name="min">Lower boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="max">Upper boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateIntegers(int n, int min, int max, int id, bool replacement = DefaultReplacement) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(min < -rangeMinMax || min > rangeMinMax || max < -rangeMinMax || max > rangeMinMax)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "Range", -rangeMinMax, rangeMinMax));

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           ParameterMin, min,
                                           ParameterMax, max,
                                           ParameterReplacement, replacement);

            var response = sendRequest(MethodInteger, parameters, id);
            return createResponse(response, RandomOrgDataType.Integer);
        }
        /// <summary>
        /// Generates true random integers.
        /// </summary>
        /// <param name="n">Number of integers to generate, must be within the [1, 1e4] range.</param>
        /// <param name="min">Lower boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="max">Upper boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateIntegers(int n, int min, int max, bool replacement = DefaultReplacement) {
            return GenerateIntegers(n, min, max, rand.Next(), replacement);
        }

        /// <summary>
        /// Generates true random decimal fractions.
        /// </summary>
        /// <param name="n">Number of decimal fractions to generate, must be within the [1, 1e4] range.</param>
        /// <param name="decimalPlaces">Number of decimal places to use, must be within the [1, 20] range.</param>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateDecimalFractions(int n, byte decimalPlaces, int id, bool replacement = DefaultReplacement) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(decimalPlaces < 1 || decimalPlaces > 20)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "decimalPlaces", 1, 20));

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           ParameterDecimalPlaces, decimalPlaces,
                                           ParameterReplacement, replacement);

            var response = sendRequest(MethodDecimalFraction, parameters, id);
            return createResponse(response, RandomOrgDataType.Decimal);
        }
        /// <summary>
        /// Generates true random decimal fractions.
        /// </summary>
        /// <param name="n">Number of decimal fractions to generate, must be within the [1, 1e4] range.</param>
        /// <param name="decimalPlaces">Number of decimal places to use, must be within the [1, 20] range.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateDecimalFractions(int n, byte decimalPlaces, bool replacement = DefaultReplacement) {
            return GenerateDecimalFractions(n, decimalPlaces, rand.Next(), replacement);
        }

        /// <summary>
        /// Generates true random numbers from a gaussian distribution.
        /// </summary>
        /// <param name="n">Number of gaussians to generate, must be within the [1, 1e4] range.</param>
        /// <param name="mean">Distribution's mean, must be within the [-1e6, 1e6] range.</param>
        /// <param name="standartDeviation">Distribution's standart deviation, must be within the [-1e6, 1e6] range.</param>
        /// <param name="significantDigits">Number of significant digits to use, must be within the [2, 20] range.</param>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateGaussians(int n, double mean, double standartDeviation, byte significantDigits, int id) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(mean < -1e6 || mean > 1e6)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "mean", -1e6, 1e6));
            if(standartDeviation < -1e6 || standartDeviation > 1e6)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "standartDeviation", -1e6, 1e6));
            if(significantDigits < 2 || significantDigits > 20)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "significantDigits", 2, 20));

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           ParameterMean, mean,
                                           ParameterStandartDeviation, standartDeviation,
                                           ParameterSignificantDigits, significantDigits);

            var response = sendRequest(MethodGaussian, parameters, id);
            return createResponse(response, RandomOrgDataType.Gaussian);
        }

        /// <summary>
        /// Generates true random Universally Unique IDentifiers (UUIDs) specified by RFC 4122.
        /// </summary>
        /// <param name="n">Number of UUIDs to generate, must be within the [1, 1e3] range.</param>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateUUIDs(int n, int id) {
            if(n < 1 || n > 1000)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, 1000));
            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n);

            var response = sendRequest(MethodUUID, parameters, id);
            return createResponse(response, RandomOrgDataType.UUID);
        }
        /// <summary>
        /// Generates true random Universally Unique IDentifiers (UUIDs) specified by RFC 4122.
        /// </summary>
        /// <param name="n">Number of UUIDs to generate, must be within the [1, 1e3] range.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateUUIDs(int n) {
            return GenerateUUIDs(n, rand.Next());
        }

        /// <summary>
        /// Generates Binary Large Objects (BLOBs) containing true random data.
        /// </summary>
        /// <param name="n">Number of blobs to generate, must be within the [1, 100] range.</param>
        /// <param name="size">Size of each blob, must be within the [1, 2^20] range.</param>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <param name="format">Format to display the blobs, values allow are "base64" and "hex".</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateBlobs(int n, int size, int id, string format = "base64") {
            if(n < 1 || n > 100)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, 100));
            if(size < 1 || size > 1024 * 1024)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "size", 1, 1024 * 1024));
            if(size % 8 != 0)
                throw new ArgumentException("size must be divisible by 8");
            if(format != "base64" || format != "hex")
                throw new ArgumentException("format has to be base64 or hex");

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           ParameterSize, size,
                                           ParameterFormat, format);

            var response = sendRequest(MethodBlob, parameters, id);
            return createResponse(response, RandomOrgDataType.Blob);
        }
        /// <summary>
        /// Generates Binary Large Objects (BLOBs) containing true random data.
        /// </summary>
        /// <param name="n">Number of blobs to generate, must be within the [1, 100] range.</param>
        /// <param name="size">Size of each blob, must be within the [1, 2^20] range.</param>
        /// <param name="format">Format to display the blobs, values allow are "base64" and "hex".</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateBlobs(int n, int size, string format = "base64") {
            return GenerateBlobs(n, size, rand.Next(), format);
        }

        /// <summary>
        /// Returns information about the usage of the given API key.
        /// </summary>
        /// <param name="id">User defined number wich will be returned in the response.</param>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GetUsage(int id) {
            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey);

            var response = sendRequest(MethodGetUsage, parameters, id);
            return new Response(response, RandomOrgDataType.Usage);
        }
        /// <summary>
        /// Returns information about the usage of the given API key.
        /// </summary>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GetUsage() {
            return GetUsage(rand.Next());
        }

        JsonObject sendRequest(string method, string methodParams, int id) {
            var request = JsonHelper.GetString(false,
                                        ParameterJsonVersion, "2.0",
                                        ParameterMethod, method,
                                        ParameterParams, methodParams,
                                        ParameterId, id);

            // Handles the blocking time
            long waitingTime = advisoryDelay - (DateTime.Now.Ticks - lastresponseRevieved);
            if(waitingTime > 0) {
                if(waitingTime > maxBlockingTime)
                    throw new TimeoutException("The advised waiting time is higher than the maximal blocking time");
                else
                    Thread.Sleep(TimeSpan.FromTicks(waitingTime));
            }

            var response = post(request);
            if(ThrowProtocolErrors)
                checkErrors(response);

            return response;
        }
        JsonObject post(string request) {
            JsonObject result;
            byte[] content = Encoding.UTF8.GetBytes(request);
            var httpRequest = WebRequest.Create(BaseUrl) as HttpWebRequest;
            httpRequest.Method = "POST";
            httpRequest.ContentType = ContentType;
            try {
                using(var requestStream = httpRequest.GetRequestStream())
                    requestStream.Write(content, 0, content.Length);
                
                using(var response = httpRequest.GetResponse())
                    using(var reader = new StreamReader(response.GetResponseStream()))
                        result = JsonHelper.GetJsonObject(reader.ReadToEnd());
                
            } catch(Exception e) {
                throw e;
            }

            return result;
        }
        void checkErrors(JsonObject response) {
            var error = JsonHelper.GetJsonObject(response, "error");
            if(error != null)
                throw new ProtocolViolationException(String.Format(ProtocolException, error["code"], error["message"]));
        }
        Response createResponse(JsonObject rawResponse, RandomOrgDataType dataType) {
            var result = new Response(rawResponse, dataType);
            Exception ex = new Exception();
            
            advisoryDelay = result.AdvisoryDelay * TimeSpan.TicksPerMillisecond;
            lastresponseRevieved = DateTime.Now.Ticks;

            return result;
        }

        /// <summary>
        /// Maximal time this class is blocking the thread while waiting for the server to accept new requests.
        /// </summary>
        /// <exception cref="System.ArgumentException"></exception>
        public int MaxBlockingTime {
            get {
                return (int)(maxBlockingTime / TimeSpan.TicksPerMillisecond);
            }
            set {
                if(value < 0)
                    throw new ArgumentException("Blocking time cannot be negative.");

                maxBlockingTime = TimeSpan.TicksPerMillisecond * value;
            }
        }

        public bool ThrowProtocolErrors { get; set; }
    }
}
