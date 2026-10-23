/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.H2Sharp;
using LinqToDB.SchemaProvider;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The catalogue of H2 as the provider reads it: tables, columns, keys.
/// </summary>
public class ForumSchemaTests
{
    [Fact]
    public void The_catalog_lists_the_tables_and_their_columns()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalog_lists_the_tables_and_their_columns));

        DatabaseSchema schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

        Assert.Contains(schema.Tables, t => t.TableName == "Forums");
        Assert.Contains(schema.Tables, t => t.TableName == "Topics");

        var topics = schema.Tables.Single(t => t.TableName == "Topics");
        Assert.Contains(topics.Columns, c => c.ColumnName == "Title");
        Assert.Contains(topics.Columns, c => c.ColumnName == "ViewCount" && !c.IsNullable);
        Assert.Contains(topics.Columns, c => c.ColumnName == "Id" && c.IsIdentity);
    }

    [Fact]
    public void The_catalogue_lists_their_types_for_typed_columns()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_lists_their_types_for_typed_columns));

        db.CreateTable<TypedRow>();

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            var table = schema.Tables.Single(t => t.TableName == "TypedRows");
            var byName = table.Columns.ToDictionary(c => c.ColumnName);

            // The catalogue spells the precision the engine reports: the provider
            // writes the type the way H2 writes it in its own catalogue.
            Assert.Equal("SMALLINT(16, 0)", byName["Tiny"].ColumnType);
            Assert.Equal("REAL(24)", byName["Real"].ColumnType);
            Assert.Equal("TIMESTAMP WITH TIME ZONE", byName["Stamp"].ColumnType);
            Assert.Equal("UUID", byName["Key"].ColumnType);
            Assert.Equal("NUMERIC(18, 2)", byName["Money"].ColumnType);
            Assert.True(byName["Id"].IsIdentity);
        }
        finally
        {
            db.DropTable<TypedRow>();
        }
    }

    [LinqToDB.Mapping.Table("TypedRows")]
    private sealed class TypedRow
    {
        [LinqToDB.Mapping.Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
        public int Id { get; set; }

        [LinqToDB.Mapping.Column("Tiny")]
        public byte Tiny { get; set; }

        [LinqToDB.Mapping.Column("Real")]
        public float Real { get; set; }

        [LinqToDB.Mapping.Column("Money", Precision = 18, Scale = 2)]
        public decimal Money { get; set; }

        [LinqToDB.Mapping.Column("Stamp")]
        public DateTimeOffset Stamp { get; set; }

        [LinqToDB.Mapping.Column("Key")]
        public Guid Key { get; set; }
    }

    [Fact]
    public void The_catalogue_names_every_h2_type()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_names_every_h2_type));

        db.Execute("""
            CREATE TABLE "ExoticTypes" (
                "AChar" CHAR, "AText" CLOB, "ABlob" BLOB, "ABin" BINARY(10), "ATiny" TINYINT,
                "ABig" BIGINT, "ADouble" DOUBLE, "ADate" DATE, "ATime" TIME, "ADecfloat" DECFLOAT,
                "AJson" JSON)
            """);

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            var table = schema.Tables.Single(t => t.TableName == "ExoticTypes");
            var byName = table.Columns.ToDictionary(c => c.ColumnName);

        Assert.Equal(LinqToDB.DataType.Char, byName["AChar"].DataType);
        Assert.Equal(LinqToDB.DataType.Text, byName["AText"].DataType);
        Assert.Equal(LinqToDB.DataType.Blob, byName["ABlob"].DataType);
        Assert.Equal(LinqToDB.DataType.Binary, byName["ABin"].DataType);
        Assert.Equal(LinqToDB.DataType.SByte, byName["ATiny"].DataType);
            Assert.Equal(LinqToDB.DataType.Int64, byName["ABig"].DataType);
            Assert.Equal(LinqToDB.DataType.Double, byName["ADouble"].DataType);
            Assert.Equal(LinqToDB.DataType.Date, byName["ADate"].DataType);
            Assert.Equal(LinqToDB.DataType.Time, byName["ATime"].DataType);
            Assert.Equal(LinqToDB.DataType.DecFloat, byName["ADecfloat"].DataType);
            Assert.Equal(LinqToDB.DataType.Json, byName["AJson"].DataType);
        }
        finally
        {
            db.Execute("""DROP TABLE "ExoticTypes" """);
        }
    }

    [Fact]
    public void The_catalogue_reads_a_column_whose_name_starts_with_a_digit()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_reads_a_column_whose_name_starts_with_a_digit));

        // H2 2.5 quotes any name, including one that starts with a digit: the port
        // expects an identifier there instead.
        db.Execute("""CREATE TABLE "Digits" ("1st" INTEGER)""");

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            Assert.Contains(
                schema.Tables.Single(t => t.TableName == "Digits").Columns,
                c => c.ColumnName == "1st");
        }
        finally
        {
            db.Execute("""DROP TABLE "Digits" """);
        }
    }

    [Fact]
    public void The_catalogue_skips_the_excluded_schema()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_skips_the_excluded_schema));

        var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(
            db, new GetSchemaOptions { ExcludedSchemas = ["PUBLIC"] });

        Assert.DoesNotContain(schema.Tables, t => t.SchemaName == "PUBLIC");
    }

    [Fact]
    public void The_catalogue_lists_no_procedures()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_lists_no_procedures));

        var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(
            db, new GetSchemaOptions { GetProcedures = true });

        Assert.Empty(schema.Procedures);
    }

    [Fact]
    public void The_catalogue_reads_a_reference_between_tables()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_reads_a_reference_between_tables));

        db.Execute("""
            CREATE TABLE "Parents" ("Id" INT NOT NULL, CONSTRAINT "PK_Parents" PRIMARY KEY ("Id"))
            """);
        db.Execute("""
            CREATE TABLE "Children" ("Id" INT NOT NULL, "ParentId" INT,
                CONSTRAINT "FK_Children_Parents" FOREIGN KEY ("ParentId") REFERENCES "Parents" ("Id"))
            """);

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            var children = schema.Tables.Single(t => t.TableName == "Children");
            var key = Assert.Single(children.ForeignKeys);

            Assert.Equal("FK_Children_Parents", key.KeyName);
            Assert.Equal("Parents", key.OtherTable.TableName);
            Assert.Equal(["ParentId"], key.ThisColumns.Select(c => c.ColumnName));
            Assert.Equal(["Id"], key.OtherColumns.Select(c => c.ColumnName));
            Assert.True(key.CanBeNull);
        }
        finally
        {
            db.Execute("""DROP TABLE "Children" """);
            db.Execute("""DROP TABLE "Parents" """);
        }
    }

    [Fact]
    public void The_catalogue_reads_the_comments_on_tables_and_columns()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_reads_the_comments_on_tables_and_columns));

        db.Execute("""CREATE TABLE "Commented" ("Id" INT)""");
        db.Execute("""COMMENT ON TABLE "Commented" IS 'A commented table'""");
        db.Execute("""COMMENT ON COLUMN "Commented"."Id" IS 'A commented column'""");

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            var table = schema.Tables.Single(t => t.TableName == "Commented");
            Assert.Equal("A commented table", table.Description);
            Assert.Equal("A commented column", table.Columns.Single(c => c.ColumnName == "Id").Description);
        }
        finally
        {
            db.Execute("""DROP TABLE "Commented" """);
        }
    }

    [Fact]
    public void The_catalogue_reads_a_time_zone_an_interval_an_enum_and_a_geometry()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_reads_a_time_zone_an_interval_an_enum_and_a_geometry));

        db.Execute("""
            CREATE TABLE "MoreExotic" (
                "ATz" TIME WITH TIME ZONE, "AInterval" INTERVAL DAY TO SECOND,
                "AEnum" ENUM('A', 'B'), "AGeom" GEOMETRY)
            """);

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            var table = schema.Tables.Single(t => t.TableName == "MoreExotic");
            var byName = table.Columns.ToDictionary(c => c.ColumnName);

            Assert.Equal(LinqToDB.DataType.TimeTZ, byName["ATz"].DataType);
            Assert.Equal(typeof(DateTimeOffset), byName["ATz"].SystemType);
            Assert.Equal(LinqToDB.DataType.Interval, byName["AInterval"].DataType);
            Assert.Equal(typeof(TimeSpan), byName["AInterval"].SystemType);
            Assert.Equal(LinqToDB.DataType.Enum, byName["AEnum"].DataType);
            Assert.Equal(typeof(string), byName["AEnum"].SystemType);
            Assert.Equal(LinqToDB.DataType.Blob, byName["AGeom"].DataType);
            Assert.Equal(typeof(byte[]), byName["AGeom"].SystemType);
        }
        finally
        {
            db.Execute("""DROP TABLE "MoreExotic" """);
        }
    }

    [Fact]
    public void The_catalogue_makes_a_member_name_of_a_column_starting_with_a_digit()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_makes_a_member_name_of_a_column_starting_with_a_digit));

        db.Execute("""CREATE TABLE "Digits" ("1st" INTEGER)""");

        try
        {
            var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(db);

            var column = schema.Tables.Single(t => t.TableName == "Digits").Columns.Single();
            Assert.Equal("_1st", column.MemberName);
        }
        finally
        {
            db.Execute("""DROP TABLE "Digits" """);
        }
    }

    [Fact]
    public void The_catalogue_returns_the_catalogue_schema_when_asked()
    {
        using var db = ForumDatabase.Filled(nameof(The_catalogue_returns_the_catalogue_schema_when_asked));

        var schema = ((H2SharpProvider)db.DataProvider).GetSchemaProvider().GetSchema(
            db, new GetSchemaOptions { IncludedSchemas = ["INFORMATION_SCHEMA"] });

        Assert.Contains(schema.Tables, t => t.SchemaName == "INFORMATION_SCHEMA");
    }
}
