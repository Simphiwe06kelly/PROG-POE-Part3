using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace POE_Part3

{
    /// <summary>
    /// Handles all MySQL persistence for CyberTask records: database/table setup,
    /// and add / view / complete / delete operations. Instantiate once (e.g. as a
    /// field on ChatViewModel) and reuse it for the lifetime of the app.
    ///
    /// Requires the MySql.Data NuGet package:
    ///   Tools > NuGet Package Manager > Manage NuGet Packages for Solution
    ///   Search "MySql.Data" by Oracle and install it.
    /// </summary>
    public class TaskService
    {
        // ===== EDIT THESE TO MATCH YOUR LOCAL MYSQL SETUP =====
        private const string Server = "localhost";
        private const string User = "root";
        private const string Password = "kelly"; // set your MySQL password here
        private const string Database = "cyberbot_db";
        // ========================================================

        private readonly string _connectionString;

        public TaskService()
        {
            // First connect without a database selected so we can create it if needed.
            string setupConnectionString = $"Server={Server};Uid={User};Pwd={Password};";
            EnsureDatabaseAndTableExist(setupConnectionString);

            // From here on, all operations use the database-scoped connection string.
            _connectionString = $"Server={Server};Uid={User};Pwd={Password};Database={Database};";
        }

        /// <summary>
        /// Creates the database and tasks table if they don't already exist.
        /// Called automatically on startup, so no manual SQL setup is required
        /// beyond having a MySQL server running and reachable.
        /// </summary>
        private void EnsureDatabaseAndTableExist(string setupConnectionString)
        {
            using (var connection = new MySqlConnection(setupConnectionString))
            {
                connection.Open();

                string createDb = $"CREATE DATABASE IF NOT EXISTS {Database};";
                using (var cmd = new MySqlCommand(createDb, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                connection.ChangeDatabase(Database);

                string createTable = @"
                    CREATE TABLE IF NOT EXISTS tasks (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(255) NOT NULL,
                        Description TEXT,
                        ReminderDate DATETIME NULL,
                        IsCompleted BOOLEAN NOT NULL DEFAULT FALSE,
                        CreatedDate DATETIME NOT NULL
                    );";
                using (var cmd = new MySqlCommand(createTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Inserts a new task and returns the same object with its
        /// database-generated Id populated.
        /// </summary>
        public CyberTask AddTask(CyberTask task)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string insert = @"
                    INSERT INTO tasks (Title, Description, ReminderDate, IsCompleted, CreatedDate)
                    VALUES (@title, @description, @reminderDate, @isCompleted, @createdDate);
                    SELECT LAST_INSERT_ID();";

                using (var cmd = new MySqlCommand(insert, connection))
                {
                    cmd.Parameters.AddWithValue("@title", task.Title);
                    cmd.Parameters.AddWithValue("@description", task.Description ?? "");
                    cmd.Parameters.AddWithValue("@reminderDate",
                        task.ReminderDate.HasValue ? (object)task.ReminderDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@isCompleted", task.IsCompleted);
                    cmd.Parameters.AddWithValue("@createdDate", task.CreatedDate);

                    var result = cmd.ExecuteScalar();
                    task.Id = Convert.ToInt32(result);
                }
            }
            return task;
        }

        /// <summary>
        /// Returns every task in the database, most recently created first.
        /// </summary>
        public List<CyberTask> GetAllTasks()
        {
            var tasks = new List<CyberTask>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string select = "SELECT * FROM tasks ORDER BY CreatedDate DESC;";

                using (var cmd = new MySqlCommand(select, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(MapReaderToTask(reader));
                    }
                }
            }
            return tasks;
        }

        /// <summary>
        /// Returns only the tasks that are not yet marked complete.
        /// </summary>
        public List<CyberTask> GetPendingTasks()
        {
            var tasks = new List<CyberTask>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string select = "SELECT * FROM tasks WHERE IsCompleted = FALSE ORDER BY CreatedDate DESC;";

                using (var cmd = new MySqlCommand(select, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(MapReaderToTask(reader));
                    }
                }
            }
            return tasks;
        }

        /// <summary>
        /// Marks the task with the given Id as complete. Returns true if a row
        /// was updated (i.e. the task existed).
        /// </summary>
        public bool CompleteTask(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string update = "UPDATE tasks SET IsCompleted = TRUE WHERE Id = @id;";

                using (var cmd = new MySqlCommand(update, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }

        /// <summary>
        /// Deletes the task with the given Id. Returns true if a row was removed.
        /// </summary>
        public bool DeleteTask(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string delete = "DELETE FROM tasks WHERE Id = @id;";

                using (var cmd = new MySqlCommand(delete, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }

        /// <summary>
        /// Fetches a single task by Id, or null if no task with that Id exists.
        /// </summary>
        public CyberTask GetTaskById(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string select = "SELECT * FROM tasks WHERE Id = @id;";

                using (var cmd = new MySqlCommand(select, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToTask(reader);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns tasks whose reminder date is today or earlier and that aren't
        /// yet complete — useful for proactively surfacing due reminders in chat.
        /// </summary>
        public List<CyberTask> GetDueReminders()
        {
            var tasks = new List<CyberTask>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string select = @"
                    SELECT * FROM tasks
                    WHERE IsCompleted = FALSE
                      AND ReminderDate IS NOT NULL
                      AND ReminderDate <= NOW()
                    ORDER BY ReminderDate ASC;";

                using (var cmd = new MySqlCommand(select, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(MapReaderToTask(reader));
                    }
                }
            }
            return tasks;
        }

        private CyberTask MapReaderToTask(MySqlDataReader reader)
        {
            return new CyberTask
            {
                Id = reader.GetInt32("Id"),
                Title = reader.GetString("Title"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? ""
                    : reader.GetString("Description"),
                ReminderDate = reader.IsDBNull(reader.GetOrdinal("ReminderDate"))
                    ? (DateTime?)null
                    : reader.GetDateTime("ReminderDate"),
                IsCompleted = reader.GetBoolean("IsCompleted"),
                CreatedDate = reader.GetDateTime("CreatedDate")
            };
        }
    }
}