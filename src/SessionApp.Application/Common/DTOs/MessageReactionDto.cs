using System;

namespace SessionApp.Application.Common.DTOs;

public class MessageReactionDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public required string UserId { get; set; }
    public required string Username { get; set; }
    public required string ReactionCiphertext { get; set; }
}
