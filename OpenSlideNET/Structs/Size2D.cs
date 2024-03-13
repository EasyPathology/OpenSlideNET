using System;
using System.Runtime.InteropServices;

namespace OpenSlideNET.Structs;

/// <summary>
/// Double形式记录的Size
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Size2D(double Width, double Height) : IFormattable {
	public static Size2D operator +(Size2D a, Size2D b) => new(a.Width + b.Width, a.Height + b.Height);

	public static Size2D operator -(Size2D a, Size2D b) => new(a.Width - b.Width, a.Height - b.Height);

	public static Size2D operator *(Size2D a, Size2D b) => new(a.Width * b.Width, a.Height * b.Height);

	public static Size2D operator /(Size2D a, Size2D b) => new(a.Width / b.Width, a.Height / b.Height);

	public static Size2D operator *(Size2D a, double b) => new(a.Width * b, a.Height * b);

	public static Size2D operator /(Size2D a, double b) => new(a.Width / b, a.Height / b);

	public static implicit operator Size2D(Size2I p) {
		return new Size2D(p.Width, p.Height);
	}

	public double Mean => Width / 2 + Height / 2;  // 防止溢出

	public bool Equals(Size2D other) {
		return Width.Equals(other.Width) && Height.Equals(other.Height);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Width, Height);
	}

	public override string ToString() {
		return ToString(null, null);
	}

	public string ToString(string? format, IFormatProvider? formatProvider) {
		return $"({Width.ToString(format, formatProvider)}, {Height.ToString(format, formatProvider)})";
	}
}