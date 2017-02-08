using SQLite;

namespace Database {
	/// <summary>
	/// Encapsulates database-management code dependant on the underlying platform.
	/// </summary>
	public partial class DatabasePathHelper {
		/// <summary>
		/// Given a base filename, decorate it according to a platform-dependent
		/// database path.
		/// </summary>
		/// <param name="name">
		/// The underlying filename of the database, without any path or extension.
		/// </param>
		static partial void DecorateDatabasePath(ref string name);

		/// <summary>
		/// Decorates the database name with the local path as long as it's not
		/// intended to be saved in-memory, in which case <paramref name="name"/>
		/// is simply returned unchanged.
		/// </summary>
		/// <param name="name">
		/// The underlying filename of the database, without any path or extension.
		/// </param>
		/// <returns>The complete file path of the database.</returns>
		/// <seealso cref="DecorateDatabasePath(ref string)"/>
		public static string MemoryOrLocalPath(string name) {
			if (name == null)
				throw new System.ArgumentNullException("name");
			else if (name.Length == 0)
				throw new System.ArgumentOutOfRangeException("name", "The file name must not be empty");
			else if (name.Equals(":memory:") || name.StartsWith("file::memory:") || name.Contains("mode=memory"))
				return name;
			else {
				DecorateDatabasePath(ref name);
				return name;
			}
		}
	}

	/// <summary>
	/// Represents an open connection to a SQLite database, delegating file path
	/// selection to platform-specific code.
	/// </summary>
	/// <seealso cref="LocalDatabaseAsync"/>
	public class LocalDatabase : SQLiteConnection {
		/// <summary>
		/// Opens or creates a SQLite database at a system path generated from the
		/// given name.
		/// </summary>
		/// <param name="name">
		/// The underlying filename of the database, without any path or extension.
		/// </param>
		/// <seealso cref="DatabasePathHelper.MemoryOrLocalPath(string)"/>
		public LocalDatabase(string name)
			: this(name, (SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create)) { }

		/// <summary>
		/// Opens a SQLite database at a system path generated from the given name.
		/// </summary>
		/// <param name="name">
		/// The underlying filename of the database, without any path or extension.
		/// </param>
		/// <param name="openFlags">
		/// Bit array specifying the nature of the opened connection.
		/// </param>
		/// <seealso cref="DatabasePathHelper.MemoryOrLocalPath(string)"/>
		public LocalDatabase(string name, SQLiteOpenFlags openFlags)
			: base(DatabasePathHelper.MemoryOrLocalPath(name), openFlags, true) { }
	}

	/// <summary>
	/// Represents an open connection to a SQLite database, running I/O in the
	/// background and delegating file path selection to platform-specific code.
	/// </summary>
	/// <seealso cref="LocalDatabase"/>
	public class LocalDatabaseAsync : SQLiteAsyncConnection {
		/// <summary>
		/// Opens or creates a SQLite database at a system path generated from the
		/// given name.
		/// </summary>
		/// <param name="name">
		/// The underlying filename of the database, without any path or extension.
		/// </param>
		/// <seealso cref="DatabasePathHelper.MemoryOrLocalPath(string)"/>
		public LocalDatabaseAsync(string name)
			: this(name, (SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create)) { }

		/// <summary>
		/// Opens a SQLite database at a system path generated from the given name.
		/// </summary>
		/// <param name="name">
		/// The underlying filename of the database, without any path or extension.
		/// </param>
		/// <param name="openFlags">
		/// Bit array specifying the nature of the opened connection.
		/// </param>
		/// <seealso cref="DatabasePathHelper.MemoryOrLocalPath(string)"/>
		public LocalDatabaseAsync(string name, SQLiteOpenFlags openFlags)
			: base(DatabasePathHelper.MemoryOrLocalPath(name), openFlags, true) { }
	}
}
