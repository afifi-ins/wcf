// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using CoreWCF;
using CoreWCF.Channels;
#else
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
#endif
using System.Security.Cryptography.X509Certificates;

namespace WcfService
{
    [TestServiceDefinition(Schema = ServiceSchema.NETTCP, BasePath = "TcpInvalidEkuServerCert.svc")]
    public class TcpInvalidEkuServerCertTestServiceHost : TestServiceHostBase<IWcfService>
    {
        protected override string Address { get { return "tcp-InvalidEkuServerCert"; } }

        protected override Binding GetBinding()
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;

            return binding;
        }

        protected override void ApplyConfiguration()
        {
            base.ApplyConfiguration();

            X509Certificate2 cert = TestHost.CertificateFromFriendlyName(StoreName.My, StoreLocation.LocalMachine, "WCF Bridge - TcpInvalidEkuServerCert");
            this.Credentials.ServiceCertificate.Certificate = cert;
        }

        public TcpInvalidEkuServerCertTestServiceHost(params Uri[] baseAddresses)
            : base(typeof(WcfService), baseAddresses)
        {
        }
    }
}
