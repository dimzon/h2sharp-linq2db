/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Mapping;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     Every column type the provider writes: the table is made with them and a row comes back.
/// </summary>
public class ForumTypedColumnTests
{
    [Table("TypedRows")]
    private sealed class TypedRow
    {
        [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
        public int Id { get; set; }

        [Column("Tiny")]
        public byte Tiny { get; set; }

        [Column("Small")]
        public short Small { get; set; }

        [Column("Little")]
        public sbyte Little { get; set; }

        [Column("Real")]
        public float Real { get; set; }

        [Column("Money", DataType = DataType.Money, Precision = 18, Scale = 2)]
        public decimal Money { get; set; }

        [Column("Precise", Precision = 10)]
        public decimal Precise { get; set; }

        [Column("Letter", DataType = DataType.Char)]
        public string Letter { get; set; } = "";

        [Column("Note", DataType = DataType.NText)]
        public string Note { get; set; } = "";

        [Column("Raw", DataType = DataType.VarBinary)]
        public byte[] Raw { get; set; } = [];

        [Column("Day", DataType = DataType.Date)]
        public DateTime Day { get; set; }

        [Column("Moment", DataType = DataType.Time)]
        public TimeOnly Moment { get; set; }

        [Column("Stamp")]
        public DateTimeOffset Stamp { get; set; }

        [Column("Key")]
        public Guid Key { get; set; }
    }

    [Table("JsonRows")]
    private sealed class JsonRow
    {
        [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
        public int Id { get; set; }

        [Column("Doc", DataType = DataType.Json)]
        public string Doc { get; set; } = "";
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void Json_documents_come_back_as_they_went_in()
    {
        using var db = ForumDatabase.Open(nameof(Json_documents_come_back_as_they_went_in));

        db.CreateTable<JsonRow>();

        try
        {
            int id = Convert.ToInt32(db.InsertWithIdentity(new JsonRow { Doc = "{\"a\": 1}" }));

            Assert.Equal("{\"a\": 1}", db.GetTable<JsonRow>().Single(r => r.Id == id).Doc);
        }
        finally
        {
            db.DropTable<JsonRow>();
        }
    }

    [Fact]
    public void Every_column_type_comes_back_as_it_went_in()
    {
        using var db = ForumDatabase.Open(nameof(Every_column_type_comes_back_as_it_went_in));

        db.CreateTable<TypedRow>();

        var row = new TypedRow
        {
            Tiny = 250,
            Small = -32000,
            Little = -100,
            Real = 1.5f,
            Money = 123.45m,
            Precise = 7m,
            Letter = "Q",
            Note = "memo",
            Raw = [9, 8, 7],
            Day = new DateTime(2024, 2, 29),
            Moment = new TimeOnly(23, 59, 58),
            Stamp = new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.FromHours(3)),
            Key = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
        };

        int id = Convert.ToInt32(db.InsertWithIdentity(row));

        TypedRow back = db.GetTable<TypedRow>().Single(r => r.Id == id);

        Assert.Equal(row.Tiny, back.Tiny);
        Assert.Equal(row.Small, back.Small);
        Assert.Equal(row.Little, back.Little);
        Assert.Equal(row.Real, back.Real);
        Assert.Equal(row.Money, back.Money);
        Assert.Equal(row.Precise, back.Precise);
        Assert.Equal(row.Letter, back.Letter);
        Assert.Equal(row.Note, back.Note);
        Assert.Equal(row.Raw, back.Raw);
        Assert.Equal(row.Day, back.Day);
        Assert.Equal(row.Moment, back.Moment);
        Assert.Equal(row.Stamp, back.Stamp);
        Assert.Equal(row.Key, back.Key);

        db.DropTable<TypedRow>();
    }
}
