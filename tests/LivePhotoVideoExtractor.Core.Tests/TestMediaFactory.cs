using System.Buffers.Binary;
using System.Text;

namespace LivePhotoVideoExtractor.Core.Tests;

internal static class TestMediaFactory
{
    public static byte[] MinimalMp4(bool extendedMdat = false)
    {
        return Combine(
            Box("ftyp", Encoding.ASCII.GetBytes("isom\0\0\u0002\0isommp42")),
            Box("moov", Array.Empty<byte>()),
            Box("mdat", new byte[] { 1, 2, 3, 4, 5 }, extendedMdat));
    }

    public static byte[] JpegWithTrailer(byte[] trailer, byte[]? beforeEoi = null)
    {
        return Combine(
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x02 },
            beforeEoi ?? Array.Empty<byte>(),
            new byte[] { 0xFF, 0xD9 },
            trailer);
    }

    public static byte[] Box(string type, byte[] payload, bool extendedSize = false)
    {
        if (type.Length != 4)
        {
            throw new ArgumentException("MP4 box types must contain four ASCII characters.", nameof(type));
        }

        var headerSize = extendedSize ? 16 : 8;
        var result = new byte[headerSize + payload.Length];
        if (extendedSize)
        {
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0, 4), 1);
            Encoding.ASCII.GetBytes(type, result.AsSpan(4, 4));
            BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(8, 8), (ulong)result.Length);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0, 4), (uint)result.Length);
            Encoding.ASCII.GetBytes(type, result.AsSpan(4, 4));
        }

        payload.CopyTo(result, headerSize);
        return result;
    }

    public static byte[] TruncatedBox(string type, uint declaredSize, byte[] actualPayload)
    {
        var result = new byte[8 + actualPayload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0, 4), declaredSize);
        Encoding.ASCII.GetBytes(type, result.AsSpan(4, 4));
        actualPayload.CopyTo(result, 8);
        return result;
    }

    public static byte[] Combine(params byte[][] parts)
    {
        var length = parts.Sum(part => part.Length);
        var result = new byte[length];
        var offset = 0;

        foreach (var part in parts)
        {
            part.CopyTo(result, offset);
            offset += part.Length;
        }

        return result;
    }
}
