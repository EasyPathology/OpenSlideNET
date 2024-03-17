using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace OpenSlideNET;

public static class TiffPageDeleter
{
    public record IFD(uint Offset, Tag[] Tags, uint NextOffset)
    {
        public string CompressionName => Tags.FirstOrDefault(x => x.Id == TagId.Compression)?.ValueOrValueOffset switch
        {
            1u => "None",
            5u => "LZW",
            6u => "JPEG",
            7u => "JPEG",
            34712u => "JP2K",
            33003u => "JP2K",
            33005u => "JP2K",
            _ => "Unknown",
        };
    }

    public enum TagId : ushort
    {
        ImageWidth = 0x100,
        ImageLength = 257,
        BitsPerSample = 258,
        Compression = 259,
        PhotometricInterpretation = 262,
        StripOffsets = 273,
        SamplesPerPixel = 277,
        RowsPerStrip = 278,
        StripByteCounts = 279
    }

    public enum TagValueType : ushort
    {
        Byte = 1,
        ASCII,
        Short,
        Long,
        Rational,
        SByte,
        Undefined,
        SShort,
        SLong,
        SRational,
        Float,
        Double
    }

    public record Tag(uint Offset, TagId Id, TagValueType ValueType, uint ValueCount, uint ValueOrValueOffset)
    {
        public bool IsValueOffset
        {
            get
            {
                var result = ValueType switch
                {
                    TagValueType.Byte => ValueCount > 4,
                    TagValueType.ASCII => ValueCount > 4,
                    TagValueType.SByte => ValueCount > 4,
                    TagValueType.Undefined => ValueCount > 4,
                    TagValueType.Short => ValueCount > 2,
                    TagValueType.SShort => ValueCount > 2,
                    TagValueType.Long => ValueCount > 1,
                    TagValueType.SLong => ValueCount > 1,
                    TagValueType.Float => ValueCount > 1,
                    TagValueType.Rational => true,
                    TagValueType.SRational => true,
                    TagValueType.Double => true,
                    _ => throw new ArgumentOutOfRangeException(nameof(ValueType), $"Unsupported value type: {ValueType}")
                };
                return result;
            }
        }

        public uint ReadActualValue(MemoryMappedViewAccessor accessor)
        {
            return IsValueOffset ? accessor.ReadUInt32(ValueOrValueOffset) : ValueOrValueOffset;
        }

        public void Write(MemoryMappedViewAccessor accessor)
        {
            accessor.Write(Offset, (uint)Id);
            accessor.Write(Offset + 2, (uint)ValueType);
            accessor.Write(Offset + 4, ValueCount);
            accessor.Write(Offset + 8, ValueOrValueOffset);
        }
    }

    public static void NdpiDeleteMacro(string inputPath, string outputPath) =>
        DeletePages(inputPath, outputPath, x => x.Count > 2 ? [x.Count - 2] : []);

