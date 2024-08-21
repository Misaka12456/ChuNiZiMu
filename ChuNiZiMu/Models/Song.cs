namespace ChuNiZiMu.Models;

/// <summary>
/// 表示音游开字母(出你字母/Chu Ni Zi Mu)游戏中一首曲目的信息。
/// </summary>
public class Song
{
	/// <summary>
	/// 该曲目的完整曲名。
	/// </summary>
	public string FullSecretSongTitle { get; init; }
	
	/// <summary>
	/// 该曲目当前已揭露的曲名。(初始状态下为全问号；如果设置了<c>revealSpacesInitially = true</c>，则初始状态下除空格外的字符均为问号)
	/// </summary>
	public char[] HiddenSongTitle { get; private set; }
	
	/// <summary>
	/// 该曲目当前已揭露的字符集合(仅存储曲目标题中存在的字符。如果<c>revealSpacesInitially = true</c>，则该集合初始化时即包含一个元素：空格)<br />
	/// 如果指定猜的字母不在曲目标题中，则会直接丢弃请求，也不会写入该集合中。
	/// </summary>
	public HashSet<char> RevealedCharacters { get; private set; } = [];

	public Song(string title, bool revealSpacesInitially = false)
	{
		FullSecretSongTitle = title;
		HiddenSongTitle = new char[title.Length];
		if (revealSpacesInitially)
		{
			for (int i = 0; i < title.Length; i++)
			{
				if (title[i] == ' ')
				{
					HiddenSongTitle[i] = ' '; // just reveal spaces when initializing
				}
				else
				{
					HiddenSongTitle[i] = '?';
				}
			}
		}
		else
		{
			Array.Fill(HiddenSongTitle, '?');
		}
	}
	
	/// <summary>
	/// 揭露该曲目的一个字母。
	/// </summary>
	/// <param name="letter">要揭露的字母。可以是特殊字符，也可以是UTF-8编码下的非ASCII字符。</param>
	/// <returns>该字母的揭露结果。</returns>
	public RevealResult RevealLetter(char letter)
	{
		if (new string(RevealedCharacters.ToArray()).Equals(FullSecretSongTitle, StringComparison.OrdinalIgnoreCase)) // 忽略大小写，只要字母&特殊字符一样即可，无需必须大小写严格一致
		{
			return RevealResult.AlreadyCompleted; // 此曲目已经完全揭露
		}
		
		letter = letter.ToString().ToLower()[0]; // 忽略大小写

		if (RevealedCharacters.Contains(letter))
		{
			return RevealResult.AlreadyRevealed; // 该字母已经揭露过
		}
		
		string lowerSongTitle = FullSecretSongTitle.ToLower();
		
		if (!lowerSongTitle.Contains(letter))
		{
			return RevealResult.NotInTitle; // 不在曲目标题中
		}
		RevealedCharacters.Add(letter);

		for (int i = 0; i < lowerSongTitle.Length; i++)
		{
			// 将曲目标题中与指定字母相同的字符揭露出来
			// 揭露出来的永远是小写字母
			if (lowerSongTitle[i] == letter)
			{
				HiddenSongTitle[i] = letter;
			}
		}
		
		if (!HiddenSongTitle.Contains('?')) // 如果没有问号了，说明已经完全揭露，此时将“揭露结果”显示为实际大小写的完整曲名（而不是全小写的）
		{
			HiddenSongTitle = FullSecretSongTitle.ToCharArray();
		}
		
		return RevealResult.Success;
	}
	
	public void RevealAll()
	{
		HiddenSongTitle = FullSecretSongTitle.ToCharArray();
	}

	public override string ToString()
	{
		if (!HiddenSongTitle.Contains('?'))
		{
			HiddenSongTitle = FullSecretSongTitle.ToCharArray();
		}
		return new string(HiddenSongTitle);
	}
}