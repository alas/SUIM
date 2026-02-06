namespace SUIM;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// Parser for SUIM markup files.
/// Handles the expansion pass, resolution pass, and layout calculations.
/// </summary>
public class SUIMLexer
{
    private string _input = "";
    private int _position = 0;
    private int _line = 1;
    private int _column = 1;

    public List<Token> Tokenize(string input)
    {
        _input = input;
        _position = 0;
        _line = 1;
        _column = 1;

        var tokens = new List<Token>();

        while (_position < _input.Length)
        {
            SkipWhitespace();

            if (_position >= _input.Length)
                break;

            var current = _input[_position];

            if (current == '<')
            {
                tokens.Add(ParseTag());
            }
            else if (current == '@')
            {
                tokens.Add(ParseControlFlow());
            }
            else
            {
                tokens.Add(ParseText());
            }
        }

        tokens.Add(new Token { Type = TokenType.EOF, Value = "", Line = _line, Column = _column });
        return tokens;
    }

    private void SkipWhitespace()
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
        {
            if (_input[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }

    private Token ParseTag()
    {
        int startLine = _line, startColumn = _column;
        _position++; // consume '<'
        _column++;

        var tagContent = new StringBuilder();
        while (_position < _input.Length && _input[_position] != '>')
        {
            tagContent.Append(_input[_position]);
            _position++;
            _column++;
        }

        if (_position < _input.Length)
        {
            _position++; // consume '>'
            _column++;
        }

        return new Token
        {
            Type = TokenType.Tag,
            Value = tagContent.ToString(),
            Line = startLine,
            Column = startColumn
        };
    }

    private Token ParseControlFlow()
    {
        int startLine = _line, startColumn = _column;
        _position++; // consume '@'
        _column++;

        var content = new StringBuilder();
        while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]))
        {
            content.Append(_input[_position]);
            _position++;
            _column++;
        }

        var keyword = content.ToString().ToLower();
        var tokenType = keyword switch
        {
            "if" => TokenType.ControlIf,
            "else" => TokenType.ControlElse,
            "switch" => TokenType.ControlSwitch,
            "foreach" => TokenType.ControlForEach,
            _ => TokenType.At
        };

        return new Token
        {
            Type = tokenType,
            Value = keyword,
            Line = startLine,
            Column = startColumn
        };
    }

    private Token ParseText()
    {
        int startLine = _line, startColumn = _column;
        var text = new StringBuilder();

        while (_position < _input.Length && _input[_position] != '<' && _input[_position] != '@')
        {
            text.Append(_input[_position]);
            if (_input[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }

        return new Token
        {
            Type = TokenType.Text,
            Value = text.ToString(),
            Line = startLine,
            Column = startColumn
        };
    }
}

/// <summary>
/// Token types for SUIM parsing.
/// </summary>
public enum TokenType
{
    Tag,
    Text,
    At,
    ControlIf,
    ControlElse,
    ControlSwitch,
    ControlForEach,
    EOF
}

/// <summary>
/// Represents a lexical token.
/// </summary>
public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }

    public override string ToString() => $"{Type}: {Value} @{Line}:{Column}";
}