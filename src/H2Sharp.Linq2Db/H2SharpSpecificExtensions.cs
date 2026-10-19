/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     The built-in functions of H2, as they are called in a query.
/// </summary>
/// <remarks>
///     <para>
///         These are the functions the EF Core provider of H2Sharp offers, the same functions and
///         the same names, so that a query reads the same in both. Each one is a function of H2
///         2.5 and nothing else: the name in the attribute is the name of the function in the
///         database.
///     </para>
///     <para>
///         A method that returns nothing has no type in C#, so a function without a result is
///         declared as returning <see cref="object" /> and it is null when it is called.
///     </para>
/// </remarks>
public static class H2SharpSpecificExtensions
{
    #region Date and time

    /// <summary>
    ///     Adds a number of units to a date: <c>DATEADD('DAY', 1, x)</c>.
    /// </summary>
    [H2Function("DATEADD")]
    public static DateTime DateAdd([ExprParameter(DoNotParameterize = true)] string unit, int count, DateTime value)
        => throw new ServerSideOnlyException(nameof(DateAdd));

    /// <summary>
    ///     The number of units between two dates: <c>DATEDIFF('DAY', a, b)</c>.
    /// </summary>
    [H2Function("DATEDIFF")]
    public static long DateDiff([ExprParameter(DoNotParameterize = true)] string unit, DateTime start, DateTime end)
        => throw new ServerSideOnlyException(nameof(DateDiff));

    /// <summary>
    ///     The date with the part cut off: <c>DATE_TRUNC('DAY', x)</c>.
    /// </summary>
    [H2Function("DATE_TRUNC")]
    public static DateTime DateTrunc([ExprParameter(DoNotParameterize = true)] string unit, DateTime value)
        => throw new ServerSideOnlyException(nameof(DateTrunc));

    /// <summary>
    ///     The year of a date: <c>YEAR(x)</c>.
    /// </summary>
    [H2Function("YEAR")]
    public static int Year(DateTime value)
        => throw new ServerSideOnlyException(nameof(Year));

    /// <summary>
    ///     The month of a date: <c>MONTH(x)</c>.
    /// </summary>
    [H2Function("MONTH")]
    public static int Month(DateTime value)
        => throw new ServerSideOnlyException(nameof(Month));

    /// <summary>
    ///     The day of the month of a date: <c>DAY(x)</c>.
    /// </summary>
    [H2Function("DAY")]
    public static int Day(DateTime value)
        => throw new ServerSideOnlyException(nameof(Day));

    /// <summary>
    ///     The day of the month of a date: <c>DAY_OF_MONTH(x)</c>.
    /// </summary>
    [H2Function("DAY_OF_MONTH")]
    public static int DayOfMonth(DateTime value)
        => throw new ServerSideOnlyException(nameof(DayOfMonth));

    /// <summary>
    ///     The day of the week of a date: <c>DAY_OF_WEEK(x)</c>.
    /// </summary>
    [H2Function("DAY_OF_WEEK")]
    public static int DayOfWeek(DateTime value)
        => throw new ServerSideOnlyException(nameof(DayOfWeek));

    /// <summary>
    ///     The day of the year of a date: <c>DAY_OF_YEAR(x)</c>.
    /// </summary>
    [H2Function("DAY_OF_YEAR")]
    public static int DayOfYear(DateTime value)
        => throw new ServerSideOnlyException(nameof(DayOfYear));

    /// <summary>
    ///     The hour of a date: <c>HOUR(x)</c>.
    /// </summary>
    [H2Function("HOUR")]
    public static int Hour(DateTime value)
        => throw new ServerSideOnlyException(nameof(Hour));

    /// <summary>
    ///     The minute of a date: <c>MINUTE(x)</c>.
    /// </summary>
    [H2Function("MINUTE")]
    public static int Minute(DateTime value)
        => throw new ServerSideOnlyException(nameof(Minute));

    /// <summary>
    ///     The second of a date: <c>SECOND(x)</c>.
    /// </summary>
    [H2Function("SECOND")]
    public static int Second(DateTime value)
        => throw new ServerSideOnlyException(nameof(Second));

    /// <summary>
    ///     The quarter of a date: <c>QUARTER(x)</c>.
    /// </summary>
    [H2Function("QUARTER")]
    public static int Quarter(DateTime value)
        => throw new ServerSideOnlyException(nameof(Quarter));

    /// <summary>
    ///     The week of a date: <c>WEEK(x)</c>.
    /// </summary>
    [H2Function("WEEK")]
    public static int Week(DateTime value)
        => throw new ServerSideOnlyException(nameof(Week));

