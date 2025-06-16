using System.ComponentModel.DataAnnotations;

namespace panelOrmo.Models
{
    public class Collection
    {
        public int BID { get; set; }
        public string BName { get; set; }
        public string BTitle { get; set; }
        public string BSummary { get; set; }
        public string BImage { get; set; }
        public int BLanguageID { get; set; }
        public bool BIsValid { get; set; }
        public DateTime BDate { get; set; }
        public DateTime BDate2 { get; set; }
        public DateTime BCreatedDate { get; set; }
        public int? BCreatedUserID { get; set; }
        public int BOrder { get; set; }
    }

    public class CollectionViewModel
    {
        [Required]
        public string Name { get; set; }
        public string Summary { get; set; }
        [Required]
        public int LanguageID { get; set; }
        public IFormFile Image { get; set; }
        public DateTime? StartDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
