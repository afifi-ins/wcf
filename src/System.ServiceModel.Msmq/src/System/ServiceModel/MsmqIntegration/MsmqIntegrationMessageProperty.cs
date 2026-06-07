// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.Versioning;
using System.ServiceModel.Channels;
using MSMQ.Messaging;

namespace System.ServiceModel.MsmqIntegration
{
    // Carries MSMQ message metadata (label, priority, correlation, …)
    // alongside the message body. Mirrors the netfx public type so
    // applications porting from .NET Framework can keep their usage
    // unchanged. The enums (AcknowledgeTypes, Acknowledgment,
    // MessageType, MessagePriority) come from MSMQ.Messaging, which is
    // now a runtime dependency of the package.
    [SupportedOSPlatform("windows")]
    public sealed class MsmqIntegrationMessageProperty
    {
        public const string Name = "MsmqIntegrationMessageProperty";

        public object Body { get; set; }
        [CLSCompliant(false)] public AcknowledgeTypes? AcknowledgeType { get; set; }
        [CLSCompliant(false)] public Acknowledgment? Acknowledgment { get; internal set; }
        public Uri AdministrationQueue { get; set; }
        public int? AppSpecific { get; set; }
        public DateTime? ArrivedTime { get; internal set; }
        public bool? Authenticated { get; internal set; }
        public int? BodyType { get; set; }
        public string CorrelationId { get; set; }
        public Uri DestinationQueue { get; internal set; }
        public byte[] Extension { get; set; }
        public string Id { get; internal set; }
        public string Label { get; set; }
        [CLSCompliant(false)] public MessageType? MessageType { get; internal set; }
        [CLSCompliant(false)] public MessagePriority? Priority { get; set; }
        public Uri ResponseQueue { get; set; }
        public byte[] SenderId { get; internal set; }
        public DateTime? SentTime { get; internal set; }
        public TimeSpan? TimeToReachQueue { get; set; }

        public static MsmqIntegrationMessageProperty Get(System.ServiceModel.Channels.Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return message.Properties.TryGetValue(Name, out object value) ? value as MsmqIntegrationMessageProperty : null;
        }

        // Copies the user-settable MSMQ properties onto the outgoing
        // MSMQ.Messaging.Message instance just before it is dispatched.
        internal void ApplyTo(MSMQ.Messaging.Message msmqMessage)
        {
            if (AcknowledgeType.HasValue) msmqMessage.AcknowledgeType = AcknowledgeType.Value;
            if (AdministrationQueue != null) msmqMessage.AdministrationQueue = new MessageQueue("FormatName:" + MsmqUri.UriToFormatNameByScheme(AdministrationQueue));
            if (AppSpecific.HasValue) msmqMessage.AppSpecific = AppSpecific.Value;
            if (BodyType.HasValue) msmqMessage.BodyType = BodyType.Value;
            if (!string.IsNullOrEmpty(CorrelationId)) msmqMessage.CorrelationId = CorrelationId;
            if (Extension != null) msmqMessage.Extension = Extension;
            if (!string.IsNullOrEmpty(Label)) msmqMessage.Label = Label;
            if (Priority.HasValue) msmqMessage.Priority = Priority.Value;
            if (ResponseQueue != null) msmqMessage.ResponseQueue = new MessageQueue("FormatName:" + MsmqUri.UriToFormatNameByScheme(ResponseQueue));
            if (TimeToReachQueue.HasValue) msmqMessage.TimeToReachQueue = TimeToReachQueue.Value;
        }
    }
}
