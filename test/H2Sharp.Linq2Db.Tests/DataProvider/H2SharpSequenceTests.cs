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
///     Numbers that come from a sequence.
/// </summary>
/// <remarks>
///     <para>
///         These are the PostgreSQL tests of linq2db, carried over to H2. In H2 2.5 a sequence is
///         made with <c>CREATE SEQUENCE</c> and a column takes the next number of it with
///         <c>DEFAULT NEXT VALUE FOR</c>.
///     </para>
/// </remarks>
public class H2SharpSequenceTests
{
    [Table("SequenceRows")]
    public class SequenceRow
    {
        [Column("Id", IsPrimaryKey = true, SkipOnInsert = true)]
        public int Id { get; set; }

        [Column("Value")]
        public string Value { get; set; } = "";
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    // The engine reads the sequences now, but it wants the number of a LIMIT written
    // out, and linq2db puts it into a parameter: "LIMIT/OFFSET takes an integer literal
    // here". The failure is the port's, not the provider's: see docs/DIVERGENCES.md.
    public void A_number_comes_from_the_sequence()
    {
        using var db = H2SharpDatabase.Open(nameof(A_number_comes_from_the_sequence));

        // H2 2.5 makes a sequence and takes the next number of it as the default of a column.
        db.Execute("CREATE SEQUENCE \"SequenceRows_Id\"");

        db.Execute("""
            CREATE TABLE "SequenceRows" (
                "Id" integer NOT NULL DEFAULT NEXT VALUE FOR "SequenceRows_Id",
                "Value" varchar(50) NOT NULL
            )
            """);

        db.Insert(new SequenceRow { Value = "SeqValue" });

        var id = db.GetTable<SequenceRow>().Single(_ => _.Value == "SeqValue").Id;

        Assert.Equal(1, id);

        db.GetTable<SequenceRow>().Where(_ => _.Id == id).Delete();

        Assert.Equal(0, db.GetTable<SequenceRow>().Count(_ => _.Value == "SeqValue"));
    }

    [Fact]
    public void The_sequence_goes_on()
    {
        using var db = H2SharpDatabase.Open(nameof(The_sequence_goes_on));

        db.Execute("CREATE SEQUENCE \"SequenceRows_Id\"");

        db.Execute("""
            CREATE TABLE "SequenceRows" (
                "Id" integer NOT NULL DEFAULT NEXT VALUE FOR "SequenceRows_Id",
                "Value" varchar(50) NOT NULL
            )
            """);

        db.Insert(new SequenceRow { Value = "First" });
        db.Insert(new SequenceRow { Value = "Second" });

        var ids = db.GetTable<SequenceRow>().OrderBy(_ => _.Id).Select(_ => _.Id).ToList();

        Assert.Equal(new[] { 1, 2 }, ids);
    }
}
