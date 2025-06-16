using System.ComponentModel.DataAnnotations;

namespace panelOrmo.Models
{
    public class News
    {
        public int CID { get; set; }
        public string CName { get; set; }
        public string CTitle { get; set; }
        public string CContent { get; set; }
        public int CLanguageID { get; set; }
        public string CImage { get; set; }
        public bool CIsValid { get; set; }
        public DateTime CDate { get; set; }
        public DateTime CDate2 { get; set; }
        public DateTime CCreatedDate { get; set; }
        public int? CCreatedUserID { get; set; }
        public int COrder { get; set; }
    }

    public class NewsViewModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public int LanguageID { get; set; }
        public IFormFile Image { get; set; }
        public DateTime? PublishDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}