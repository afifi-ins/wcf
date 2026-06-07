// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.ServiceModel;
using Infrastructure.Common;
using MSMQ.Messaging;
using Xunit;

[SupportedOSPlatform("windows")]
public static class MsmqMessagingInteropTest
{
    private static readonly MethodInfo s_toMsmqException =
        typeof(System.ServiceModel.NetMsmqBinding).Assembly
            .GetType("System.ServiceModel.Channels.MsmqMessagingInterop", throwOnError: true)
            .GetMethod("ToMsmqException", BindingFlags.Static | BindingFlags.NonPublic);

    // MessageQueueException's only non-serialization ctor is the
    // internal `ctor(int hresult)`. The hresult arg is interpreted
    // through `MessageQueueErrorCode (MessageQueueErrorCode)hresult`
    // so we can drive the exception's MessageQueueErrorCode value by
    // passing the native MQ_ERROR_* int.
    private static MessageQueueException CreateException(uint nativeCode)
    {
        ConstructorInfo ctor = typeof(MessageQueueException)
            .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
                            new[] { typeof(int) }, modifiers: null);
        Assert.NotNull(ctor);
        return (MessageQueueException)ctor.Invoke(new object[] { unchecked((int)nativeCode) });
    }

    private static MsmqException Convert(MessageQueueException mqEx)
        => (MsmqException)s_toMsmqException.Invoke(null, new object[] { mqEx });

    // Regression for the bug uncovered in slice 6:
    //
    //   MessageQueueException.ErrorCode is the *generic HRESULT*
    //   (typically 0x80004005 = E_FAIL). The native MSMQ error
    //   (MQ_ERROR_QUEUE_NOT_FOUND = 0xC00E0003, etc.) lives in the
    //   MessageQueueErrorCode enum.
    //
    // Earlier code passed mqEx.ErrorCode straight into MsmqException's
    // (string, int) ctor, so every native error fell into the default
    // branch of MsmqException.TuneBehavior — callers always saw a bare
    // MsmqException instead of EndpointNotFoundException / TimeoutException /
    // etc. This test pins the fix: the native value, not the HRESULT,
    // is preserved through the wrap.
    [WcfFact]
    public static void ToMsmqException_UsesNativeCodeNotHResult()
    {
        MessageQueueException mqEx = CreateException(0xC00E0003u); // MQ_ERROR_QUEUE_NOT_FOUND
        Assert.Equal(MessageQueueErrorCode.QueueNotFound, mqEx.MessageQueueErrorCode);
        Assert.NotEqual(unchecked((int)0xC00E0003u), mqEx.ErrorCode); // demonstrate the bug surface

        MsmqException converted = Convert(mqEx);
        Assert.Equal(unchecked((int)0xC00E0003u), converted.ErrorCode);
    }

    // End-to-end pin: the normalized WCF exception for an MSMQ
    // QueueNotFound now comes out as EndpointNotFoundException. If
    // someone re-introduces the slice-6 bug (using ErrorCode instead
    // of MessageQueueErrorCode), this test fails because the
    // converted MsmqException's ErrorCode no longer matches the
    // mapping table and the result becomes a bare MsmqException.
    [WcfTheory]
    [InlineData(0xC00E0003u, typeof(EndpointNotFoundException))] // QueueNotFound
    [InlineData(0xC00E001Bu, typeof(TimeoutException))]          // IOTimeout
    [InlineData(0xC00E001Eu, typeof(ArgumentException))]         // IllegalFormatName
    [InlineData(0xC00E000Eu, typeof(EndpointNotFoundException))] // RemoteMachineNotAvailable
    public static void ToMsmqException_NormalizesToWcfException(uint nativeCode, Type expected)
    {
        MessageQueueException mqEx = CreateException(nativeCode);
        MsmqException converted = Convert(mqEx);

        Type t = typeof(MsmqException);
        Type normalizedType = (Type)t.GetProperty("NormalizedType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(converted);
        Assert.Equal(expected, normalizedType);
    }
}
