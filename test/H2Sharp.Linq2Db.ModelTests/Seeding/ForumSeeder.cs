/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;
using LinqToDB.Data;

namespace H2Sharp.Linq2Db.ModelTests.Seeding;

/// <summary>
///     Puts the data of the forum into an empty database.
/// </summary>
/// <remarks>
///     Every test that asks a question about the forum starts from the same rows, so that the
///     answer is the same every time and can be written down.
/// </remarks>
public static class ForumSeeder
{
    /// <summary>
    ///     Fills the database with the forum.
    /// </summary>
    /// <param name="db">The connection to the empty database.</param>
    public static void Fill(DataConnection db)
    {
        List<int> forumIds = Insert(db, ForumData.Forums(), identity: true);
        List<int> userIds = Insert(db, ForumData.Users(), identity: true);
        List<int> tagIds = Insert(db, ForumData.Tags(), identity: true);

        Insert(db, ForumData.UserProfiles(userIds), identity: false);

        List<int> topicIds = Insert(db, ForumData.Topics(), identity: true);

        Insert(db, ForumData.TopicStates(topicIds), identity: false);

        List<int> postIds = Insert(db, ForumData.Posts(), identity: true);

        Insert(db, ForumData.TopicTags(topicIds, tagIds), identity: false);

        Insert(db, ForumData.Votes(postIds), identity: true);
    }

    /// <summary>
    ///     Puts the rows of one table into the database, one row at a time, and gives back the
    ///     keys the identity column gave out.
    /// </summary>
    /// <typeparam name="TEntity">The type of the rows.</typeparam>
    /// <param name="db">The connection to the database.</param>
    /// <param name="rows">The rows to put in.</param>
    /// <param name="identity">
    ///     Whether the table has an identity column: the rows of a table with one are put in
    ///     with the key the engine gives, and the rows of a table without one carry the key
    ///     with them.
    /// </param>
    /// <returns>The keys of the rows, in the order they were put in.</returns>
    private static List<int> Insert<TEntity>(DataConnection db, List<TEntity> rows, bool identity)
        where TEntity : class
    {
        List<int> keys = [];

        foreach (TEntity row in rows)
        {
            object key = identity ? db.InsertWithIdentity(row) : db.Insert(row);
            keys.Add(Convert.ToInt32(key));
        }

        return keys;
    }
}
