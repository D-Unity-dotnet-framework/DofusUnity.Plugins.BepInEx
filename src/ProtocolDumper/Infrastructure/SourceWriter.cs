﻿using static ProtocolDumper.Infrastructure.WellKnownChars;
using MemoryExtensions = Il2CppSystem.MemoryExtensions;

using Il2CppSystem.Text;
using Environment = Il2CppSystem.Environment;


namespace ProtocolDumper.Infrastructure;

/// <summary>
/// A thin wrapper over <see cref="StringBuilder" /> that adds indentation to each line built.
/// </summary>
internal sealed class SourceWriter
{
	private readonly StringBuilder _sb = new();
	private readonly bool _useCachedToString;
	private string? _cachedToString;
	private int _indentation;

	/// <summary>
	/// Initializes a new instance of the <see cref="SourceWriter"/> class.
	/// </summary>
	/// <param name="indentationChar">The character used for indentation</param>
	/// <param name="charsPerIndentation">The number of characters used for indentation</param>
	/// <param name="cacheToString">Whether the <see cref="ToString"/> result must be cached or not.</param>
	/// <exception cref="ArgumentOutOfRangeException">Indentation != whitespace or chars per indentation lower than 1.</exception>
	public SourceWriter(char indentationChar, int charsPerIndentation, bool cacheToString = true)
	{
		if (!char.IsWhiteSpace(indentationChar)) throw new ArgumentOutOfRangeException(nameof(indentationChar));
		if (charsPerIndentation < 1) throw new ArgumentOutOfRangeException(nameof(charsPerIndentation));

		_useCachedToString = cacheToString;
		IndentationChar = indentationChar;
		CharsPerIndentation = charsPerIndentation;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SourceWriter"/> class with a default indentation of 4 spaces.
	/// </summary>
	/// <param name="cacheToString">Whether the <see cref="ToString"/> result must be cached or not.</param>
	public SourceWriter(bool cacheToString = true) : this(' ', 4, cacheToString)
	{
	}

	/// <summary>
	/// Gets the current length of the underlying <see cref="StringBuilder"/> instance.
	/// </summary>
	public int Length => _sb.Length;

	/// <summary>
	/// Gets the character used for indentation.
	/// </summary>
	public char IndentationChar { get; }

	/// <summary>
	/// Gets the number of <see cref="IndentationChar"/> characters used for indentation.
	/// </summary>
	public int CharsPerIndentation { get; }

	/// <summary>
	/// Gets or sets the current indentation level.
	/// </summary>
	public int Indentation
	{
		get => _indentation;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			_indentation = value;
		}
	}

	/// <summary>
	/// Open a code block by writing an indented open brace followed by the default line terminator to the text stream.
	/// </summary>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter OpenBlock()
	{
		AppendLine(OpenBrace);
		Indentation++;
		return this;
	}

	/// <summary>
	/// Closes a code block by writing an indented close brace followed by the default line terminator to the text stream.
	/// If the current <see cref="Indentation"/> is 0, the close brace will be written without following default line terminator.
	/// </summary>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter CloseBlock()
	{
		Indentation--;

		AppendLine(CloseBrace);

		return this;
	}

	/// <summary>
	/// Closes all the currently opened code blocks.
	/// </summary>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter CloseAllBlocks()
	{
		while (Indentation > 0)
			CloseBlock();

		return this;
	}

	/// <summary>
	/// Writes <paramref name="linesCount"/> empty lines to the text stream.
	/// </summary>
	/// <param name="linesCount">The number of empty lines to append to the text stream.</param>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter AppendEmptyLines(int linesCount)
	{
		for (int i = 0; i < linesCount; i++)
			_sb.AppendLine();

		return this;
	}

	/// <summary>
	/// Writes an indented character followed by the default line terminator to the text stream.
	/// </summary>
	/// <param name="value">The character to append to the text stream.</param>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter AppendLine(char value)
	{
		AddIndentation();

		_sb.Append(value);
		_sb.AppendLine();
		return this;
	}

	/// <summary>
	/// Writes the specified string followed by the default line terminator to the text stream.
	/// Each line will be indented according to the current <see cref="Indentation"/> value.
	/// </summary>
	/// <param name="text">The text to write.</param>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter AppendLine(string text)
	{
		if (_indentation == 0)
		{
			_sb.AppendLine(text);
			return this;
		}

		if (!text.Contains(Environment.NewLine, StringComparison.Ordinal))
		{
			AddIndentation();
			_sb.AppendLine(text);
			return this;
		}

		foreach (var line in text.Split(Environment.NewLine))
		{
			AddIndentation();
			_sb.AppendLine(line);
		}

		return this;
	}

	/// <summary>
	/// Append an empty line to the text stream.
	/// </summary>
	/// <returns>A self <see cref="SourceWriter"/> instance to chain calls.</returns>
	public SourceWriter AppendLine()
	{
		_sb.AppendLine();
		return this;
	}

	/// <inheritdoc />
	public override string ToString() => _useCachedToString
		? GetCachedToString()
		: _sb.ToString();

	private string GetCachedToString() => _cachedToString ??= _sb.ToString();

	private void AddIndentation() => _sb.Append(IndentationChar, CharsPerIndentation * _indentation);
}