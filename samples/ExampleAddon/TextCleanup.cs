using System.Text;

namespace ExampleAddon;

internal static class TextCleanup
{
    /// <summary>
    /// Removes emoji and other characters outside the Basic Multilingual Plane. The game's fonts can't draw
    /// them, and AI text often has some.
    /// </summary>
    public static string StripEmoji(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsSurrogate(c) || c is '️' or '‍')
                continue;

            builder.Append(c);
        }

        return builder.ToString();
    }
}
