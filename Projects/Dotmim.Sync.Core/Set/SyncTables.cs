using Dotmim.Sync.DatabaseStringParsers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Dotmim.Sync
{
    /// <summary>
    /// Represents a collection of tables used by the SyncSet.
    /// </summary>
    [CollectionDataContract(Name = "tbls", ItemName = "tbl"), Serializable]
    public class SyncTables : ICollection<SyncTable>, IList<SyncTable>
    {
        /// <summary>
        /// Gets or sets exposing the InnerCollection for serialization purpose.
        /// </summary>
        [DataMember(Name = "c", IsRequired = true, Order = 1)]
        public Collection<SyncTable> InnerCollection { get; set; } = [];

        /// <summary>
        /// Gets table's schema.
        /// </summary>
        [IgnoreDataMember]
        public SyncSet Schema { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncTables"/> class.
        /// Create a default collection for SerializersFactory.
        /// </summary>
        public SyncTables()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncTables"/> class.
        /// Create a new collection of tables for a SyncSchema.
        /// </summary>
        public SyncTables(SyncSet schema) => this.Schema = schema;

        /// <summary>
        /// Since we don't serializer the reference to the schema, this method will reaffect the correct schema.
        /// </summary>
        public void EnsureTables(SyncSet schema)
        {
            this.Schema = schema;
            if (this.InnerCollection != null)
            {
                foreach (var table in this)
                    table.EnsureTable(schema);
            }
        }

        /// <summary>
        /// Get a table by its name.
        /// </summary>
        public SyncTable this[string tableName]
        {
            get
            {
                if (string.IsNullOrEmpty(tableName))
                    throw new ArgumentNullException(nameof(tableName));

                var parser = new TableParser(tableName);
                var tblName = parser.TableName;
                var schemaName = parser.SchemaName ?? string.Empty;

                return this.FindTable(tblName, schemaName);
            }
        }

        /// <summary>
        /// Get a table by its name.
        /// </summary>
        public SyncTable this[string tableName, string schemaName]
        {
            get
            {
                if (string.IsNullOrEmpty(tableName))
                    throw new ArgumentNullException(nameof(tableName));

                var parser = new TableParser(tableName);
                var tblName = parser.TableName;

                return this.FindTable(tblName, schemaName ?? string.Empty);
            }
        }

        /// <summary>
        /// Single-pass lookup: match by (table, schema), falling back to "dbo" then "public" schemas for SQL Server / Postgres ergonomics.
        /// Avoids the three successive <see cref="Enumerable.FirstOrDefault{T}(IEnumerable{T}, Func{T, bool})"/> chains (each allocating a closure + lambda) that the previous implementation used.
        /// </summary>
        private SyncTable FindTable(string tblName, string schemaName)
        {
            var sc = SyncGlobalization.DataSourceStringComparison;

            SyncTable primary = null;
            SyncTable fallbackDbo = null;
            SyncTable fallbackPublic = null;

            foreach (var innerTable in this.InnerCollection)
            {
                if (!string.Equals(innerTable.TableName, tblName, sc))
                    continue;

                var innerSchema = string.IsNullOrEmpty(innerTable.SchemaName) ? string.Empty : innerTable.SchemaName;

                if (string.Equals(innerSchema, schemaName, StringComparison.Ordinal))
                {
                    primary = innerTable;
                    break;
                }

                if (fallbackDbo is null && string.Equals(innerSchema, "dbo", StringComparison.Ordinal))
                    fallbackDbo = innerTable;
                else if (fallbackPublic is null && string.Equals(innerSchema, "public", StringComparison.Ordinal))
                    fallbackPublic = innerTable;
            }

            return primary ?? fallbackDbo ?? fallbackPublic;
        }

        /// <summary>
        /// Add a new table to the Schema table collection.
        /// </summary>
        public void Add(SyncTable item)
        {
            Guard.ThrowIfNull(item);

            item.Schema = this.Schema;
            this.InnerCollection.Add(item);
        }

        /// <summary>
        /// Add a table, by its name. Be careful, can contains schema name.
        /// </summary>
        public void Add(string table)
        {
            var parser = new TableParser(table);
            var tableName = parser.TableName;
            var schemaName = parser.SchemaName;
            var sTable = new SyncTable(tableName, schemaName);
            this.Add(sTable);
        }

        /// <summary>
        /// Add some tables to ContainerSet Tables property.
        /// </summary>
        public void Add(IEnumerable<string> tables)
        {
            Guard.ThrowIfNull(tables);

            foreach (var t in tables)
                this.Add(t);
        }

        /// <summary>
        /// Clear all the Tables.
        /// </summary>
        public void Clear()
        {
            foreach (var table in this)
                table.Clear();

            this.InnerCollection.Clear();
        }

        /// <summary>
        /// Gets get the count of tables in the collection.
        /// </summary>
        public int Count => this.InnerCollection.Count;

        /// <summary>
        /// Gets a value indicating whether gets if the collection is readonly.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Get the index of a table in the collection.
        /// </summary>
        public SyncTable this[int index] { get => this.InnerCollection[index]; set => this.InnerCollection[index] = value; }

        /// <summary>
        /// Remove a table from the collection.
        /// </summary>
        public bool Remove(SyncTable item) => this.InnerCollection.Remove(item);

        /// <summary>
        /// Check if the collection contains a table.
        /// </summary>
        public bool Contains(SyncTable item) => this.InnerCollection.Contains(item);

        /// <summary>
        /// Copy the collection to an array.
        /// </summary>
        public void CopyTo(SyncTable[] array, int arrayIndex) => this.InnerCollection.CopyTo(array, arrayIndex);

        /// <summary>
        /// Get the index of a table in the collection.
        /// </summary>
        public int IndexOf(SyncTable item) => this.InnerCollection.IndexOf(item);

        /// <summary>
        /// Remove a table at a specific index.
        /// </summary>
        public void RemoveAt(int index) => this.InnerCollection.RemoveAt(index);

        /// <summary>
        /// Get the enumerator for the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => this.InnerCollection.GetEnumerator();

        /// <summary>
        /// Get the enumerator for the collection.
        /// </summary>
        public IEnumerator<SyncTable> GetEnumerator() => this.InnerCollection.GetEnumerator();

        /// <summary>
        /// Insert a table at a specific index.
        /// </summary>
        public void Insert(int index, SyncTable item) => this.InnerCollection.Insert(index, item);

        /// <summary>
        /// Return the collection as a string representing the tables count.
        /// </summary>
        public override string ToString() => this.InnerCollection.Count.ToString(CultureInfo.InvariantCulture);
    }
}