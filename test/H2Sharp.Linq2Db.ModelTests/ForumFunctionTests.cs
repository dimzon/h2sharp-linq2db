/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;
using H2Sharp.Linq2Db.ModelTests.Seeding;

using LinqToDB;
using LinqToDB.DataProvider.H2Sharp;

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

        int topics = db.Topics().Count(t => t.Title.Contains("query"));

        Assert.Equal(1, topics);
    }

    [Fact]
    public void The_place_of_a_word_in_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_place_of_a_word_in_the_text_is_asked_for));

        int place = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.IndexOf("query"))
            .First();

        Assert.Equal(10, place);
    }

    [Fact]
    public void The_beginning_of_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_beginning_of_the_text_is_asked_for));

        string beginning = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Substring(0, 7))
            .First();

        Assert.Equal("Reading", beginning);
    }

    [Fact]
    public void The_middle_of_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_middle_of_the_text_is_asked_for));

        string middle = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Substring(8, 5))
            .First();

        Assert.Equal("a que", middle);
    }

    [Fact]
    public void The_length_of_the_text_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_length_of_the_text_is_asked_for));

        int length = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Length)
            .First();

        Assert.Equal(20, length);
    }

    [Fact]
    public void The_text_is_asked_for_in_the_upper_case()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_asked_for_in_the_upper_case));

        string name = db.Users()
            .Where(u => u.UserName == "ada")
            .Select(u => u.UserName.ToUpper())
            .First();

        Assert.Equal("ADA", name);
    }

    [Fact]
    public void The_text_is_asked_for_in_the_lower_case()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_asked_for_in_the_lower_case));

        string name = db.Users()
            .Where(u => u.UserName == "ada")
            .Select(u => u.UserName.ToLower())
            .First();

        Assert.Equal("ada", name);
    }

    [Fact]
    public void The_spaces_around_the_text_are_cut_off()
    {
        using var db = ForumDatabase.Filled(nameof(The_spaces_around_the_text_are_cut_off));

        string trimmed = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => ("  " + t.Title + "  ").Trim())
            .First();

        Assert.Equal("Reading a query plan", trimmed);
    }

    [Fact]
    public void A_part_of_the_text_is_replaced_with_another()
    {
        using var db = ForumDatabase.Filled(nameof(A_part_of_the_text_is_replaced_with_another));

        string replaced = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Replace("query", "plan of the query"))
            .First();

        Assert.Equal("Reading a plan of the query plan", replaced);
    }

    [Fact]
    public void The_beginning_of_the_text_is_taken()
    {
        using var db = ForumDatabase.Filled(nameof(The_beginning_of_the_text_is_taken));

        string left = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.Substring(0, 7))
            .First();

        Assert.Equal("Reading", left);
    }

    [Fact]
    public void The_two_texts_are_joined()
    {
        using var db = ForumDatabase.Filled(nameof(The_two_texts_are_joined));

        string joined = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title + "!")
            .First();

        Assert.Equal("Reading a query plan!", joined);
    }

    [Fact]
    public void The_number_is_asked_for_without_its_sign()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_is_asked_for_without_its_sign));

        int views = db.Topics()
            .Where(t => t.ViewCount == 1200)
            .Select(t => Math.Abs(t.ViewCount - 1200))
            .First();

        Assert.Equal(0, views);
    }

    [Fact]
    public void The_number_is_rounded()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_is_rounded));

        int rounded = db.Topics()
            .Where(t => t.ViewCount == 1200)
            .Select(t => (int)Math.Round(t.ViewCount / 3.0))
            .First();

        Assert.Equal(400, rounded);
    }

    [Fact]
    public void The_view_counts_are_combined_with_bit_operations()
    {
        using var db = ForumDatabase.Filled(nameof(The_view_counts_are_combined_with_bit_operations));

        // H2 2.5 has no "&", "|" or "^": the reference reads BITAND, BITOR and BITXOR
        // instead, and the provider writes those.
        var query = db.Topics().OrderBy(t => t.Id).Select(
            t => new
            {
                And = t.ViewCount & 6,
                Or = t.ViewCount | 1,
                Xor = t.ViewCount ^ 3,
            }).ToList();

        Assert.Equal(
            ForumData.Topics().OrderBy(t => t.Id).Select(t => new
            {
                And = t.ViewCount & 6,
                Or = t.ViewCount | 1,
                Xor = t.ViewCount ^ 3,
            }).ToList(),
            query.Select(x => new { x.And, x.Or, x.Xor }).ToList());
    }

    [Fact]
    public void The_bits_of_two_numbers_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_bits_of_two_numbers_are_asked_for));

        long and = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.BitAnd(t.ViewCount, 6L)).First();
        long or = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.BitOr(t.ViewCount, 1L)).First();
        long xor = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.BitXor(t.ViewCount, 3L)).First();
        long not = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.BitNot(t.ViewCount)).First();

        Assert.Equal(1200L & 6L, and);
        Assert.Equal(1200L | 1L, or);
        Assert.Equal(1200L ^ 3L, xor);
        Assert.Equal(~1200L, not);
    }

    [Fact]
    public void The_number_is_moved_left_and_right()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_is_moved_left_and_right));

        long left = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.LeftShift(t.ViewCount, 1)).First();
        long right = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.RightShift(t.ViewCount, 1)).First();
        long count = db.Topics().Where(t => t.ViewCount == 1200).Select(t => H2SharpSpecificExtensions.BitCount(t.ViewCount)).First();

        Assert.Equal(1200L << 1, left);
        Assert.Equal(1200L >> 1, right);
        Assert.Equal(4L, count);
    }

    [Fact]
    public void The_balances_are_rounded_the_way_dotnet_rounds()
    {
        using var db = ForumDatabase.Filled(nameof(The_balances_are_rounded_the_way_dotnet_rounds));

        // Math.Round answers with .NET's numbers (12 for 12.50): H2's ROUND breaks
        // ties the other way, so the check is against what .NET answers.
        List<decimal> rounded = db.UserProfiles().OrderBy(p => p.UserId).Select(p => Math.Round(p.Balance)).ToList();

        Assert.Equal([12m, 3m, 0m, 8m, 0m], rounded);
    }

    [Fact]
    public void A_date_minus_itself_is_not_translated()
    {
        using var db = ForumDatabase.Filled(nameof(A_date_minus_itself_is_not_translated));

        // A difference of dates has no unit, and H2 counts crossed unit boundaries
        // with DATEDIFF instead: the provider declines the subtraction and names
        // Sql.DateDiff, which answers the question that has a unit.
        var refused = Assert.Throws<LinqToDB.LinqToDBException>(
            () => db.Topics()
                .Where(t => t.Title == "Reading a query plan")
                .Select(t => (t.CreatedAt - t.CreatedAt).TotalDays)
                .ToList());

        Assert.Contains("DateDiff", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_search_is_case_insensitive_without_case_functions()
    {
        using var db = ForumDatabase.Filled(nameof(The_search_is_case_insensitive_without_case_functions));

        List<string> titles = db.Topics()
            .Where(t => t.Title.Contains("THE", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Title)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "The cost of an abstraction",
                "The forum moves",
                "Transactions and the order of the writes",
                "What a compiler does with the source",
            ],
            titles);
    }

    [Fact]
    public void The_dates_are_built_from_their_parts()
    {
        using var db = ForumDatabase.Filled(nameof(The_dates_are_built_from_their_parts));

        // H2 2.5 has no make_date or make_timestamp: the parts are concatenated and
        // cast, the way a timestamp literal reads.
        List<string> titles = db.Topics()
            .Where(t => t.CreatedAt >= new DateTime(t.CreatedAt.Year, 1, 1))
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(7, titles.Count);

        DateOnly first = db.Topics()
            .OrderBy(t => t.Id)
            .Select(t => new DateOnly(t.CreatedAt.Year, t.CreatedAt.Month, 1))
            .First();

        Assert.Equal(new DateOnly(2024, 1, 1), first);
    }

    [Fact]
    public void The_text_is_repeated_and_cut()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_repeated_and_cut));

        string repeated = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Repeat("ab", 3))
            .First();

        Assert.Equal("ababab", repeated);

        string spaced = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Space(3))
            .First();

        Assert.Equal("   ", spaced);
    }

    [Fact]
    public void The_character_of_a_code_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_character_of_a_code_is_asked_for));

        string ch = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Chr(65) + H2SharpSpecificExtensions.Char(66))
            .First();

        Assert.Equal("AB", ch);
    }

    [Fact]
    public void The_names_that_sound_close_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_names_that_sound_close_are_asked_for));

        int closeness = db.Users()
            .Where(u => u.UserName == "ada")
            .Select(u => H2SharpSpecificExtensions.SoundexDifference(u.UserName, "ada"))
            .Single();

        Assert.Equal(4, closeness);
    }

    [Fact]
    public void The_texts_are_inserted_into_each_other()
    {
        using var db = ForumDatabase.Filled(nameof(The_texts_are_inserted_into_each_other));

        string inserted = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Insert(t.Title, 1, 7, "Scanning"))
            .First();

        Assert.Equal("Scanning a query plan", inserted);
    }

    [Fact]
    public void The_day_of_the_week_of_a_known_date_is_asked()
    {
        using var db = ForumDatabase.Filled(nameof(The_day_of_the_week_of_a_known_date_is_asked));

        // A date the query writes itself is folded before the translation:
        // the 1st of January 2024 was a Monday.
        DayOfWeek monday = db.Topics().Where(t => t.Id == 1).Select(t => new DateTime(2024, 1, 1).DayOfWeek).First();

        Assert.Equal(DayOfWeek.Monday, monday);
    }

    [Fact]
    public void The_year_of_the_date_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_year_of_the_date_is_asked_for));

        int years = db.Topics().Count(t => t.CreatedAt.Year == 2024);

        Assert.Equal(7, years);
    }

    [Fact]
    public void The_month_of_the_date_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_month_of_the_date_is_asked_for));

        int months = db.Topics().Count(t => t.CreatedAt.Month == 1);

        Assert.Equal(4, months);
    }

    [Fact]
    public void The_day_of_the_date_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_day_of_the_date_is_asked_for));

        int days = db.Topics().Count(t => t.CreatedAt.Day == 11);

        Assert.Equal(1, days);
    }

    [Fact]
    public void The_largest_and_the_smallest_view_counts_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_largest_and_the_smallest_view_counts_are_asked_for));

        int largest = db.Topics().Where(t => t.ViewCount == 1200).Select(t => Math.Max(t.ViewCount, 1000)).First();
        int smallest = db.Topics().Where(t => t.ViewCount == 1200).Select(t => Math.Min(t.ViewCount, 1000)).First();

        Assert.Equal(1200, largest);
        Assert.Equal(1000, smallest);
    }

    [Fact]
    public void The_place_of_a_word_from_a_place_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_place_of_a_word_from_a_place_is_asked_for));

        int place = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.Title.IndexOf("e", 2))
            .First();

        Assert.Equal(12, place);
    }

    [Fact]
    public void The_place_of_a_word_is_asked_for_with_a_start()
    {
        using var db = ForumDatabase.Filled(nameof(The_place_of_a_word_is_asked_for_with_a_start));

        int place = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Locate("plan", t.Title, 5))
            .First();

        Assert.Equal(17, place);
    }

    [Fact]
    public void The_text_is_matched_without_case()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_matched_without_case));

        List<string> titles = db.Topics()
            .Where(t => H2SharpSpecificExtensions.ILike(t.Title, "the%"))
            .OrderBy(t => t.Title)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(["The cost of an abstraction", "The forum moves"], titles);
    }

    [Fact]
    public void The_texts_are_joined_with_a_separator()
    {
        using var db = ForumDatabase.Filled(nameof(The_texts_are_joined_with_a_separator));

        string joined = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.ConcatWithSeparator(" | ", t.Title, "notes", "more"))
            .First();

        Assert.Equal("Reading a query plan | notes | more", joined);

        string pair = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.ConcatWithSeparator(" | ", t.Title, "notes"))
            .First();

        Assert.Equal("Reading a query plan | notes", pair);
    }

    [Fact]
    public void The_name_that_sounds_like_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_name_that_sounds_like_is_asked_for));

        List<string> names = db.Users()
            .Where(u => H2SharpSpecificExtensions.Soundex(u.UserName) == H2SharpSpecificExtensions.Soundex("ada"))
            .OrderBy(u => u.Id)
            .Select(u => u.UserName)
            .ToList();

        Assert.Contains("ada", names);
    }

    [Fact]
    public void The_characters_of_a_text_are_translated()
    {
        using var db = ForumDatabase.Filled(nameof(The_characters_of_a_text_are_translated));

        string translated = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Translate(t.Title, "aeiou", "AEIOU"))
            .First();

        Assert.Equal("REAdIng A qUEry plAn", translated);
    }

    [Fact]
    public void The_days_between_two_dates_are_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_days_between_two_dates_are_counted));

        long days = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.DateDiff("DAY", new DateTime(2024, 1, 1), t.CreatedAt))
            .First();

        Assert.Equal(15, days);
    }

    [Fact]
    public void The_parts_of_a_date_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_parts_of_a_date_are_asked_for));

        // The parts come from the properties themselves: linq2db asks the provider,
        // and the provider writes EXTRACT with the H2 field.
        var parts = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => new
            {
                t.CreatedAt.Year,
                t.CreatedAt.Month,
                t.CreatedAt.Day,
                t.CreatedAt.Hour,
                t.CreatedAt.Minute,
                t.CreatedAt.Second,
                t.CreatedAt.Millisecond,
                t.CreatedAt.DayOfWeek,
                t.CreatedAt.DayOfYear,
            })
            .First();

        Assert.Equal((2024, 1, 16, 9, 0, 0, 0, DayOfWeek.Tuesday, 16),
            (parts.Year, parts.Month, parts.Day, parts.Hour, parts.Minute, parts.Second, parts.Millisecond, parts.DayOfWeek, parts.DayOfYear));
    }

    [Fact]
    public void The_week_and_the_quarter_of_a_date_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_week_and_the_quarter_of_a_date_are_asked_for));

        var parts = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => new
            {
                Quarter = H2SharpSpecificExtensions.Quarter(t.CreatedAt),
                WeekDay = t.CreatedAt.DayOfWeek,
                DayOfYear = t.CreatedAt.DayOfYear,
            })
            .First();

        Assert.Equal(1, parts.Quarter);
        Assert.Equal(DayOfWeek.Tuesday, parts.WeekDay);
        Assert.Equal(16, parts.DayOfYear);
    }

    [Fact]
    public void The_date_is_moved_by_an_hour()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_moved_by_an_hour));

        DateTime moved = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.CreatedAt.AddHours(2))
            .First();

        Assert.Equal(new DateTime(2024, 1, 16, 11, 0, 0), moved);
    }

    [Fact]
    public void The_text_is_padded_on_both_sides()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_padded_on_both_sides));

        string left = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.LeftPad(t.Title, 25))
            .First();
        string right = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.RightPad(t.Title, 25, "."))
            .First();

        Assert.Equal("     Reading a query plan", left);
        Assert.Equal("Reading a query plan.....", right);
    }

    [Fact]
    public void The_code_of_the_first_character_is_taken()
    {
        using var db = ForumDatabase.Filled(nameof(The_code_of_the_first_character_is_taken));

        int code = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.Ascii(t.Title))
            .First();

        Assert.Equal('R', (char)code);
    }

    [Fact]
    public void The_date_is_cut_to_the_month()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_cut_to_the_month));

        DateTime month = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => H2SharpSpecificExtensions.DateTrunc("MONTH", t.CreatedAt))
            .First();

        Assert.Equal(new DateTime(2024, 1, 1), month);
    }

    [Fact]
    public void The_names_of_the_day_and_the_month_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_names_of_the_day_and_the_month_are_asked_for));

        var names = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => new { Day = H2SharpSpecificExtensions.DayName(t.CreatedAt), Month = H2SharpSpecificExtensions.MonthName(t.CreatedAt) })
            .First();

        Assert.Equal("Tuesday", names.Day);
        Assert.Equal("January", names.Month);
    }

    [Fact]
    public void The_date_is_moved_by_a_day()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_moved_by_a_day));

        DateTime moved = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.CreatedAt.AddDays(1))
            .First();

        Assert.Equal(new DateTime(2024, 1, 17, 9, 0, 0), moved);
    }

    [Fact]
    public void The_date_is_moved_by_a_month()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_moved_by_a_month));

        DateTime moved = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => t.CreatedAt.AddMonths(1))
            .First();

        Assert.Equal(new DateTime(2024, 2, 16, 9, 0, 0), moved);
    }

    [Fact]
    public void The_empty_value_is_replaced_with_another()
    {
        using var db = ForumDatabase.Filled(nameof(The_empty_value_is_replaced_with_another));

        string note = db.TopicStates()
            .Where(s => s.ModeratorNote == null)
            .Select(s => s.ModeratorNote ?? "no note")
            .First();

        Assert.Equal("no note", note);
    }

    [Fact]
    public void The_value_is_asked_for_by_the_condition()
    {
        using var db = ForumDatabase.Filled(nameof(The_value_is_asked_for_by_the_condition));

        string title = db.Topics()
            .Where(t => t.ViewCount > 1000)
            .Select(t => t.ViewCount > 1000 ? "many" : "few")
            .First();

        Assert.Equal("many", title);
    }

    [Fact]
    public void The_parts_of_a_date_are_asked_for_by_the_part_name()
    {
        using var db = ForumDatabase.Filled(nameof(The_parts_of_a_date_are_asked_for_by_the_part_name));

        var parts = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => new
            {
                Year = Sql.DatePart(Sql.DateParts.Year, t.CreatedAt),
                Quarter = Sql.DatePart(Sql.DateParts.Quarter, t.CreatedAt),
                Month = Sql.DatePart(Sql.DateParts.Month, t.CreatedAt),
                Day = Sql.DatePart(Sql.DateParts.Day, t.CreatedAt),
                Week = Sql.DatePart(Sql.DateParts.Week, t.CreatedAt),
                Hour = Sql.DatePart(Sql.DateParts.Hour, t.CreatedAt),
                Minute = Sql.DatePart(Sql.DateParts.Minute, t.CreatedAt),
                Second = Sql.DatePart(Sql.DateParts.Second, t.CreatedAt),
                Millisecond = Sql.DatePart(Sql.DateParts.Millisecond, t.CreatedAt),
                WeekDay = Sql.DatePart(Sql.DateParts.WeekDay, t.CreatedAt),
                DayOfYear = Sql.DatePart(Sql.DateParts.DayOfYear, t.CreatedAt),
            })
            .First();

        Assert.Equal(2024, parts.Year);
        Assert.Equal(1, parts.Quarter);
        Assert.Equal(1, parts.Month);
        Assert.Equal(16, parts.Day);
        Assert.Equal(3, parts.Week);
        Assert.Equal(9, parts.Hour);
        Assert.Equal(0, parts.Minute);
        Assert.Equal(0, parts.Second);
        Assert.Equal(0, parts.Millisecond);
        Assert.Equal(2, parts.WeekDay);
        Assert.Equal(16, parts.DayOfYear);
    }

    [Fact]
    public void The_date_is_moved_by_every_part()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_is_moved_by_every_part));

        var moved = db.Topics()
            .Where(t => t.Title == "Reading a query plan")
            .Select(t => new
            {
                Year = Sql.DateAdd(Sql.DateParts.Year, 1, t.CreatedAt),
                Quarter = Sql.DateAdd(Sql.DateParts.Quarter, 1, t.CreatedAt),
                Month = Sql.DateAdd(Sql.DateParts.Month, 1, t.CreatedAt),
                Week = Sql.DateAdd(Sql.DateParts.Week, 1, t.CreatedAt),
                Hour = Sql.DateAdd(Sql.DateParts.Hour, 1, t.CreatedAt),
                Minute = Sql.DateAdd(Sql.DateParts.Minute, 1, t.CreatedAt),
                Second = Sql.DateAdd(Sql.DateParts.Second, 1, t.CreatedAt),
                Millisecond = Sql.DateAdd(Sql.DateParts.Millisecond, 1, t.CreatedAt),
            })
            .First();

        Assert.Equal(new DateTime(2025, 1, 16, 9, 0, 0), moved.Year);
        Assert.Equal(new DateTime(2024, 4, 16, 9, 0, 0), moved.Quarter);
        Assert.Equal(new DateTime(2024, 2, 16, 9, 0, 0), moved.Month);
        Assert.Equal(new DateTime(2024, 1, 23, 9, 0, 0), moved.Week);
        Assert.Equal(new DateTime(2024, 1, 16, 10, 0, 0), moved.Hour);
        Assert.Equal(new DateTime(2024, 1, 16, 9, 1, 0), moved.Minute);
        Assert.Equal(new DateTime(2024, 1, 16, 9, 0, 1), moved.Second);
        Assert.Equal(new DateTime(2024, 1, 16, 9, 0, 0, 1), moved.Millisecond);
    }
}
