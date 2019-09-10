#region Copyright PROSA GmbH

// ///////////////////////////////////////////////////////////////////////////////
// // Copyright © 2018 by PROSA GmbH, All rights reserved.
// //
// // The information contained herein is confidential, proprietary to PROSA GmbH,
// // and considered a trade secret. Use of this information by anyone other than
// // authorized employees of PROSA GmbH is granted only under a written nondisclosure
// // agreement, expressly prescribing the the scope and manner of such use.
// //
// ///////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Prosa.Log4View.Infrastructure;
using Prosa.Log4View.Interfaces;
using Prosa.Log4View.Message;
using Prosa.Log4View.Parser;
using Prosa.Log4View.Receiver;
using Prosa.Log4View.Receiver.Net;

namespace Prosa.Log4View.Log4jReceiver
{
    internal class Log4jNetReceiver : LogReceiver {
        private readonly TcpListener _listener;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public Log4jNetReceiver(IReceiverFactory factory, Log4jNetReceiverConfig config) : base(factory, config) {
            this.Hostname = config.HostName;
            this.Port = config.Port;
            this.Name = config.Name;
            this._listener = new TcpListener(IPAddress.Loopback, this.Port);
        }

        private static readonly ILogger _logger = LogManager.GetLogger<Log4jNetReceiver>();

        protected string Hostname { get; }

        protected int Port { get; }

        protected CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        protected string Address
        {
            get
            {
                return string.Format("{0}:{1}", this.Hostname, this.Port);
            }
        }

        protected override void BeginReceive()
        {
            Task.Run((Action)(() => this.StartReceive()));
        }

        private void StartReceive()
        {
            try
            {
                this._listener.Start();
                this.StartAccept();
                this.IsInitiated = true;
            }
            catch (Exception ex)
            {
                _logger.Warn(string.Format("Could not listen in Tcp Server Receiver {0}:{1}", this.Name, this.Port), ex, true);
                this.Disconnect();
            }
        }

        private async void StartAccept()
        {
            if (Cts.IsCancellationRequested)
                return;
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync().WithCancellation<TcpClient>(Cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.Warn(ex.Message, ex);
                return;
            }
            this.StartAccept();
            this.Receive(client);
        }

        private async void Receive(TcpClient client)
        {
            
            this._clients.Add(client);
            Log4jXmlParser parser = new Log4jXmlParser(this, string.Format("{0} - {1}", this.Address, client));
            UnprocessedString unprocessed = new UnprocessedString();
            while (!this.Cts.IsCancellationRequested)
            {
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int num;
                try
                {
                    num = await client.GetStream().ReadAsync(buffer, 0, buffer.Length, this.Cts.Token);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.Warn($"Client on TcpReceiver {this.Name}, {this.Address} disconnected foul", ex, true);
                    break;
                }
                catch(IOException ioEx)
                {
                    _logger.Warn($"Client on TcpReceiver {this.Name}, {this.Address} disconnected foul", ioEx, true);
                    break;
                }
                if (num > 0)
                {
                    MessageBlock withUnprocessed = parser.ParseWithUnprocessed(new InputBuffer(buffer, buffer.Length, this.Encoding, new int?(), new CancellationToken(), int.MaxValue), unprocessed, new DateTimeOffset?(), new DateTimeOffset?(), new int?(), new CancellationToken());
                    this.AddNewMessages(withUnprocessed);
                    buffer = (byte[])null;
                }
                else
                    break;
            }
            try
            {
                client?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Client on TcpReceiver {this.Name}, {this.Address} could not be closed/disposed", ex, true);
            }
        }
        public void Disconnect()
        {

        }

        public override void Dispose()
        {
            this.Cts.Cancel();
            this.Disconnect();
            base.Dispose();
        }
    }
}
