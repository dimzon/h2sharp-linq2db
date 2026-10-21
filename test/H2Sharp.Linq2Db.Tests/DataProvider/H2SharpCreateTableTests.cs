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
///     A table of H2's types, and a row that goes into it and comes back out of it.
/// </summary>
/// <remarks>
///     <para>
///         This is the PostgreSQL test of linq2db, carried over to H2: the table is made with the
///         types of H2, the row is put in through the provider, and the row is read back through
///         the provider.
///     </para>
/// </remarks>
public class H2SharpCreateTableTests
{
    [Table("CreatedRows")]
    public class CreatedRow
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("Guid")]
        public Guid Guid { get; set; }

        [Column("TimeOffset")]
        public DateTimeOffset TimeOffset { get; set; }
    }

    [Fact]
    public void A_row_goes_into_a_table_of_H2_types_and_comes_back_out_of_it()
    {
        using var db = H2SharpDatabase.Open(nameof(A_row_goes_into_a_table_of_H2_types_and_comes_back_out_of_it));

        db.Execute("""
            CREATE TABLE "CreatedRows" (
                "Id" integer NOT NULL,
                "Guid" uuid NOT NULL,
                "TimeOffset" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_CreatedRows" PRIMARY KEY ("Id")
            )
            """);

        var guid = new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF");
        var time = new DateTimeOffset(2017, 06, 17, 16, 40, 33, TimeSpan.FromHours(-3));

        db.Insert(new CreatedRow { Id = 1, Guid = guid, TimeOffset = time });

        var row = db.GetTable<CreatedRow>().FirstOrDefault(_ => _.Guid == guid);

        Assert.NotNull(row);
        Assert.Equal(guid, row!.Guid);
        Assert.Equal(time, row.TimeOffset);
    }
}
