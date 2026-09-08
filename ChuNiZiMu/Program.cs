using System.Diagnostics;
using System.Text.RegularExpressions;
using ChuNiZiMu.Models;
using Ookii.CommandLine;

namespace ChuNiZiMu;

public static class Program
{
    private static List<Song>? _songs = null;

    public static void Main(string[] args)
    {
        var parser = ProgramArguments.CreateParser(new ParseOptions()
        {
            Mode = ParsingMode.LongShort,
            ArgumentNameTransform = NameTransform.DashCase
        });
        ProgramArguments? arguments = parser.ParseWithErrorHandling(args);
        if (parser.ParseResult.Status != ParseStatus.Success || parser.ParseResult.HelpRequested)
        {
            return;
        }

        GameInit(arguments);
    }

    private static bool HandleSongFile(string path, bool revealSpacesInitially)
    {
        bool successful = TryReadSongsFromFile(path, revealSpacesInitially);
        if (successful)
        {
            if (!CheckSongsEnough())
            {
                HintSongsNotEnough();
                successful = false;
            }
        }

        if (!successful)
        {
            if (!RuntimeFlagRequest(
                    "Error detected while reading song file, ignore it and continue with manually input?", false))
            {
                return false;
            }

            _songs = null;
        }
        else
        {
            Console.WriteLine($"Song file detected, imported {_songs!.Count} songs.");
        }

        return true;
    }

