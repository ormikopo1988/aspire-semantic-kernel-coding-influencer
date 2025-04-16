using ChatApp.KernelMemory.Models;
using System.Threading.Tasks;

namespace ChatApp.KernelMemory.Interfaces
{
    public interface IMemoryService
    {
        Task<bool> StoreText(string text);

        Task<bool> StoreFile(string content, string filename);

        Task<bool> StoreWebsite(string url);

        Task<KernelResponse> AskQuestion(string question);
    }
}
