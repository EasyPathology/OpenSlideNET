using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenSlideNET;

public static class SlideHash
{
    /// <summary>
    /// 只有前 1000x1024 bytes 会被读取，用于快速哈希，不足补 0
    /// </summary>
    /// <param name="inStream"></param>
    private class HashStream(Stream inStream) : Stream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = Math.Min(count, 1000 * 1024 - (int)Position);
            if (readCount <= 0) return 0;
            var actualReadCount = inStream.Read(buffer, offset, readCount);
            if (actualReadCount < readCount)
            {
                Array.Clear(buffer, offset + actualReadCount, readCount - actualReadCount);
            }
            return actualReadCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inStream.Seek(offset, origin);
        }

        public override void Flush() => inStream.Flush();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => inStream.CanRead;
        public override bool CanSeek => inStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => 1000 * 1024;
        
        public override long Position
        {
            get => inStream.Position;
            set => inStream.Position = value;
        }
    }
    
    public static string GetHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return GetHash(stream);
    }
    
    // public static string GetDziHash(string dziFilePath)
    // {
    //     using var stream = File.OpenRead(filePath);
    //     return GetHash(stream);
    // }
    
    public static string GetHash2(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return GetHash2(stream);
    }

    public static string GetHash(Stream stream)
    {
        if (!stream.CanRead || stream.Length == 0)
        {
            throw new OpenSlideException("Quick hash Failed: Empty stream.");
        }

        return Convert.ToHexString(SHA256.HashData(new HashStream(stream)));
    }

    public static string GetHash2(Stream stream)
    {
        if (!stream.CanRead || stream.Length == 0)
        {
            throw new OpenSlideException("Quick hash Failed: Empty stream.");
        }

        return Convert.ToHexString(MD5.HashData(stream));
    }
    
    public static async ValueTask<string> GetHashAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return await GetHashAsync(stream);
    }
    
    public static async ValueTask<string> GetHash2Async(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return await GetHash2Async(stream);
    }

    public static async ValueTask<string> GetHashAsync(Stream stream)
    {
        if (!stream.CanRead || stream.Length == 0)
        {
            throw new OpenSlideException("Quick hash Failed: Empty stream.");
        }

        return Convert.ToHexString(await SHA256.HashDataAsync(new HashStream(stream)));
    }

    public static async ValueTask<string> GetHash2Async(Stream stream)
    {
        if (!stream.CanRead || stream.Length == 0)
        {
            throw new OpenSlideException("Quick hash Failed: Empty stream.");
        }

        return Convert.ToHexString(await MD5.HashDataAsync(stream));
    }
}