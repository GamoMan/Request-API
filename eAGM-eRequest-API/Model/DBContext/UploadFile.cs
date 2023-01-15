using System.ComponentModel.DataAnnotations;

namespace Models.DBContext
{
    public class UploadFile
    {
        [Key]
        public Guid ID { get; set; }
        public string HolderID { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] FileContent { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
