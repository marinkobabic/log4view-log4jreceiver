#region Copyright PROSA GmbH
// /////////////////////////////////////////////////////////////////////////////// 
// // Copyright © 2008 by PROSA GmbH, All rights reserved. 
// // 
// // The information contained herein is confidential, proprietary to PROSA GmbH, 
// // and considered a trade secret. Use of this information by anyone other than 
// // authorized employees of PROSA GmbH is granted only under a written nondisclosure
// // agreement, expressly prescribing the the scope and manner of such use.
// //
// /////////////////////////////////////////////////////////////////////////////// 
#endregion

using System;
using System.Runtime.Serialization;
using Prosa.Log4View.Configuration;
using Prosa.Log4View.Interfaces;
using Prosa.Log4View.Receiver.Net;

namespace Prosa.Log4View.Log4jReceiver
{
    [DataContract, Serializable]
    public class Log4jNetReceiverConfig : ReceiverConfig
    {
        public Log4jNetReceiverConfig() : base("Log4j Network Receiver") {
            this.Port = 878;
            this.ParserType = Core.ParserType.XML;
            this.HostName = "localhost";
            this.Encoding = System.Text.Encoding.UTF8.WebName;
        }


        [DataMember]
        public override string ReceiverTypeId => Log4jNetReceiverFactory.TypeId;

        [DataMember]
        public string HostName { get; set; }

        [DataMember]
        public int Port { get; set; }

        public override string SourceDescription
        {
            get
            {
                return string.Format("Log4j2 Receiver {0} Port {1}", this.HostName, this.Port);
            }
        }
    }
}