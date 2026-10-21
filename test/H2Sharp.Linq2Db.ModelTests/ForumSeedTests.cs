/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The forum is in the database: the tables are made and the rows are put in.
/// </summary>
public class ForumSeedTests
{
    [Fact]
    public void The_tables_are_made_by_the_provider()
    {
        using var db = ForumDatabase.Empty(nameof(The_tables_are_made_by_the_provider));

        var tables = db.DataProvider
            .GetSchemaProvider()
            .GetSchema(db)
            .Tables
            .Select(t => t.TableName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            [
                "Forums",
                "Posts",
                "Tags",
                "TopicStates",
                "TopicTags",
                "Topics",
                "UserProfiles",
                "Users",
                "Votes",
            ],
            tables);
    }

    [Fact]
    public void The_forum_is_in_the_database()
    {
        using var db = ForumDatabase.Filled(nameof(The_forum_is_in_the_database));

        Assert.Equal(3, db.GetTable<Forum>().Count());
        Assert.Equal(5, db.GetTable<User>().Count());
        Assert.Equal(5, db.GetTable<UserProfile>().Count());
        Assert.Equal(7, db.GetTable<Topic>().Count());
        Assert.Equal(7, db.GetTable<TopicState>().Count());
        Assert.Equal(14, db.GetTable<Post>().Count());
        Assert.Equal(4, db.GetTable<Tag>().Count());
        Assert.Equal(7, db.GetTable<TopicTag>().Count());
        Assert.Equal(5, db.GetTable<Vote>().Count());
    }

    [Fact]
    public void The_rows_keep_what_the_seed_put_in_them()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_keep_what_the_seed_put_in_them));

        var topic = db.GetTable<Topic>().OrderBy(t => t.Id).First();

        Assert.Equal("What a compiler does with the source", topic.Title);
        Assert.Equal(1200, topic.ViewCount);
    }

    [Fact]
    public void The_key_of_a_row_comes_back_from_the_database()
    {
        using var db = ForumDatabase.Filled(nameof(The_key_of_a_row_comes_back_from_the_database));

        var topic = db.GetTable<Topic>().OrderBy(t => t.Id).First();

        Assert.True(topic.Id > 0);
    }
}
