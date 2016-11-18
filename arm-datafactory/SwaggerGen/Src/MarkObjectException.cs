// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;

namespace Microsoft.ADF.SwaggerGen
{
    internal class MarkObjectException : Exception
    {
        public MarkObjectException(string exception)
            : base(exception)
        {
        }

        public MarkObjectException(string exception, Exception innerException)
            : base(exception, innerException)
        {
        }
    }
}
