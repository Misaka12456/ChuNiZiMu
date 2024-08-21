using System.Diagnostics;
using ChuNiZiMu.Models;

namespace ChuNiZiMu;

public static class Program
{
	public static void Main(string[] args)
	{
		if (args.Contains("--help"))
		{
			Console.WriteLine("Chu Ni Zi Mu is a tiny utility to manage the game which to guess the song name by the revealed characters in the song title.\n" +
			                  "Usage: chunizimu [options]\n" +
			                  "\n" +
			                  "Options:" +
			                  "--help		   \tShow this help message and exit.\n" +
			                  "<no options>    \tStart the game session.");
			return;
		}
		if (args.Length > 0)
		{
			Console.Error.WriteLine($"Unknown argument: {args[0]}.\n" +
			                  "Use chunizimu --help to show help message.");
			return;
		}
		GameInit(args);
	}

	private static void GameInit(string[] args)
	{
		ConsolePlus.SetTitle("音游开字母(Chu Ni Zi Mu) - 根据已揭露(Reveal)的字符盲猜音游曲名");
        Console.Clear();
        Console.WriteLine("Welcome to Chu Ni Zi Mu, a tiny utility to manage the game which to guess the song name by the revealed characters in the song title.");
        Console.WriteLine("Do you want to reveal spaces initially? This settings can only be set once before the game session starts. (y/N)");
        bool revealSpacesInitially = (Console.ReadLine() ?? string.Empty).Trim().ToLower() == "y";
        Console.WriteLine("To start the game session, please init the songs pool by input the song name once per line, and input a blank line or EOF to start the game session:");
        var songs = new List<Song>();
		while (true)
		{
			string? songName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(songName) || songName.Trim().Equals("eof", StringComparison.CurrentCultureIgnoreCase))
			{
				if (songs.Count < 2)
				{
					Console.Error.Write("Please at least input 2 songs to start the game session.");
					continue;
				}
				break;
			}
			songs.Add(new Song(songName, revealSpacesInitially));
		}
		
		Console.WriteLine("Show correct answers during every round in the game session (for reference)?\n" +
		                  "This should be set true when and ONLY when just using this tool as a game backend manager, instead of a game player. (Y/n)");
		bool showCorrectAnswers = (Console.ReadLine() ?? string.Empty).Trim().ToLower() != "n";
		
		Console.WriteLine("Please check the following song list for the game session:");
		for (int i = 0; i < songs.Count; i++)
		{
			Console.WriteLine($"[{i + 1}] {songs[i].FullSecretSongTitle}");
		}
		Console.WriteLine("And the initial state of the game session:");
		for (int i = 0; i < songs.Count; i++)
		{
			Console.WriteLine($"[{i + 1}] {new string(songs[i].HiddenSongTitle)}");
		}
		Console.WriteLine($"If check correct, press any key to start the game session. ({songs.Count} songs)");
		Console.ReadKey(true);
		
		GameMain(songs, showCorrectAnswers);
	}

	private static void GameMain(IReadOnlyList<Song> songs, bool showCorrectAnswers)
	{
		bool gameFinished = false;
		var revealedChars = new HashSet<string>();
		var stopwatch = Stopwatch.StartNew();
		for (int round = 1; ; round++)
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
					Console.ForegroundColor = ConsoleColor.Green; // completed
					Console.WriteLine($"[{i + 1}] {song.FullSecretSongTitle}");
					Console.ResetColor();
				}
				else
				{
					Console.WriteLine($"[{i + 1}] {new string(song.HiddenSongTitle)}");
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
			if (option.StartsWith(":d") && option.Split(' ').Length > 1 && uint.TryParse(option.Split(' ')[1], out uint num) && num <= songs.Count)
			{
				var targetSong = songs[(int) num - 1];
				targetSong.RevealAll();
			}
			else if (option == ":q")
			{
				Console.WriteLine("Game quit.");
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
				if (revealResult.All(result => result != RevealResult.Success) && revealResult.Any(result => result == RevealResult.NotInTitle))
				{
					// 没有成功的记录，并且至少有一个报错“不在标题中”
					Console.WriteLine($"The letter {letter} is not in any song title. Any key continue.");
					Console.ReadKey(true);
					continue;
				}
				revealedChars.Add(letter == ' ' ? "<空格>" : letter.ToString());
			}
			#endregion
		}
	}
}