    private static bool TryReadSongsFromFile(string path, bool revealSpacesInitially)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: Cannot read song file.");
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
            return false;
        }

        _songs = new List<Song>();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;
            _songs.Add(new Song(line, revealSpacesInitially));
        }

        return true;
    }

    private static void HandleManualInputSongs(bool revealSpacesInitially)
    {
        Console.WriteLine(
            "To start the game session, please init the songs pool by input the song name once per line, and input a blank line to start the game session:");
        _songs = new List<Song>();
        while (true)
        {
            string? songName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(songName))
            {
                if (!CheckSongsEnough())
                {
                    HintSongsNotEnough();
                    continue;
                }

                break;
            }

            _songs.Add(new Song(songName, revealSpacesInitially));
        }
    }

    private static bool CheckSongsEnough() => _songs is { Count: >= 2 };

    private static void HintSongsNotEnough() =>
        Console.Error.WriteLine("Please at least input 2 songs to start the game session.");

    private static bool RuntimeFlagRequest(string description, bool defaultFlag)
    {
        Console.Write(description);
        if (defaultFlag)
        {
            Console.WriteLine(" (Y/n)");
            return !(Console.ReadLine() ?? string.Empty).Trim().Equals("n", StringComparison.CurrentCultureIgnoreCase);
        }

        Console.WriteLine(" (y/N)");
        return (Console.ReadLine() ?? string.Empty).Trim().Equals("y", StringComparison.CurrentCultureIgnoreCase);
    }

    private static void GameInit(ProgramArguments? args)
    {
        ConsolePlus.SetTitle("音游开字母(Chu Ni Zi Mu) - 根据已揭露(Reveal)的字符盲猜音游曲名");
        Console.Clear();
        Console.ResetColor();
        Console.WriteLine(
            "Welcome to Chu Ni Zi Mu, a tiny utility to manage the game which to guess the song name by the revealed characters in the song title.");
        bool revealSpacesInitially = RuntimeFlagRequest(
            "Do you want to reveal spaces initially? This settings can only be set once before the game session starts.",
            false);

        if (args?.SongFilePath != null)
        {
            if (!HandleSongFile(args.SongFilePath, revealSpacesInitially))
            {
                return;
            }
        }

        if (_songs == null)
        {
            HandleManualInputSongs(revealSpacesInitially);
        }

        bool showCorrectAnswers = RuntimeFlagRequest(
            "Display correct answers following the puzzle during every round in the game session (for reference)?\n" +
            "This should be set true when and ONLY when just using this tool as a game backend manager, instead of a game player.",
            true);

        bool preserveAnyRevealedLetter = RuntimeFlagRequest(
            "Always display the revealed letter EVEN THOUGH the letter doesn't exist in any song title?\n" +
            "By enabling this feature, the revealed letter list will act better as a hint list, which was widely used in the real game chat before.",
            true);

        bool bonusSetFlag =
            RuntimeFlagRequest(
                "Is there any Bonus track set? Bonus tracks will be highlighted in a special color.",
                false);

        if (bonusSetFlag)
        {
            while (true)
            {
                Console.WriteLine("Please enter the Bonus track number (start with 1) and input a blank line or EOF to finish");
                string? bonusString = Console.ReadLine();
                if (string.IsNullOrEmpty(bonusString))
                {
                    Console.WriteLine(
                        "If you want to confirm that you don't set the bonus, please enter the enter again to confirm.");
                    string? confirmString = Console.ReadLine();
                    if (string.IsNullOrEmpty(confirmString))
                    {
                        break;
                    }
                }
                else
                {
                    Regex regex = new Regex("[^0-9]+$");
                    if (regex.IsMatch(bonusString))
                    {
                        Console.WriteLine(
                            "Error: You entered a character other than a number, please press enter key to re-enter");
                        continue;
                    }
                    else
                    {
                        int[] bonusList = Array.ConvertAll(bonusString.Split(" "), int.Parse);
                        foreach (int bonusIndex in bonusList)
                        {
                            if (bonusIndex > _songs.Count)
                            {
                                continue;
                            }

                            _songs[bonusIndex - 1].IsBonusSong = true;
                        }
                    }

                    break;
                }
            }
        }


        Console.WriteLine("Please check the following song list for the game session:");
        for (int i = 0; i < _songs.Count; i++)
        {
            Console.WriteLine($"[{i + 1}] {_songs[i].FullSecretSongTitle}");
        }

        Console.WriteLine("And the initial state of the game session:");
        for (int i = 0; i < _songs.Count; i++)
        {
            Console.WriteLine($"[{i + 1}] {new string(_songs[i].HiddenSongTitle)}");
        }

        Console.WriteLine($"If check correct, press any key to start the game session. ({_songs.Count} songs)");
        Console.ReadKey(true);

        GameMain(_songs, revealSpacesInitially, showCorrectAnswers, preserveAnyRevealedLetter);
    }

    private static void GameMain(IReadOnlyList<Song> songs, bool revealSpacesInitially, bool showCorrectAnswers,
        bool preserveAnyRevealedLetter)
    {
        bool gameFinished = false;
        var revealedChars = new HashSet<string>();
        if (revealSpacesInitially) revealedChars.Add("<空格>");
        var stopwatch = Stopwatch.StartNew();
        for (int round = 1;; round++)
        {
            Console.ResetColor();
            Console.Clear();
            ConsolePlus.SetTitle($"Chu Ni Zi Mu - Round {(gameFinished ? "Final" : round)}");

            #region Game Main Songs Panel

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"已开：{string.Join(' ', revealedChars)}");
            Console.ResetColor();
            Console.WriteLine();
            for (int i = 0; i < songs.Count; i++)
            {
                var song = songs[i];
                if (song.ToString() == song.FullSecretSongTitle)
                {
                    Console.ForegroundColor =
                        song.IsBonusSong
                            ? ConsoleColor.Magenta
                            : ConsoleColor.Green; // check ? bonus completed : normal completed

                    Console.WriteLine($"[{i + 1}] {song.FullSecretSongTitle}");
                    Console.ResetColor();
                }
                else
                {
                    if (song.IsNonASCIICharacters)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else if (song.IsBonusSong)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }

                    Console.Write($"[{i + 1}] ");
                    Console.ResetColor();
                    Console.WriteLine($"{new string(song.HiddenSongTitle)}");
                    //Console.WriteLine($"[{i + 1}] {new string(song.HiddenSongTitle)}");
                }
            }

            #endregion

            #region Game Finish logics

            if (gameFinished)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                round--;
                stopwatch.Stop();
                Console.WriteLine("Game result statistics:\n" +
                                  $"Total rounds: {round}\n" +
                                  $"Total used time: {stopwatch.Elapsed:g}");
                Console.WriteLine("Press any key to quit.");
                Console.ReadKey(true);
                Console.ResetColor();
                return;
            }

            #endregion

            #region Correct Answer Show logic

            if (showCorrectAnswers)
            {
                Console.WriteLine();
                Console.WriteLine("Correct answers (for reference):");
                for (int i = 0; i < songs.Count; i++)
                {
                    Console.WriteLine($"[{i + 1}] {songs[i].FullSecretSongTitle}");
                }
            }

            #endregion

            #region Game Menu and Input logics

            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("<single char> - reveal, :d <num> - directly complete a song, :q - quit");
            Console.Write("Input: ");

            string option = Console.ReadLine() ?? string.Empty;
            option = option.ToLower(); // 注意这里千万不能直接Trim，因为Trim会把空格也去掉，而有可能玩家此时目的就是开<空格>这个字符
            if (option.StartsWith(":d") && option.Split(' ').Length > 1 &&
                uint.TryParse(option.Split(' ')[1], out uint num) && num <= songs.Count)
            {
                var targetSong = songs[(int)num - 1];
                targetSong.RevealAll();
            }
            else if (option == ":q")
            {
                Console.WriteLine("Game quit.");
                Console.ResetColor();
                return;
            }
            else if (!string.IsNullOrEmpty(option))
            {
                if (option.Length > 1)
                {
                    Console.WriteLine("Only single char is allowed. Any key continue.");
                    Console.ReadKey(true);
                    continue;
                }

                char letter = option[0];
                var revealResult = songs.Select(song => song.RevealLetter(letter)).ToList();
                if (revealResult.All(result => result == RevealResult.AlreadyCompleted))
                {
                    gameFinished = true;
                    continue; // directly continue to show the game final result
                }

                string shownLetter = letter == ' ' ? "<空格>" : letter.ToString();
                if (revealedChars.Contains(letter.ToString()) || (letter == ' ' && revealedChars.Contains("<空格>")))
                {
                    Console.WriteLine($"The letter {shownLetter} has already been revealed. Any key continue.");
                    Console.ReadKey(true);
                    continue;
                }

                if (revealResult.All(result => result != RevealResult.Success) &&
                    revealResult.Any(result => result == RevealResult.NotInTitle))
                {
                    // 没有成功的记录，并且至少有一个报错“不在标题中”
                    Console.WriteLine($"The letter {shownLetter} is not in any song title. Any key continue.");
                    Console.ReadKey(true);
                    if (preserveAnyRevealedLetter) revealedChars.Add(shownLetter);
                    continue;
                }

                revealedChars.Add(shownLetter);
            }

            #endregion
        }
    }
}