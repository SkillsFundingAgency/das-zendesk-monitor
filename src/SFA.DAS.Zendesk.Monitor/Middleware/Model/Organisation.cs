namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class Organisation
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public OrganisationFields OrganizationFields { get; set; }
    }
}