    /// <summary>
    ///     The name of the day of a date: <c>DAYNAME(x)</c>.
    /// </summary>
    [H2Function("DAYNAME")]
    public static string DayName(DateTime value)
        => throw new ServerSideOnlyException(nameof(DayName));

    /// <summary>
    ///     The name of the month of a date: <c>MONTHNAME(x)</c>.
    /// </summary>
    [H2Function("MONTHNAME")]
    public static string MonthName(DateTime value)
        => throw new ServerSideOnlyException(nameof(MonthName));

    #endregion

    #region Strings

    /// <summary>
    ///     Whether a text matches a pattern, ignoring the case: <c>x ILIKE pattern</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         H2 2.5 has <c>ILIKE</c> as an operator and not as a function, so this is an
    ///         operator in the query too. The text has to be a column of the table for that: a
    ///         parameter is not a value an operator can stand on.
    ///     </para>
    /// </remarks>
    [Sql.Extension("{matchExpression} ILIKE {pattern}")]
    public static bool ILike(
        [ExprParameter("matchExpression")] string matchExpression,
        [ExprParameter("pattern")] string pattern)
        => throw new ServerSideOnlyException(nameof(ILike));

    /// <summary>
    ///     The place of a text in another text: <c>LOCATE(substring, value)</c>.
    /// </summary>
    /// <remarks>
    ///     H2 2.5 takes the text that is looked for first and the text that is looked in second,
    ///     the other way round from what the function of the SQL standard does.
    /// </remarks>
    [H2Function("LOCATE")]
    public static int Locate(string substring, string value)
        => throw new ServerSideOnlyException(nameof(Locate));

    /// <summary>
    ///     The place of a text in another text, from a place onwards: <c>LOCATE(substring, value, start)</c>.
    /// </summary>
    [H2Function("LOCATE")]
    public static int Locate(string substring, string value, int startPosition)
        => throw new ServerSideOnlyException(nameof(Locate));

    /// <summary>
    ///     The text with spaces added on the left: <c>LPAD(value, length, pad)</c>.
    /// </summary>
    [H2Function("LPAD")]
    public static string LeftPad(string value, int length, string padString = " ")
        => throw new ServerSideOnlyException(nameof(LeftPad));

    /// <summary>
    ///     The text with spaces added on the right: <c>RPAD(value, length, pad)</c>.
    /// </summary>
    [H2Function("RPAD")]
    public static string RightPad(string value, int length, string padString = " ")
        => throw new ServerSideOnlyException(nameof(RightPad));

    /// <summary>
    ///     The text with the characters of one text taken for the characters of another:
    ///     <c>TRANSLATE(value, from, to)</c>.
    /// </summary>
    [H2Function("TRANSLATE")]
    public static string Translate(string value, string from, string to)
        => throw new ServerSideOnlyException(nameof(Translate));

    /// <summary>
    ///     The text repeated: <c>REPEAT(value, count)</c>.
    /// </summary>
    [H2Function("REPEAT")]
    public static string Repeat(string value, int count)
        => throw new ServerSideOnlyException(nameof(Repeat));

    /// <summary>
    ///     A text of spaces: <c>SPACE(count)</c>.
    /// </summary>
    [H2Function("SPACE")]
    public static string Space(int count)
        => throw new ServerSideOnlyException(nameof(Space));

    /// <summary>
    ///     The name of an object, in quotes: <c>QUOTE_IDENT(name)</c>.
    /// </summary>
    [H2Function("QUOTE_IDENT")]
    public static string QuoteIdentifier(string identifier)
        => throw new ServerSideOnlyException(nameof(QuoteIdentifier));

    /// <summary>
    ///     The code of a character: <c>ASCII(value)</c>.
    /// </summary>
    [H2Function("ASCII")]
    public static int Ascii(string value)
        => throw new ServerSideOnlyException(nameof(Ascii));

    /// <summary>
    ///     The character of a code: <c>CHAR(code)</c>.
    /// </summary>
    [H2Function("CHAR")]
    public static string Char(int code)
        => throw new ServerSideOnlyException(nameof(Char));

    /// <summary>
    ///     The character of a code: <c>CHR(code)</c>.
    /// </summary>
    [H2Function("CHR")]
    public static string Chr(int code)
        => throw new ServerSideOnlyException(nameof(Chr));

    /// <summary>
    ///     The text of a text that holds bytes: <c>STRINGDECODE(value)</c>.
    /// </summary>
    [H2Function("STRINGDECODE")]
    public static string StringDecode(string value)
        => throw new ServerSideOnlyException(nameof(StringDecode));

