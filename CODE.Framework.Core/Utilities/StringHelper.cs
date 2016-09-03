using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides a number of (static) methods that are useful when working with strings.
    /// Some of these methods have been migrated from the VFPToolkit class written by Kamal Patel.
    /// Special thanks go to Kamal. (www.KamalPatel.com)
    /// </summary>
    public static class StringHelper
    {
        /// <summary>Returns a culture-neutral to-lower operation on the string.</summary>
        /// <param name="originalString">Original string</param>
        /// <returns>Lower-case string</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static string Lower(string originalString)
        {
            return originalString.ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>Returns a culture-neutral to-upper operation on the string.</summary>
        /// <param name="originalString">Original string</param>
        /// <returns>Upper-case string</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static string Upper(string originalString)
        {
            return originalString.ToUpper(CultureInfo.InvariantCulture);
        }

        /// <summary>Returns the string in a culture-neutral fashion</summary>
        /// <param name="value">Value to be turned into a string</param>
        /// <returns>String</returns>
        public static string ToString(object value)
        {
            var formattableValue = value as IFormattable;
            return formattableValue != null ? formattableValue.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        }

        /// <summary>Returns a string representation of the provided value. Returns an empty string if the value is null</summary>
        /// <param name="value">Value to be turned into a string</param>
        /// <returns>String</returns>
        public static string ToStringSafe(object value)
        {
            if (value == null) return string.Empty;
            return ToString(value);
        }

        /// <summary>Returns true if the two strings match.</summary>
        /// <param name="firstString">First string</param>
        /// <param name="secondString">Second string</param>
        /// <returns>True or False</returns>
        /// <remarks>The strings are trimmed and compared in a case-insensitive, culture neutral fashion.</remarks>
        public static bool Compare(string firstString, string secondString)
        {
            var pos = string.Compare(firstString.Trim(), secondString.Trim(), true, CultureInfo.InvariantCulture);
            return pos == 0;
        }

        /// <summary>Returns true if the two strings match.</summary>
        /// <param name="firstString">First string</param>
        /// <param name="secondString">Second string</param>
        /// <param name="ignoreCase">Should case (upper/lower) be ignored?</param>
        /// <returns>True or False</returns>
        /// <remarks>The strings are trimmed and compared in a case-insensitive, culture neutral fashion.</remarks>
        public static bool Compare(string firstString, string secondString, bool ignoreCase)
        {
            var pos = string.Compare(firstString.Trim(), secondString.Trim(), ignoreCase, CultureInfo.InvariantCulture);
            return pos == 0;
        }

        /// <summary>
        /// Receives a string as a parameter and returns the string in Proper format (makes each letter after a space
        /// capital)
        /// </summary>
        /// <example>StringHelper.Proper("joe doe is a good man");	//returns "Joe Doe Is A Good Man"</example>
        /// <param name="originalString">String</param>
        /// <returns>Proper string</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static string Proper(string originalString)
        {
            var sb = new StringBuilder(originalString);

            int counter;
            var length = originalString.Length;

            for (counter = 0; counter < length; counter++)
                //look for a blank space and once found make the next character to uppercase
                if ((counter == 0) || char.IsWhiteSpace(originalString[counter]))
                {
                    //Handle the first character differently
                    int counter2;
                    if (counter == 0)
                        counter2 = counter;
                    else
                        counter2 = counter + 1;

                    //Make the next character uppercase and update the stringBuilder
                    sb.Remove(counter2, 1);
                    sb.Insert(counter2, char.ToUpper(originalString[counter2], CultureInfo.InvariantCulture));
                }
            return sb.ToString();
        }

        /// <summary>
        /// This method returns strings in proper case.
        /// However, contrary to regular Proper() methods,
        /// this method can be used to format names.
        /// For instance, "MacLeod" will remain "MacLeod",
        /// "macLeod" will be "MacLeod", "MACLEOD" will be turned into
        /// "Macleod". "macleod" will also be turned into "Macleod".
        /// </summary>
        /// <param name="originalString">String that is to be formatted</param>
        /// <returns>Properly formatted string</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static string SmartProper(string originalString)
        {
            var chars = originalString.Trim().ToCharArray();

            var sb = new StringBuilder();
            var bLastWasNewWord = true; // Indicated that the last character started a new word
            bool bEncounteredLower = false, bEncounteredUpper = false;
            for (var counter = 0; counter < chars.Length; counter++)
            {
                var dummy = chars[counter].ToString();

                // We figure out whether this was a lower or upper case character
                if (dummy.ToLower(CultureInfo.InvariantCulture) == dummy)
                    bEncounteredLower = true;
                else
                    bEncounteredUpper = true;

                if (bLastWasNewWord)
                    // Ever time we start a new word, the first char is upper case, no matter what.
                    sb.Append(dummy.ToUpper(CultureInfo.InvariantCulture));
                else
                {
                    // We are in the middle of a word. We may have to lower chars, unless the word was in camel case before
                    if (bEncounteredUpper && bEncounteredLower)
                        // We have a camel chase word. We do not change anything
                        sb.Append(dummy);
                    else
                        sb.Append(dummy.ToLower(CultureInfo.InvariantCulture));
                }

                // We check whether the current char starts a new word.
                bLastWasNewWord = dummy == " " || dummy == "-" || dummy == "'" || dummy == "." || dummy == "," || dummy == ";" || dummy == ":";
                if (!bLastWasNewWord) continue;
                bEncounteredLower = false;
                bEncounteredUpper = false;
            }

            return sb.ToString();
        }


        /// <summary>
        /// This method takes a camel-case string (such as one defined by an enum) and returns is with a space before
        /// every upper-case letter.
        /// </summary>
        /// <example>StringHelper.SpaceCamelCase("CamelCaseWord"); // returns"Camel Case Word"</example>
        /// <param name="originalString">String</param>
        /// <returns>String with spaces</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static string SpaceCamelCase(string originalString)
        {
            var chars = originalString.Trim().ToCharArray();
            var sb = new StringBuilder();
            for (var counter = 0; counter < chars.Length; counter++)
            {
                var dummy = chars[counter].ToString();
                if (counter > 0)
                    if (dummy.ToUpper(CultureInfo.InvariantCulture) == dummy)
                        sb.Append(" ");
                sb.Append(dummy);
            }
            return sb.ToString();
        }

        /// <summary>Receives a string and a file name as parameters and writes the contents of the string to that file</summary>
        /// <example>
        /// string text = "This is the line we want to insert in our file.";
        /// StringHelper.ToFile(text, @"c:\My Folders\MyFile.txt");
        /// </example>
        /// :
        /// <param name="expression">String to be written</param>
        /// <param name="fileName">File name the string is to be written to.</param>
        public static void ToFile(string expression, string fileName)
        {
            ToFile(expression, fileName, Encoding.Default);
        }

        /// <summary>Receives a string and a file name as parameters and writes the contents of the string to that file</summary>
        /// <example>
        /// string text = "This is the line we want to insert in our file.";
        /// StringHelper.ToFile(text, "c:\\My Folders\\MyFile.txt");
        /// </example>
        /// <param name="expression">String to be written</param>
        /// <param name="fileName">File name the string is to be written to.</param>
        /// <param name="encoding">File encoding</param>
        public static void ToFile(string expression, string fileName, Encoding encoding)
        {
            //Check if the sepcified file exists
            if (File.Exists(fileName))
                //If so then Erase the file first as in this case we are overwriting
                File.Delete(fileName);

            using (var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(stream, encoding))
            {
                writer.Write(expression);
                writer.Flush();
                writer.Close();
                stream.Close();
            }
        }

        /// <summary>Loads a file from disk and returns it as a string</summary>
        /// <param name="fileName">File to be loaded</param>
        /// <returns>String containing the file contents</returns>
        public static string FromFile(string fileName)
        {
            var reader = File.OpenText(fileName);
            var retVal = reader.ReadToEnd();
            reader.Close();
            return retVal;
        }

        /// <summary>This method takes any regular string, and returns its base64 encoded representation</summary>
        /// <param name="original">Original String</param>
        /// <returns>Base64 encoded string</returns>
        public static string Base64Encode(string original)
        {
            return Convert.ToBase64String(new ASCIIEncoding().GetBytes(original));
        }

        /// <summary>Takes a base64 encoded string and converts it into a regular string</summary>
        /// <param name="encodedString">Base64 encoded string</param>
        /// <returns>Decoded string</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "This is a special case and the 'string' part is not identifying the type.")]
        public static string Base64Decode(string encodedString)
        {
            return new ASCIIEncoding().GetString(Convert.FromBase64String(encodedString));
        }

        /// <summary>
        /// Receives two strings as parameters and searches for one string within another. If found, returns the beginning
        /// numeric position otherwise returns 0
        /// </summary>
        /// <example>StringHelper.At("D", "Joe Doe");	//returns 5</example>
        /// <param name="searchFor">String to search for</param>
        /// <param name="searchIn">String to search in</param>
        /// <returns>Position</returns>
        public static int At(string searchFor, string searchIn)
        {
            return searchIn.IndexOf(searchFor, StringComparison.Ordinal) + 1;
        }

        /// <summary>
        /// Receives two strings and an occurrence position (1st, 2nd etc) as parameters and
        /// searches for one string within another for that position.
        /// If found, returns the beginning numeric position otherwise returns 0
        /// </summary>
        /// <example>
        /// StringHelper.At("o", "Joe Doe", 1);	//returns 2
        /// StringHelper.At("o", "Joe Doe", 2);	//returns 6
        /// </example>
        /// <param name="searchFor">String to search for</param>
        /// <param name="searchIn">String to search in</param>
        /// <param name="occurrence">The occurrence of the string</param>
        /// <returns>Position</returns>
        public static int At(string searchFor, string searchIn, int occurrence)
        {
            int counter;
            var occurred = 0;
            var position = 0;

            //Loop through the string and get the position of the requiref occurrence
            for (counter = 1; counter <= occurrence; counter++)
            {
                position = searchIn.IndexOf(searchFor, position, StringComparison.Ordinal);

                if (position < 0) break;
                //Increment the occurred counter based on the current mode we are in
                occurred++;

                //Check if this is the occurrence we are looking for
                if (occurred == occurrence) return position + 1;
                position++;
            }
            return 0;
        }

        /// <summary>Receives a character as a parameter and returns its ANSI code</summary>
        /// <example>Asc('#'); //returns 35</example>
        /// <param name="character">Character</param>
        /// <returns>ASCII value</returns>
        public static int Asc(char character)
        {
            return character;
        }

        /// <summary>Receives an integer ANSI code and returns a character associated with it</summary>
        /// <example>StringHelper.Chr(35); //returns '#'</example>
        /// <param name="ansiCode">Character Code</param>
        /// <returns>Char that corresponds with the ascii code</returns>
        public static char Chr(int ansiCode)
        {
            return (char) ansiCode;
        }

        /// <summary>Receives a string as a parameter and counts the number of words in that string</summary>
        /// <example>
        /// string lcString = "Joe Doe is a good man";
        /// StringHelper.GetWordCount(lcString); // returns 6
        /// </example>
        /// <param name="sourceString">String</param>
        /// <returns>Word Count</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static long GetWordCount(string sourceString)
        {
            int counter;
            long length = sourceString.Length;
            long wordCount = 0;

            //Begin by checking for the first word
            if (!char.IsWhiteSpace(sourceString[0])) wordCount++;

            //Now look for white spaces and count each word
            for (counter = 0; counter < length; counter++)
                //Check for a space to begin counting a word
                if (char.IsWhiteSpace(sourceString[counter]))
                    //We think we encountered a word
                    //Remove any following white spaces if any after this word
                    do
                    {
                        //Check if we have reached the limit and if so then exit the loop
                        counter++;
                        if (counter >= length) break;
                        if (!char.IsWhiteSpace(sourceString[counter]))
                        {
                            wordCount++;
                            break;
                        }
                    } while (true);
            return wordCount;
        }


        /// <summary>
        /// Based on the position specified, returns a word from a string. Receives a string as a parameter and counts the
        /// number of words in that string.
        /// </summary>
        /// <example>
        /// string lcString = "Joe Doe is a good man";
        /// StringHelper.GetWordNumber(lcString, 5); // returns "good"
        /// </example>
        /// <param name="sourceString">String</param>
        /// <param name="wordPosition">Word Position</param>
        /// <returns>Word number</returns>
        [Browsable(false)]
        public static string GetWordNumb(string sourceString, int wordPosition)
        {
            if (wordPosition < 1) return string.Empty;
            var words = sourceString.Split(' ');
            return wordPosition <= words.Length ? words[wordPosition - 1] : string.Empty;
        }

        /// <summary>
        /// Based on the position specified, returns a word from a string. Receives a string as a parameter and counts the
        /// number of words in that string.
        /// </summary>
        /// <example>
        /// string lcString = "Joe Doe is a good man";
        /// StringHelper.GetWordNumber(lcString, 5); // returns "good"
        /// </example>
        /// <param name="sourceString">String</param>
        /// <param name="wordPosition">Word Position</param>
        /// <returns>Word number</returns>
        public static string GetWordNumber(string sourceString, int wordPosition)
        {
            return GetWordNumb(sourceString, wordPosition);
        }

        /// <summary>Returns a bool indicating if the first character in a string is an alphabet or not</summary>
        /// <example>StringHelper.IsAlpha("Joe Doe"); // returns true</example>
        /// <param name="expression">Expression</param>
        /// <returns>True or False depending on whether the string only had alphanumeric chars</returns>
        public static bool IsAlpha(string expression)
        {
            //Check if the first character is a letter
            return char.IsLetter(expression[0]);
        }

        /// <summary>Returns the number of occurrences of a character within a string</summary>
        /// <example>StringHelper.Occurs('o', "Joe Doe"); // returns 2</example>
        /// <param name="character">Search Character</param>
        /// <param name="expression">Expression</param>
        /// <returns>Number of occurrences</returns>
        public static int Occurs(char character, string expression)
        {
            int counter, occurred = 0;

            //Loop through the string
            for (counter = 0; counter < expression.Length; counter++)
                //Check if each expression is equal to the one we want to check against
                if (expression[counter] == character)
                    //if  so increment the counter
                    occurred++;
            return occurred;
        }

        /// <summary>Returns the number of occurrences of one string within another string</summary>
        /// <example>
        /// StringHelper.Occurs("oe", "Joe Doe"); //returns 2
        /// StringHelper.Occurs("Joe", "Joe Doe"); //returns 1
        /// </example>
        /// <param name="searchString">Search String</param>
        /// <param name="stringSearched">Expression</param>
        /// <returns>Number of occurrences</returns>
        public static int Occurs(string searchString, string stringSearched)
        {
            var position = 0;
            var occurred = 0;
            do
            {
                //Look for the search string in the expression
                position = stringSearched.IndexOf(searchString, position, StringComparison.Ordinal);

                if (position < 0) break;
                //Increment the occurred counter based on the current mode we are in
                occurred++;
                position++;
            } while (true);

            //Return the number of occurrences
            return occurred;
        }

        /// <summary>
        /// Receives a string expression and a numeric value indicating number of time and replicates that string for the
        /// specified number of times.
        /// </summary>
        /// <example>StringHelper.Replicate("Joe", 5); // returns JoeJoeJoeJoeJoe</example>
        /// <param name="expression">Expression</param>
        /// <param name="times">Number of times the string is to be replicated</param>
        /// <returns>New string</returns>
        public static string Replicate(string expression, int times)
        {
            var sb = new StringBuilder();
            sb.Insert(0, expression, times);
            return sb.ToString();
        }

        /// <summary>Overloaded method for SubStr() that receives starting position and length</summary>
        /// <param name="expression">String expression</param>
        /// <param name="startIndex">Start Index</param>
        /// <param name="length">Length</param>
        /// <returns>Substring</returns>
        public static string SubstringSafe(string expression, int startIndex, int length)
        {
            return SubStr(expression, startIndex, length);
        }

        /// <summary>Overloaded method for SubStr() that receives starting position and length</summary>
        /// <param name="expression">Expression</param>
        /// <param name="startPosition">Start Position</param>
        /// <param name="length">Length</param>
        /// <returns>Substring</returns>
        public static string SubStr(string expression, int startPosition, int length)
        {
            if (startPosition >= expression.Length) return string.Empty;
            return length + startPosition - 1 > expression.Length ? expression.Substring(startPosition - 1) : expression.Substring(startPosition - 1, length);
        }

        /// <summary>Receives a string and converts it to an integer</summary>
        /// <example>StringHelper.AtLine("Is", "Is Life Beautiful? \r\n It sure is"); // returns 1</example>
        /// <param name="searchExpression">Search Expression</param>
        /// <param name="expressionSearched">Expression Searched</param>
        /// <returns>Line number</returns>
        public static int AtLine(string searchExpression, string expressionSearched)
        {
            var counter = 0;
            var position = At(searchExpression, expressionSearched);
            if (position > 0 && position < expressionSearched.Length)
            {
                var text = SubStr(expressionSearched, 1, position - 1);
                counter = Occurs(@"\r", text) + 1;
            }
            return counter;
        }

        /// <summary>
        ///     Receives a string as a parameter and returns a bool indicating if the left most character in the string is a
        ///     valid digit.
        /// </summary>
        /// <example>if(StringHelper.IsDigit("1Kamal")){...}	//returns true</example>
        /// <param name="sourceString">Expression</param>
        /// <returns>True or False</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        public static bool IsDigit(string sourceString)
        {
            //get the first character in the string
            var chr = sourceString[0];
            return char.IsDigit(chr);
        }

        /// <summary>Takes a fully qualified file name, and returns just the path</summary>
        /// <param name="path">File name with path</param>
        /// <returns>Just the path as a string</returns>
        public static string JustPath(string path)
        {
            return path.Substring(0, At("\\", path, Occurs("\\", path)) - 1);
        }

        /// <summary>Returns just the file name part of a full path</summary>
        /// <param name="path">The full path to the file</param>
        /// <returns>File name</returns>
        public static string JustFileName(string path)
        {
            var parts = path.Split('\\');
            if (parts.Length > 0)
                return parts[parts.Length - 1];
            return string.Empty;
        }

        /// <summary>Makes sure the specified path ends with a back-slash</summary>
        /// <param name="path">Path</param>
        /// <returns>Path with BS</returns>
        public static string AddBS(string path)
        {
            if (!path.EndsWith("\\"))
                path += "\\";
            return path;
        }

        /// <summary>Returns true if the array contains the string we are looking for</summary>
        /// <param name="hostArray">The host array.</param>
        /// <param name="searchText">The search string.</param>
        /// <returns>True or false</returns>
        /// <example>
        /// string[] testArray = new string[] { "One", "Two", "Three" };
        /// bool result1 = StringHelper.ArrayContainsString(testArray, "one", true); // returns true
        /// bool result2 = StringHelper.ArrayContainsString(testArray, "one"); // returns false
        /// bool result3 = StringHelper.ArrayContainsString(testArray, "One"); // returns true
        /// bool result4 = StringHelper.ArrayContainsString(testArray, "Four"); // returns false
        /// </example>
        public static bool ArrayContainsString(string[] hostArray, string searchText)
        {
            return ArrayContainsString(hostArray, searchText, false);
        }

        /// <summary>Returns true if the array contains the string we are looking for</summary>
        /// <param name="hostArray">The host array.</param>
        /// <param name="searchText">The search string.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>True or false</returns>
        /// <example>
        /// string[] testArray = new string[] { "One", "Two", "Three" };
        /// bool result1 = StringHelper.ArrayContainsString(testArray, "one", true); // returns true
        /// bool result2 = StringHelper.ArrayContainsString(testArray, "one"); // returns false
        /// bool result3 = StringHelper.ArrayContainsString(testArray, "One"); // returns true
        /// bool result4 = StringHelper.ArrayContainsString(testArray, "Four"); // returns false
        /// </example>
        public static bool ArrayContainsString(string[] hostArray, string searchText, bool ignoreCase)
        {
            return hostArray.Any(item => Compare(item, searchText, ignoreCase));
        }

        /// <summary>Tries to parse a string value as an integer. If the parse fails, the provided default value will be inserted</summary>
        /// <param name="value">The value.</param>
        /// <param name="failedDefault">The failed default.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "1";
        /// int valueInt = StringHelper.TryIntParse(value, -1);
        /// </example>
        public static int TryIntParse(string value, int failedDefault)
        {
            int parsedValue;
            return int.TryParse(value, out parsedValue) ? parsedValue : failedDefault;
        }

        /// <summary>Tries to parse a string value as an Guid. If the parse fails, the provided default value will be inserted</summary>
        /// <param name="value">The value.</param>
        /// <param name="failedDefault">The failed default.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "xxx";
        /// Guid valueGuid = StringHelper.TryGuidParse(value, Guid.Empty);
        /// </example>
        public static Guid TryGuidParse(string value, Guid failedDefault)
        {
            try
            {
                return new Guid(value);
            }
            catch
            {
                return failedDefault;
            }
        }

        /// <summary>Tries to parse a string value as an Guid. If the parse fails, Guid.Empty will be returned</summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "xxx";
        /// Guid valueGuid = StringHelper.TryGuidParse(value);
        /// </example>
        public static Guid TryGuidParse(string value)
        {
            return TryGuidParse(value, Guid.Empty);
        }

        /// <summary>Tries to parse a string value as a boolean.</summary>
        /// <param name="value">The value.</param>
        /// <param name="failedDefault">Value returned if the string cannot be converted to a boolean.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "xxx";
        /// bool valueBool = StringHelper.TryBoolParse(value);
        /// </example>
        public static bool TryBoolParse(string value, bool failedDefault = false)
        {
            bool parsedValue;
            return bool.TryParse(value, out parsedValue) ? parsedValue : failedDefault;
        }
    }
}