// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;
using System.IO;

namespace Microsoft.ADF.SwaggerGen
{
    /// <summary>
    /// Generate a copy of initial swagger doc, as well as an enhanced one. Diff'ing these two should highlight the differences.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new Exception("Please provide an input swagger file.");
            }

            string filepath = Path.GetFullPath(args[0]);
            string json = File.ReadAllText(filepath);

            SwaggerGenerator swaggerGen = new SwaggerGenerator();
            string originalJson = swaggerGen.CreateSameSwaggerSpec(json);
            string modifiedJson = swaggerGen.CreateNewSwaggerSpec(json);

            string newFilenameWithOriginalContent = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) + "-original" + Path.GetExtension(filepath));
            File.WriteAllText(newFilenameWithOriginalContent, originalJson);

            string newFilenameWithModifiedContent = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) + "-modified" + Path.GetExtension(filepath));
            File.WriteAllText(newFilenameWithModifiedContent, modifiedJson);

            Environment.Exit(0);
        }
    }
}
