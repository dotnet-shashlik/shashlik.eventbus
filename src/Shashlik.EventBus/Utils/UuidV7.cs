using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace Shashlik.EventBus.Utils;

public struct UuidV7
{
    private readonly ulong _timePart;
    private readonly byte[] _randomPart;

    public UuidV7(ulong timePart, byte[] randomPart)
    {
        if (randomPart.Length != 10)
            throw new ArgumentOutOfRangeException(nameof(_randomPart));
        if (randomPart[0] >> 4 != 7)
            throw new ArgumentOutOfRangeException(nameof(_randomPart));
        if (randomPart[2] >> 6 != 2)
            throw new ArgumentOutOfRangeException(nameof(_randomPart));

        _timePart = timePart;
        _randomPart = randomPart;
    }

    public static UuidV7 NewUuid() => NewUuid(DateTimeOffset.UtcNow);

    public static UuidV7 NewUuid(DateTimeOffset dateTimeOffset)
    {
        var randomBytes = new byte[10];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(randomBytes);

        randomBytes[0] = (byte)((randomBytes[6] & 0x0F) | 0x70);
        randomBytes[2] = (byte)((randomBytes[8] & 0x3F) | 0x80);
        return new UuidV7((ulong)dateTimeOffset.ToUnixTimeMilliseconds(), randomBytes);
    }

    private byte[] AsByteArray()
    {
        var result = new byte[16];
        var millisLow = (int)_timePart;
        var millisHigh = (short)(_timePart >> 32);
        if (BitConverter.IsLittleEndian)
        {
            millisLow = IPAddress.HostToNetworkOrder(millisLow);
            millisHigh = IPAddress.HostToNetworkOrder(millisHigh);
        }

        Buffer.BlockCopy(BitConverter.GetBytes(millisHigh), 0, result, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(millisLow), 0, result, 2, 4);
        Buffer.BlockCopy(_randomPart, 0, result, 6, 10);

        return result;
    }

    public override string ToString() => AsString();

    private string AsString(bool uppercase = false, bool includeHyphens = true)
    {
        var hex = BitConverter.ToString(AsByteArray()).Replace("-", "");
        if (!uppercase)
        {
            hex = hex.ToLowerInvariant();
        }

        if (!includeHyphens) return hex;

        var builder = new StringBuilder(hex).Insert(20, '-').Insert(16, '-').Insert(12, '-').Insert(8, '-');
        return builder.ToString();
    }
}