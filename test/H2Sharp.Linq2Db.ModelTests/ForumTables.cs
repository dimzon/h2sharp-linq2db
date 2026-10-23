/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The tables of the forum, named as they are called in the queries.
/// </summary>
public static class ForumTables
{
    /// <summary>
    ///     The forums.
    /// </summary>
    public static ITable<Forum> Forums(this IDataContext db)
        => db.GetTable<Forum>();

    /// <summary>
    ///     The people who write in the forum.
    /// </summary>
    public static ITable<User> Users(this IDataContext db)
        => db.GetTable<User>();

    /// <summary>
    ///     What the people are worth and when they were born.
    /// </summary>
    public static ITable<UserProfile> UserProfiles(this IDataContext db)
        => db.GetTable<UserProfile>();

    /// <summary>
    ///     The topics of the forums.
    /// </summary>
    public static ITable<Topic> Topics(this IDataContext db)
        => db.GetTable<Topic>();

    /// <summary>
    ///     The state of the topics: closed, pinned, with a note.
    /// </summary>
    public static ITable<TopicState> TopicStates(this IDataContext db)
        => db.GetTable<TopicState>();

    /// <summary>
    ///     The answers to the topics.
    /// </summary>
    public static ITable<Post> Posts(this IDataContext db)
        => db.GetTable<Post>();

    /// <summary>
    ///     The tags.
    /// </summary>
    public static ITable<Tag> Tags(this IDataContext db)
        => db.GetTable<Tag>();

    /// <summary>
    ///     The tags of the topics.
    /// </summary>
    public static ITable<TopicTag> TopicTags(this IDataContext db)
        => db.GetTable<TopicTag>();

    /// <summary>
    ///     The votes for the answers.
    /// </summary>
    public static ITable<Vote> Votes(this IDataContext db)
        => db.GetTable<Vote>();
}
