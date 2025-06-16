namespace panelOrmo.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string TableName { get; set; }
        public int? RecordId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
