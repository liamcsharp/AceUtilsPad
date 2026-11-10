using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUP;

internal sealed class AviWriter : IDisposable
{
	private const uint HasIndexFlag = 0x10;
	private const uint KeyFrameFlag = 0x10;

	private readonly FileStream stream;
	private readonly BinaryWriter writer;
	private readonly List<uint> offsets = new List<uint>();
	private readonly List<uint> sizes = new List<uint>();
	private readonly long totalFramesPosition;
	private readonly long streamLengthPosition;
	private readonly long maxBytesPosition;
	private readonly long moviSizePosition;
	private readonly long moviStart;
	private readonly int framesPerSecond;
	private uint largestFrame;
	private bool closed;

	public AviWriter(string path, int width, int height, int fps)
	{
		framesPerSecond = Math.Max(1, fps);
		stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
		writer = new BinaryWriter(stream);

		Tag("RIFF");
		writer.Write(0u);
		Tag("AVI ");

		Tag("LIST");
		writer.Write(192u);
		Tag("hdrl");
		Tag("avih");
		writer.Write(56u);
		writer.Write((uint)(1000000 / framesPerSecond));
		maxBytesPosition = stream.Position;
		writer.Write(0u);
		writer.Write(0u);
		writer.Write(HasIndexFlag);
		totalFramesPosition = stream.Position;
		writer.Write(0u);
		writer.Write(0u);
		writer.Write(1u);
		writer.Write((uint)(width * height * 3));
		writer.Write((uint)width);
		writer.Write((uint)height);
		writer.Write(0u);
		writer.Write(0u);
		writer.Write(0u);
		writer.Write(0u);

		Tag("LIST");
		writer.Write(116u);
		Tag("strl");
		Tag("strh");
		writer.Write(56u);
		Tag("vids");
		Tag("MJPG");
		writer.Write(0u);
		writer.Write((ushort)0);
		writer.Write((ushort)0);
		writer.Write(0u);
		writer.Write(1u);
		writer.Write((uint)framesPerSecond);
		writer.Write(0u);
		streamLengthPosition = stream.Position;
		writer.Write(0u);
		writer.Write((uint)(width * height * 3));
		writer.Write(0xFFFFFFFFu);
		writer.Write(0u);
		writer.Write((ushort)0);
		writer.Write((ushort)0);
		writer.Write((ushort)width);
		writer.Write((ushort)height);

		Tag("strf");
		writer.Write(40u);
		writer.Write(40u);
		writer.Write(width);
		writer.Write(height);
		writer.Write((ushort)1);
		writer.Write((ushort)24);
		Tag("MJPG");
		writer.Write((uint)(width * height * 3));
		writer.Write(0);
		writer.Write(0);
		writer.Write(0u);
		writer.Write(0u);

		Tag("LIST");
		moviSizePosition = stream.Position;
		writer.Write(0u);
		moviStart = stream.Position;
		Tag("movi");
	}

	public int FrameCount => offsets.Count;

	public void AddFrame(byte[] jpeg)
	{
		if (closed || jpeg == null || jpeg.Length == 0)
		{
			return;
		}

		offsets.Add((uint)(stream.Position - moviStart));
		sizes.Add((uint)jpeg.Length);
		largestFrame = Math.Max(largestFrame, (uint)jpeg.Length);
		Tag("00dc");
		writer.Write((uint)jpeg.Length);
		writer.Write(jpeg);
		if ((jpeg.Length & 1) == 1)
		{
			writer.Write((byte)0);
		}
	}

	public void Dispose()
	{
		if (closed)
		{
			return;
		}

		closed = true;
		try
		{
			long moviEnd = stream.Position;
			Tag("idx1");
			writer.Write((uint)(offsets.Count * 16));
			for (int i = 0; i < offsets.Count; i++)
			{
				Tag("00dc");
				writer.Write(KeyFrameFlag);
				writer.Write(offsets[i]);
				writer.Write(sizes[i]);
			}

			long end = stream.Position;
			Patch(4, (uint)(end - 8));
			Patch(moviSizePosition, (uint)(moviEnd - moviStart));
			Patch(totalFramesPosition, (uint)offsets.Count);
			Patch(streamLengthPosition, (uint)offsets.Count);
			Patch(maxBytesPosition, largestFrame * (uint)framesPerSecond);
		}
		finally
		{
			writer.Flush();
			stream.Dispose();
		}
	}

	private void Tag(string fourCc)
	{
		writer.Write(Encoding.ASCII.GetBytes(fourCc));
	}

	private void Patch(long position, uint value)
	{
		long restore = stream.Position;
		stream.Position = position;
		writer.Write(value);
		stream.Position = restore;
	}
}
