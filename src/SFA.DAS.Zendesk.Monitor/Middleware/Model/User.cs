namespace SFA.DAS.Zendesk.Monitor.Middleware.Model
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public UserFields UserFields { get; set; }
    }
}