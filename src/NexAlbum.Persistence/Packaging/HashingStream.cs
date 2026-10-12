using System.Security.Cryptography;

namespace NexAlbum.Persistence.Packaging;

/// <summary>
/// Yazılan veriyi hedefe iletirken SHA-256 özetini hesaplar (büyük dosyalar belleğe alınmaz).
/// </summary>
internal sealed class HashingStream(Stream inner) : Stream
{
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public string Finish() => Convert.ToHexStringLower(hash.GetHashAndReset());

    public override void Write(byte[] buffer, int offset, int count)
    {
        hash.AppendData(buffer, offset, count);
        inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        hash.AppendData(buffer);
        inner.Write(buffer);
    }

    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hash.Dispose();
        }

        base.Dispose(disposing);
    }
}
