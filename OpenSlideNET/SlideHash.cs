using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;

namespace OpenSlideNET;

public static class SlideHash {
	private static readonly MD5 Md5 = MD5.Create();
	private static readonly SHA256 Sha256 = SHA256.Create();

	public static string GetHash(string filePath) {
		using var stream = File.OpenRead(filePath);
		return GetHash(stream);
	}
	
	public static string GetHash2(string filePath) {
		using var stream = File.OpenRead(filePath);
		return GetHash2(stream);
	}

	public static string GetHash(Stream stream) {
		if (!stream.CanRead || stream.Length == 0) {
			throw new OpenSlideException("Quick hash Failed: Empty stream.");
		}

		var length = (int)Math.Min(stream.Length, 1024 * 1000);
		var buffer = ArrayPool<byte>.Shared.Rent(length);

		try {
			if (stream.Read(buffer, 0, length) != length) {
				throw new OpenSlideException("Quick hash Failed: Unexpected end of stream.");
			}

			var output = Md5.ComputeHash(buffer, 0, length);
			return Convert.ToHexString(output);
		} finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	public static string GetHash2(Stream stream) {
		if (!stream.CanRead || stream.Length == 0) {
			throw new OpenSlideException("Quick hash Failed: Empty stream.");
		}

		return Convert.ToHexString(Sha256.ComputeHash(stream));
	}
}