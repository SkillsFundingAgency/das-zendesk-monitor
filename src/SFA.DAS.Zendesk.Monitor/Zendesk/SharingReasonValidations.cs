using System.Collections.Generic;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    internal static class SharingReasonValidations
    {
        internal static List<ISharingValidation> List => new List<ISharingValidation>
        {
            new EscalatedResponseValidation(),
            new HandedOffResponseValidation(),
            new SolvedResponseValidation(),
        };
    }
}