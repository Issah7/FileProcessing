using FileProcessing.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Aspose.Words;
using Aspose.Cells;
using Aspose.Slides;
using ImageMagick;
using SkiaSharp;
using MimeKit;

public class FileProcessingController : Controller
{
    // GET: /FileProcessing
    public IActionResult Index()
    {
        return View();
    }

    // POST: /FileProcessing/BatchRename
    [HttpPost]
    public async Task<IActionResult> BatchRename(FileProcessingModel model)
    {
        if (model == null)
        {
            ViewBag.Message = "Model is null.";
            return View("Index");
        }

        if (model.Files == null || model.Files.Count == 0)
        {
            ViewBag.Message = "No files selected.";
            return View("Index");
        }

        if (string.IsNullOrEmpty(model.DestinationDirectory))
        {
            ViewBag.Message = "Destination directory is not specified.";
            return View("Index");
        }

        try
        {
            string sanitizedDirectory = Path.GetFullPath(model.DestinationDirectory);

            if (!Directory.Exists(sanitizedDirectory))
            {
                Directory.CreateDirectory(sanitizedDirectory);
            }

            foreach (var formFile in model.Files)
            {
                if (formFile.Length > 0)
                {
                    // Temporary file path
                    string tempFilePath = Path.GetTempFileName();

                    // Destination path
                    string originalFilePath = Path.Combine(sanitizedDirectory, formFile.FileName);

                    using (var tempStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(tempStream);
                    }

                    if (System.IO.File.Exists(originalFilePath))
                    {
                        // Generate unique file name if file already exists
                        string uniqueFileName = $"{Path.GetFileNameWithoutExtension(formFile.FileName)}_{DateTime.Now.Ticks}{Path.GetExtension(formFile.FileName)}";
                        originalFilePath = Path.Combine(sanitizedDirectory, uniqueFileName);
                    }

                    // Move and rename the file
                    System.IO.File.Move(tempFilePath, originalFilePath);

                    string renamedFileName = $"{Path.GetFileNameWithoutExtension(originalFilePath)}_renamed{Path.GetExtension(originalFilePath)}";
                    string renamedFilePath = Path.Combine(sanitizedDirectory, renamedFileName);
                    System.IO.File.Move(originalFilePath, renamedFilePath);
                }
            }

            ViewBag.Message = $"Batch rename completed. Files are located at: {sanitizedDirectory}";
        }
        catch (Exception ex)
        {
            ViewBag.Message = $"Error: {ex.Message}";
        }

        return View("Index");
    }







    // POST: /FileProcessing/FileOrganization
    [HttpPost]
    public async Task<IActionResult> FileOrganization(FileProcessingModel model)
    {
        if (!string.IsNullOrEmpty(model.DestinationDirectory) && Directory.Exists(model.DestinationDirectory))
        {
            // Log the directory for debugging
            Console.WriteLine($"Destination Directory: {model.DestinationDirectory}");

            // Get all files from the specified directory
            string[] files = Directory.GetFiles(model.DestinationDirectory);

            // Log the number of files found
            Console.WriteLine($"Files found: {files.Length}");

            foreach (var file in files)
            {
                // Get file extension and prepare the destination directory
                string extension = Path.GetExtension(file).TrimStart('.').ToLower();
                string destinationDirectory = Path.Combine(model.DestinationDirectory, extension);

                // Create the directory if it does not exist
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                // Prepare the destination file path
                string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(file));

                // Log the file movement
                Console.WriteLine($"Moving file {file} to {destinationFile}");

                // Move the file
                System.IO.File.Move(file, destinationFile);
            }

            ViewBag.Message = "File organization completed.";
        }
        else
        {
            ViewBag.Message = "Invalid destination directory.";
        }

        return View("Index");
    }


    // POST: /FileProcessing/FileFormatConversion
    [HttpPost]
    public async Task<IActionResult> FileFormatConversion(FileProcessingModel model)
    {
        if (model == null || model.Files == null || model.Files.Count == 0 || string.IsNullOrEmpty(model.SelectedFormat))
        {
            ViewBag.Message = "Invalid input.";
            return View("Index");
        }

        try
        {
            foreach (var formFile in model.Files)
            {
                if (formFile.Length > 0)
                {
                    string fileExtension;
                    string outputFileName;

                    using (var stream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(stream);
                        stream.Position = 0;

                        switch (model.SelectedFormat)
                        {
                            case "PDF":
                                fileExtension = ".pdf";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                var wordDocument = new Aspose.Words.Document(stream);
                                wordDocument.Save(outputFileName, Aspose.Words.SaveFormat.Pdf);
                                break;

                            case "Word":
                                fileExtension = ".docx";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                var wordDoc = new Aspose.Words.Document(stream);
                                wordDoc.Save(outputFileName, Aspose.Words.SaveFormat.Docx);
                                break;

                            case "Excel":
                                fileExtension = ".xlsx";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                var workbook = new Aspose.Cells.Workbook(stream);
                                workbook.Save(outputFileName, Aspose.Cells.SaveFormat.Xlsx);
                                break;

                            case "CSV":
                                fileExtension = ".csv";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                using (var reader = new StreamReader(stream))
                                using (var writer = new StreamWriter(outputFileName))
                                {
                                    string line;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        writer.WriteLine(line); // CSV-specific logic
                                    }
                                }
                                break;

                            case "Image":
                                fileExtension = ".png";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                using (var image = new MagickImage(stream))
                                {
                                    image.Write(outputFileName);
                                }
                                break;

                            case "Presentation":
                                fileExtension = ".pptx";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                using (var presentation = new Presentation(stream))
                                {
                                    presentation.Save(outputFileName, Aspose.Slides.Export.SaveFormat.Pptx);
                                }
                                break;


                            case "Zip":
                                fileExtension = ".zip";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                using (var archive = ZipFile.Open(outputFileName, ZipArchiveMode.Create))
                                {
                                    var entry = archive.CreateEntry("file.txt");
                                    using (var entryStream = entry.Open())
                                    using (var writer = new StreamWriter(entryStream))
                                    using (var reader = new StreamReader(stream))
                                    {
                                        writer.Write(reader.ReadToEnd());
                                    }
                                }
                                break;

                            case "Email":
                                fileExtension = ".eml";
                                outputFileName = Path.Combine(model.DestinationDirectory, $"output{fileExtension}");

                                using (var message = MimeMessage.Load(stream))
                                using (var outputFile = new FileStream(outputFileName, FileMode.Create))
                                {
                                    message.WriteTo(outputFile);
                                }
                                break;

                            default:
                                ViewBag.Message = "Unsupported format.";
                                return View("Index");
                        }
                    }
                }
            }

            ViewBag.Message = "File format conversion completed.";
        }
        catch (Exception ex)
        {
            ViewBag.Message = $"Error: {ex.Message}";
        }

        return View("Index");
    }




    // POST: /FileProcessing/DuplicateFileCheck
    [HttpPost]
    public IActionResult DuplicateFileCheck(FileProcessingModel model)
    {
        var fileHashes = new Dictionary<string, List<string>>();
        string[] files = Directory.GetFiles(model.DestinationDirectory);

        foreach (var file in files)
        {
            string hash = ComputeFileHash(file);
            if (!fileHashes.ContainsKey(hash))
            {
                fileHashes[hash] = new List<string>();
            }
            fileHashes[hash].Add(file);
        }

        ViewBag.Duplicates = fileHashes.Where(x => x.Value.Count > 1).ToList();
        return View("Index");
    }

    private string ComputeFileHash(string filePath)
    {
        using (var stream = System.IO.File.OpenRead(filePath))
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
