namespace eAGM_eRequest_API.Model.DBContext
{
    public class FileUploadModel
    {
        public IEnumerable<IFormFile> Files { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
