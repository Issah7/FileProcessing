namespace FileProcessing.Models
{
    public class FileProcessingModel
    {
        public IFormFileCollection Files { get; set; }
        public string FileName { get; set; }
        public string DestinationDirectory { get; set; }
        public bool IncludeErrors { get; set; }

        public string SelectedFormat { get; set; }
    }
}
