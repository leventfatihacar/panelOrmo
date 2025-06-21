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

    public class ProductImage
    {
        public int PIID { get; set; }
        public int PIProductID { get; set; }
        public string? PISmallImage { get; set; }
        public string? PIMediumImage { get; set; }
        public string? PIDescription { get; set; }
        public bool PIIsValid { get; set; }
        public DateTime PICreatedDate { get; set; }
        public int? PICreatedUserID { get; set; }
    }

    public class ImagePair
    {
        public IFormFile? SmallImage { get; set; }
        public IFormFile? MediumImage { get; set; }
    }

    public class ExistingImagePair
    {
        public int PIID { get; set; }
        public IFormFile? NewSmallImage { get; set; }
        public IFormFile? NewMediumImage { get; set; }
    }

    public class CollectionProductViewModel
    {
        [Required]
        public string ProductCode { get; set; }
        [Required]
        public int CollectionGroupID { get; set; }
        [Required]
        public string Content { get; set; }
        public bool IsActive { get; set; } = true;

        // For multiple images
        public List<ImagePair> ImagePairs { get; set; } = new List<ImagePair>();

        // For backward compatibility (will be removed)
        public IFormFile? SmallImage { get; set; }
        public IFormFile? MediumImage { get; set; }
    }

    public class CollectionProductEditViewModel
    {
        public CollectionProductViewModel Product { get; set; }
        public List<ExistingImagePair> ExistingImages { get; set; } = new List<ExistingImagePair>();
        public List<ImagePair> NewImagePairs { get; set; } = new List<ImagePair>();
        public string? DeletedImageIds { get; set; }

        // For backward compatibility (will be removed)
        public string? CurrentSmallImage { get; set; }
        public string? CurrentMediumImage { get; set; }
    }
}