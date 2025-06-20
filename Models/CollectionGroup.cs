using System.ComponentModel.DataAnnotations;

namespace panelOrmo.Models
{
    public class CollectionGroup
    {
        public int DID { get; set; }
        public int DLanguageID { get; set; }
        public int DParentID { get; set; }
        public string DName { get; set; }
        public string? DPicture { get; set; }
        public bool DIsValid { get; set; }
        public DateTime DCreatedDate { get; set; }
        public int? DCreatedUserID { get; set; }
        public int DOrder { get; set; }
    }

    public class CollectionGroupViewModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public int ParentCollectionID { get; set; }
        [Required]
        public int LanguageID { get; set; }
        public IFormFile? Image { get; set; }
        public bool IsActive { get; set; } = true;
    }
}