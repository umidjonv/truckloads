using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TL.Module.AIProcessing.Worker.Dtos;

public class PerplexityRequestBodyMessageDto
{
    public string Role { get; set; } = MessageRole.System;

    public required string Content { get; set; }

    public static class MessageRole
    {
        public static string System = "system";
        public static string User = "user";
        public static string Assistant = "assistant";
    }
}