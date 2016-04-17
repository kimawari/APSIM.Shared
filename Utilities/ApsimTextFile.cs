// -----------------------------------------------------------------------
// <copyright file="ApsimTextFile.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------

// An APSIMInputFile is either a ".met" file or a ".out" file.
// They are both text files that share the same format. 
// These classes are used to read/write these files and create an object instance of them.


namespace APSIM.Shared.Utilities
{
    using System;
    using System.Data;
    using System.IO;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A simple type for encapsulating a constant
    /// </summary>
    [Serializable]
    public class ApsimConstant
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApsimConstant"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="val">The value.</param>
        /// <param name="units">The units.</param>
        /// <param name="comm">The comm.</param>
        public ApsimConstant(string name, string val, string units, string comm)
        {
            Name = name;
            Value = val;
            Units = units;
            Comment = comm;
        }

        /// <summary>
        /// The name
        /// </summary>
        public string Name;
        /// <summary>
        /// The value
        /// </summary>
        public string Value;
        /// <summary>
        /// The units
        /// </summary>
        public string Units;
        /// <summary>
        /// The comment
        /// </summary>
        public string Comment;
    }

    /// <summary>
    /// This class encapsulates an APSIM input file providing methods for
    /// reading data.
    /// </summary>
    [Serializable]
    public class ApsimTextFile
    {
        /// <summary>
        /// The _ file name
        /// </summary>
        private string _FileName;
        /// <summary>
        /// The headings
        /// </summary>
        public StringCollection Headings;
        /// <summary>
        /// The units
        /// </summary>
        public StringCollection Units;
        /// <summary>
        /// The _ constants
        /// </summary>
        private ArrayList _Constants = new ArrayList();
        /// <summary>
        /// The CSV
        /// </summary>
        private bool CSV = false;
        /// <summary>
        /// The in
        /// </summary>
        private StreamReaderRandomAccess In;
        /// <summary>
        /// The _ first date
        /// </summary>
        private DateTime _FirstDate;
        /// <summary>
        /// The _ last date
        /// </summary>
        private DateTime _LastDate;
        /// <summary>
        /// The first line position
        /// </summary>
        private int FirstLinePosition;
        /// <summary>
        /// The words
        /// </summary>
        private StringCollection Words = new StringCollection();
        /// <summary>
        /// The column types
        /// </summary>
        private Type[] ColumnTypes;

        /// <summary>
        /// A helper to cleanly get a DataTable from the contents of a file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        /// The data table.
        /// </returns>
        public static System.Data.DataTable ToTable(string fileName)
        {
            ApsimTextFile file = new ApsimTextFile();
            try
            {
                file.Open(fileName);
                System.Data.DataTable data = file.ToTable();
                data.TableName = Path.GetFileNameWithoutExtension(fileName);
                return data;
            }
            finally
            {
                file.Close();
            }
        }

        /// <summary>
        /// Open the file ready for reading.
        /// </summary>
        /// <param name="FileName">Name of the file.</param>
        /// <exception cref="System.Exception">Cannot find file:  + FileName</exception>
        public void Open(string FileName)
        {
            if (FileName == "")
                return;

            if (!File.Exists(FileName))
                throw new Exception("Cannot find file: " + FileName);

            _FileName = FileName;
            CSV = System.IO.Path.GetExtension(FileName).ToLower() == ".csv";
            In = new StreamReaderRandomAccess(_FileName);
            Open();
        }

        /// <summary>
        /// Opens the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Open(Stream stream)
        {
            _FileName = "Memory stream";
            CSV = false;
            In = new StreamReaderRandomAccess(stream);
            Open();
        }

        /// <summary>
        /// Open the file ready for reading.
        /// </summary>
        /// <exception cref="System.Exception">
        /// Cannot find headings and units line in  + _FileName
        /// or
        /// Cannot find last row of file:  + _FileName
        /// </exception>
        private void Open()
        {
            _Constants.Clear();
            ReadApsimHeader(In);
            if (Headings != null)
            {
                FirstLinePosition = In.Position;

                // Read in first line.
                StringCollection Words = new StringCollection();
                GetNextLine(In, ref Words);
                ColumnTypes = DetermineColumnTypes(In, Words);

                // Get first date.
                object[] Values = ConvertWordsToObjects(Words, ColumnTypes);
                _FirstDate = GetDateFromValues(Values);

                // Now we need to seek to the end of file and find the last full line in the file.
                In.Seek(0, SeekOrigin.End);
                if (In.Position >= 1000 && In.Position - 1000 > FirstLinePosition)
                {
                    In.Seek(-1000, SeekOrigin.End);
                    In.ReadLine(); // throw away partial line.
                }
                else
                    In.Seek(FirstLinePosition, SeekOrigin.Begin);
                while (GetNextLine(In, ref Words))
                { }

                // Get the date from the last line.
                if (Words.Count == 0)
                    throw new Exception("Cannot find last row of file: " + _FileName);
                Values = ConvertWordsToObjects(Words, ColumnTypes);
                _LastDate = GetDateFromValues(Values);

                In.Seek(FirstLinePosition, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Close this file.
        /// </summary>
        public void Close()
        {
            In.Close();
        }

        /// <summary>
        /// Gets the first date.
        /// </summary>
        /// <value>
        /// The first date.
        /// </value>
        public DateTime FirstDate { get { return _FirstDate; } }
        /// <summary>
        /// Gets the last date.
        /// </summary>
        /// <value>
        /// The last date.
        /// </value>
        public DateTime LastDate { get { return _LastDate; } }

        /// <summary>
        /// Gets the constants.
        /// </summary>
        /// <value>
        /// The constants.
        /// </value>
        public ArrayList Constants
        {
            get
            {
                return _Constants;
            }
        }
        /// <summary>
        /// Constants the specified constant name.
        /// </summary>
        /// <param name="constantName">Name of the constant.</param>
        /// <returns></returns>
        public ApsimConstant Constant(string constantName)
        {
            // -------------------------------------
            // Return a given constant to caller
            // -------------------------------------

            foreach (ApsimConstant c in _Constants)
            {
                if (StringUtilities.StringsAreEqual(c.Name, constantName))
                {
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a constant as double.
        /// </summary>
        /// <param name="constantName">Name of the constant.</param>
        /// <returns></returns>
        public double ConstantAsDouble(string constantName)
        {
            return Convert.ToDouble(Constant(constantName).Value, CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Sets the constant.
        /// </summary>
        /// <param name="constantName">Name of the constant.</param>
        /// <param name="constantValue">The constant value.</param>
        public void SetConstant(string constantName, string constantValue)
        {
            // -------------------------------------
            // Set a given constant's value.
            // -------------------------------------

            foreach (ApsimConstant c in _Constants)
            {
                if (StringUtilities.StringsAreEqual(c.Name, constantName))
                    c.Value = constantValue;
            }
        }
        /// <summary>
        /// Adds the constant.
        /// </summary>
        /// <param name="constantName">Name of the constant.</param>
        /// <param name="constantValue">The constant value.</param>
        /// <param name="units">The units.</param>
        /// <param name="comment">The comment.</param>
        public void AddConstant(string constantName, string constantValue, string units, string comment)
        {
            // -------------------------------------
            // Add and set a given constant's value.
            // -------------------------------------

            _Constants.Add(new ApsimConstant(constantName, constantValue, units, comment));
        }
        /// <summary>
        /// Convert this file to a DataTable.
        /// </summary>
        /// <returns></returns>
        public System.Data.DataTable ToTable(List<string>addConsts = null)
        {
            System.Data.DataTable Data = new System.Data.DataTable();
            Data.TableName = "Data";

            ArrayList addedConstants = new ArrayList();

            StringCollection words = new StringCollection();
            bool checkHeadingsExist = true;
            while (GetNextLine(In, ref words))
            {
                if (checkHeadingsExist)
                {
                    for (int w = 0; w != ColumnTypes.Length; w++)
                        Data.Columns.Add(new DataColumn(Headings[w], ColumnTypes[w]));

                    if (addConsts != null)
                    {
                        foreach (ApsimConstant constant in Constants)
                        {
                            if (addConsts.Contains(constant.Name, StringComparer.OrdinalIgnoreCase) && Data.Columns.IndexOf(constant.Name) == -1)
                            {
                                Type ColumnType = StringUtilities.DetermineType(constant.Value, "");
                                Data.Columns.Add(new DataColumn(constant.Name, ColumnType));
                                addedConstants.Add(new ApsimConstant(constant.Name, constant.Value, constant.Units, ColumnType.ToString()));
                            }
                        }
                    }
                }
                DataRow newMetRow = Data.NewRow();
                object[] values = ConvertWordsToObjects(words, ColumnTypes);

                for (int w = 0; w != words.Count; w++)
                {
                    int TableColumnNumber = newMetRow.Table.Columns.IndexOf(Headings[w]);
                    if (!Convert.IsDBNull(values[TableColumnNumber]))
                        newMetRow[TableColumnNumber] = values[TableColumnNumber];
                }

                foreach (ApsimConstant constant in addedConstants)
                {
                    if (constant.Comment == typeof(Single).ToString() || constant.Comment == typeof(Double).ToString())
                        newMetRow[constant.Name] = Double.Parse(constant.Value);
                    else
                        newMetRow[constant.Name] = constant.Value;
                }
                Data.Rows.Add(newMetRow);
                checkHeadingsExist = false;
            }
            return Data;
        }
        /// <summary>
        /// Reads the apsim header lines.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <param name="constantLines">The constant lines.</param>
        /// <param name="headingLines">The heading lines.</param>
        private void ReadApsimHeaderLines(StreamReaderRandomAccess In,
                                          ref StringCollection constantLines,
                                          ref StringCollection headingLines)
        {
            string PreviousLine = "";

            string Line = In.ReadLine();
            while (!In.EndOfStream)
            {
                int PosEquals = Line.IndexOf('=');
                if (PosEquals != -1)
                {
                    // constant found.
                    constantLines.Add(Line);
                }
                else
                {
                    if (CSV)
                    {
                        headingLines.Add(Line);
                        break;
                    }

                    char[] whitespace = { ' ', '\t' };
                    int PosFirstNonBlankChar = StringUtilities.IndexNotOfAny(Line, whitespace);
                    if (PosFirstNonBlankChar != -1 && Line[PosFirstNonBlankChar] == '(')
                    {
                        headingLines.Add(PreviousLine);
                        headingLines.Add(Line);
                        break;
                    }
                }
                PreviousLine = Line;
                Line = In.ReadLine();
            }

        }

        /// <summary>
        /// Add our constants to every row in the specified table beginning with the specified StartRow.
        /// </summary>
        /// <param name="table">The table.</param>
        public void AddConstantsToData(DataTable table)
        {
            foreach (ApsimConstant Constant in Constants)
            {
                if (table.Columns.IndexOf(Constant.Name) == -1)
                {
                    Type ColumnType = StringUtilities.DetermineType(Constant.Value, "");
                    table.Columns.Add(new DataColumn(Constant.Name, ColumnType));
                }
                for (int Row = 0; Row < table.Rows.Count; Row++)
                {
                    double Value;
                    if (Double.TryParse(Constant.Value, NumberStyles.Float, new CultureInfo("en-US"), out Value))
                        table.Rows[Row][Constant.Name] = Value;
                    else
                        table.Rows[Row][Constant.Name] = Constant.Value;
                }
            }
        }

        /// <summary>
        /// Read in the APSIM header - headings/units and constants.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <exception cref="System.Exception">The number of headings and units doesn't match in file:  + _FileName</exception>
        private void ReadApsimHeader(StreamReaderRandomAccess In)
        {
            StringCollection ConstantLines = new StringCollection();
            StringCollection HeadingLines = new StringCollection();
            ReadApsimHeaderLines(In, ref ConstantLines, ref HeadingLines);

            bool TitleFound = false;
            foreach (string ConstantLine in ConstantLines)
            {
                string Line = ConstantLine;
                string Comment = StringUtilities.SplitOffAfterDelimiter(ref Line, "!");
                Comment.Trim();
                int PosEquals = Line.IndexOf('=');
                if (PosEquals != -1)
                {
                    string Name = Line.Substring(0, PosEquals).Trim();
                    if (Name.ToLower() == "title")
                    {
                        TitleFound = true;
                        Name = "Title";
                    }
                    string Value = Line.Substring(PosEquals + 1).Trim();
                    string Unit = string.Empty;
                    if (Name != "Title")
                        Unit = StringUtilities.SplitOffBracketedValue(ref Value, '(', ')');
                    _Constants.Add(new ApsimConstant(Name, Value, Unit, Comment));
                }
            }
            if (HeadingLines.Count >= 1)
            {
                if (CSV)
                {
                    HeadingLines[0] = HeadingLines[0].TrimEnd(',');
                    Headings = new StringCollection();
                    Units = new StringCollection();
                    Headings.AddRange(HeadingLines[0].Split(",".ToCharArray()));
                    for (int i = 0; i < Headings.Count; i++)
                    {
                        Headings[i] = Headings[i].Trim();
                        Units.Add("()");
                    }
                }
                else
                {
                    Headings = StringUtilities.SplitStringHonouringQuotes(HeadingLines[0], " \t");
                    Units = StringUtilities.SplitStringHonouringQuotes(HeadingLines[1], " \t");
                }
                TitleFound = TitleFound || StringUtilities.IndexOfCaseInsensitive(Headings, "title") != -1;
                if (Headings.Count != Units.Count)
                    throw new Exception("The number of headings and units doesn't match in file: " + _FileName);
            }
            if (!TitleFound)
                _Constants.Add(new ApsimConstant("Title", System.IO.Path.GetFileNameWithoutExtension(_FileName), "", ""));
        }

        /// <summary>
        /// Determine and return the data types of the specfied words.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <param name="words">The words.</param>
        /// <returns></returns>
        private Type[] DetermineColumnTypes(StreamReaderRandomAccess In, StringCollection words)
        {
            Type[] Types = new Type[words.Count];
            for (int w = 0; w != words.Count; w++)
            {
                if (words[w] == "?" || words[w] == "*" || words[w] == "")
                    Types[w] = StringUtilities.DetermineType(LookAheadForNonMissingValue(In, w), Units[w]);
                else
                    Types[w] = StringUtilities.DetermineType(words[w], Units[w]);
            }
            return Types;
        }

        /// <summary>
        /// Convert the specified words to the specified column types and return their values.
        /// </summary>
        /// <param name="words">The words.</param>
        /// <param name="columnTypes">The column types.</param>
        /// <returns></returns>
        private object[] ConvertWordsToObjects(StringCollection words, Type[] columnTypes)
        {
            object[] values = new object[words.Count];
            for (int w = 0; w != words.Count; w++)
            {
                try
                {
                    words[w] = words[w].Trim();
                    if (words[w] == "?" || words[w] == "*" || words[w] == "")
                        values[w] = DBNull.Value;

                    else if (columnTypes[w] == typeof(DateTime))
                    {
                        // Need to get a sanitised date e.g. d/M/yyyy 
                        string DateFormat = Units[w].ToLower();
                        DateFormat = StringUtilities.SplitOffBracketedValue(ref DateFormat, '(', ')');
                        DateFormat = DateFormat.Replace("mmm", "MMM");
                        DateFormat = DateFormat.Replace("mm", "m");
                        DateFormat = DateFormat.Replace("dd", "d");
                        DateFormat = DateFormat.Replace("m", "M");
                        if (DateFormat == "")
                            DateFormat = "yyyy-MM-dd";
                        DateTime Value = DateTime.ParseExact(words[w], DateFormat, CultureInfo.InvariantCulture);
                        values[w] = Value;
                    }
                    else if (columnTypes[w] == typeof(float))
                    {
                        double Value;
                        if (double.TryParse(words[w], NumberStyles.Float, CultureInfo.InvariantCulture, out Value))
                            values[w] = Value;
                        else
                            values[w] = DBNull.Value;
                    }
                    else
                        values[w] = words[w];
                }
                catch (Exception)
                {
                    values[w] = DBNull.Value;
                }
            }
            return values;
        }

        /// <summary>
        /// Return the next line in the file as a collection of words.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <param name="words">The words.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid number of values on line:  + Line + \r\nin file:  + _FileName</exception>
        private bool GetNextLine(StreamReaderRandomAccess In, ref StringCollection words)
        {
            if (In.EndOfStream)
                return false;

            string Line = In.ReadLine();

            if (Line == null || Line.Length == 0)
                return false;

            if (Line.IndexOf("!") > 0) //used to ignore "!" in a row
                Line = Line.Substring(0, Line.IndexOf("!") - 1);

            if (CSV)
            {
                words.Clear();
                Line = Line.TrimEnd(',');
                words.AddRange(Line.Split(",".ToCharArray()));
            }
            else
                words = StringUtilities.SplitStringHonouringQuotes(Line, " \t");
            if (words.Count != Headings.Count)
                throw new Exception("Invalid number of values on line: " + Line + "\r\nin file: " + _FileName);

            // Remove leading / trailing double quote chars.
            for (int i = 0; i < words.Count; i++)
                words[i] = words[i].Trim("\"".ToCharArray());
            return true;
        }
        /// <summary>
        /// Looks the ahead for non missing value.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <param name="w">The w.</param>
        /// <returns></returns>
        private string LookAheadForNonMissingValue(StreamReaderRandomAccess In, int w)
        {
            if (In.EndOfStream)
                return "?";

            int Pos = In.Position;

            StringCollection Words = new StringCollection();
            while (GetNextLine(In, ref Words) && (Words[w] == "?" || Words[w] == "*"));

            In.Position = Pos;

            if (Words.Count > w)
                return Words[w];
            else
                return "?";
        }



        /// <summary>
        /// Return the first date from the specified objects. Will return empty DateTime if not found.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        public DateTime GetDateFromValues(object[] values)
        {
            int Year = 0;
            int Month = 0;
            int Day = 0;
            for (int Col = 0; Col != values.Length; Col++)
            {
                string ColumnName = Headings[Col];
                if (ColumnName.Equals("date", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (ColumnTypes[Col] == typeof(DateTime))
                        return (DateTime)values[Col];
                    else
                        return DateTime.Parse(values[Col].ToString());
                }
                else if (ColumnName.Equals("year", StringComparison.CurrentCultureIgnoreCase))
                    Year = Convert.ToInt32(values[Col]);
                else if (ColumnName.Equals("month", StringComparison.CurrentCultureIgnoreCase))
                    Month = Convert.ToInt32(values[Col]);
                else if (ColumnName.Equals("day", StringComparison.CurrentCultureIgnoreCase))
                    Day = Convert.ToInt32(values[Col]);
            }
            if (Year > 0)
            {
                if (Day > 0)
                    return new DateTime(Year, 1, 1).AddDays(Day - 1);
                else
                    Day = 1;
                if (Month == 0)
                    Month = 1;
                return new DateTime(Year, Month, Day);
            }
            return new DateTime();
        }

        /// <summary>
        /// Seek to the specified date. Will throw if date not found.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <exception cref="System.Exception">Date  + Date.ToString() +  doesn't exist in file:  + _FileName</exception>
        public void SeekToDate(DateTime date)
        {
            if (date < _FirstDate)
                throw new Exception("Date " + date.ToString() + " doesn't exist in file: " + _FileName);

            int NumRowsToSkip = (date - _FirstDate).Days;

            In.Seek(FirstLinePosition, SeekOrigin.Begin);
            while (!In.EndOfStream && NumRowsToSkip > 0)
            {
                In.ReadLine();
                NumRowsToSkip--;
            }
        }

        /// <summary>
        /// Return the next line of data from the file as an array of objects.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception">End of file reached while reading file:  + _FileName</exception>
        public object[] GetNextLineOfData()
        {
            Words.Clear();

            if (GetNextLine(In, ref Words))
                return ConvertWordsToObjects(Words, ColumnTypes);
            else
                throw new Exception("End of file reached while reading file: " + _FileName);
        }

        /// <summary>Return the current file position</summary>
        public int GetCurrentPosition()
        {
            return In.Position;
        }

        /// <summary>Seek to the specified file position</summary>
        public void SeekToPosition(int position)
        {
            In.Seek(position, SeekOrigin.Begin);
        }

    }
}
