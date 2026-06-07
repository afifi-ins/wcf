// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.IO;
using System.Runtime.Versioning;
using System.ServiceModel.MsmqIntegration;
using System.Transactions;
using MSMQ.Messaging;

namespace System.ServiceModel.Channels
{
    // Thin wrapper around MSMQ.Messaging used by both NetMsmqBinding and
    // MsmqIntegrationBinding output channels. Slice 5 replaces the
    // reflective load used in slice 4 with direct typed calls now that
    // MSMQ.Messaging is a real PackageReference on the package.
    [SupportedOSPlatform("windows")]
    internal static class MsmqMessagingInterop
    {
        // Simple body-only send (used by NetMsmqBinding channels).
        internal static void Send(
            string formatName,
            byte[] body,
            int offset,
            int count,
            Transaction ambientTransaction,
            TimeSpan timeToLive,
            TimeSpan sendTimeout)
        {
            byte[] payload = SliceBody(body, offset, count);
            var message = new MSMQ.Messaging.Message
            {
                BodyStream = new MemoryStream(payload, writable: false),
                TimeToBeReceived = timeToLive,
            };
            SendCore(formatName, message, ambientTransaction, sendTimeout);
        }

        // Full MSMQ-integration send: copies the user's integration message
        // property bag (label, priority, correlation, etc.) onto the
        // outgoing MSMQ message.
        internal static void Send(
            string formatName,
            byte[] body,
            int offset,
            int count,
            MsmqIntegrationMessageProperty property,
            Transaction ambientTransaction,
            TimeSpan timeToLive,
            TimeSpan sendTimeout)
        {
            byte[] payload = SliceBody(body, offset, count);
            var message = new MSMQ.Messaging.Message
            {
                BodyStream = new MemoryStream(payload, writable: false),
                TimeToBeReceived = timeToLive,
            };
            property?.ApplyTo(message);
            SendCore(formatName, message, ambientTransaction, sendTimeout);
        }

        private static void SendCore(
            string formatName,
            MSMQ.Messaging.Message message,
            Transaction ambientTransaction,
            TimeSpan sendTimeout)
        {
            _ = sendTimeout; // MSMQ.Messaging.MessageQueue.Send has no per-call timeout.
            try
            {
                using var queue = new MessageQueue("FormatName:" + formatName);
                if (ambientTransaction != null)
                {
                    using var tx = new MessageQueueTransaction();
                    tx.Begin();
                    try
                    {
                        queue.Send(message, tx);
                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Abort(); } catch { }
                        throw;
                    }
                }
                else
                {
                    queue.Send(message);
                }
            }
            catch (MessageQueueException mqEx)
            {
                // MessageQueueException.ErrorCode exposes a generic
                // HRESULT (often 0x80004005). The actual native MSMQ
                // error code (MQ_ERROR_*) is in MessageQueueErrorCode,
                // whose enum values match the native constants exactly.
                int code = unchecked((int)(uint)mqEx.MessageQueueErrorCode);
                throw new MsmqException(mqEx.Message, code).Normalized;
            }
        }

        private static byte[] SliceBody(byte[] body, int offset, int count)
        {
            if (offset == 0 && count == body.Length)
            {
                return body;
            }
            var slice = new byte[count];
            Buffer.BlockCopy(body, offset, slice, 0, count);
            return slice;
        }
    }
}
