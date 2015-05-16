using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace Demot.RandomOrgApi
{
    internal static class JsonHelper
    {
        const char OpenSection = '{',
                       CloseSection = '}',
                       OpenArray = '[',
                       CloseArray = ']',
                       KVSeperator = ':',
                       ESeperator = ',',
                       StringIndicator = '"';
        const string True = "true",
                     False = "false",
                     Null = "null",
                     Seperators = ",}]";

        public static string GetString(bool isArray, params object[] entries) {
            var result = new StringBuilder();
            bool readKey = true;

            if(isArray)
                result.Append(OpenArray);
            else
                result.Append(OpenSection);

            for(int i = 0; i < entries.Length; i++) {
                object current = entries[i];

                if(readKey && !isArray) {
                    if(!(current is string))
                        throw new FormatException("Key has to be a string");
                    result.Append(StringIndicator);
                    result.Append(current);
                    result.Append(StringIndicator);
                    result.Append(KVSeperator);
                } else {
                    if(current is string) {
                        if(current.ToString()[0] == OpenSection)
                            result.Append(current);
                        else {
                            result.Append(StringIndicator);
                            result.Append(current);
                            result.Append(StringIndicator);
                        }
                    } else if(current is int || current is uint ||
                              current is short || current is ushort ||
                              current is byte || current is sbyte ||
                              current is long || current is ulong ||
                              current is float || current is double ||
                              current is decimal) {
                        result.Append(current);
                    } else if(current is bool || current is bool?) {
                        result.Append((bool)current ? True : False);
                    } else if(current is JsonObject)
                        // should be placed before funktion checks for ICollection
                        result.Append(GetString(false, current as JsonObject));
                    else if(current is ICollection) {
                        var array = current as ICollection;
                        var arrayEntries = new object[array.Count];
                        
                        int j = 0;
                        foreach(var element in array)
                            arrayEntries[j++] = element;
                        result.Append(GetString(true, arrayEntries));
                    } else if(current == null)
                        result.Append(Null);
                    else
                        throw new NotImplementedException();

                    result.Append(ESeperator);
                }

                readKey = !readKey;
            }

            // Replace last entry seperator with a closing token
            result[result.Length - 1] = isArray ? CloseArray : CloseSection;

            return result.ToString();
        }

        public static string GetString(bool isArray, JsonObject obj) {
            var objArr = new object[obj.Count * 2];

            for(int i = 0; i < obj.Count; i++) {
                var element = obj.ElementAt(i);
                objArr[i * 2] = element.Key;
                objArr[i * 2 + 1] = element.Value;
            }

            return GetString(isArray, objArr);
        }

        public static JsonObject GetJsonObject(JsonObject obj, string path) {
            JsonObject current = obj;
            var pathSections = path.Split('.');
            for(int i = 0; i < pathSections.Length; i++) {
                object value;
                if(current.TryGetValue(pathSections[i], out value) && value is JsonObject)
                    current = value as JsonObject;
                else
                    return null;
            }
            return current;
        }
        
        public static T CastValue<T>(object obj) {
            if(obj is T)
                return (T)obj;
            else
                return (T)Convert.ChangeType(obj, typeof(T));
            
        }

        static string jsonObjectString;
        static int jsonObjectIndex;
        public static JsonObject GetJsonObject(string jsonString) {
            jsonObjectString = jsonString;
            jsonObjectIndex = 0;

            if(String.IsNullOrWhiteSpace(jsonString))
                return null;
            
            if(skipWhiteSpace() != OpenSection)
                throw new FormatException("No valid jsonString");

            var result = readObject();

            jsonObjectString = null;
            jsonObjectIndex = 0;

            return result;
        }

        static char skipWhiteSpace() {
            char c;
            do {
                c = jsonObjectString[jsonObjectIndex++];
                if(jsonObjectIndex > jsonObjectString.Length)
                    throw new FormatException("Expected data, end of file reached.");
                
            } while(Char.IsWhiteSpace(c));
            return c;
        }
        static JsonObject readObject() {
            var result = new JsonObject();

            do {
                if(skipWhiteSpace() != StringIndicator)
                    throw new FormatException("Missing key token");
                string key = readString();

                if(skipWhiteSpace() == KVSeperator)
                    result.Add(key, readValue());
                else
                    throw new FormatException("Missing key value seperator");

                char c = skipWhiteSpace();
                if(c == ESeperator)
                    continue;
                else if(c == CloseSection)
                    break;
            } while(true);

            return result;
        }
        static object readValue() {
            char c = skipWhiteSpace();
            switch(c) {
            case StringIndicator:
                return readString();
            case OpenArray:
                var array = new List<object>();
                do {
                    array.Add(readValue());
                    c = skipWhiteSpace();
                } while(c == ESeperator);
                if(c != CloseArray)
                    throw new FormatException("Missing close array token");
                return array.ToArray();
            case OpenSection:
                return readObject();
            default:
                // set the index back on c
                jsonObjectIndex--;
                if(Char.IsNumber(c) || c == '+' || c == '-' || c == '.') {
                    return readNumber();
                } else if(c == True[0] && jsonObjectIndex + True.Length < jsonObjectString.Length) {
                    for(int i = 0; i < True.Length; i++)
                        if(jsonObjectString[jsonObjectIndex + i] != True[i])
                            throw new FormatException("Exptected value true");
                    jsonObjectIndex += True.Length;
                    return true;
                } else if(c == False[0] && jsonObjectIndex + False.Length < jsonObjectString.Length) {
                    for(int i = 0; i < False.Length; i++)
                        if(jsonObjectString[jsonObjectIndex + i] != False[i])
                            throw new FormatException("Exptected value false");
                    jsonObjectIndex += False.Length;
                    return false;
                } else if(c == Null[0] && jsonObjectIndex + Null.Length < jsonObjectString.Length) {
                    for(int i = 0; i < Null.Length; i++)
                        if(jsonObjectString[jsonObjectIndex + i] != Null[i])
                            throw new FormatException("Exptected value null");
                    jsonObjectIndex += Null.Length;
                    return null;
                }
                throw new FormatException("Unknown json format");
            }
        }
        static string readString() {
            int left = jsonObjectIndex;
            for(; jsonObjectIndex < jsonObjectString.Length; jsonObjectIndex++) {
                char c = jsonObjectString[jsonObjectIndex];
                if(c == StringIndicator) {
                    if(jsonObjectIndex <= left)
                        break;
                    return jsonObjectString.Substring(left, jsonObjectIndex++ - left);
                }
            }
            throw new FormatException("No valid jsonString");;
        }
        static object readNumber() {
            // Kinda a tricky part:
            // We have to search for a seperator to define the right end of the number
            // First put the right cut at the end of the string, so we can work towards the left
            int right = jsonObjectString.Length;
            // Then we loop throw all seperators we have (kindly found in Seperators)
            for(int i = 0; i < Seperators.Length; i++) {
                // In this function we limit the range to look for a sep., so we only find a closer sep. to the left
                int right2 = jsonObjectString.IndexOf(Seperators[i], jsonObjectIndex, right - jsonObjectIndex);
                // If there is one closer towards the left side, we set the right cut to it's index
                if(right2 != -1)
                    right = right2;
            }
            // If we didn't find any of those fancy sep. there is a good chance the string has a invalid format
            if(right == jsonObjectString.Length)
                throw new FormatException("No valid json string");
            
            
            string number = jsonObjectString.Substring(jsonObjectIndex, right - jsonObjectIndex);
            
            int iResult;
            long lResult;
            double dResult;
            if(checked(Int32.TryParse(number, out iResult))) {
                jsonObjectIndex = right;
                return iResult;
            } else if(Int64.TryParse(number, out lResult)) {
                jsonObjectIndex = right;
                return lResult;
            } else if(Double.TryParse(number,
                                      NumberStyles.Float,
                                      CultureInfo.InvariantCulture,
                                    out dResult)) {
                    jsonObjectIndex = right;
                    return dResult;
            } else
                throw new FormatException("Expected number");
        }
    }
}
