/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;
using LinqToDB.Async;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The rows of the forum are asked for without waiting.
/// </summary>
/// <remarks>
///     The answers are the same as the answers of the same questions asked at once: the tests
///     here ask the same questions as the query tests, and expect the same.
/// </remarks>
public class ForumAsyncTests
{
    [Fact]
    public async Task The_rows_come_back_without_waiting()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_come_back_without_waiting));

        List<string> titles = await db.GetTable<Topic>()
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToListAsync();

        Assert.Equal(7, titles.Count);
        Assert.Equal("What a compiler does with the source", titles[0]);
    }

    [Fact]
    public async Task The_number_of_the_rows_is_counted_without_waiting()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_counted_without_waiting));

        int count = await db.GetTable<Topic>().CountAsync();

        Assert.Equal(7, count);
    }

    [Fact]
    public async Task The_first_row_comes_back_without_waiting()
    {
        using var db = ForumDatabase.Filled(nameof(The_first_row_comes_back_without_waiting));

        Topic topic = await db.GetTable<Topic>().OrderBy(t => t.Id).FirstAsync();

        Assert.Equal("What a compiler does with the source", topic.Title);
    }

    [Fact]
    public async Task The_rows_are_asked_for_by_their_name_without_waiting()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_by_their_name_without_waiting));

        List<string> titles = await db.GetTable<Topic>()
            .Where(t => t.Title.Contains("compiler"))
            .Select(t => t.Title)
            .ToListAsync();

        Assert.Equal(["What a compiler does with the source"], titles);
    }

    [Fact]
    public async Task The_rows_are_put_in_without_waiting()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_put_in_without_waiting));

        await db.InsertAsync(new Tag { Name = "async" });

        Assert.Equal(5, db.GetTable<Tag>().Count());
        Assert.Equal("async", db.GetTable<Tag>().Single(t => t.Name == "async").Name);
    }

    [Fact]
    public async Task The_row_is_thrown_away_without_waiting()
    {
        using var db = ForumDatabase.Filled(nameof(The_row_is_thrown_away_without_waiting));

        Topic topic = await db.GetTable<Topic>().OrderBy(t => t.Id).FirstAsync();

        await db.DeleteAsync(topic);

        Assert.Equal(6, db.GetTable<Topic>().Count());
    }
}
