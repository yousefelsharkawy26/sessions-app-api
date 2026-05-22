using System;
using System.Security.Cryptography;
using System.Text;

namespace SessionApp.Application.Common.Helpers;

public static class MnemonicHelper
{
    private static readonly string[] WordList = new[]
    {
        "apple", "banana", "cherry", "danger", "eagle", "forest", "grape", "harbor", "island", "jungle",
        "kitten", "lemon", "mountain", "nature", "ocean", "palace", "queen", "river", "shadow", "tiger",
        "umbrella", "valley", "window", "xenon", "yellow", "zebra", "anchor", "beacon", "canyon", "desert",
        "engine", "feather", "garden", "helmet", "ivory", "jacket", "koala", "lantern", "marble", "needle",
        "oxygen", "pebble", "quartz", "radar", "saddle", "tunnel", "update", "vessel", "wisdom", "youth"
    };

    public static string GenerateMnemonic()
    {
        var words = new string[12];
        for (int i = 0; i < 12; i++)
        {
            var randomIndex = RandomNumberGenerator.GetInt32(WordList.Length);
            words[i] = WordList[randomIndex];
        }
        return string.Join(" ", words);
    }

    public static string HashMnemonic(string mnemonic)
    {
        var clean = mnemonic.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(clean);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyMnemonic(string mnemonic, string storedHash)
    {
        var incomingHash = HashMnemonic(mnemonic);
        return string.Equals(incomingHash, storedHash, StringComparison.Ordinal);
    }
}