    /// <summary>
    ///     The bytes of a text: <c>STRINGENCODE(value)</c>.
    /// </summary>
    [H2Function("STRINGENCODE")]
    public static string StringEncode(string value)
        => throw new ServerSideOnlyException(nameof(StringEncode));

    /// <summary>
    ///     The bytes of a text in UTF-8: <c>STRINGTOUTF8(value)</c>.
    /// </summary>
    [H2Function("STRINGTOUTF8")]
    public static byte[] StringToUtf8(string value)
        => throw new ServerSideOnlyException(nameof(StringToUtf8));

    /// <summary>
    ///     The text of bytes in UTF-8: <c>UTF8TOSTRING(value)</c>.
    /// </summary>
    [H2Function("UTF8TOSTRING")]
    public static string Utf8ToString(byte[] value)
        => throw new ServerSideOnlyException(nameof(Utf8ToString));

    /// <summary>
    ///     The bytes of a text written in hexadecimal: <c>HEXTORAW(value)</c>.
    /// </summary>
    [H2Function("HEXTORAW")]
    public static byte[] HexToRaw(string hex)
        => throw new ServerSideOnlyException(nameof(HexToRaw));

    /// <summary>
    ///     The text of bytes written in hexadecimal: <c>RAWTOHEX(value)</c>.
    /// </summary>
    [H2Function("RAWTOHEX")]
    public static string RawToHex(byte[] value)
        => throw new ServerSideOnlyException(nameof(RawToHex));

    /// <summary>
    ///     The text with another text put in it: <c>INSERT(value, position, length, insert)</c>.
    /// </summary>
    [H2Function("INSERT")]
    public static string Insert(string value, int position, int length, string insert)
        => throw new ServerSideOnlyException(nameof(Insert));

    /// <summary>
    ///     The texts put together with a text between them: <c>CONCAT_WS(separator, first, second, rest)</c>.
    /// </summary>
    [Sql.Extension("CONCAT_WS({separator}, {first}, {second}, {rest, ', '})")]
    public static string ConcatWithSeparator(
        [ExprParameter("separator")] string separator,
        [ExprParameter("first")] string first,
        [ExprParameter("second")] string second,
        [ExprParameter("rest")] params string[] rest)
        => throw new ServerSideOnlyException(nameof(ConcatWithSeparator));

    #endregion

    #region Numbers

    /// <summary>
    ///     The sign of a number: <c>SIGN(value)</c>.
    /// </summary>
    [H2Function("SIGN")]
    public static int Sign(decimal value)
        => throw new ServerSideOnlyException(nameof(Sign));

    /// <summary>
    ///     The number without its fraction: <c>TRUNC(value)</c>.
    /// </summary>
    [H2Function("TRUNC")]
    public static decimal Truncate(decimal value)
        => throw new ServerSideOnlyException(nameof(Truncate));

    /// <summary>
    ///     The number with the fraction cut off at a place: <c>TRUNCATE(value, precision)</c>.
    /// </summary>
    [H2Function("TRUNCATE")]
    public static decimal Truncate(decimal value, int precision)
        => throw new ServerSideOnlyException(nameof(Truncate));

    /// <summary>
    ///     The cotangent of a number: <c>COT(value)</c>.
    /// </summary>
    [H2Function("COT")]
    public static double Cotangent(double value)
        => throw new ServerSideOnlyException(nameof(Cotangent));

    /// <summary>
    ///     The number rounded the way of the bank: <c>ROUNDMAGIC(value)</c>.
    /// </summary>
    [H2Function("ROUNDMAGIC")]
    public static decimal RoundMagic(decimal value)
        => throw new ServerSideOnlyException(nameof(RoundMagic));

    #endregion

    #region Bits

    /// <summary>
    ///     The bits of two numbers, and: <c>BITAND(left, right)</c>.
    /// </summary>
    [H2Function("BITAND")]
    public static long BitAnd(long left, long right)
        => throw new ServerSideOnlyException(nameof(BitAnd));

    /// <summary>
    ///     The bits of two numbers, or: <c>BITOR(left, right)</c>.
    /// </summary>
    [H2Function("BITOR")]
    public static long BitOr(long left, long right)
        => throw new ServerSideOnlyException(nameof(BitOr));

    /// <summary>
    ///     The bits of two numbers, or not: <c>BITXOR(left, right)</c>.
    /// </summary>
    [H2Function("BITXOR")]
    public static long BitXor(long left, long right)
        => throw new ServerSideOnlyException(nameof(BitXor));

