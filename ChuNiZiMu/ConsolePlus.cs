namespace ChuNiZiMu;

public static class ConsolePlus
{
	public static void SetTitle(string title)
	{
		try
		{
			Console.Title = title;
		}
		catch (PlatformNotSupportedException)
		{
			Console.Clear();
			Console.WriteLine(title);
		}
	}
}