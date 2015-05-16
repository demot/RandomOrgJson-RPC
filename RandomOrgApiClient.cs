using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
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
            
            // Basic API methods
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
            MethodSignedBlob = "generateSignedBlobs",
            MethodVerifySignature = "verifySignature",
            // Parameters
            ParameterApiKey = "apiKey",
            ParameterCount = "n",
            ParameterReplacement = "replacement",

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
        string apiKey,
               hashedKey;

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
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="characters">Characters to occur in the random strings, maximal length: 80 UTF-8 encoded
        ///                          characters. If the value is null, the default value will be used</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateStrings(int n, int length, int id, bool signed, string characters = StringCharacters, bool replacement = DefaultReplacement) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(!String.IsNullOrEmpty(characters) && characters.Length > 80)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "characters length", 1, 80));
            else
                characters = StringCharacters;

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           "length", length,
                                           "characters", characters,
                                           ParameterReplacement, replacement);

            var response = sendRequest(signed ? MethodSignedString : MethodString, parameters, id);
            return createResponse(response, signed ? RandomOrgDataType.SignedString : RandomOrgDataType.String);
        }
        /// <summary>
        /// Generates true random strings.
        /// </summary>
        /// <param name="n">Number of strings to generate, must be within the [1, 1e4] range.</param>
        /// <param name="length">Length of each generated string, must be within the [1, 20] range.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="characters">Characters to occur in the random strings, maximal length: 80 UTF-8 encoded
        ///                          characters. If the value is null, the default value will be used</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateStrings(int n, int length, bool signed, string characters = StringCharacters, bool replacement = DefaultReplacement) {
            return GenerateStrings(n, length, rand.Next(), signed, characters, replacement);
        }
        /// <summary>
        /// Generates true random strings.
        /// </summary>
        /// <param name="n">Number of strings to generate, must be within the [1, 1e4] range.</param>
        /// <param name="length">Length of each generated string, must be within the [1, 20] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="characters">Characters to occur in the random strings, maximal length: 80 UTF-8 encoded
        ///                          characters. If the value is null, the default value will be used</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateStrings(int n, int length, int id, string characters = StringCharacters, bool replacement = DefaultReplacement) {
            return GenerateStrings(n, length, id, false, characters, replacement);
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
            return GenerateStrings(n, length, rand.Next(), false, characters, replacement);
        }

        /// <summary>
        /// Generates true random integers.
        /// </summary>
        /// <param name="n">Number of integers to generate, must be within the [1, 1e4] range.</param>
        /// <param name="min">Lower boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="max">Upper boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateIntegers(int n, int min, int max, int id, bool signed, bool replacement = DefaultReplacement) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(min < -rangeMinMax || min > rangeMinMax || max < -rangeMinMax || max > rangeMinMax)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "Range", -rangeMinMax, rangeMinMax));

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           "min", min,
                                           "max", max,
                                           ParameterReplacement, replacement);

            var response = sendRequest(signed ? MethodSignedInteger : MethodInteger, parameters, id);
            return createResponse(response, signed ? RandomOrgDataType.SignedInteger : RandomOrgDataType.Integer);
        }
        /// <summary>
        /// Generates true random integers.
        /// </summary>
        /// <param name="n">Number of integers to generate, must be within the [1, 1e4] range.</param>
        /// <param name="min">Lower boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="max">Upper boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateIntegers(int n, int min, int max, bool signed, bool replacement = DefaultReplacement) {
            return GenerateIntegers(n, min, max, rand.Next(), signed, replacement);
        }
        /// <summary>
        /// Generates true random integers.
        /// </summary>
        /// <param name="n">Number of integers to generate, must be within the [1, 1e4] range.</param>
        /// <param name="min">Lower boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="max">Upper boundary for the range, must be within the [-1e9, 1e9] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateIntegers(int n, int min, int max, int id, bool replacement = DefaultReplacement) {
            return GenerateIntegers(n, min, max, id, false, replacement);
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
            return GenerateIntegers(n, min, max, rand.Next(), false, replacement);
        }

        /// <summary>
        /// Generates true random decimal fractions.
        /// </summary>
        /// <param name="n">Number of decimal fractions to generate, must be within the [1, 1e4] range.</param>
        /// <param name="decimalPlaces">Number of decimal places to use, must be within the [1, 20] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateDecimalFractions(int n, byte decimalPlaces, int id, bool signed, bool replacement = DefaultReplacement) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(decimalPlaces < 1 || decimalPlaces > 20)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "decimalPlaces", 1, 20));

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           "decimalPlaces", decimalPlaces,
                                           ParameterReplacement, replacement);

            var response = sendRequest(signed ? MethodSignedDecimalFraction : MethodDecimalFraction, parameters, id);
            return createResponse(response, signed ? RandomOrgDataType.SignedDecimal : RandomOrgDataType.Decimal);
        }
        /// <summary>
        /// Generates true random decimal fractions.
        /// </summary>
        /// <param name="n">Number of decimal fractions to generate, must be within the [1, 1e4] range.</param>
        /// <param name="decimalPlaces">Number of decimal places to use, must be within the [1, 20] range.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateDecimalFractions(int n, byte decimalPlaces, bool signed, bool replacement = DefaultReplacement) {
            return GenerateDecimalFractions(n, decimalPlaces, rand.Next(), signed, replacement);
        }
        /// <summary>
        /// Generates true random decimal fractions.
        /// </summary>
        /// <param name="n">Number of decimal fractions to generate, must be within the [1, 1e4] range.</param>
        /// <param name="decimalPlaces">Number of decimal places to use, must be within the [1, 20] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="replacement">Specifies if numbers should be picked with replacement. If true, the generated numbers may contain
        ///                           duplicates, otherwise the picked numbers are unique.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateDecimalFractions(int n, byte decimalPlaces, int id, bool replacement = DefaultReplacement) {
            return GenerateDecimalFractions(n, decimalPlaces, id, false, replacement);
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
        /// <param name="standardDeviation">Distribution's standard deviation, must be within the [-1e6, 1e6] range.</param>
        /// <param name="significantDigits">Number of significant digits to use, must be within the [2, 20] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateGaussians(int n, double mean, double standardDeviation, byte significantDigits, int id, bool signed) {
            if(n < 1 || n > maxN)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, maxN));
            if(mean < -1e6 || mean > 1e6)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "mean", -1e6, 1e6));
            if(standardDeviation < -1e6 || standardDeviation > 1e6)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "standardDeviation", -1e6, 1e6));
            if(significantDigits < 2 || significantDigits > 20)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "significantDigits", 2, 20));

            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n,
                                           "mean", mean,
                                           "standardDeviation", standardDeviation,
                                           "significantDigits", significantDigits);

            var response = sendRequest(signed ? MethodSignedGaussian : MethodGaussian, parameters, id);
            return createResponse(response, signed ? RandomOrgDataType.SignedGaussian : RandomOrgDataType.Gaussian);
        }
        /// <summary>
        /// Generates true random numbers from a gaussian distribution.
        /// </summary>
        /// <param name="n">Number of gaussians to generate, must be within the [1, 1e4] range.</param>
        /// <param name="mean">Distribution's mean, must be within the [-1e6, 1e6] range.</param>
        /// <param name="standardDeviation">Distribution's standard deviation, must be within the [-1e6, 1e6] range.</param>
        /// <param name="significantDigits">Number of significant digits to use, must be within the [2, 20] range.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateGaussians(int n, double mean, double standardDeviation, byte significantDigits, bool signed) {
            return GenerateGaussians(n, mean, standardDeviation, significantDigits, rand.Next(), signed);
        }
        /// <summary>
        /// Generates true random numbers from a gaussian distribution.
        /// </summary>
        /// <param name="n">Number of gaussians to generate, must be within the [1, 1e4] range.</param>
        /// <param name="mean">Distribution's mean, must be within the [-1e6, 1e6] range.</param>
        /// <param name="standardDeviation">Distribution's standard deviation, must be within the [-1e6, 1e6] range.</param>
        /// <param name="significantDigits">Number of significant digits to use, must be within the [2, 20] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateGaussians(int n, double mean, double standardDeviation, byte significantDigits, int id) {
            return GenerateGaussians(n, mean, standardDeviation, significantDigits, id, false);
        }
        /// <summary>
        /// Generates true random numbers from a gaussian distribution.
        /// </summary>
        /// <param name="n">Number of gaussians to generate, must be within the [1, 1e4] range.</param>
        /// <param name="mean">Distribution's mean, must be within the [-1e6, 1e6] range.</param>
        /// <param name="standardDeviation">Distribution's standard deviation, must be within the [-1e6, 1e6] range.</param>
        /// <param name="significantDigits">Number of significant digits to use, must be within the [2, 20] range.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateGaussians(int n, double mean, double standardDeviation, byte significantDigits) {
            return GenerateGaussians(n, mean, standardDeviation, significantDigits, rand.Next(), false);
        }

        /// <summary>
        /// Generates true random Universally Unique IDentifiers (UUIDs) specified by RFC 4122.
        /// </summary>
        /// <param name="n">Number of UUIDs to generate, must be within the [1, 1e3] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateUUIDs(int n, int id, bool signed) {
            if(n < 1 || n > 1000)
                throw new ArgumentOutOfRangeException(String.Format(ArgumentRangeException, "n", 1, 1000));
            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey,
                                           ParameterCount, n);

            var response = sendRequest(signed ? MethodSignedUUID : MethodUUID, parameters, id);
            return createResponse(response, signed ? RandomOrgDataType.SignedUUID : RandomOrgDataType.UUID);
        }
        /// <summary>
        /// Generates true random Universally Unique IDentifiers (UUIDs) specified by RFC 4122.
        /// </summary>
        /// <param name="n">Number of UUIDs to generate, must be within the [1, 1e3] range.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateUUIDs(int n, bool signed) {
            return GenerateUUIDs(n, rand.Next(), signed);
        }
        /// <summary>
        /// Generates true random Universally Unique IDentifiers (UUIDs) specified by RFC 4122.
        /// </summary>
        /// <param name="n">Number of UUIDs to generate, must be within the [1, 1e3] range.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateUUIDs(int n, int id) {
            return GenerateUUIDs(n, id, false);
        }
        /// <summary>
        /// Generates true random Universally Unique IDentifiers (UUIDs) specified by RFC 4122.
        /// </summary>
        /// <param name="n">Number of UUIDs to generate, must be within the [1, 1e3] range.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateUUIDs(int n) {
            return GenerateUUIDs(n, rand.Next(), false);
        }

        /// <summary>
        /// Generates Binary Large Objects (BLOBs) containing true random data.
        /// </summary>
        /// <param name="n">Number of blobs to generate, must be within the [1, 100] range.</param>
        /// <param name="size">Size of each blob, must be within the [1, 2^20] range and must be divisable by 8.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="format">Format to display the blobs, values allow are "base64" and "hex".</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateBlobs(int n, int size, int id, bool signed, string format = "base64") {
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
                                           "size", size,
                                           "format", format);

            var response = sendRequest(signed ? MethodSignedBlob : MethodBlob, parameters, id);
            return createResponse(response, signed ? RandomOrgDataType.SignedBlob : RandomOrgDataType.Blob);
        }
        /// <summary>
        /// Generates Binary Large Objects (BLOBs) containing true random data.
        /// </summary>
        /// <param name="n">Number of blobs to generate, must be within the [1, 100] range.</param>
        /// <param name="size">Size of each blob, must be within the [1, 2^20] range and must be divisable by 8.</param>
        /// <param name="signed">Specifies if the result should be signed with a SHA-512 key which you can use to verify it against
        ///                      random.org's public key.</param>
        /// <param name="format">Format to display the blobs, values allow are "base64" and "hex".</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateBlobs(int n, int size, bool signed, string format = "base64") {
            return GenerateBlobs(n, size, rand.Next(), signed, format);
        }
        /// <summary>
        /// Generates Binary Large Objects (BLOBs) containing true random data.
        /// </summary>
        /// <param name="n">Number of blobs to generate, must be within the [1, 100] range.</param>
        /// <param name="size">Size of each blob, must be within the [1, 2^20] range and must be divisable by 8.</param>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <param name="format">Format to display the blobs, values allow are "base64" and "hex".</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GenerateBlobs(int n, int size, int id, string format = "base64") {
            return GenerateBlobs(n, size, id, false, format);
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
            return GenerateBlobs(n, size, rand.Next(), false, format);
        }

        /// <summary>
        /// Returns information about the usage of the given API key.
        /// </summary>
        /// <param name="id">User defined number which will be returned in the response.</param>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GetUsage(int id) {
            var parameters = JsonHelper.GetString(false,
                                           ParameterApiKey, apiKey);

            var response = sendRequest(MethodGetUsage, parameters, id);
            return createResponse(response, RandomOrgDataType.Usage);
        }
        /// <summary>
        /// Returns information about the usage of the given API key.
        /// </summary>
        /// <exception cref="System.TimeoutException"></exception>
        /// <exception cref="ProtocolViolationException"></exception>
        public Response GetUsage() {
            return GetUsage(rand.Next());
        }

        /// <summary>
        /// Verifies the signature of a signed response previously recieved by random.org.
        /// </summary>
        /// <param name="recievedObject">Signed package previously recieved.</param>
        /// <param name="signature">SHA-512 signature to check against with.</param>
        /// <exception cref="System.TimeoutException"></exception>
        public bool VerifySignature(JsonObject recievedObject, string signature) {
            // randomObject may contain bitsUsed, bitsLeft, ... which shouldn't be part of the request
            removeUsageData(recievedObject);

            var response = sendRequest(MethodVerifySignature, JsonHelper.GetString(false, recievedObject), rand.Next());
            return extractVerification(response);
        }


        JsonObject sendRequest(string method, string methodParams, int id) {
            var request = JsonHelper.GetString(false,
                                        "jsonrpc", "2.0",
                                        "method", method,
                                        "params", methodParams,
                                        "id", id);

            // Handles the blocking time
            long waitingTime = advisoryDelay - (DateTime.Now.Ticks - lastresponseRevieved);
            if(waitingTime > 0) {
                if(waitingTime > maxBlockingTime)
                    throw new TimeoutException("The advised waiting time is higher than the maximal blocking time");
                else
                    Thread.Sleep(TimeSpan.FromTicks(waitingTime));
            }

            return post(request);
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
            } finally {
                lastresponseRevieved = DateTime.Now.Ticks;
            }

            return result;
        }
        Response createResponse(JsonObject rawResponse, RandomOrgDataType dataType) {
            // check for errors
            var error = JsonHelper.GetJsonObject(rawResponse, "error");
            if(error != null) {
                advisoryDelay = 0;
                if(ThrowProtocolErrors)
                    throw new ProtocolViolationException(String.Format(ProtocolException, error["code"], error["message"]));
                else
                    return new Response((int)error["code"], error["message"] as string);
            }
            
            var result = new Response(rawResponse, dataType);
            advisoryDelay = result.AdvisoryDelay * TimeSpan.TicksPerMillisecond;
            
            return result;
        }

        bool extractVerification(JsonObject rawResponse) {
            var result = JsonHelper.GetJsonObject(rawResponse, "result");

            if(result != null) {
                object authenticity;
                if(result.TryGetValue("authenticity", out authenticity)) {
                    if(authenticity is bool)
                        return (bool)authenticity;
                }
            }
            return false;
        }
        void removeUsageData(JsonObject rawResult) {
            rawResult.Remove("bitsUsed");
            rawResult.Remove("bitsLeft");
            rawResult.Remove("requestsLeft");
            rawResult.Remove("advisoryDelay");
        }

        static string createSHA512Hash(string value) {
            string result;

            using(var sha512 = SHA512Managed.Create())
                result = Convert.ToBase64String(sha512.ComputeHash(Encoding.UTF8.GetBytes(value)));
            
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
        /// <summary>
        /// Gets or sets if protocol violation exceptions should be thrown.
        /// </summary>
        public bool ThrowProtocolErrors { get; set; }
        /// <summary>
        /// Gets a string containing a base64-encoded SHA-512 hash of the Api key.
        /// </summary>
        public string HashedApiKey {
            get {
                if(String.IsNullOrEmpty(hashedKey))
                    hashedKey = createSHA512Hash(apiKey);
                return hashedKey;
            }
        }
    }
}
