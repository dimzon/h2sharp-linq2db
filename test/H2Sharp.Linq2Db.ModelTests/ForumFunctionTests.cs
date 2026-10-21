/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The functions of H2 are asked for through the model.
/// </summary>
/// <remarks>
///     <para>
///         The functions are the ones an application uses every day: the functions of the text,
///         the functions of the numbers and the functions of the dates. The reference was asked
///         about the forms the provider writes.
///     </para>
/// </remarks>
public class ForumFunctionTests
{
    [Fact]
    public void The_text_is_asked_for_by_a_word_in_it()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_asked_for_by_a_word_in_it));

        int topics = db.GetTable<Topic>().Count(t => t.Title.Contains("query"));

        Assert.Equal(1, topics);
    }

    [Fact]
    public void The_place_of_a_word_in_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_place_of_a_word_in_the_text_is_asked_for));

        int place = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.IndexOf("query"))
            .First();

        Assert.Equal(10, place);
    }

    [Fact]
    public void The_beginning_of_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_beginning_of_the_text_is_asked_for));

        string beginning = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Substring(0, 7))
            .First();

        Assert.Equal("Reading", beginning);
    }

    [Fact]
    public void The_middle_of_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_middle_of_the_text_is_asked_for));

        string middle = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Substring(8, 5))
            .First();

        Assert.Equal("a que", middle);
    }

    [Fact]
    public void The_length_of_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_length_of_the_text_is_asked_for));

        int length = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Length)
            .First();

        Assert.Equal(20, length);
    }

    [Fact]
    public void The_text_is_asked_for_in_the_upper_case()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_asked_for_in_the_upper_case));

        string name = db.GetTable<User>()
            .Where(u => u.UserName == "ada")
            .Select(u => u.UserName.ToUpper())
            .First();

        Assert.Equal("ADA", name);
    }

    [Fact]
    public void The_text_is_asked_for_in_the_lower_case()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_asked_for_in_the_lower_case));

        string name = db.GetTable<User>()
            .Where(u => u.UserName == "ada")
            .Select(u => u.UserName.ToLower())
            .First();

        Assert.Equal("ada", name);
    }

    [Fact]
    public void The_spaces_around_the_text_are_cut_off()
    {
        using var db = ForumDatabase.Filled(nameof(The_spaces_around_the_text_are_cut_off));

        string trimmed = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => ("  " + t.Title + "  ").Trim())
            .First();

        Assert.Equal("Reading a query plan", trimmed);
    }

    [Fact]
    public void A_part_of_the_text_is_replaced_with_another()
    {
        using var db = ForumDatabase.Filled(nameof(A_part_of_the_text_is_replaced_with_another));

        string replaced = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Replace("query", "plan of the query"))
            .First();

        Assert.Equal("Reading a plan of the query plan", replaced);
    }

    [Fact]
    public void The_beginning_of_the_text_is_taken()
    {
        using var db = ForumDatabase.Filled(nameof(The_beginning_of_the_text_is_taken));

        string left = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Substring(0, 7))
            .First();

        Assert.Equal("Reading", left);
    }

    [Fact]
    public void The_two_texts_are_joined()
    {
        using var db = ForumDatabase.Filled(nameof(The_two_texts_are_joined));

        string joined = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title + "!")
            .First();

        Assert.Equal("Reading a query plan!", joined);
    }

    [Fact]
    public void The_number_is_asked_for_without_its_sign()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_is_asked_for_without_its_sign));

        int views = db.GetTable<Topic>()
            .Where(t => t.ViewCount == 1200)
            .Select(t => Math.Abs(t.ViewCount - 1200))
            .First();

        Assert.Equal(0, views);
    }

    [Fact]
    public void The_number_is_rounded()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_is_rounded));

        int rounded = db.GetTable<Topic>()
            .Where(t => t.ViewCount == 1200)
            .Select(t => (int)Math.Round(t.ViewCount / 3.0))
            .First();

        Assert.Equal(400, rounded);
    }

    [Fact]
    public void The_year_of_the_date_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_year_of_the_date_is_asked_for));

        int years = db.GetTable<Topic>().Count(t => t.CreatedAt.Year == 2024);

        Assert.Equal(7, years);
    }

    [Fact]
    public void The_month_of_the_date_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_month_of_the_date_is_asked_for));

        int months = db.GetTable<Topic>().Count(t => t.CreatedAt.Month == 1);

        Assert.Equal(4, months);
    }

    [Fact]
    public void The_day_of_the_date_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_day_of_the_date_is_asked_for));

        int days = db.GetTable<Topic>().Count(t => t.CreatedAt.Day == 11);

        Assert.Equal(1, days);
    }

    [Fact]
    public void The_date_is_moved_by_a_day()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_moved_by_a_day));

        DateTime moved = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.CreatedAt.AddDays(1))
            .First();

        Assert.Equal(new DateTime(2024, 1, 17, 9, 0, 0), moved);
    }

    [Fact]
    public void The_date_is_moved_by_a_month()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_moved_by_a_month));

        DateTime moved = db.GetTable<Topic>()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.CreatedAt.AddMonths(1))
            .First();

        Assert.Equal(new DateTime(2024, 2, 16, 9, 0, 0), moved);
    }

    [Fact]
    public void The_empty_value_is_replaced_with_another()
    {
        using var db = ForumDatabase.Filled(nameof(The_empty_value_is_replaced_with_another));

        string note = db.GetTable<TopicState>()
            .Where(s => s.ModeratorNote == null)
            .Select(s => s.ModeratorNote ?? "no note")
            .First();

        Assert.Equal("no note", note);
    }

    [Fact]
    public void The_value_is_asked_for_by_the_condition()
    {
        using var db = ForumDatabase.Filled(nameof(The_value_is_asked_for_by_the_condition));

        string title = db.GetTable<Topic>()
            .Where(t => t.ViewCount > 1000)
            .Select(t => t.ViewCount > 1000 ? "many" : "few")
            .First();

        Assert.Equal("many", title);
    }
}
