/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
///     The names of tables and columns, and the quotes around them.
/// </summary>
/// <remarks>
///     <para>
///         These are the PostgreSQL tests of linq2db, carried over to H2. PostgreSQL folds an
///         unquoted name to lower case and H2 folds it to upper case, so the two databases do
///         not agree on when a name has to be quoted. What they do agree on is that a name with
///         a character that is not allowed in a name has to be quoted, and that a quote inside a
///         name has to be doubled.
///     </para>
/// </remarks>
public class H2SharpIdentifierTests
{
    [Table("Rows")]
    public class Row
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }
    }

    [Theory]
    [InlineData("test")]
    [InlineData("тест")]
    [InlineData("_test")]
    [InlineData("x_")]
    [InlineData("x1")]
    [InlineData("x$")]
    public void A_name_is_quoted(string tableName)
    {
        using var db = H2SharpDatabase.Open(nameof(A_name_is_quoted) + "_" + tableName);

        db.MappingSchema.GetEntityDescriptor(typeof(Row)).TableName = tableName;

        db.Execute($"CREATE TABLE \"{tableName}\" (\"Id\" integer NOT NULL)");

        db.GetTable<Row>().ToList();

        // H2 folds an unquoted name to upper case, so the name of the table would change and the
        // table would not be found. The provider quotes every name, and that is what keeps the
        // name the model has.
        Assert.Contains($"\"{tableName}\"", db.LastQuery);
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("Тест")]
    [InlineData("1test")]
    [InlineData("$test")]
    [InlineData("te-st")]
    public void A_name_that_needs_quotes_is_quoted(string tableName)
    {
        using var db = H2SharpDatabase.Open(nameof(A_name_that_needs_quotes_is_quoted) + "_" + tableName);

        db.MappingSchema.GetEntityDescriptor(typeof(Row)).TableName = tableName;

        db.Execute($"CREATE TABLE \"{tableName}\" (\"Id\" integer NOT NULL)");

        db.GetTable<Row>().ToList();

        Assert.Contains($"\"{tableName}\"", db.LastQuery);
    }

    [Fact]
    public void A_quote_inside_a_name_is_doubled()
    {
        using var db = H2SharpDatabase.Open(nameof(A_quote_inside_a_name_is_doubled));

        // H2 2.5 takes a quote inside a name when the quote is doubled, and the provider doubles
        // it. The tokenizer of the port does not read the doubled quote.
        const string tableName = "te\"st";

        db.MappingSchema.GetEntityDescriptor(typeof(Row)).TableName = tableName;

        db.Execute($"CREATE TABLE \"{tableName.Replace("\"", "\"\"")}\" (\"Id\" integer NOT NULL)");

        db.GetTable<Row>().ToList();

        Assert.Contains("\"te\"\"st\"", db.LastQuery);
    }
}
