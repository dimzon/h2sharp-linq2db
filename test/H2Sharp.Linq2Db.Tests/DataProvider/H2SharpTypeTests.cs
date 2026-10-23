/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
///     The types of H2, as the provider hands them over.
/// </summary>
/// <remarks>
///     <para>
///         These are the PostgreSQL tests of linq2db, carried over to H2. The checks are the
///         same, the types are H2's, and the SQL is H2's.
///     </para>
/// </remarks>
public class H2SharpTypeTests
{
    [Theory]
    [InlineData("BigInt",        "bigint",                   DataType.Int64)]
    [InlineData("Numeric",       "numeric",                  DataType.Decimal)]
    [InlineData("SmallInt",      "smallint",                 DataType.Int16)]
    [InlineData("Integer",       "integer",                  DataType.Int32)]
    [InlineData("Double",        "double precision",         DataType.Double)]
    [InlineData("Real",          "real",                     DataType.Single)]
    [InlineData("DateTime",      "timestamp",                DataType.DateTime)]
    [InlineData("DateTimeOffset", "timestamp with time zone", DataType.DateTimeOffset)]
    [InlineData("Date",          "date",                     DataType.Date)]
    [InlineData("Char",          "char",                     DataType.Char)]
    [InlineData("VarChar",       "varchar",                  DataType.NVarChar)]
    [InlineData("VarBinary",     "varbinary",                DataType.VarBinary)]
    [InlineData("Uuid",          "uuid",                     DataType.Guid)]
    [InlineData("Boolean",       "boolean",                  DataType.Boolean)]
    public void A_value_of_a_type_comes_back_as_it_went_in(string name, string columnType, DataType dataType)    {
        using var db = H2SharpDatabase.Open(nameof(A_value_of_a_type_comes_back_as_it_went_in) + "_" + name);

        // The value is put into a column of the type and read back, and NULL stays NULL. The
        // value is a nullable one, because a column that holds NULL has to be read as one.
        switch (name)
        {
            case "BigInt":        H2SharpTypeRoundTrip.Check<long?>(db, name, columnType, 1000000L, dataType); break;
            case "Numeric":       H2SharpTypeRoundTrip.Check<decimal?>(db, name, columnType, 9999999m, dataType); break;
            case "SmallInt":      H2SharpTypeRoundTrip.Check<short?>(db, name, columnType, 25555, dataType); break;
            case "Integer":       H2SharpTypeRoundTrip.Check<int?>(db, name, columnType, 7777777, dataType); break;
            case "Double":        H2SharpTypeRoundTrip.Check<double?>(db, name, columnType, 20.31d, dataType); break;
            case "Real":          H2SharpTypeRoundTrip.Check<float?>(db, name, columnType, 16.2f, dataType); break;
            case "DateTime":      H2SharpTypeRoundTrip.Check<DateTime?>(db, name, columnType, new DateTime(2012, 12, 12, 12, 12, 12), dataType); break;
            case "DateTimeOffset": H2SharpTypeRoundTrip.Check<DateTimeOffset?>(db, name, columnType, new DateTimeOffset(2012, 12, 12, 11, 12, 12, TimeSpan.FromHours(-5)), dataType); break;
            case "Date":          H2SharpTypeRoundTrip.Check<DateTime?>(db, name, columnType, new DateTime(2012, 12, 12), dataType); break;
            case "Char":          H2SharpTypeRoundTrip.Check<char?>(db, name, columnType, '1', dataType); break;
            case "VarChar":       H2SharpTypeRoundTrip.Check<string?>(db, name, columnType, "234", dataType); break;
            case "VarBinary":     H2SharpTypeRoundTrip.Check<byte[]?>(db, name, columnType, new byte[] { 42 }, dataType); break;
            case "Uuid":          H2SharpTypeRoundTrip.Check<Guid?>(db, name, columnType, new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF"), dataType); break;
            case "Boolean":       H2SharpTypeRoundTrip.Check<bool?>(db, name, columnType, true, dataType); break;
        }
    }

    [Fact]
    public void A_time_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_time_comes_back_as_it_went_in));

        // TIME carries the declared DbType.Time into the ADO.NET parameter; the value
        // is a time of day, distinct from an untyped TimeSpan duration.
        H2SharpTypeRoundTrip.Check<TimeSpan?>(db, "Time", "time", new TimeSpan(12, 12, 12), DataType.Time);
    }

    [Fact]
    public void A_clob_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_clob_comes_back_as_it_went_in));

        // H2 2.5 holds a long text in a CHARACTER LARGE OBJECT column. The port keeps the value
        // in the row and hands it over.
        H2SharpTypeRoundTrip.Check<string?>(db, "Clob", "clob", "567", DataType.Text);
    }

    [Fact]
    public void Two_parameters_can_be_added_up()
    {
        using var db = H2SharpDatabase.Open(nameof(Two_parameters_can_be_added_up));

        // H2 2.5 takes two parameters in one expression, and the port rewrites the names of the
        // parameters into question marks and reads the expression.
        Assert.Equal(5, db.Execute<int>("SELECT @p1 + @p2", new { p1 = 2, p2 = 3 }));
    }

    [Fact]
    public void A_parameter_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_parameter_comes_back_as_it_went_in));

        Assert.Equal("1", db.Execute<string>("SELECT @p", new { p = "1" }));
        Assert.Equal(1, db.Execute<int>("SELECT @p", new { p = new DataParameter { Value = 1 } }));
        Assert.Equal("1", db.Execute<string>("SELECT @p1", new { p1 = new DataParameter { Value = "1" } }));
        Assert.Equal("1", db.Execute<string>("SELECT @p", new { p = 1 }));
    }

    [Fact]
    public void A_date_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_date_comes_back_as_it_went_in));

        var date = new DateTime(2012, 12, 12);

        Assert.Equal(date, db.Execute<DateTime>("SELECT Cast('2012-12-12' as date)"));
        Assert.Equal(date, db.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"));
        Assert.Equal(date, db.Execute<DateTime>("SELECT @p", DataParameter.Date("p", date)));
        Assert.Equal(date, db.Execute<DateTime?>("SELECT @p", new DataParameter("p", date, DataType.Date)));
    }

    [Fact]
    public void A_date_and_a_time_come_back_as_they_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_date_and_a_time_come_back_as_they_went_in));

        var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

        Assert.Equal(dateTime, db.Execute<DateTime>("SELECT Cast('2012-12-12 12:12:12' as timestamp)"));
        Assert.Equal(dateTime, db.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp)"));
        Assert.Equal(dateTime, db.Execute<DateTime>("SELECT @p", DataParameter.DateTime("p", dateTime)));
        Assert.Equal(dateTime, db.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)));
        Assert.Equal(dateTime, db.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)));
    }

    [Fact]
    public void A_character_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_character_comes_back_as_it_went_in));

        Assert.Equal('1', db.Execute<char>("SELECT Cast('1' as char)"));
        Assert.Equal('1', db.Execute<char?>("SELECT Cast('1' as char)"));
        Assert.Equal('1', db.Execute<char>("SELECT Cast('1' as char(1))"));
        Assert.Equal('1', db.Execute<char?>("SELECT Cast('1' as char(1))"));

        Assert.Equal('1', db.Execute<char>("SELECT Cast('1' as varchar)"));
        Assert.Equal('1', db.Execute<char?>("SELECT Cast('1' as varchar)"));
        Assert.Equal('1', db.Execute<char>("SELECT Cast('1' as varchar(20))"));
        Assert.Equal('1', db.Execute<char?>("SELECT Cast('1' as varchar(20))"));

        Assert.Equal('1', db.Execute<char>("SELECT @p", DataParameter.Char("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT @p", DataParameter.Char("p", '1')));
        Assert.Equal('1', db.Execute<char>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')));
        Assert.Equal('1', db.Execute<char>("SELECT Cast(@p as char(1))", DataParameter.Char("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("p", '1')));

        Assert.Equal('1', db.Execute<char>("SELECT @p", DataParameter.VarChar("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT @p", DataParameter.VarChar("p", '1')));
        Assert.Equal('1', db.Execute<char>("SELECT @p", DataParameter.NChar("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT @p", DataParameter.NChar("p", '1')));
        Assert.Equal('1', db.Execute<char>("SELECT @p", DataParameter.NVarChar("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT @p", DataParameter.NVarChar("p", '1')));
        Assert.Equal('1', db.Execute<char>("SELECT @p", DataParameter.Create("p", '1')));
        Assert.Equal('1', db.Execute<char?>("SELECT @p", DataParameter.Create("p", '1')));

        Assert.Equal('1', db.Execute<char>("SELECT @p", new DataParameter { Name = "p", Value = '1' }));
        Assert.Equal('1', db.Execute<char?>("SELECT @p", new DataParameter { Name = "p", Value = '1' }));
    }

    [Fact]
    public void A_string_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_string_comes_back_as_it_went_in));

        // H2 pads a character type to its length and keeps the padding, so the value that comes
        // back is longer than the value that went in. PostgreSQL pads as well, but its driver
        // cuts the padding off, which is why the test of PostgreSQL expects a short string.
        Assert.Equal("12345               ", db.Execute<string>("SELECT Cast('12345' as char(20))"));
        Assert.Null(db.Execute<string>("SELECT Cast(NULL as char(20))"));

        Assert.Equal("12345", db.Execute<string>("SELECT Cast('12345' as varchar(20))"));
        Assert.Null(db.Execute<string>("SELECT Cast(NULL as varchar(20))"));

        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.Char("p", "123")));
        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.VarChar("p", "123")));
        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.Text("p", "123")));
        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.NChar("p", "123")));
        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.NVarChar("p", "123")));
        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.NText("p", "123")));
        Assert.Equal("123", db.Execute<string>("SELECT @p", DataParameter.Create("p", "123")));

        Assert.Null(db.Execute<string>("SELECT @p", DataParameter.Create("p", (string?)null)));
        Assert.Equal("1", db.Execute<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }));
    }

    [Fact]
    public void A_binary_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_binary_comes_back_as_it_went_in));

        var bytes = new byte[] { 48, 57 };

        Assert.Equal(bytes, db.Execute<byte[]>("SELECT X'3039'"));

        Assert.Equal(bytes, db.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", bytes)));
        Assert.Equal(bytes, db.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", bytes)));
        Assert.Equal(bytes, db.Execute<byte[]>("SELECT @p", DataParameter.Create("p", bytes)));
        Assert.Null(db.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)));
        Assert.Equal(Array.Empty<byte>(), db.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", Array.Empty<byte>())));
        Assert.Equal(bytes, db.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = bytes }));
    }

    [Fact]
    public void A_uuid_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_uuid_comes_back_as_it_went_in));

        var guid = new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF");

        Assert.Equal(guid, db.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"));
        Assert.Equal(guid, db.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"));

        Assert.Equal(guid, db.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)));
        Assert.Equal(guid, db.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }));
    }

    [Fact]
    public void A_json_comes_back_as_it_went_in()
    {
        using var db = H2SharpDatabase.Open(nameof(A_json_comes_back_as_it_went_in));

        var json = /*lang=json,strict*/ "{\"name\":\"bob\", \"age\":10}";

        // The engine stores the document canonically.
        Assert.Equal("{\"name\":\"bob\",\"age\":10}", db.Execute<string>("SELECT @p", new DataParameter("p", json, DataType.Json)));
    }

    private static object? Value(Type type)
    {
        if (type == typeof(char))         return '1';
        if (type == typeof(DateTime))     return new DateTime(2012, 12, 12, 12, 12, 12);
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2012, 12, 12, 11, 12, 12, TimeSpan.FromHours(-5));
        if (type == typeof(decimal))      return 9999999m;
        if (type == typeof(double))       return 20.31d;
        if (type == typeof(float))        return 16.2f;
        if (type == typeof(Guid))         return new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF");
        if (type == typeof(int))          return 7777777;
        if (type == typeof(long))         return 1000000L;
        if (type == typeof(short))        return (short)25555;
        if (type == typeof(TimeSpan))     return new TimeSpan(12, 12, 12);
        if (type == typeof(bool))         return true;

        return "234";
    }
}