    /// <summary>
    ///     The bits of a number, turned over: <c>BITNOT(value)</c>.
    /// </summary>
    [H2Function("BITNOT")]
    public static long BitNot(long value)
        => throw new ServerSideOnlyException(nameof(BitNot));

    /// <summary>
    ///     The bits of two numbers, and not: <c>BITNAND(left, right)</c>.
    /// </summary>
    [H2Function("BITNAND")]
    public static long BitNand(long left, long right)
        => throw new ServerSideOnlyException(nameof(BitNand));

    /// <summary>
    ///     The bits of two numbers, or not: <c>BITNOR(left, right)</c>.
    /// </summary>
    [H2Function("BITNOR")]
    public static long BitNor(long left, long right)
        => throw new ServerSideOnlyException(nameof(BitNor));

    /// <summary>
    ///     The bits of two numbers, the same or not: <c>BITXNOR(left, right)</c>.
    /// </summary>
    [H2Function("BITXNOR")]
    public static long BitXnor(long left, long right)
        => throw new ServerSideOnlyException(nameof(BitXnor));

    /// <summary>
    ///     One bit of a number: <c>BITGET(value, bit)</c>.
    /// </summary>
    [H2Function("BITGET")]
    public static long BitGet(long value, int bit)
        => throw new ServerSideOnlyException(nameof(BitGet));

    /// <summary>
    ///     The number of the bits that are set: <c>BITCOUNT(value)</c>.
    /// </summary>
    [H2Function("BITCOUNT")]
    public static long BitCount(long value)
        => throw new ServerSideOnlyException(nameof(BitCount));

    /// <summary>
    ///     The number moved to the left: <c>LSHIFT(value, count)</c>.
    /// </summary>
    [H2Function("LSHIFT")]
    public static long LeftShift(long value, int count)
        => throw new ServerSideOnlyException(nameof(LeftShift));

    /// <summary>
    ///     The number moved to the right: <c>RSHIFT(value, count)</c>.
    /// </summary>
    [H2Function("RSHIFT")]
    public static long RightShift(long value, int count)
        => throw new ServerSideOnlyException(nameof(RightShift));

    /// <summary>
    ///     The number moved to the left, without the sign: <c>ULSHIFT(value, count)</c>.
    /// </summary>
    [H2Function("ULSHIFT")]
    public static long UnsignedLeftShift(long value, int count)
        => throw new ServerSideOnlyException(nameof(UnsignedLeftShift));

    /// <summary>
    ///     The number moved to the right, without the sign: <c>URSHIFT(value, count)</c>.
    /// </summary>
    [H2Function("URSHIFT")]
    public static long UnsignedRightShift(long value, int count)
        => throw new ServerSideOnlyException(nameof(UnsignedRightShift));

    /// <summary>
    ///     The number turned around: <c>ROTATELEFT(value, count)</c>.
    /// </summary>
    [H2Function("ROTATELEFT")]
    public static long RotateLeft(long value, int count)
        => throw new ServerSideOnlyException(nameof(RotateLeft));

    /// <summary>
    ///     The number turned the other way around: <c>ROTATERIGHT(value, count)</c>.
    /// </summary>
    [H2Function("ROTATERIGHT")]
    public static long RotateRight(long value, int count)
        => throw new ServerSideOnlyException(nameof(RotateRight));

    #endregion

    #region Compressed text

    /// <summary>
    ///     The bytes of a text, compressed: <c>COMPRESS(value)</c>.
    /// </summary>
    [H2Function("COMPRESS")]
    public static byte[] Compress(string value)
        => throw new ServerSideOnlyException(nameof(Compress));

    /// <summary>
    ///     The text of compressed bytes: <c>EXPAND(value)</c>.
    /// </summary>
    [H2Function("EXPAND")]
    public static string Expand(byte[] value)
        => throw new ServerSideOnlyException(nameof(Expand));

    #endregion

    #region Sounds

    /// <summary>
    ///     The code of how a word sounds: <c>SOUNDEX(value)</c>.
    /// </summary>
    [H2Function("SOUNDEX")]
    public static string Soundex(string value)
        => throw new ServerSideOnlyException(nameof(Soundex));

    /// <summary>
    ///     How close two words sound: <c>DIFFERENCE(first, second)</c>.
    /// </summary>
    [H2Function("DIFFERENCE")]
    public static int SoundexDifference(string first, string second)
        => throw new ServerSideOnlyException(nameof(SoundexDifference));

    #endregion
}
