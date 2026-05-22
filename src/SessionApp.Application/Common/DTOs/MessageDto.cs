using System;

namespace SessionApp.Application.Common.DTOs;

public class MessageDto
{
    public Guid Id { get; set; }
    public required string SenderId { get; set; }
    public required string SenderUsername { get; set; }
    public string? ReceiverId { get; set; }
    public string? ReceiverUsername { get; set; }
    public Guid? GroupId { get; set; }
    public required string Ciphertext { get; set; }
    public string? EphemeralKey { get; set; }
    public int? SignedPrekeyIdUsed { get; set; }
    public int? OneTimePrekeyIdUsed { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? BurnAfterSeconds { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
}
