namespace panelOrmo.Models
{
    public class CollectionProductIndexViewModel
    {
        public int PID { get; set; }
        public string PCode { get; set; }
        public string PName { get; set; }
        public string ParentCollectionGroupName { get; set; }
        public bool PIsValid { get; set; }
        public DateTime PCreatedDate { get; set; }
    }
} 