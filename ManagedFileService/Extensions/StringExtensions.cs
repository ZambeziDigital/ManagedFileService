using System.Text;

namespace Shared.Extensions;


/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
public static class StringExtensions
{
    // Get the system-defined invalid characters + explicitly add '/'
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
                                                                .Union(new[] { '/' }) // Ensure '/' is always treated as invalid
                                                                .ToArray();

    /// <summary>
    /// Replaces characters in a string that are not valid for file names
    /// with a specified replacement character. Uses Path.GetInvalidFileNameChars()
    /// and explicitly includes the forward slash '/'.
    /// </summary>
    /// <param name="fileName">The original file name string.</param>
    /// <param name="replacementChar">The character to use for replacing invalid characters. Defaults to underscore '_'.</param>
    /// <returns>A sanitized string suitable for use as a file name, or the original string if null or empty.</returns>
    /// <remarks>
    /// This method checks against Path.GetInvalidFileNameChars() for OS-specific invalid chars
    /// and always treats '/' as invalid. Consider additional sanitization (e.g., length limits) if needed.
    /// </remarks>
    public static string ReplaceInvalidFileNameChars(this string fileName, char replacementChar = '_')
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return fileName;
        }

        // Use StringBuilder for efficient string modification
        StringBuilder sanitizedName = new StringBuilder(fileName.Length);
        bool charReplaced = false;

        foreach (char c in fileName)
        {
            if (_invalidFileNameChars.Contains(c))
            {
                sanitizedName.Append(replacementChar);
                charReplaced = true;
            }
            else
            {
                sanitizedName.Append(c);
            }
        }

        // Optional: If you want to ensure the result is never empty if the original wasn't
        if (sanitizedName.Length == 0 && fileName.Length > 0 && charReplaced)
        {
             // This happens if the original filename *only* contained invalid chars
             return replacementChar.ToString(); // Or return a default name like "file"
        }


        // Alternative using Split/Join (often concise, performance usually fine):
        // string[] parts = fileName.Split(_invalidFileNameChars, StringSplitOptions.RemoveEmptyEntries);
        // return string.Join(replacementChar.ToString(), parts);
        // Note: Split/Join might behave differently with leading/trailing invalid chars
        // or consecutive invalid chars compared to the StringBuilder approach,
        // depending on StringSplitOptions. The StringBuilder approach gives more control.


        return sanitizedName.ToString();
    }

     /// <summary>
    /// Checks if a character array contains any invalid file name characters.
    /// </summary>
    /// <param name="chars">The characters to check.</param>
    /// <returns>True if any characters are invalid, false otherwise.</returns>
    public static bool ContainsInvalidFileNameChars(this char[] chars)
    {
        return chars.Any(c => _invalidFileNameChars.Contains(c));
    }
}