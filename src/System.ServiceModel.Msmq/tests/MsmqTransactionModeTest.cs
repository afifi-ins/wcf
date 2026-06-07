// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Transactions;
using Infrastructure.Common;
using MSMQ.Messaging;
using Xunit;

[SupportedOSPlatform("windows")]
public static class MsmqTransactionModeTest
{
    private static readonly MethodInfo s_getMode =
        typeof(System.ServiceModel.NetMsmqBinding).Assembly
            .GetType("System.ServiceModel.Channels.MsmqMessagingInterop", throwOnError: true)
            .GetMethod("GetTransactionMode", BindingFlags.Static | BindingFlags.NonPublic);

    private static MessageQueueTransactionType Invoke(bool exactlyOnce, Transaction ambient)
        => (MessageQueueTransactionType)s_getMode.Invoke(null, new object[] { exactlyOnce, ambient });

    [WcfFact]
    public static void NonExactlyOnce_AnyAmbient_ReturnsNone()
    {
        using var scope = new TransactionScope(TransactionScopeOption.RequiresNew);
        Assert.Equal(MessageQueueTransactionType.None, Invoke(false, Transaction.Current));
        Assert.Equal(MessageQueueTransactionType.None, Invoke(false, null));
    }

    [WcfFact]
    public static void ExactlyOnce_NoAmbient_ReturnsSingle()
    {
        Assert.Equal(MessageQueueTransactionType.Single, Invoke(true, null));
    }

    [WcfFact]
    public static void ExactlyOnce_WithAmbient_ReturnsAutomatic()
    {
        using var scope = new TransactionScope(TransactionScopeOption.RequiresNew);
        Assert.Equal(MessageQueueTransactionType.Automatic, Invoke(true, Transaction.Current));
    }
}
