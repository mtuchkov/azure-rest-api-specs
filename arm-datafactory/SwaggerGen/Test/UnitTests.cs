// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.ADF.SwaggerGen.Test
{
    public class UnitTests
    {
        private const string JsonFolder = @".\Test\Jsons\";

        [Fact]
        public void TestAllExpectedTransformations()
        {
            string expectedJson = File.ReadAllText(UnitTests.JsonFolder + @"ExpectedOutputForSwaggerSpecWithAllVariations.json");
            JObject expected = JObject.Parse(expectedJson);

            // This contains all the valid transformation cases. Cases not mentioned here might not work as expected.
            string specsJson = File.ReadAllText(UnitTests.JsonFolder + @"SwaggerSpecWithAllVariations.json");
            SwaggerGenerator swaggerGen = new SwaggerGenerator();
            string modifiedJson = swaggerGen.CreateNewSwaggerSpec(specsJson);
            JObject modified = JObject.Parse(modifiedJson);

            Assert.True(JObject.DeepEquals(modified, expected));
        }

        [Fact]
        public void RaiseExceptionForEachModel()
        {
            const string definitionsString = "definitions";
            string specsJson = File.ReadAllText(UnitTests.JsonFolder + @"EachModelDefinitionHasError.json");
            SwaggerGenerator swaggerGen = new SwaggerGenerator();

            JObject specs = JObject.Parse(specsJson);
            JObject definitions = JsonOperations.GetProperty<JObject>(specs, definitionsString);

            foreach (JProperty jProperty in definitions.Children())
            {
                JObject newJObject = new JObject();
                newJObject[definitionsString] = new JObject();
                newJObject[definitionsString][jProperty.Name] = jProperty.Value;

                Assert.Throws<MarkObjectException>(() =>
                {
                    swaggerGen.CreateNewSwaggerSpec(JsonConvert.SerializeObject(newJObject));
                });
            }
        }

        [Fact]
        public void ValidateJsonSchema()
        {
            JSchemaPreloadedResolver schemaResolver = new JSchemaPreloadedResolver();
            schemaResolver.Add(new Uri("http://json-schema.org/draft-04/schema"), File.ReadAllText(UnitTests.JsonFolder + @".\JsonSchemaDraft4.json"));

            string jsonSchema = File.ReadAllText(UnitTests.JsonFolder + @"AzureSwaggerJsonSchema.json");
            JSchema schema = JSchema.Parse(jsonSchema, schemaResolver);

            string jsonSwaggerSpecification = File.ReadAllText(@"..\..\..\2015-10-01\swagger\service.json");
            JObject swaggerSpecification = JObject.Parse(jsonSchema);

            IList<string> errorMessages = new List<string>();
            bool isValid = SchemaExtensions.IsValid(swaggerSpecification, schema, out errorMessages);

            Assert.True(isValid, string.Join("\r\n", errorMessages));
        }
    }
}
