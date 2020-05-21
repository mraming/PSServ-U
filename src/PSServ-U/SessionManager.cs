using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSServU {

    /// <summary>
    /// Manage PS Sessions to ServU server
    /// </summary>
    internal static class SessionManager {
        internal static Session NewSession(SessionState pssession) {
            int lastSessionId = 0;
            // Get copy of sessions from the global variable or new empty list
            var sessions = pssession.PSVariable.GetValue("Global:ServUSessions") as List<Session> ?? new List<Session>();
            if(sessions.Any()) lastSessionId = sessions.Max(s => s.SessionId);

            var session = new Session(lastSessionId++);
            sessions.Add(session);
            // Set the Global Variable for the sessions.
            pssession.PSVariable.Set((new PSVariable("Global:ServUSessions", sessions, ScopedItemOptions.AllScope)));

            return session;
        }

        internal static IReadOnlyList<Session> GetSessionsById(SessionState pssession, params int[] sessionIds) {
            // If no session ids are specified and there is only one session, we use that one session as a default session
            var sessions = pssession.PSVariable.GetValue("Global:ServUSessions") as List<Session>;
            if(sessions == null) throw new ApplicationException("No ServU session exists; start a new session using New-ServUSession");
            if((sessionIds == null || sessionIds.Length == 0)) {
                if(sessions.Count == 1) {
                    return sessions.ToList();
                } else {
                    throw new ApplicationException("No sessionId specified but multiple sessions exists; cannot determine default session. Secify session id using -SessionId parameter");
                }
            }
            return sessions.Where(s => sessionIds.Contains(s.SessionId)).ToList();
        }
    }
}