    public static void DeletePages(string inputPath, string outputPath, Func<IReadOnlyList<IFD>, int[]> indices)
    {
        using var input =
            MemoryMappedFile.CreateFromFile(inputPath, FileMode.Open, null, 0L, MemoryMappedFileAccess.ReadWrite);
        using var inputAccessor = input.CreateViewAccessor();
        var       ifdList       = ReadIFDs(inputAccessor).ToList();
        if (ifdList.Count == 0)
        {
            throw new NotSupportedException("No TIFF pages found");
        }

        Debug.WriteLine("Index  Offset      Compression");
        foreach (var (i, offset, compression) in ifdList.Select((IFD x, int i) => (i, x.Offset, x.CompressionName)))
        {
            Debug.WriteLine($"{i,-5}  0x{offset:X8}  {compression}");
        }

        var inputFileInfo = new FileInfo(inputPath);
        var totalLength   = (uint)inputFileInfo.Length;
        using var output = File.Exists(outputPath) && inputFileInfo.FullName == new FileInfo(outputPath).FullName
            ? input
            : MemoryMappedFile.CreateFromFile(outputPath, FileMode.Create, null, totalLength);
        using var outputAccessor = output.CreateViewAccessor();
        var       inputPtr       = inputAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
        var       outputPtr      = outputAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
        MemoryCopy(0L, 0L, totalLength);
        var list = indices(ifdList);
        foreach (var (i, ifd) in ifdList.Select(static (x, i) => (i, x)).Where(i => list.Contains(i.i)))
        {
            var imageWidthTag      = ifd.Tags.FirstOrDefault(static tag => tag.Id == TagId.ImageWidth);
            var imageLengthTag     = ifd.Tags.FirstOrDefault(static tag => tag.Id == TagId.ImageLength);
            var compressionTag     = ifd.Tags.FirstOrDefault(static tag => tag.Id == TagId.Compression);
            var stripOffsetsTag    = ifd.Tags.FirstOrDefault(static tag => tag.Id == TagId.StripOffsets);
            var stripByteCountsTag = ifd.Tags.FirstOrDefault(static tag => tag.Id == TagId.StripByteCounts);
            if (imageWidthTag         == null
                || imageLengthTag     == null
                || compressionTag     == null
                || stripOffsetsTag    == null
                || stripByteCountsTag == null)
                throw new NotSupportedException($"Cannot find necessary tags to delete page {i} data");

            if (stripOffsetsTag.ValueCount > 1)
                throw new NotSupportedException($"Cannot handle strip data with count > 1, page {i}");

            var stripByteCount = stripByteCountsTag.ReadActualValue(inputAccessor);
            var offset         = stripOffsetsTag.ReadActualValue(inputAccessor);
            var stripSpan      = CreateSpan(outputPtr + offset, (int)stripByteCount);
            stripSpan.Clear();
            using var emptyMat = new Mat((int)imageLengthTag.ReadActualValue(inputAccessor),
                (int)imageWidthTag.ReadActualValue(inputAccessor),
                MatType.CV_8UC3,
                Scalar.Black);
            Cv2.ImEncode(".jpg", emptyMat, out var buf);

            if (buf.Length > stripByteCount)
                throw new NotSupportedException(
                    $"The new strip data is larger than the original, page {i}, cannot rewrite");

            new Span<byte>(buf).CopyTo(stripSpan);
            compressionTag = compressionTag with
            {
                ValueOrValueOffset = 1u
            };
            compressionTag.Write(outputAccessor);
        }

        return;

        unsafe void MemoryCopy(long sourceOffset, long destinationOffset, long length)
        {
            var source      = (byte*)(inputPtr  + (nint)sourceOffset);
            var destination = (byte*)(outputPtr + (nint)destinationOffset);
            if (source != destination) Buffer.MemoryCopy(source, destination, length, length);
        }
    }

    private static unsafe Span<byte> CreateSpan(long ptr, int size)
    {
        return MemoryMarshal.CreateSpan(ref *(byte*)ptr, size);
    }

    // ReSharper disable once InconsistentNaming
    private static IEnumerable<IFD> ReadIFDs(MemoryMappedViewAccessor accessor)
    {
        var ver = accessor.ReadUInt16(2L);
        if (ver != 42) yield break;
        var offset = accessor.ReadUInt32(4L);
        while (offset != 0)
        {
            var ifdOffset = offset;
            var tagCount = accessor.ReadUInt16(offset);
            var tags = new Tag[tagCount];
            offset += 2;  // Skip tag count
            for (var i = 0; i < tagCount; i++)
            {
                tags[i] = new Tag(offset,
                    (TagId)accessor.ReadUInt16(offset),
                    (TagValueType)accessor.ReadUInt16(offset + 2),
                    accessor.ReadUInt32(offset + 4),
                    accessor.ReadUInt32(offset + 8));
                offset += 12;
            }
            offset = accessor.ReadUInt32(ifdOffset + tagCount * 12 + 2);
            yield return new IFD(ifdOffset, tags, offset);
        }
    }
}