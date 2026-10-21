/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;
using H2Sharp.Linq2Db.ModelTests.Seeding;

using LinqToDB;
using LinqToDB.Data;

using LinqToDB.DataProvider.H2Sharp;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     A database of the forum, empty or filled, and the connection over it.
/// </summary>
/// <remarks>
///     <para>
///         The database is in memory and belongs to the test alone: the name of it is the name
///         of the test, so two tests never see each other's rows.
///     </para>
///     <para>
///         The tables are made by the provider, out of the mapping of the model, and the rows are
///         put in by the provider too, so the model is written once and every test works with
///         the same tables.
///     </para>
/// </remarks>
public static class ForumDatabase
{
    /// <summary>
    ///     A database with the tables of the forum and without the rows.
    /// </summary>
    /// <param name="name">The name of the test: the database is named after it.</param>
    /// <returns>The connection to the database.</returns>
    public static DataConnection Empty(string name)
    {
        var db = Open(name);

        CreateTables(db);

        return db;
    }

    /// <summary>
    ///     A database with the forum in it: the tables are made and the rows are put in.
    /// </summary>
    /// <param name="name">The name of the test: the database is named after it.</param>
    /// <returns>The connection to the database.</returns>
    public static DataConnection Filled(string name)
    {
        var db = Empty(name);

        ForumSeeder.Fill(db);

        return db;
    }

    /// <summary>
    ///     Opens a database that belongs to the test alone.
    /// </summary>
    /// <param name="name">The name of the test: the database is named after it.</param>
    /// <returns>The connection to the database.</returns>
    public static DataConnection Open(string name)
        => H2SharpTools.CreateDataConnection($"jdbc:h2:mem:{name}");

    /// <summary>
    ///     Makes the tables of the forum, the way the provider writes them.
    /// </summary>
    /// <param name="db">The connection to the database.</param>
    private static void CreateTables(DataConnection db)
    {
        db.CreateTable<Forum>();
        db.CreateTable<User>();
        db.CreateTable<UserProfile>();
        db.CreateTable<Topic>();
        db.CreateTable<TopicState>();
        db.CreateTable<Post>();
        db.CreateTable<Tag>();
        db.CreateTable<TopicTag>();
        db.CreateTable<Vote>();
    }
}
