﻿using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text;

namespace SSH
{
    [Cmdlet(VerbsCommon.Set, "SCPFile", DefaultParameterSetName = "NoKey")]
    public class SetScpFile : PSCmdlet
    {
        // Hosts to conect to
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            Position = 0,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            Position = 0,
            ParameterSetName = "Key")]
        [Alias("HostName", "Computer", "IPAddress", "Host")]
        public string[] ComputerName
        {
            get { return _computername; }
            set { _computername = value; }
        }
        private string[] _computername;

        // Credentials for Connection
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1,
            ParameterSetName = "Key")]
        public PSCredential Credential
        {
            get { return _credential; }
            set { _credential = value; }
        }
        private PSCredential _credential;

        // Port for SSH
        private Int32 _port = 22;
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        public Int32 Port
        {
            get { return _port; }
            set { _port = value; }
        }

        //Proxy Server to use
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        public String ProxyServer
        {
            get { return _proxyserver; }
            set { _proxyserver = value; }
        }
        private String _proxyserver = "";

        // Proxy Port
        private Int32 _proxyport = 8080;
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        public Int32 ProxyPort
        {
            get { return _proxyport; }
            set { _proxyport = value; }
        }

        // Proxy Credentials
        private PSCredential _proxycredential;
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty]
        public PSCredential ProxyCredential
        {
            get { return _proxycredential; }
            set { _proxycredential = value; }
        }
        

        // Proxy Type
        private string _proxytype = "HTTP";
        [ValidateSet("HTTP", "Socks4", "Socks5", IgnoreCase = true)]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        public string ProxyType
        {
            get { return _proxytype; }
            set { _proxytype = value; }
        }


        //SSH Key File
        private String _keyfile = "";
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        public String KeyFile
        {
            get { return _keyfile; }
            set { _keyfile = value; }
        }


        //Local File
        private String _localfile = "";
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            Position = 2,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            Position = 2,
            ParameterSetName = "Key")]
        public String LocalFile
        {
            get { return _localfile; }
            set { _localfile = value; }
        }


        //Remote File
        private String __remotepath = "";
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            Position = 3,
            ParameterSetName = "NoKey")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            Position = 3,
            ParameterSetName = "Key")]
        public String RemotePath
        {
            get { return __remotepath; }
            set { __remotepath = value; }
        }


        // OperationTimeout Parameter
        private int _operationtimeout = 5;

        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        public int OperationTimeout
        {
            get { return _operationtimeout; }
            set { _operationtimeout = value; }
        }

        // ConnectionTimeout Parameter
        private int _connectiontimeout = 5;
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        public int ConnectionTimeout
        {
            get { return _connectiontimeout; }
            set { _connectiontimeout = value; }
        }

        // Auto Accept key fingerprint
        private bool _acceptkey;
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Key")]
        [Parameter(Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "NoKey")]
        public bool AcceptKey
        {
            get { return _acceptkey; }
            set { _acceptkey = value; }
        }

        // Variable to hold the host/fingerprint information
        private Dictionary<string, string> _sshHostKeys;

        protected override void BeginProcessing()
        {
            // Collect host/fingerprint information from the registry.
            base.BeginProcessing();
            var keymng = new TrustedKeyMng();
            _sshHostKeys = keymng.GetKeys();
        }

        protected override void ProcessRecord()
        {
            foreach (var computer in _computername)
            {
                ConnectionInfo connectInfo;
                if (_keyfile.Equals(""))
                {
                    WriteVerbose("Using SSH Username and Password authentication for connection.");
                    var kIconnectInfo = new KeyboardInteractiveAuthenticationMethod(_credential.GetNetworkCredential().UserName);
                    connectInfo = ConnectionInfoGenerator.GetCredConnectionInfo(computer,
                        _port,
                        _credential,
                        _proxyserver,
                        _proxytype,
                        _proxyport,
                        _proxycredential,
                        kIconnectInfo);

                    // Event Handler for interactive Authentication
                    kIconnectInfo.AuthenticationPrompt += delegate(object sender, AuthenticationPromptEventArgs e)
                    {
                        foreach (var prompt in e.Prompts)
                        {
                            if (prompt.Request.Contains("Password"))
                                prompt.Response = _credential.GetNetworkCredential().Password;
                        }
                    };

                }
                else
                {
                    WriteVerbose("Using SSH Key authentication for connection.");
                    connectInfo = ConnectionInfoGenerator.GetKeyConnectionInfo(computer,
                        _port,
                        _keyfile,
                        _credential,
                        _proxyserver,
                        _proxytype,
                        _proxyport,
                        _proxycredential);
                }

                //Ceate instance of SSH Client with connection info
                var client = new ScpClient(connectInfo);


                // Handle host key
                var computer1 = computer;
                client.HostKeyReceived += delegate(object sender, HostKeyEventArgs e)
                {
                    var sb = new StringBuilder();
                    foreach (var b in e.FingerPrint)
                    {
                        sb.AppendFormat("{0:x}:", b);
                    }
                    var fingerPrint = sb.ToString().Remove(sb.ToString().Length - 1);

                    if (_sshHostKeys.ContainsKey(computer1))
                    {
                        if (_sshHostKeys[computer1] == fingerPrint)
                        {
                            if (MyInvocation.BoundParameters.ContainsKey("Verbose"))
                            {
                                Host.UI.WriteVerboseLine("Fingerprint matched trusted fingerprint for host " + computer1);
                            }
                            e.CanTrust = true;
                        }
                        else
                        {
                            var ex = new System.Security.SecurityException("SSH fingerprint mismatch for host " + computer1);
                            ThrowTerminatingError(new ErrorRecord(
                                ex,
                                "SSH fingerprint mismatch for host " + computer1,
                                ErrorCategory.SecurityError,
                                computer1));
                        }
                    }
                    else
                    {
                        int choice;
                        if (_acceptkey)
                        {
                            choice = 0;
                        }
                        else
                        {
                            var choices = new Collection<ChoiceDescription>
                                {
                                    new ChoiceDescription("Y"),
                                    new ChoiceDescription("N")
                                };

                            choice = Host.UI.PromptForChoice("Server SSH Fingerprint", "Do you want to trust the fingerprint " + fingerPrint, choices, 1);
                        }
                        if (choice == 0)
                        {
                            var keymng = new TrustedKeyMng();
                            keymng.SetKey(computer1, fingerPrint);
                            e.CanTrust = true;
                        }
                        else
                        {
                            e.CanTrust = false;
                        }
                    }
                };
                // Set the connection timeout
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(_connectiontimeout);

                // Connect to host using Connection info
                client.Connect();
                //client.BufferSize = 1024;

                var counter = 0;
                // Print progess of download.
                client.Uploading += delegate(object sender, ScpUploadEventArgs e)
                {
                    if (e.Size != 0)
                    {
                        counter ++;
                        
                        if (counter > 900)
                        {
                            var percent = Convert.ToInt32((e.Uploaded * 100) / e.Size);

                            if (percent == 100)
                            {
                                return;
                            }

                            var progressRecord = new ProgressRecord(1, 
                                "Uploading " + e.Filename, 
                                String.Format("{0} Bytes Uploaded of {1}", 
                                e.Uploaded, e.Size)) {PercentComplete = percent};

                            Host.UI.WriteProgress(1, progressRecord);
                            counter = 0;
                        }
                    }
                };

                WriteVerbose("Connection successful");
                
                // Resolve the path even if a relative one is given.
                ProviderInfo provider;
                var pathinfo = GetResolvedProviderPathFromPSPath(_localfile, out provider);
                var localfullPath = pathinfo[0];

                if (File.Exists(@localfullPath))
                {
                    WriteVerbose("Uploading " + localfullPath);
                    var fil = new FileInfo(@localfullPath);
                    var remoteFullpath = RemotePath.TrimEnd(new[] { '/' }) + "/" + fil.Name;
                    client.Upload(fil, remoteFullpath);

                    client.Disconnect();
                }
                else
                {
                    var ex = new FileNotFoundException("File to upload " + localfullPath + " was not found.");

                    ThrowTerminatingError(new ErrorRecord(
                                                    ex,
                                                    "File to upload " + localfullPath + " was not found.",
                                                    ErrorCategory.InvalidArgument,
                                                    localfullPath));
                }
            }

        } // End process record


    } //end of the class for the Set-SCPFile
    ////###################################################
}
