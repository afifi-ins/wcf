﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Security;
#else
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
#endif
using System.Security.Cryptography.X509Certificates;

namespace WcfService
{

    [TestServiceDefinition(Schema = ServiceSchema.NETTCP, BasePath = "TcpTransSecMessCredsCert.svc")]
    internal class TcpTransportSecurityMessageCredentialsCertTestServiceHost : TestServiceHostBase<IWcfService>
    {
        protected override string Address { get { return "tcp-message-credentials-cert"; } }

        protected override Binding GetBinding()
        {
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.TransportWithMessageCredential);
            binding.Security.Message.ClientCredentialType = MessageCredentialType.Certificate;

            return binding;
        }

        protected override void ApplyConfiguration()
        {
            base.ApplyConfiguration();
            this.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            this.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new MyX509CertificateValidator("DO_NOT_TRUST_WcfBridgeRootCA");

            string thumbprint = TestHost.CertificateFromFriendlyName(StoreName.My, StoreLocation.LocalMachine, "WCF Bridge - Machine certificate generated by the CertificateManager").Thumbprint;
            this.Credentials.ServiceCertificate.SetCertificate(StoreLocation.LocalMachine,
                                                      StoreName.My,
                                                      X509FindType.FindByThumbprint,
                                                      thumbprint);
        }

        public TcpTransportSecurityMessageCredentialsCertTestServiceHost(params Uri[] baseAddresses) : base(typeof(WcfService), baseAddresses)
        {
        }
    }
}
