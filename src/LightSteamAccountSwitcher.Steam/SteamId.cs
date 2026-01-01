using System.Globalization;

namespace LightSteamAccountSwitcher.Steam;

public class SteamId
{
    public string Id { get; private set; } = "STEAM_0:";

    public string Id3 { get; private set; } = "U:1:";

    public string Id32 { get; private set; } = "";

    public string Id64 { get; private set; } = "";

    private enum SteamIdType
    {
        SteamId,
        SteamId3,
        SteamId32,
        SteamId64
    }

    private readonly string _input;
    private SteamIdType _inputType;

    private static readonly char[] Sid3Strings = { 'U', 'I', 'M', 'G', 'A', 'P', 'C', 'g', 'T', 'L', 'C', 'a' };
    private const long ChangeVal = 76561197960265728;

    public SteamId(string anySteamId)
    {
        _input = anySteamId;
        GetIdType();
        ConvertAll();
    }

    private void GetIdType()
    {
        if (string.IsNullOrEmpty(_input))
        {
            throw new SteamIdConvertException("Input SteamID cannot be null or empty.");
        }

        if (_input[0] == 'S')
        {
            _inputType = SteamIdType.SteamId;
        }
        else if (Sid3Strings.Contains(_input[0]))
        {
            _inputType = SteamIdType.SteamId3;
        }
        else if (char.IsNumber(_input[0]))
        {
            _inputType = _input.Length switch
            {
                < 17 => SteamIdType.SteamId32,
                17 => SteamIdType.SteamId64,
                _ => _inputType
            };
        }
        else
        {
            throw new SteamIdConvertException("Input SteamID was not recognised!");
        }
    }

    private static string GetOddity(string input)
    {
        if (long.TryParse(input, out var val))
        {
            return (val % 2).ToString();
        }

        return "0";
    }

    private static string FloorDivide(string sIn, double divIn)
    {
        if (long.TryParse(sIn, out var val))
        {
            return Math.Floor(val / divIn).ToString(CultureInfo.InvariantCulture);
        }

        return "0";
    }

    private void CalcSteamId()
    {
        if (_inputType == SteamIdType.SteamId)
        {
            Id = _input;
        }
        else
        {
            var s = _inputType switch
            {
                SteamIdType.SteamId3 => _input.Length > 4 ? _input[4..] : "",
                SteamIdType.SteamId32 => _input,
                SteamIdType.SteamId64 => CalcSteamId32(),
                _ => ""
            };

            if (!string.IsNullOrEmpty(s))
            {
                Id += GetOddity(s) + ":" + FloorDivide(s, 2);
            }
        }
    }

    private void CalcSteamId3()
    {
        if (_inputType == SteamIdType.SteamId3)
        {
            Id3 = _input;
        }
        else
        {
            Id3 += CalcSteamId32();
        }

        Id3 = $"[{Id3}]";
    }

    private string CalcSteamId32()
    {
        if (_inputType == SteamIdType.SteamId32)
        {
            Id32 = _input;
        }
        else
        {
            // Safety parsing
            try
            {
                Id32 = _inputType switch
                {
                    SteamIdType.SteamId => (long.Parse(_input[10..]) * 2 + long.Parse($"{_input[8]}")).ToString(),
                    SteamIdType.SteamId3 => _input.Length > 4 ? _input[4..] : "0",
                    SteamIdType.SteamId64 => (long.Parse(_input) - ChangeVal).ToString(),
                    _ => Id32
                };
            }
            catch
            {
                Id32 = "0";
            }
        }

        return Id32;
    }

    private void CalcSteamId64()
    {
        if (_inputType == SteamIdType.SteamId64)
        {
            Id64 = _input;
        }
        else
        {
            try
            {
                Id64 = _inputType switch
                {
                    SteamIdType.SteamId => (long.Parse(_input[10..]) * 2 + long.Parse($"{_input[8]}") + ChangeVal)
                        .ToString(),
                    SteamIdType.SteamId3 => (long.Parse(_input[4..]) + ChangeVal).ToString(),
                    SteamIdType.SteamId32 => (long.Parse(_input) + ChangeVal).ToString(),
                    _ => Id64
                };
            }
            catch
            {
                Id64 = "0";
            }
        }
    }

    public void ConvertAll()
    {
        CalcSteamId();
        CalcSteamId3();
        _ = CalcSteamId32();
        CalcSteamId64();
    }

    private class SteamIdConvertException : Exception
    {
        public SteamIdConvertException(string message) : base(message) { }
    }
}