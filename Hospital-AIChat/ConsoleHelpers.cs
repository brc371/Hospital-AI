namespace Hospital_AIChat
{
    /// <summary>
    /// Utility methods for writing colored text to the console.
    /// </summary>
    public static class ConsoleHelpers
    {
        /// <summary>
        /// Writes a line of text to the console using the specified foreground color,
        /// then restores the original color.
        /// </summary>
        /// <param name="consoleColor">The foreground color to use for the output.</param>
        /// <param name="textToOutput">The text to write to the console.</param>
        public static void ConsoleWriteLine(ConsoleColor consoleColor, string textToOutput)
        {
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(textToOutput);
            Console.ForegroundColor = original;
        }

        /// <summary>
        /// Writes text to the console (without a newline) using the specified foreground color,
        /// then restores the original color.
        /// </summary>
        /// <param name="consoleColor">The foreground color to use for the output.</param>
        /// <param name="textToOutput">The text to write to the console.</param>
        public static void ConsoleWrite(ConsoleColor consoleColor, string textToOutput)
        {
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.Write(textToOutput);
            Console.ForegroundColor = original;
        }
    }
}
