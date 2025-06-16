using System.ComponentModel.DataAnnotations;

namespace panelOrmo.Models
{
    public class CollectionProduct
    {
        public int PID { get; set; }
        public string PCode { get; set; }
        public string PName { get; set; }
        public string PInfoPreview { get; set; }
        public string PContent { get; set; }
        public bool PIsValid { get; set; }
        public DateTime PCreatedDate { get; set; }
        public int? PCreatedUserID { get; set; }
    }

    public class CollectionProductViewModel
    {
        [Required]
        public string ProductCode { get; set; }
        [Required]
        public int CollectionGroupID { get; set; }
        [Required]
        public string Content { get; set; }
        public IFormFile SmallImage { get; set; }
        public IFormFile MediumImage { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
