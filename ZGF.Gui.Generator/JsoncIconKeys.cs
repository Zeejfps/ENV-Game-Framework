using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ZGF.Gui.Generator;

/// <summary>Reads the keys of one object of a JSON file that may hold comments and trailing commas,
/// each with the <c>//</c> comment lines written on their own lines right above it. A blank line ends a
/// comment, so a heading set apart from the keys below it describes none of them.</summary>
internal sealed class JsoncIconKeys
{
    private readonly string _text;
    private readonly List<string> _comment = new();
    private int _pos;

    private JsoncIconKeys(string text)
    {
        _text = text;
    }

    /// <summary>The keys of the object under the root's <paramref name="section"/> key, in order, with
    /// their comments; none when the root has no such object.</summary>
    public static List<(string Key, string Description)> Read(string text, string section)
    {
        var reader = new JsoncIconKeys(text);
        return reader.ReadRoot(section);
    }

    private List<(string Key, string Description)> ReadRoot(string section)
    {
        var keys = new List<(string Key, string Description)>();
        SkipTrivia();
        Expect('{');
        while (NextKey(out var key, out _))
        {
            if (key == section && Peek() == '{')
                keys = ReadKeys();
            else
                SkipValue();
            SkipComma();
        }
        return keys;
    }

    private List<(string Key, string Description)> ReadKeys()
    {
        var keys = new List<(string Key, string Description)>();
        Expect('{');
        while (NextKey(out var key, out var description))
        {
            keys.Add((key, description));
            SkipValue();
            SkipComma();
        }
        return keys;
    }

    /// <summary>Moves onto the value of the next key of the object being read, taking the comment above
    /// the key as its description; false past the object's end.</summary>
    private bool NextKey(out string key, out string description)
    {
        SkipTrivia();
        key = "";
        description = string.Join(" ", _comment);
        _comment.Clear();
        if (Peek() == '}')
        {
            _pos++;
            return false;
        }
        key = ReadString();
        SkipTrivia();
        Expect(':');
        SkipTrivia();
        return true;
    }

    private void SkipComma()
    {
        SkipTrivia();
        if (Peek() == ',')
            _pos++;
    }

    private void SkipValue()
    {
        SkipValueText();
        _comment.Clear();
    }

    private void SkipValueText()
    {
        var c = Peek();
        if (c == '"')
        {
            ReadString();
            return;
        }
        if (c == '{' || c == '[')
        {
            SkipNested();
            return;
        }
        SkipLiteral();
    }

    private void SkipNested()
    {
        var depth = 0;
        while (_pos < _text.Length)
        {
            SkipTrivia();
            var c = Peek();
            if (c == '"')
            {
                ReadString();
                continue;
            }
            _pos++;
            if (c == '{' || c == '[')
                depth++;
            if (c == '}' || c == ']')
                depth--;
            if (depth == 0)
                return;
        }
        throw new FormatException("An object or array is not closed.");
    }

    private void SkipLiteral()
    {
        var start = _pos;
        while (_pos < _text.Length && IsLiteralChar(_text[_pos]))
            _pos++;
        if (_pos == start)
            throw new FormatException($"Expected a value at position {_pos}.");
    }

    private static bool IsLiteralChar(char c) => char.IsLetterOrDigit(c) || c == '-' || c == '+' || c == '.';

    /// <summary>Skips whitespace and comments, keeping the text of each <c>//</c> comment that stands on
    /// its own line, until a blank line.</summary>
    private void SkipTrivia()
    {
        var newlines = 0;
        while (_pos < _text.Length)
        {
            var c = _text[_pos];
            if (c == '\n')
            {
                newlines++;
                if (newlines > 1)
                    _comment.Clear();
                _pos++;
                continue;
            }
            if (char.IsWhiteSpace(c))
            {
                _pos++;
                continue;
            }
            if (StartsWith("//"))
            {
                ReadLineComment();
                newlines = 0;
                continue;
            }
            if (StartsWith("/*"))
            {
                SkipBlockComment();
                continue;
            }
            return;
        }
    }

    private void ReadLineComment()
    {
        var standsAlone = IsAtLineStart();
        var end = _text.IndexOf('\n', _pos);
        if (end < 0)
            end = _text.Length;
        var text = _text.Substring(_pos + 2, end - _pos - 2).Trim();
        _pos = end;
        if (standsAlone)
            _comment.Add(text);
    }

    private bool IsAtLineStart()
    {
        for (var i = _pos - 1; i >= 0; i--)
        {
            var c = _text[i];
            if (c == '\n')
                return true;
            if (!char.IsWhiteSpace(c))
                return false;
        }
        return true;
    }

    private void SkipBlockComment()
    {
        var end = _text.IndexOf("*/", _pos + 2, StringComparison.Ordinal);
        if (end < 0)
            throw new FormatException("A comment is not closed.");
        _pos = end + 2;
    }

    private bool StartsWith(string text) => string.CompareOrdinal(_text, _pos, text, 0, text.Length) == 0;

    private string ReadString()
    {
        Expect('"');
        var text = new StringBuilder();
        while (true)
        {
            if (_pos >= _text.Length)
                throw new FormatException("A string is not closed.");
            var c = _text[_pos++];
            if (c == '"')
                return text.ToString();
            if (c != '\\')
            {
                text.Append(c);
                continue;
            }
            ReadEscape(text);
        }
    }

    private void ReadEscape(StringBuilder text)
    {
        if (_pos >= _text.Length)
            throw new FormatException("An escape is not finished.");
        var c = _text[_pos++];
        switch (c)
        {
            case 'n':
                text.Append('\n');
                return;
            case 't':
                text.Append('\t');
                return;
            case 'r':
                text.Append('\r');
                return;
            case 'b':
                text.Append('\b');
                return;
            case 'f':
                text.Append('\f');
                return;
            case 'u':
                text.Append(ReadUnicodeEscape());
                return;
            default:
                text.Append(c);
                return;
        }
    }

    private char ReadUnicodeEscape()
    {
        if (_pos + 4 > _text.Length)
            throw new FormatException("A \\u escape is cut short.");
        var hex = _text.Substring(_pos, 4);
        _pos += 4;
        return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private char Peek()
    {
        if (_pos < _text.Length)
            return _text[_pos];
        return '\0';
    }

    private void Expect(char expected)
    {
        if (Peek() != expected)
            throw new FormatException($"Expected '{expected}' at position {_pos}.");
        _pos++;
    }
}
