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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prosa.Log4View.Interfaces;

namespace Prosa.Log4View.Log4jReceiver {
    public class Log4jNetReceiverConfigVm : ICustomReceiverConfigurator, INotifyPropertyChanged {

        private readonly Log4jNetReceiverConfig _config;
        private string _hostName;
        private int _port;

        public Log4jNetReceiverConfigVm(Log4jNetReceiverConfig config) {
            _config = config;

            HostName = _config.HostName;
            Port = _config.Port;
        }

        public string HostName {
            get => _hostName;
            set {
                _hostName = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }

        public int Port {
            get => _port;
            set {
                _port = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }

        public bool IsValid() {
            return !string.IsNullOrWhiteSpace(HostName) && Port > 0;
        }

        public bool IsModified { get; private set; }

        /// <summary>
        /// Return true, if a configuration dialog should be shown.
        /// </summary>
        public bool ShowDialog => true;

        public void WriteConfiguration() {
            _config.HostName = HostName;
            _config.Port = Port;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
