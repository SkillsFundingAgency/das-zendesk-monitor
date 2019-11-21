#nullable disable

namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class Organisation
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public OrganizationFields OrganizationFields { get; set; }
    }
}