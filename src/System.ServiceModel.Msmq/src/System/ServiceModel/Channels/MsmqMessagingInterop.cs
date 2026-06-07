// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Reflection;
using System.Runtime.Versioning;
using System.Transactions;

namespace System.ServiceModel.Channels
{
    // Thin reflection-based wrapper over MSMQ.Messaging so the package can
    // be compiled and unit-tested on platforms where the dependency is not
    // resolvable. At runtime on Windows we expect MSMQ.Messaging to be
    // available via the PackageReference declared in the csproj.
    [SupportedOSPlatform("windows")]
    internal static class MsmqMessagingInterop
    {
        private static readonly Lazy<Bindings> s_bindings = new(LoadBindings, isThreadSafe: true);

        internal static void Send(
            string formatName,
            byte[] body,
            int offset,
            int count,
            Transaction ambientTransaction,
            TimeSpan timeToLive,
            TimeSpan sendTimeout)
        {
            _ = sendTimeout; // MSMQ.Messaging has no timeout knob on Send
            Bindings b = s_bindings.Value;

            string path = "FormatName:" + formatName;
            object queue = b.MessageQueueCtor.Invoke(new object[] { path });
            try
            {
                object message = b.MessageCtor.Invoke(Array.Empty<object>());
                byte[] payload;
                if (offset == 0 && count == body.Length)
                {
                    payload = body;
                }
                else
                {
                    payload = new byte[count];
                    Buffer.BlockCopy(body, offset, payload, 0, count);
                }
                b.MessageBodyStreamSetter.Invoke(message, new object[] { new System.IO.MemoryStream(payload, writable: false) });
                b.MessageTimeToBeReceivedSetter.Invoke(message, new object[] { timeToLive });

                if (ambientTransaction != null)
                {
                    object msmqTx = b.MessageQueueTransactionCtor.Invoke(Array.Empty<object>());
                    b.MessageQueueTransactionBegin.Invoke(msmqTx, Array.Empty<object>());
                    try
                    {
                        b.MessageQueueSendWithTx.Invoke(queue, new[] { message, msmqTx });
                        b.MessageQueueTransactionCommit.Invoke(msmqTx, Array.Empty<object>());
                    }
                    catch
                    {
                        try { b.MessageQueueTransactionAbort.Invoke(msmqTx, Array.Empty<object>()); } catch { }
                        throw;
                    }
                    finally
                    {
                        (msmqTx as IDisposable)?.Dispose();
                    }
                }
                else
                {
                    b.MessageQueueSend.Invoke(queue, new[] { message });
                }
            }
            finally
            {
                (queue as IDisposable)?.Dispose();
            }
        }

        private static Bindings LoadBindings()
        {
            Assembly asm;
            try
            {
                asm = Assembly.Load(new AssemblyName("MSMQ.Messaging"));
            }
            catch (Exception ex)
            {
                throw new PlatformNotSupportedException(SR.MsmqMessagingNotAvailable, ex);
            }

            Type queueType = asm.GetType("MSMQ.Messaging.MessageQueue", throwOnError: true);
            Type messageType = asm.GetType("MSMQ.Messaging.Message", throwOnError: true);
            Type txType = asm.GetType("MSMQ.Messaging.MessageQueueTransaction", throwOnError: true);

            return new Bindings
            {
                MessageQueueCtor = queueType.GetConstructor(new[] { typeof(string) }),
                MessageQueueSend = queueType.GetMethod("Send", new[] { typeof(object) }),
                MessageQueueSendWithTx = queueType.GetMethod("Send", new[] { typeof(object), txType }),
                MessageCtor = messageType.GetConstructor(Type.EmptyTypes),
                MessageBodyStreamSetter = messageType.GetProperty("BodyStream").GetSetMethod(),
                MessageTimeToBeReceivedSetter = messageType.GetProperty("TimeToBeReceived").GetSetMethod(),
                MessageQueueTransactionCtor = txType.GetConstructor(Type.EmptyTypes),
                MessageQueueTransactionBegin = txType.GetMethod("Begin"),
                MessageQueueTransactionCommit = txType.GetMethod("Commit"),
                MessageQueueTransactionAbort = txType.GetMethod("Abort"),
            };
        }

        private sealed class Bindings
        {
            public ConstructorInfo MessageQueueCtor;
            public MethodInfo MessageQueueSend;
            public MethodInfo MessageQueueSendWithTx;
            public ConstructorInfo MessageCtor;
            public MethodInfo MessageBodyStreamSetter;
            public MethodInfo MessageTimeToBeReceivedSetter;
            public ConstructorInfo MessageQueueTransactionCtor;
            public MethodInfo MessageQueueTransactionBegin;
            public MethodInfo MessageQueueTransactionCommit;
            public MethodInfo MessageQueueTransactionAbort;
        }
    }
}
