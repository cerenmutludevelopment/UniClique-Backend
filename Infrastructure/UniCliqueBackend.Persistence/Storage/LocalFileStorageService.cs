using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Application.Interfaces.Services;

namespace UniCliqueBackend.Persistence.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private static readonly HashSet<string> AllowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf","image/jpeg","image/png"
        };
        private static readonly HashSet<string> AllowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",".jpg",".jpeg",".png"
        };

        public LocalFileStorageService()
        {
            _basePath = Path.Combine(AppContext.BaseDirectory, "data", "student-proofs");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> SaveStudentProofAsync(Stream stream, string contentType, string originalFileName)
        {
            if (!AllowedTypes.Contains(contentType)) throw new Exception("Unsupported file type.");
            if (stream == null) throw new Exception("File is required.");
            var ext = Path.GetExtension(originalFileName);
            ext = string.IsNullOrEmpty(ext) ? null : ext.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExts.Contains(ext))
            {
                ext = contentType switch
                {
                    "application/pdf" => ".pdf",
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    _ => ".bin"
                };
            }
            var filename = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(_basePath, filename);
            using (var fs = new FileStream(path, FileMode.Create))
            {
                await stream.CopyToAsync(fs);
            }
            return Path.GetFileNameWithoutExtension(filename);
        }

        public (string Path, string ContentType)? OpenStudentProof(string id)
        {
            var matches = Directory.GetFiles(_basePath, id + ".*", SearchOption.TopDirectoryOnly);
            if (matches.Length == 0) return null;
            var path = matches[0];
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
            return (path, contentType);
        }
    }
}
