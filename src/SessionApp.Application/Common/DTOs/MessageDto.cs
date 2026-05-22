using System;

namespace SessionApp.Application.Common.DTOs;

public class MessageDto
{
    public Guid Id { get; set; }
    public required string SenderId { get; set; }
    public required string SenderUsername { get; set; }
    public required string ReceiverId { get; set; }
    public required string ReceiverUsername { get; set; }
    public required string Ciphertext { get; set; }
    public required string EphemeralKey { get; set; }
    public int SignedPrekeyIdUsed { get; set; }
    public int? OneTimePrekeyIdUsed { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? BurnAfterSeconds { get; set; }
}
