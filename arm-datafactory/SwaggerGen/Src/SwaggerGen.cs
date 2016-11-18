// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ADF.SwaggerGen
{
    internal class SwaggerGenerator
    {
        private const string KeyDefinitions = "definitions";
        private const string KeyProperties = "properties";
        private const string KeyType = "type";
        private const string KeyRef = "$ref";
        private const string KeyMarkObject = "x-ms-mark-object";
        private const string KeyDescription = "description";

        private const string KeyItems = "items";
        private const string KeyAdditionalProperties = "additionalProperties";

        private const string ValueObject = "object";
        private const string ValueArray = "array";

        private static readonly string[] AdditionalPropertiesToRemove = new string[]
        {
            "additionalProperties",
            "items",
            "format",
            "minimum",
            "maximum"
        };

        private static StringComparison PropertyNameComparison = StringComparison.OrdinalIgnoreCase;

        private static readonly Regex UnProcessedMarkObject = new Regex("['\"]" + SwaggerGenerator.KeyMarkObject + "['\"]\\s*:\\s*true", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private JsonSerializerSettings settings { get; set; }

        public SwaggerGenerator()
        {
            this.settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
        }

        public string CreateSameSwaggerSpec(string json)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), this.settings);
        }

        public string CreateNewSwaggerSpec(string json)
        {
            JObject spec = JsonConvert.DeserializeObject<JObject>(json);
            JObject definitions = JsonOperations.GetProperty<JObject>(spec, SwaggerGenerator.KeyDefinitions);
            foreach (JProperty model in definitions.Children())
            {
                this.UpdateModel(model.Value as JObject);
            }

            string modifiedJson = JsonConvert.SerializeObject(spec, this.settings);
            if (SwaggerGenerator.UnProcessedMarkObject.Match(modifiedJson).Success)
            {
                throw new MarkObjectException(string.Format(CultureInfo.InvariantCulture, "Some instances of {0} were not processed.", SwaggerGenerator.KeyMarkObject));
            }

            return modifiedJson;
        }

        private void UpdateModel(JObject model)
        {
            try
            {
                JObject additionalSection = JsonOperations.GetProperty<JObject>(model, SwaggerGenerator.KeyItems)
                    ?? JsonOperations.GetProperty<JObject>(model, SwaggerGenerator.KeyAdditionalProperties);
                if (additionalSection != null)
                {
                    JToken markValue = JsonOperations.GetProperty<JToken>(additionalSection, KeyMarkObject);
                    if (markValue != null && (bool)markValue == true)
                    {
                        this.HandleMarkObject(additionalSection);
                    }
                    else
                    {
                        this.UpdateModel(additionalSection);
                    }
                }

                JObject propertiesSection = JsonOperations.GetProperty<JObject>(model, SwaggerGenerator.KeyProperties);
                if (propertiesSection != null)
                {
                    // else iterate on all the properties
                    foreach (JProperty modelProperty in propertiesSection.Children())
                    {
                        JObject modelPropertyValue = modelProperty.Value as JObject;

                        JToken markValue = JsonOperations.GetProperty<JToken>(modelPropertyValue, KeyMarkObject);
                        if (markValue != null && (bool)markValue == true)
                        {
                            this.HandleMarkObject(modelPropertyValue);
                        }
                        else
                        {
                            JToken inlinedComplexModel = JsonOperations.GetProperty<JToken>(modelPropertyValue, SwaggerGenerator.KeyProperties);
                            if (inlinedComplexModel != null)
                            {
                                this.UpdateModel(modelPropertyValue);
                            }
                        }
                    }
                }
            }
            catch (MarkObjectException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MarkObjectException(string.Format(CultureInfo.InvariantCulture, "Error in translating model {0}.", model.Path), ex);
            }
        }

        private void HandleMarkObject(JObject model)
        {
            var type = JsonOperations.GetProperty<string>(model, SwaggerGenerator.KeyType) ?? ValueObject;

            // Since we will remove the items and additionalProperties section, check if it contains a complex type. If so throw.
            if (type.Equals(ValueArray, SwaggerGenerator.PropertyNameComparison))
            {
                this.ValidateItemsOrAdditionalProperties(model, SwaggerGenerator.KeyItems);
            }
            else if (type.Equals(ValueObject, SwaggerGenerator.PropertyNameComparison))
            {
                this.ValidateItemsOrAdditionalProperties(model, SwaggerGenerator.KeyAdditionalProperties);
            }

            try
            {
                // Remove properties, update mark object property and description.
                JObject newMarkObject = new JObject();
                JsonOperations.SetProperty(newMarkObject, SwaggerGenerator.KeyType, type);

                StringBuilder description = new StringBuilder();
                string modelDescription = JsonOperations.GetProperty<string>(model, KeyDescription);
                if (modelDescription != null)
                {
                    description.Append(modelDescription);
                    description.Append(" ");
                }

                description.AppendFormat("type: {0}", JsonOperations.GetProperty<string>(model, SwaggerGenerator.KeyType));
                JsonOperations.SetProperty(model, SwaggerGenerator.KeyType, ValueObject);

                foreach (string additionalPropertyName in SwaggerGenerator.AdditionalPropertiesToRemove)
                {
                    this.RemoveAdditionalProperties(model, additionalPropertyName, ref newMarkObject, ref description);
                }

                JsonOperations.SetProperty(model, KeyMarkObject, newMarkObject);
                description.Append(".");
                JsonOperations.SetProperty(model, KeyDescription, description.ToString());
            }
            catch (MarkObjectException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MarkObjectException(string.Format(CultureInfo.InvariantCulture, "Error in translating property {0}.", model.Path), ex);
            }
        }

        private void RemoveAdditionalProperties(JObject property, string keyName, ref JObject markObject, ref StringBuilder description)
        {
            JToken value = JsonOperations.GetProperty<JToken>(property, keyName);
            if (value != null)
            {
                JsonOperations.RemoveProperty(property, keyName);
                JsonOperations.SetProperty(markObject, keyName, value);

                if (value.Type == JTokenType.Object)
                {
                    // For items, and additionalProperties only show ref and type properties.
                    string refProperty = JsonOperations.GetProperty<string>(value as JObject, SwaggerGenerator.KeyRef);
                    string typeProperty = JsonOperations.GetProperty<string>(value as JObject, SwaggerGenerator.KeyType);
                    if (refProperty != null)
                    {
                        description.AppendFormat(", itemType: {0}", refProperty.Split('/').Last());
                    }
                    else if (typeProperty != null)
                    {
                        description.AppendFormat(", itemType: {0}", typeProperty);
                    }
                }
                else
                {
                    description.AppendFormat(", {0}: {1}", keyName, value);
                }
            }
        }

        /// <summary>
        /// Throws if items or additionalProperties have complex objects.
        /// </summary>
        private void ValidateItemsOrAdditionalProperties(JObject property, string key)
        {
            JObject itemsOrAdditionalProperties = JsonOperations.GetProperty<JObject>(property, key);
            if (itemsOrAdditionalProperties == null)
            {
                return;
            }

            string itemReference = JsonOperations.GetProperty<string>(itemsOrAdditionalProperties, SwaggerGenerator.KeyRef);
            string itemType = JsonOperations.GetProperty<string>(itemsOrAdditionalProperties, SwaggerGenerator.KeyType);
            if (itemReference == null && itemType == null)
            {
                throw new MarkObjectException(string.Format("Please define the inline definition at {0} separately. Updating the type may not generate this inline definition.", property.Path));
            }
        }
    }
}
