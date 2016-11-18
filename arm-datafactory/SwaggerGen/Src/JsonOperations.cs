// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.ADF.SwaggerGen
{
    internal static class JsonOperations
    {
        private static StringComparison PropertyNameComparison = StringComparison.OrdinalIgnoreCase;

        public static bool RemoveProperty(JObject jObject, string key)
        {
            JToken token;
            if (jObject.TryGetValue(key, JsonOperations.PropertyNameComparison, out token))
            {
                token.Parent.Remove();
                return true;
            }
            else
            {
                return false;
            }
        }

        public static T GetProperty<T>(JObject jObject, string key)
        {
            JToken token;
            if (jObject.TryGetValue(key, JsonOperations.PropertyNameComparison, out token))
            {
                JValue jValue = token as JValue;
                if (jValue != null && jValue.Value == null)
                {
                    return default(T);
                }

                return (dynamic)token;
            }

            return default(T);
        }

        public static void SetProperty(JObject jObject, string key, object value)
        {
            JToken token;
            if (jObject.TryGetValue(key, JsonOperations.PropertyNameComparison, out token))
            {
                token.Parent.Remove();
            }

            if (value != null)
            {
                jObject[key] = JToken.FromObject(value);
            }
        }
    }
}
