namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class MotionPhotoParserTests
{
    [Fact]
    public void Locate_finds_valid_ftyp_moov_mdat_trailer()
    {
        var mp4 = TestMediaFactory.MinimalMp4();
        var jpeg = TestMediaFactory.JpegWithTrailer(mp4);
        using var stream = new MemoryStream(jpeg);
        stream.Position = 1;

        var segment = MotionPhotoParser.Locate(stream);

        Assert.Equal(new EmbeddedVideoSegment(jpeg.Length - mp4.Length, mp4.Length), segment);
        Assert.Equal(1, stream.Position);
    }

    [Fact]
    public void Locate_ignores_ftyp_text_inside_jpeg_pixels()
    {
        var falseCandidate = new byte[] { 0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p' };
        var mp4 = TestMediaFactory.MinimalMp4();
        var jpeg = TestMediaFactory.JpegWithTrailer(mp4, falseCandidate);
        using var stream = new MemoryStream(jpeg);

        var segment = MotionPhotoParser.Locate(stream);

        Assert.NotNull(segment);
        Assert.Equal(jpeg.Length - mp4.Length, segment.Value.Offset);
    }

    [Fact]
    public void Locate_rejects_static_jpeg()
    {
        var jpeg = TestMediaFactory.JpegWithTrailer(Array.Empty<byte>());
        using var stream = new MemoryStream(jpeg);

        var segment = MotionPhotoParser.Locate(stream);

        Assert.Null(segment);
    }

    [Fact]
    public void Locate_rejects_truncated_mp4_box()
    {
        var trailer = TestMediaFactory.Combine(
            TestMediaFactory.Box("ftyp", new byte[] { 1, 2, 3, 4 }),
            TestMediaFactory.Box("moov", Array.Empty<byte>()),
            TestMediaFactory.TruncatedBox("mdat", 256, new byte[] { 1, 2, 3 }));
        var jpeg = TestMediaFactory.JpegWithTrailer(trailer);
        using var stream = new MemoryStream(jpeg);

        var segment = MotionPhotoParser.Locate(stream);

        Assert.Null(segment);
    }

    [Fact]
    public void Locate_accepts_64_bit_mp4_box_sizes()
    {
        var mp4 = TestMediaFactory.MinimalMp4(extendedMdat: true);
        var jpeg = TestMediaFactory.JpegWithTrailer(mp4);
        using var stream = new MemoryStream(jpeg);

        var segment = MotionPhotoParser.Locate(stream);

        Assert.Equal(new EmbeddedVideoSegment(jpeg.Length - mp4.Length, mp4.Length), segment);
    }
}
