namespace ChuNiZiMu.Models;

public enum RevealResult
{
	NotInTitle = 0, // 指定字符不在曲目标题中
	Success = 1, // 成功揭露标题中出现的全部同一字符
	AlreadyRevealed = 2, // 该字符已经被揭露过
	AlreadyCompleted = 3 // 该曲目已经被完全揭露过
}