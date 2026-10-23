/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.H2Sharp;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The provider itself: its tools, its options and the contract of its functions.
/// </summary>
/// <remarks>
///     The functions the provider offers live in the database and nowhere else: called
///     by the program they refuse, and the query carries them to the engine instead.
/// </remarks>
public class ForumProviderTests
{
    [Fact]
    public void The_functions_live_in_the_database_and_nowhere_else()
    {
        var date = new DateTime(2024, 1, 11);

        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DateAdd("DAY", 1, date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DateDiff("DAY", date, date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DateTrunc("DAY", date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Year(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Month(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Day(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DayOfMonth(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DayOfWeek(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DayOfYear(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Hour(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Minute(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Second(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Quarter(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Week(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.DayName(date));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.MonthName(date));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ILike("a", "a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Locate("a", "a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Locate("a", "a", 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.LeftPad("a", 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.RightPad("a", 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Translate("a", "a", "a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Repeat("a", 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Space(1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.QuoteIdentifier("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Ascii("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Char(65));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Chr(65));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.StringDecode("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.StringEncode("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.StringToUtf8("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Utf8ToString(null!));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.HexToRaw("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.RawToHex(null!));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Insert("a", 1, 1, "a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ConcatWithSeparator(" ", "a", "b", "c"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ConcatWithSeparator(" ", "a", "b"));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Sign(1m));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Truncate(1m));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Truncate(1m, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Cotangent(1d));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.RoundMagic(1m));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitAnd(1L, 1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitOr(1L, 1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitXor(1L, 1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitNot(1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitNand(1L, 1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitNor(1L, 1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitXnor(1L, 1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitGet(1L, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.BitCount(1L));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.LeftShift(1L, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.RightShift(1L, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.UnsignedLeftShift(1L, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.UnsignedRightShift(1L, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.RotateLeft(1L, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.RotateRight(1L, 1));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Compress("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Expand(null!));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Soundex("a"));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.SoundexDifference("a", "b"));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ArrayAppend(new[] { 1 }, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ArrayCat(new[] { 1 }, new[] { 1 }));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ArrayContains(new[] { 1 }, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.Cardinality(new[] { 1 }));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpSpecificExtensions.ArrayLength(new[] { 1 }));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpTableFunctions.SystemRange(null!, 1, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpTableFunctions.SystemRange(null!, 1, 1, 1));
    }

    [Fact]
    public void The_table_functions_live_in_the_database_too()
    {
        using var db = ForumDatabase.Open(nameof(The_table_functions_live_in_the_database_too));

        Assert.Throws<ServerSideOnlyException>(() => H2SharpTableFunctions.SystemRange(db, 1, 1));
        Assert.Throws<ServerSideOnlyException>(() => H2SharpTableFunctions.SystemRange(db, 1, 1, 1));
    }

    [Fact]
    public void The_attribute_names_the_function_of_the_database()
    {
        var attribute = new H2FunctionAttribute("LOCATE");

        Assert.Equal("LOCATE", attribute.Name);
        Assert.True(attribute.ServerSideOnly);
        Assert.True(attribute.IgnoreGenericParameters);
    }

    [Fact]
    public void The_tools_hand_out_the_provider_and_its_connections()
    {
        Assert.IsType<H2SharpProvider>(H2SharpTools.GetDataProvider());
        Assert.IsType<H2SharpProvider>(H2SharpTools.GetDataProvider(H2SharpVersion.v2_5, "jdbc:h2:mem:tools"));

        using var byString = H2SharpTools.CreateDataConnection("jdbc:h2:mem:tools_string");
        Assert.NotNull(byString);

        using var opened = ForumDatabase.Open(nameof(The_tools_hand_out_the_provider_and_its_connections));
        using var connection = new H2Sharp.Data.H2Connection("jdbc:h2:mem:tools_connection");
        connection.Open();
        using var byConnection = H2SharpTools.CreateDataConnection(connection);
        Assert.NotNull(byConnection);

        using var transaction = connection.BeginTransaction();
        using var byTransaction = H2SharpTools.CreateDataConnection(transaction);
        Assert.NotNull(byTransaction);
    }

    [Fact]
    public void The_connection_string_names_the_same_database()
    {
        Assert.Equal(string.Empty, H2SharpConnectionString.ToUrl(null));
        Assert.Equal("jdbc:h2:mem:tools", H2SharpConnectionString.ToUrl("jdbc:h2:mem:tools"));
        Assert.Equal("jdbc:h2:mem:tools", H2SharpConnectionString.ToUrl("Data Source=mem:tools"));
        Assert.Equal(
            "jdbc:h2:mem:tools;PASSWORD=secret",
            H2SharpConnectionString.ToUrl("Data Source=mem:tools;Password=secret"));
    }

    [Fact]
    public void The_options_carry_the_bulk_copy()
    {
        var options = new H2SharpOptions();

        Assert.Equal("RowByRow", options.ToString());

        var copy = options with { BulkCopyType = LinqToDB.Data.BulkCopyType.RowByRow };

        Assert.Equal(options, copy);
    }

    [Fact]
    public void The_provider_names_itself_and_its_reader()
    {
        var provider = new H2SharpProvider(H2SharpProviderName.H2Sharp, H2SharpVersion.v2_5);

        Assert.Equal("H2Sharp.Data", provider.ConnectionNamespace);
        Assert.Equal(typeof(H2Sharp.Data.H2DataReader), provider.DataReaderType);
        Assert.Equal(LinqToDB.TableOptions.NotSet, provider.SupportedTableOptions);
        Assert.NotNull(provider.GetSchemaProvider());
    }
}
