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
            file.Open(fileName);
            System.Data.DataTable data = file.ToTable();
            data.TableName = Path.GetFileNameWithoutExtension(fileName);
            file.Close();
            return data;
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
            if (Headings == null || Units == null)
                throw new Exception("Cannot find headings and units line in " + _FileName);
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
        /// <param name="ConstantName">Name of the constant.</param>
        /// <returns></returns>
        public ApsimConstant Constant(string ConstantName)
        {
            // -------------------------------------
            // Return a given constant to caller
            // -------------------------------------

            foreach (ApsimConstant c in _Constants)
            {
                if (StringUtilities.StringsAreEqual(c.Name, ConstantName))
                {
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a constant as double.
        /// </summary>
        /// <param name="ConstantName">Name of the constant.</param>
        /// <returns></returns>
        public double ConstantAsDouble(string ConstantName)
        {
            return Convert.ToDouble(Constant(ConstantName).Value, CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Sets the constant.
        /// </summary>
        /// <param name="ConstantName">Name of the constant.</param>
        /// <param name="ConstantValue">The constant value.</param>
        public void SetConstant(string ConstantName, string ConstantValue)
        {
            // -------------------------------------
            // Set a given constant's value.
            // -------------------------------------

            foreach (ApsimConstant c in _Constants)
            {
                if (StringUtilities.StringsAreEqual(c.Name, ConstantName))
                    c.Value = ConstantValue;
            }
        }
        /// <summary>
        /// Adds the constant.
        /// </summary>
        /// <param name="ConstantName">Name of the constant.</param>
        /// <param name="ConstantValue">The constant value.</param>
        /// <param name="Units">The units.</param>
        /// <param name="Comment">The comment.</param>
        public void AddConstant(string ConstantName, string ConstantValue, string Units, string Comment)
        {
            // -------------------------------------
            // Add and set a given constant's value.
            // -------------------------------------

            _Constants.Add(new ApsimConstant(ConstantName, ConstantValue, Units, Comment));
        }
        /// <summary>
        /// Convert this file to a DataTable.
        /// </summary>
        /// <returns></returns>
        public System.Data.DataTable ToTable()
        {
            System.Data.DataTable Data = new System.Data.DataTable();
            Data.TableName = "Data";

            ArrayList addedConstants = new ArrayList();

            StringCollection Words = new StringCollection();
            bool CheckHeadingsExist = true;
            while (GetNextLine(In, ref Words))
            {
                if (CheckHeadingsExist)
                {
                    for (int w = 0; w != ColumnTypes.Length; w++)
                        Data.Columns.Add(new DataColumn(Headings[w], ColumnTypes[w]));
                    foreach (ApsimConstant Constant in Constants)
                    {
                        if (Data.Columns.IndexOf(Constant.Name) == -1)
                        {
                            Type ColumnType = StringUtilities.DetermineType(Constant.Value, "");
                            Data.Columns.Add(new DataColumn(Constant.Name, ColumnType));
                            addedConstants.Add(new ApsimConstant(Constant.Name, Constant.Value, Constant.Units, ColumnType.ToString()));
                        }
                    }
                }
                DataRow NewMetRow = Data.NewRow();
                object[] Values = ConvertWordsToObjects(Words, ColumnTypes);

                for (int w = 0; w != Words.Count; w++)
                {
                    int TableColumnNumber = NewMetRow.Table.Columns.IndexOf(Headings[w]);
                    if (!Convert.IsDBNull(Values[TableColumnNumber]))
                        NewMetRow[TableColumnNumber] = Values[TableColumnNumber];
                }

                foreach (ApsimConstant Constant in addedConstants)
                {
                    if (Constant.Comment == typeof(Single).ToString() || Constant.Comment == typeof(Double).ToString())
                        NewMetRow[Constant.Name] = Double.Parse(Constant.Value);
                    else
                        NewMetRow[Constant.Name] = Constant.Value;
                }
                Data.Rows.Add(NewMetRow);
                CheckHeadingsExist = false;
            }
            return Data;
        }
        /// <summary>
        /// Reads the apsim header lines.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <param name="ConstantLines">The constant lines.</param>
        /// <param name="HeadingLines">The heading lines.</param>
        private void ReadApsimHeaderLines(StreamReaderRandomAccess In,
                                          ref StringCollection ConstantLines,
                                          ref StringCollection HeadingLines)
        {
            string PreviousLine = "";

            string Line = In.ReadLine();
            while (!In.EndOfStream)
            {
                int PosEquals = Line.IndexOf('=');
                if (PosEquals != -1)
                {
                    // constant found.
                    ConstantLines.Add(Line);
                }
                else
                {
                    char[] whitespace = { ' ', '\t' };
                    int PosFirstNonBlankChar = StringUtilities.IndexNotOfAny(Line, whitespace);
                    if (PosFirstNonBlankChar != -1 && Line[PosFirstNonBlankChar] == '(')
                    {
                        HeadingLines.Add(PreviousLine);
                        HeadingLines.Add(Line);
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
        /// <param name="Table">The table.</param>
        public void AddConstantsToData(System.Data.DataTable Table)
        {
            foreach (ApsimConstant Constant in Constants)
            {
                if (Table.Columns.IndexOf(Constant.Name) == -1)
                {
                    Type ColumnType = StringUtilities.DetermineType(Constant.Value, "");
                    Table.Columns.Add(new DataColumn(Constant.Name, ColumnType));
                }
                for (int Row = 0; Row < Table.Rows.Count; Row++)
                {
                    double Value;
                    if (Double.TryParse(Constant.Value, NumberStyles.Float, new CultureInfo("en-US"), out Value))
                        Table.Rows[Row][Constant.Name] = Value;
                    else
                        Table.Rows[Row][Constant.Name] = Constant.Value;
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
                    string Unit = StringUtilities.SplitOffBracketedValue(ref Value, '(', ')');
                    _Constants.Add(new ApsimConstant(Name, Value, Unit, Comment));
                }
            }
            if (HeadingLines.Count >= 2)
            {
                if (CSV)
                {
                    HeadingLines[0] = HeadingLines[0].TrimEnd(',');
                    HeadingLines[1] = HeadingLines[1].TrimEnd(',');
                    Headings = new StringCollection();
                    Units = new StringCollection();
                    Headings.AddRange(HeadingLines[0].Split(",".ToCharArray()));
                    Units.AddRange(HeadingLines[1].Split(",".ToCharArray()));
                    for (int i = 0; i < Headings.Count; i++)
                        Headings[i] = Headings[i].Trim();
                    for (int i = 0; i < Units.Count; i++)
                        Units[i] = Units[i].Trim();
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
        /// <param name="Words">The words.</param>
        /// <returns></returns>
        private Type[] DetermineColumnTypes(StreamReaderRandomAccess In, StringCollection Words)
        {
            Type[] Types = new Type[Words.Count];
            for (int w = 0; w != Words.Count; w++)
            {
                if (Words[w] == "?" || Words[w] == "*" || Words[w] == "")
                    Types[w] = StringUtilities.DetermineType(LookAheadForNonMissingValue(In, w), Units[w]);
                else
                    Types[w] = StringUtilities.DetermineType(Words[w], Units[w]);
            }
            return Types;
        }

        /// <summary>
        /// Convert the specified words to the specified column types and return their values.
        /// </summary>
        /// <param name="Words">The words.</param>
        /// <param name="ColumnTypes">The column types.</param>
        /// <returns></returns>
        private object[] ConvertWordsToObjects(StringCollection Words, Type[] ColumnTypes)
        {
            object[] Values = new object[Words.Count];
            for (int w = 0; w != Words.Count; w++)
            {
                try
                {
                    Words[w] = Words[w].Trim();
                    if (Words[w] == "?" || Words[w] == "*" || Words[w] == "")
                        Values[w] = DBNull.Value;

                    else if (ColumnTypes[w] == typeof(DateTime))
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
                        DateTime Value = DateTime.ParseExact(Words[w], DateFormat, CultureInfo.InvariantCulture);
                        Values[w] = Value;
                    }
                    else if (ColumnTypes[w] == typeof(float))
                    {
                        double Value;
                        if (double.TryParse(Words[w], NumberStyles.Float, CultureInfo.InvariantCulture, out Value))
                            Values[w] = Value;
                        else
                            Values[w] = DBNull.Value;
                    }
                    else
                        Values[w] = Words[w];
                }
                catch (Exception)
                {
                    Values[w] = DBNull.Value;
                }
            }
            return Values;
        }

        /// <summary>
        /// Return the next line in the file as a collection of words.
        /// </summary>
        /// <param name="In">The in.</param>
        /// <param name="Words">The words.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid number of values on line:  + Line + \r\nin file:  + _FileName</exception>
        private bool GetNextLine(StreamReaderRandomAccess In, ref StringCollection Words)
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
                Words.Clear();
                Line = Line.TrimEnd(',');
                Words.AddRange(Line.Split(",".ToCharArray()));
            }
            else
                Words = StringUtilities.SplitStringHonouringQuotes(Line, " \t");
            if (Words.Count != Headings.Count)
                throw new Exception("Invalid number of values on line: " + Line + "\r\nin file: " + _FileName);

            // Remove leading / trailing double quote chars.
            for (int i = 0; i < Words.Count; i++)
                Words[i] = Words[i].Trim("\"".ToCharArray());
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
        /// <param name="Values">The values.</param>
        /// <returns></returns>
        public DateTime GetDateFromValues(object[] Values)
        {
            int Year = 0;
            int Month = 0;
            int Day = 0;
            for (int Col = 0; Col != Values.Length; Col++)
            {
                string ColumnName = Headings[Col];
                if (ColumnName.Equals("date", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (ColumnTypes[Col] == typeof(DateTime))
                        return (DateTime)Values[Col];
                    else
                        return DateTime.Parse(Values[Col].ToString());
                }
                else if (ColumnName.Equals("year", StringComparison.CurrentCultureIgnoreCase))
                    Year = Convert.ToInt32(Values[Col]);
                else if (ColumnName.Equals("month", StringComparison.CurrentCultureIgnoreCase))
                    Month = Convert.ToInt32(Values[Col]);
                else if (ColumnName.Equals("day", StringComparison.CurrentCultureIgnoreCase))
                    Day = Convert.ToInt32(Values[Col]);
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
        /// <param name="Date">The date.</param>
        /// <exception cref="System.Exception">Date  + Date.ToString() +  doesn't exist in file:  + _FileName</exception>
        public void SeekToDate(DateTime Date)
        {
            if (Date < _FirstDate)
                throw new Exception("Date " + Date.ToString() + " doesn't exist in file: " + _FileName);

            int NumRowsToSkip = (Date - _FirstDate).Days;

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
    }
}
