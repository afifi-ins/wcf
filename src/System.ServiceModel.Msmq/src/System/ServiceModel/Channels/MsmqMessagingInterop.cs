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
    // MsmqIntegrationBinding output channels.
    [SupportedOSPlatform("windows")]
    internal static class MsmqMessagingInterop
    {
        internal static void Send(
            string formatName,
            byte[] body,
            int offset,
            int count,
            bool exactlyOnce,
            TimeSpan timeToLive,
            TimeSpan sendTimeout)
        {
            var message = BuildMessage(body, offset, count, timeToLive);
            SendCore(formatName, message, exactlyOnce, sendTimeout);
        }

        internal static void Send(
            string formatName,
            byte[] body,
            int offset,
            int count,
            MsmqIntegrationMessageProperty property,
            bool exactlyOnce,
            TimeSpan timeToLive,
            TimeSpan sendTimeout)
        {
            var message = BuildMessage(body, offset, count, timeToLive);
            property?.ApplyTo(message);
            SendCore(formatName, message, exactlyOnce, sendTimeout);
        }

        private static MSMQ.Messaging.Message BuildMessage(byte[] body, int offset, int count, TimeSpan timeToLive)
        {
            byte[] payload = SliceBody(body, offset, count);
            return new MSMQ.Messaging.Message
            {
                BodyStream = new MemoryStream(payload, writable: false),
                TimeToBeReceived = timeToLive,
            };
        }

        // Picks the MSMQ transaction mode that matches the binding's
        // ExactlyOnce contract and the ambient System.Transactions
        // transaction:
        //
        //   ExactlyOnce  Transaction.Current   MSMQ mode
        //   -----------  -------------------   -----------
        //   true         non-null              Automatic   (enlist in ambient DTC tx)
        //   true         null                  Single      (start a one-shot MSMQ tx)
        //   false        any                   None        (non-transactional send)
        //
        // MessageQueueTransactionType.Automatic delegates enlistment to
        // mqrt.dll which uses the native MSMQ DTC integration — the
        // MSMQ send commits or aborts atomically with any other
        // resource managers participating in Transaction.Current.
        internal static MessageQueueTransactionType GetTransactionMode(bool exactlyOnce, Transaction ambient)
        {
            if (!exactlyOnce)
            {
                return MessageQueueTransactionType.None;
            }
            return ambient != null
                ? MessageQueueTransactionType.Automatic
                : MessageQueueTransactionType.Single;
        }

        private static void SendCore(
            string formatName,
            MSMQ.Messaging.Message message,
            bool exactlyOnce,
            TimeSpan sendTimeout)
        {
            _ = sendTimeout; // MSMQ.Messaging.MessageQueue.Send has no per-call timeout.
            try
            {
                using var queue = new MessageQueue("FormatName:" + formatName);
                MessageQueueTransactionType mode = GetTransactionMode(exactlyOnce, Transaction.Current);
                if (mode == MessageQueueTransactionType.None)
                {
                    queue.Send(message);
                }
                else
                {
                    queue.Send(message, mode);
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
