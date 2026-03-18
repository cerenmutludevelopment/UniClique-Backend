using System.IO;
using System.Threading.Tasks;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveStudentProofAsync(Stream stream, string contentType, string originalFileName);
        (string Path, string ContentType)? OpenStudentProof(string id);
    }
}
