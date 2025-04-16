namespace ChatApp.Contracts.Models;

public sealed class CreateWriterMessage
{
    public required string ResearchContext { get; init; }

    public required string InternalKnowledgeContext { get; init; }
    
    public required string WritingAsk { get; init; }
}
