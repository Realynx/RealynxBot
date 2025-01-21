using Microsoft.Extensions.AI;

namespace RealynxBot.Services.Interfaces {
    internal interface ILmComputerVision {
        Task<string> DescribeImage(List<ChatMessage> chatContext, byte[] imageData, string mimeType);
    }
}