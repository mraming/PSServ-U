using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSServU;
using PSServU.ServU;

namespace PSServ_U {
    [Cmdlet(VerbsCommon.Get, "ServUChildItems", DefaultParameterSetName = "SessionId")]
    [OutputType(typeof(RemoteFileSystemInfo))]
    public class GetChildItemsCmdlet : PSCmdlet {
        /// <summary>
        /// Index of the ServU Session.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "SessionId")]
        public int[] SessionId { get; set; }

        /// <summary>
        /// Session paramter that takes a session object 
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Session")]
        public Session[] Session { get; set; }

        /// <summary>
        /// Remote path from which to get the child items
        /// </summary>
        /// <remarks>Root folder when omitted</remarks>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, Position = 1)]
        public string RemotePath { get; set; }

        // Supress progress bar.
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName = "SessionId")]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName = "Session")]
        public SwitchParameter NoProgress { get; set; }

        private IReadOnlyList<Session> SessionsToProcess { get; set; }

        protected override void BeginProcessing() {
            // Collect the sessions
            base.BeginProcessing();
            switch(ParameterSetName) {
                case "Session":
                    SessionsToProcess = Session;
                    break;
                case "SessionId":
                    SessionsToProcess = SessionManager.GetSessionsById(SessionState, SessionId);
                    break;
                default:
                    throw new NotImplementedException($"Support for ParameterSetName {ParameterSetName} has not been implemented");
            }
        }

        protected override void ProcessRecord() {
            foreach(var session in SessionsToProcess) {
                try {
                    WriteObject(session.Client.GetFileSystemInfo(RemotePath), true);
                } catch(Exception e) {
                    WriteDebug(e.ToString());
                    ThrowTerminatingError(new ErrorRecord(e, null, ErrorCategory.NotSpecified, session));
                }
            }
            }
    }
}
