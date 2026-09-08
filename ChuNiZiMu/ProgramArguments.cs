using System.ComponentModel;
using Ookii.CommandLine;

namespace ChuNiZiMu;

[GeneratedParser]
[Description("Chu Ni Zi Mu is a tiny utility to manage the game which to guess the song name by the revealed characters in the song title.")]
partial class ProgramArguments
{
    [CommandLineArgument("file", ShortName = 'f', IsLong = true, IsShort = true)]
    [Description("Initialize songs by the given file path.\nOne song per line, and blank lines will be ignored.\nNote: lines with white space characters are not regarded as blank\n")]
    public string? SongFilePath { get; set; }
}