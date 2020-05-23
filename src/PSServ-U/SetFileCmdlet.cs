using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSServU;
using PSServU.ServU;
using static PSServU.ServU.Client;

namespace PSServ_U {
    [Cmdlet(VerbsCommon.Set, "ServUFile", DefaultParameterSetName = "SessionId")]
    public class SetFileCmdlet : PSCmdlet {
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
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 1)]
        [Alias("PSPath")]
        public string[] File { get; set; }

        /// <summary>
        /// Remote path from which to get the child items
        /// </summary>
        /// <remarks>Root folder when omitted</remarks>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, Position = 2)]
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

            bool reportProgress = !NoProgress.ToBool();
            int activityId = 0;
            string fqfn = null;
            int lastPercentReported = -1;
            ProgressReportHandler progressHandler = (int percent, long bytesSent, long? totalBytes) => {
                if(lastPercentReported != percent) {
                    var progressRecord = new ProgressRecord(activityId, $"Uploading {fqfn}", $"{bytesSent:#,##0} Bytes Uploaded of {totalBytes:#,##0}") {
                        PercentComplete = percent
                    };

                    Host.UI.WriteProgress(1, progressRecord);
                    lastPercentReported = percent;
                }
            };

            foreach(var session in SessionsToProcess) {
                activityId++;
                lastPercentReported = -1;
                try {
                    if(reportProgress) session.Client.ProgressReport += progressHandler;

                    foreach(var f in File) {
                        fqfn = f;
                        lastPercentReported = -1;
                        try {
                            session.Client.SendFile(fqfn, RemotePath);
                        } catch(Exception e) {
                            WriteError(new ErrorRecord(e, null, ErrorCategory.NotSpecified, session));
                        }
                    }

                } finally {
                    if(reportProgress) session.Client.ProgressReport -= progressHandler;
                }
            }
        }
    }
}
