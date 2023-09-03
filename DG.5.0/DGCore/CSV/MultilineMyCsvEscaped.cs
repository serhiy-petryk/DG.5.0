// From http://www.blackbeltcoder.com/Articles/files/reading-and-writing-csv-files-in-c
// Based on https://www.codeproject.com/Articles/415732/Reading-and-Writing-CSV-Files-in-Csharp
/* Usage:
		  var columns = new List<string>();
		  var cnt = 0;
      var culture = CultureInfo.GetCultureInfo("de-DE");
		  const int BufferSize = 4 * 1024;
		  //using (var fileStream = File.OpenRead(path))
		    //using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
		    // using (var reader = new CsvFileReader(fileStream, Encoding.UTF8, true, BufferSize))
		  // using (var reader = new MultilineMyCsvFileReader(fileStream, Encoding.GetEncoding(culture.TextInfo.ANSICodePage), true, BufferSize))
		  using (var reader = new MultilineMyCsvEscapedFileReader(path))
		    while (reader.ReadRow(columns))
		    {
		      foreach (var s in columns)
		        var s1 = s;
		      cnt++;
		    }
		  }
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DGCore.CSV
{
    /// <summary>
    /// Determines how empty lines are interpreted when reading CSV files.
    /// These values do not affect empty lines that occur within quoted fields
    /// or empty lines that appear at the end of the input file.
    /// </summary>
    public enum EmptyLineBehavior
    {
        /// <summary>
        /// Empty lines are interpreted as a line with zero columns.
        /// </summary>
        NoColumns,

        /// <summary>
        /// Empty lines are interpreted as a line with a single empty column.
        /// </summary>
        EmptyColumn,

        /// <summary>
        /// Empty lines are skipped over as though they did not exist.
        /// </summary>
        Ignore,

        /// <summary>
        /// An empty line is interpreted as the end of the input file.
        /// </summary>
        EndOfFile,
    }

    /// <summary>
    /// Common base class for CSV reader and writer classes.
    /// </summary>
    public abstract class MultilineMyCsvEscapedFileCommon
    {
        /// <summary>
        /// These are special characters in CSV files. If a column contains any
        /// of these characters, the entire column is wrapped in double quotes.
        /// </summary>
        protected char[] SpecialChars = new char[] { ',', '"', '\r', '\n' };

        // Indexes into SpecialChars for characters with specific meaning
        private const int DelimiterIndex = 0;

        private const int QuoteIndex = 1;

        /// <summary>
        /// Gets/sets the character used for column delimiters.
        /// </summary>
        public char Delimiter
        {
            get { return SpecialChars[DelimiterIndex]; }
            set { SpecialChars[DelimiterIndex] = value; }
        }

        /// <summary>
        /// Gets/sets the character used for column quotes.
        /// </summary>
        public char Quote
        {
            get { return SpecialChars[QuoteIndex]; }
            set { SpecialChars[QuoteIndex] = value; }
        }
    }

    /// <summary>
    /// Class for reading from comma-separated-value (CSV) files
    /// </summary>
    public class MultilineMyCsvEscapedFileReader : MultilineMyCsvEscapedFileCommon, IDisposable
    {
        private const bool _emptyStringIsNull = true;
        // Private members
        private StreamReader Reader;

        private string CurrLine;
        private int CurrLineLength;
        private int CurrPos;
        private EmptyLineBehavior EmptyLineBehavior;

        private char MyDelimiter;
        private char MyQuote;
        private int columnsCount;

        public MultilineMyCsvEscapedFileReader(StreamReader stream, EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.Ignore)
        {
            Reader = stream;
            EmptyLineBehavior = emptyLineBehavior;
            MyDelimiter = Delimiter;
            MyQuote = Quote;
        }
        /// <summary>
        /// Initializes a new instance of the MultilineMyCsvEscapedFileReader class for the
        /// specified stream.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="emptyLineBehavior">Determines how empty lines are handled</param>
        /*public MultilineMyCsvEscapedFileReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks,
          int bufferSize, EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.Ignore)
        {
          Reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize);
          EmptyLineBehavior = emptyLineBehavior;
          MyDelimiter = Delimiter;
          MyQuote = Quote;
        }*/
        public MultilineMyCsvEscapedFileReader(Stream stream, EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.Ignore)
        {
            Reader = new StreamReader(stream);
            EmptyLineBehavior = emptyLineBehavior;
            MyDelimiter = Delimiter;
            MyQuote = Quote;
        }

        /// <summary>
        /// Initializes a new instance of the MultilineMyCsvEscapedFileReader class for the
        /// specified file path.
        /// </summary>
        /// <param name="path">The name of the CSV file to read from</param>
        /// <param name="emptyLineBehavior">Determines how empty lines are handled</param>
        public MultilineMyCsvEscapedFileReader(string path, EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.Ignore)
        {
            Reader = new StreamReader(path);
            EmptyLineBehavior = emptyLineBehavior;
            MyDelimiter = Delimiter;
            MyQuote = Quote;
        }
        public MultilineMyCsvEscapedFileReader(string path, CultureInfo cultureInfo, EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.Ignore)
        {
            Reader = new StreamReader(path, Encoding.GetEncoding(cultureInfo.TextInfo.ANSICodePage));
            EmptyLineBehavior = emptyLineBehavior;
            MyDelimiter = Delimiter;
            MyQuote = Quote;
        }

        /// <summary>
        /// Reads a row of columns from the current CSV file. Returns false if no
        /// more data could be read because the end of the file was reached.
        /// </summary>
        /// <param name="columns">Collection to hold the columns read</param>
        public bool ReadRow(List<string> columns)
        {
            // Verify required argument
            if (columns == null)
                throw new ArgumentNullException("columns");

            columnsCount = columns.Count;

        ReadNextLine:
            // Read next line from the file
            CurrLine = Reader.ReadLine();
            // Test for end of file
            if (CurrLine == null)
                return false;

            CurrPos = 0;
            CurrLineLength = CurrLine.Length;
            // Test for empty line
            if (CurrLineLength == 0)
            {
                switch (EmptyLineBehavior)
                {
                    case EmptyLineBehavior.NoColumns:
                        columns.Clear();
                        columnsCount = 0;
                        return true;
                    case EmptyLineBehavior.Ignore:
                        goto ReadNextLine;
                    case EmptyLineBehavior.EndOfFile:
                        return false;
                }
            }

            // Parse line
            string column;
            int numColumns = 0;
            while (true)
            {
                // Read next column
                if (CurrPos < CurrLineLength && CurrLine[CurrPos] == MyQuote)
                    column = ReadQuotedColumn();
                else
                    column = ReadUnquotedColumn();
                
                // Add column to list
                if (string.IsNullOrEmpty(column))
                    column = null;
                if (numColumns < columnsCount)
                    columns[numColumns] = column;
                else
                {
                    columns.Add(column);
                    columnsCount++;
                }
                numColumns++;
                // Break if we reached the end of the line
                if (CurrLine == null || CurrPos == CurrLineLength)
                    break;
                // Otherwise skip delimiter
                //Debug.Assert(CurrLine[CurrPos] == Delimiter);
                CurrPos++;
            }
            // Remove any unused columns from collection
            if (numColumns < columnsCount)
                columns.RemoveRange(numColumns, columnsCount - numColumns);
            columnsCount = columns.Count;
            // Indicate success
            return true;
        }

        /// <summary>
        /// Reads a quoted column by reading from the current line until a
        /// closing quote is found or the end of the file is reached. On return,
        /// the current position points to the delimiter or the end of the last
        /// line in the file. Note: CurrLine may be set to null on return.
        /// </summary>
        private string ReadQuotedColumn()
        {
            // Skip opening quote character
            //Debug.Assert(CurrPos < CurrLineLength && CurrLine[CurrPos] == Quote);
            CurrPos++;

            // Parse column
            var builder = new StringBuilder();
            while (true)
            {
                while (CurrPos == CurrLineLength)
                {
                    // End of line so attempt to read the next line
                    CurrLine = Reader.ReadLine();
                    // Done if we reached the end of the file
                    if (CurrLine == null)
                        return builder.ToString();

                    CurrPos = 0;
                    CurrLineLength = CurrLine.Length;
                    // Otherwise, treat as a multi-line field
                    builder.Append(Environment.NewLine);
                }

                // Test for quote character
                if (CurrLine[CurrPos] == MyQuote)
                {
                    // If two quotes, skip first and treat second as literal
                    int nextPos = (CurrPos + 1);
                    if (nextPos < CurrLineLength && CurrLine[nextPos] == MyQuote)
                        CurrPos++;
                    else
                        break; // Single quote ends quoted sequence
                }
                else if (CurrLine[CurrPos] == '\\') // unescape string
                {
                    int nextPos = (CurrPos + 1);
                    if (nextPos < CurrLineLength && (CurrLine[nextPos] == MyQuote || CurrLine[nextPos] == '\\'))
                        CurrPos++;
                }
                // Add current character to the column
                builder.Append(CurrLine[CurrPos++]);
            }

            if (CurrPos < CurrLineLength)
            {
                // Consume closing quote
                //Debug.Assert(CurrLine[CurrPos] == Quote);
                CurrPos++;
                // Append any additional characters appearing before next delimiter
                builder.Append(ReadUnquotedColumn());
            }
            // Return column value
            return builder.ToString();
        }

        /// <summary>
        /// Reads an unquoted column by reading from the current line until a
        /// delimiter is found or the end of the line is reached. On return, the
        /// current position points to the delimiter or the end of the current
        /// line.
        /// </summary>
        private string ReadUnquotedColumn()
        {
            int startPos = CurrPos;
            CurrPos = CurrLine.IndexOf(MyDelimiter, CurrPos);
            if (CurrPos == -1)
                CurrPos = CurrLineLength;
            if (CurrPos > startPos)
                return CurrLine.Substring(startPos, CurrPos - startPos);
            return String.Empty;
        }

        // Propagate Dispose to StreamReader
        public void Dispose()
        {
            Reader.Dispose();
        }
    }

    /// <summary>
    /// Class for writing to comma-separated-value (CSV) files.
    /// </summary>
    public class MultilineMyCsvEscapedFileWriter : MultilineMyCsvEscapedFileCommon, IDisposable
    {
        // Private members
        private StreamWriter Writer;

        private string OneQuote = null;
        private string TwoQuotes = null;
        private string QuotedFormat = null;

        /// <summary>
        /// Initializes a new instance of the CsvFileWriter class for the
        /// specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        public MultilineMyCsvEscapedFileWriter(Stream stream)
        {
            Writer = new StreamWriter(stream);
        }

        /// <summary>
        /// Initializes a new instance of the CsvFileWriter class for the
        /// specified file path.
        /// </summary>
        /// <param name="path">The name of the CSV file to write to</param>
        public MultilineMyCsvEscapedFileWriter(string path)
        {
            Writer = new StreamWriter(path);
        }

        /// <summary>
        /// Writes a row of columns to the current CSV file.
        /// </summary>
        /// <param name="columns">The list of columns to write</param>
        public void WriteRow(List<string> columns)
        {
            // Verify required argument
            if (columns == null)
                throw new ArgumentNullException("columns");

            // Ensure we're using current quote character
            if (OneQuote == null || OneQuote[0] != Quote)
            {
                OneQuote = String.Format("{0}", Quote);
                TwoQuotes = String.Format("{0}{0}", Quote);
                QuotedFormat = String.Format("{0}{{0}}{0}", Quote);
            }

            // Write each column
            for (int i = 0; i < columns.Count; i++)
            {
                // Add delimiter if this isn't the first column
                if (i > 0)
                    Writer.Write(Delimiter);
                // Write this column
                if (columns[i].IndexOfAny(SpecialChars) == -1)
                    Writer.Write(columns[i]);
                else
                    Writer.Write(QuotedFormat, columns[i].Replace(OneQuote, TwoQuotes));
            }
            Writer.WriteLine();
        }

        // Propagate Dispose to StreamWriter
        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}