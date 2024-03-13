using System;
using System.Runtime.InteropServices;

namespace OpenSlideNET.Structs;

/// <summary>
/// Int形式记录的Size
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Size2I(int Width, int Height) {
	public static Size2I operator +(Size2I a, Size2I b) => new(a.Width + b.Width, a.Height + b.Height);

	public static Size2I operator -(Size2I a, Size2I b) => new(a.Width + b.Width, a.Height + b.Height);

	public static Size2I operator *(Size2I a, int b) => new(a.Width * b, a.Height * b);

	public static Size2I operator /(Size2I a, int b) => new(a.Width / b, a.Height / b);
	
	public static Size2D operator *(Size2I a, double b) => new(a.Width * b, a.Height * b);

	public static Size2D operator /(Size2I a, double b) => new(a.Width / b, a.Height / b);

	public static implicit operator Size2I(Size2D p) {
		return new Size2I((int)p.Width, (int)p.Height);
	}

	public bool Equals(Size2I other) {
		return Width.Equals(other.Width) && Height.Equals(other.Height);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Width, Height);
	}

	public override string ToString() {
		return $"({Width}, {Height})";
	}
}