using Microsoft.KernelMemory;
using System.Collections.Generic;

namespace ChatApp.KernelMemory.Models
{
    public class KernelResponse
    {
        public string Answer { get; set; } = string.Empty;

        public List<Citation> Citations { get; set; } = [];
    }
}
