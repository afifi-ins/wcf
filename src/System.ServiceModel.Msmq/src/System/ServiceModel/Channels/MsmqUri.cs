// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.ServiceModel.Channels
{
    // Stub for the MSMQ URI / format-name address translator. Filled in
    // by a later slice that adds full MsmqUri parsing and the per-protocol
    // address translators (Net, ActiveDirectory, Srmp, SrmpSecure).
    internal static partial class MsmqUri
    {
        internal interface IAddressTranslator
        {
            string UriToFormatName(Uri uri);
            Uri FormatNameToUri(string formatName);
        }
    }
}
