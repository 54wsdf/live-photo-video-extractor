using System.Buffers.Binary;

namespace LivePhotoVideoExtractor.Core;

public static class MotionPhotoParser
{
    private const int ScanBufferSize = 64 * 1024;
    private static readonly byte[] FtypSignature = [(byte)'f', (byte)'t', (byte)'y', (byte)'p'];

    public static EmbeddedVideoSegment? Locate(Stream jpegStream)
    {
        ArgumentNullException.ThrowIfNull(jpegStream);
        if (!jpegStream.CanRead || !jpegStream.CanSeek)
        {
            throw new ArgumentException("The source stream must be readable and seekable.", nameof(jpegStream));
        }

        var originalPosition = jpegStream.Position;
        try
        {
            if (!HasJpegHeader(jpegStream))
            {
                return null;
            }

            var jpegEnd = FindJpegEnd(jpegStream);
            if (jpegEnd < 0 || jpegEnd >= jpegStream.Length)
            {
                return null;
            }

            foreach (var candidate in FindFtypCandidates(jpegStream, jpegEnd))
            {
                if (TryParseMp4(jpegStream, candidate, out var length))
                {
                    return new EmbeddedVideoSegment(candidate, length);
                }
            }

            return null;
        }
        finally
        {
            jpegStream.Position = originalPosition;
        }
    }

    internal static bool IsValidMp4(Stream mp4Stream)
    {
        ArgumentNullException.ThrowIfNull(mp4Stream);
        if (!mp4Stream.CanRead || !mp4Stream.CanSeek)
        {
            return false;
        }

        var originalPosition = mp4Stream.Position;
        try
        {
            return TryParseMp4(mp4Stream, 0, out var length) && length == mp4Stream.Length;
        }
        finally
        {
            mp4Stream.Position = originalPosition;
        }
    }

    private static bool HasJpegHeader(Stream stream)
    {
        if (stream.Length < 4)
        {
            return false;
        }

        stream.Position = 0;
        return stream.ReadByte() == 0xFF && stream.ReadByte() == 0xD8;
    }

    private static long FindJpegEnd(Stream stream)
    {
        stream.Position = 2;
        var buffer = new byte[ScanBufferSize];
        var previous = -1;
        long absolutePosition = 2;

        while (true)
        {
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                return -1;
            }

            for (var index = 0; index < bytesRead; index++, absolutePosition++)
            {
                var current = buffer[index];
                if (previous == 0xFF && current == 0xD9)
                {
                    return absolutePosition + 1;
                }

                previous = current;
            }
        }
    }

    private static IReadOnlyList<long> FindFtypCandidates(Stream stream, long startPosition)
    {
        var candidates = new List<long>();
        var buffer = new byte[ScanBufferSize];
        var matched = 0;
        var absolutePosition = startPosition;
        stream.Position = startPosition;

        while (true)
        {
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                break;
            }

            for (var index = 0; index < bytesRead; index++, absolutePosition++)
            {
                var current = buffer[index];
                if (current == FtypSignature[matched])
                {
                    matched++;
                    if (matched == FtypSignature.Length)
                    {
                        var candidate = absolutePosition - 7;
                        if (candidate >= startPosition)
                        {
                            candidates.Add(candidate);
                        }

                        matched = 0;
                    }
                }
                else
                {
                    matched = current == FtypSignature[0] ? 1 : 0;
                }
            }
        }

        return candidates;
    }

    private static bool TryParseMp4(Stream stream, long startPosition, out long length)
    {
        length = 0;
        if (startPosition < 0 || stream.Length - startPosition < 12)
        {
            return false;
        }

        var position = startPosition;
        var lastValidEnd = startPosition;
        var firstBox = true;
        var foundFtyp = false;
        var foundMoov = false;
        var foundMdat = false;

        while (stream.Length - position >= 8)
        {
            if (!TryReadBox(stream, position, out var box))
            {
                break;
            }

            if (firstBox && box.Type != "ftyp")
            {
                return false;
            }

            if (box.Type == "ftyp" && box.Size < 12)
            {
                return false;
            }

            foundFtyp |= box.Type == "ftyp";
            foundMoov |= box.Type == "moov";
            foundMdat |= box.Type == "mdat";
            firstBox = false;
            lastValidEnd = position + box.Size;
            position = lastValidEnd;

            if (box.ExtendsToEndOfFile)
            {
                break;
            }
        }

        if (!foundFtyp || !foundMoov || !foundMdat)
        {
            return false;
        }

        length = lastValidEnd - startPosition;
        return length > 0;
    }

    private static bool TryReadBox(Stream stream, long position, out Mp4Box box)
    {
        box = default;
        Span<byte> header = stackalloc byte[16];
        stream.Position = position;
        if (!TryReadExactly(stream, header[..8]))
        {
            return false;
        }

        var shortSize = BinaryPrimitives.ReadUInt32BigEndian(header[..4]);
        var headerSize = 8L;
        ulong unsignedSize;
        var extendsToEndOfFile = shortSize == 0;

        if (shortSize == 1)
        {
            if (!TryReadExactly(stream, header[8..16]))
            {
                return false;
            }

            headerSize = 16;
            unsignedSize = BinaryPrimitives.ReadUInt64BigEndian(header[8..16]);
        }
        else if (shortSize == 0)
        {
            unsignedSize = (ulong)(stream.Length - position);
        }
        else
        {
            unsignedSize = shortSize;
        }

        if (unsignedSize < (ulong)headerSize || unsignedSize > long.MaxValue)
        {
            return false;
        }

        var size = (long)unsignedSize;
        if (size > stream.Length - position)
        {
            return false;
        }

        var type = new string(
        [
            (char)header[4],
            (char)header[5],
            (char)header[6],
            (char)header[7],
        ]);

        box = new Mp4Box(type, size, extendsToEndOfFile);
        return true;
    }

    private static bool TryReadExactly(Stream stream, Span<byte> destination)
    {
        var totalRead = 0;
        while (totalRead < destination.Length)
        {
            var bytesRead = stream.Read(destination[totalRead..]);
            if (bytesRead == 0)
            {
                return false;
            }

            totalRead += bytesRead;
        }

        return true;
    }

    private readonly record struct Mp4Box(string Type, long Size, bool ExtendsToEndOfFile);
}
