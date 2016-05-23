﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using MzLite.Binary;
using MzLite.IO;
using MzLite.Json;
using MzLite.Model;

namespace MzLite.SQL
{
    public class MzLiteSQL : IMzLiteDataWriter, IMzLiteDataReader
    {

        private readonly BinaryDataEncoder encoder = new BinaryDataEncoder();
        private readonly SQLiteConnection connection;
        private readonly MzLiteModel model;
        private bool disposed = false;
        private MzLiteSQLTransactionScope currentScope = null;

        public MzLiteSQL(string path)
        {

            if (path == null)
                throw new ArgumentNullException("path");

            try
            {
                if (!File.Exists(path))
                    using (File.Create(path)) { }

                connection = GetConnection(path);
                SqlRunPragmas(connection);

                using (var scope = BeginTransaction())
                {

                    try
                    {

                        SqlInitSchema();

                        if (!SqlTrySelect(out model))
                        {
                            model = new MzLiteModel(Path.GetFileNameWithoutExtension(path));
                            SqlSave(model);
                        }

                        scope.Commit();
                    }
                    catch
                    {
                        scope.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MzLiteIOException("Error opening mzlite sql file.", ex);
            }
        }

        #region IMzLiteIO Members
        
        public ITransactionScope BeginTransaction()
        {

            RaiseDisposed();

            if (IsOpenScope)
                throw new MzLiteIOException("Illegal attempt transaction scope reentrancy.");

            try
            {
                currentScope = new MzLiteSQLTransactionScope(this, connection);
                return currentScope;
            }
            catch (Exception ex)
            {
                throw new MzLiteIOException(ex.Message, ex);
            }
        }       

        public MzLiteModel GetModel()
        {
            RaiseDisposed();
            return model;
        }

        public void SaveModel()
        {

            RaiseDisposed();

            if (IsOpenScope)
            {
                SqlSave(model);
            }
            else
            {
                using (var scope = BeginTransaction())
                {
                    SqlSave(model);
                    scope.Commit();
                }
            }
        }

        #endregion

        #region IMzLiteDataWriter Members

        public void Insert(string runID, MassSpectrum spectrum, Peak1DArray peaks)
        {

            RaiseDisposed();

            if (IsOpenScope)
            {
                SqlInsert(runID, spectrum, peaks);
            }
            else
            {
                using (var scope = BeginTransaction())
                {
                    SqlInsert(runID, spectrum, peaks);
                    scope.Commit();
                }
            }
        }

        public void Insert(string runID, Chromatogram chromatogram, Peak2DArray peaks)
        {

            RaiseDisposed();

            if (IsOpenScope)
            {
                SqlInsert(runID, chromatogram, peaks);
            }
            else
            {
                using (var scope = BeginTransaction())
                {
                    SqlInsert(runID, chromatogram, peaks);
                    scope.Commit();
                }
            }
        }

        #endregion

        #region IMzLiteDataReader Members

        public IEnumerable<MassSpectrum> ReadMassSpectra(string runID)
        {
            throw new NotImplementedException();
        }

        public MassSpectrum ReadMassSpectrum(string spectrumID)
        {
            throw new NotImplementedException();
        }

        public Peak1DArray ReadSpectrumPeaks(string spectrumID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Chromatogram> ReadChromatograms(string runID)
        {
            throw new NotImplementedException();
        }

        public Chromatogram ReadChromatogram(string chromatogramID)
        {
            throw new NotImplementedException();
        }

        public Peak2DArray ReadChromatogramPeaks(string chromatogramID)
        {
            throw new NotImplementedException();
        }

        #endregion

        internal void ReleaseTransactionScope()
        {
            currentScope = null;
        }

        private bool IsOpenScope { get { return currentScope != null; } }


        #region sql statements

        private static SQLiteConnection GetConnection(string path)
        {
            SQLiteConnection conn =
                new SQLiteConnection(string.Format("DataSource={0}", path));
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        private static void SqlRunPragmas(SQLiteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA synchronous=OFF";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA journal_mode=MEMORY";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA temp_store=MEMORY";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA ignore_check_constraints=OFF";
                cmd.ExecuteNonQuery();
            }
        }

        private void SqlInitSchema()
        {
            using (SQLiteCommand cmd = currentScope.CreateCommand("CREATE TABLE IF NOT EXISTS Model (Lock INTEGER  NOT NULL PRIMARY KEY DEFAULT(0) CHECK (Lock=0), Content TEXT NOT NULL)"))
                cmd.ExecuteNonQuery();
            using (SQLiteCommand cmd = currentScope.CreateCommand("CREATE TABLE IF NOT EXISTS Spectrum (RunID TEXT NOT NULL, SpectrumID TEXT NOT NULL PRIMARY KEY, Description TEXT NOT NULL, PeakArray TEXT NOT NULL, PeakData BINARY NOT NULL);"))
                cmd.ExecuteNonQuery();
            using (SQLiteCommand cmd = currentScope.CreateCommand("CREATE TABLE IF NOT EXISTS Chromatogram (RunID TEXT NOT NULL, ChromatogramID TEXT NOT NULL PRIMARY KEY, Description TEXT NOT NULL, PeakArray TEXT NOT NULL, PeakData BINARY NOT NULL);"))
                cmd.ExecuteNonQuery();
        }

        private void SqlInsert(string runID, MassSpectrum spectrum, Peak1DArray peaks)
        {
            SQLiteCommand cmd;

            if (!currentScope.TryGetCommand("INSERT_SPECTRUM_CMD", out cmd))
            {
                cmd = currentScope.PrepareCommand("INSERT_SPECTRUM_CMD", "INSERT INTO Spectrum VALUES(@runID, @spectrumID, @description, @peakArray, @peakData);");
            }

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@runID", runID);
            cmd.Parameters.AddWithValue("@spectrumID", spectrum.ID);
            cmd.Parameters.AddWithValue("@description", MzLiteJson.ToJson(spectrum));
            cmd.Parameters.AddWithValue("@peakArray", MzLiteJson.ToJson(peaks));
            cmd.Parameters.AddWithValue("@peakData", encoder.Encode(peaks));

            cmd.ExecuteNonQuery();

        }

        private void SqlInsert(string runID, Chromatogram chromatogram, Peak2DArray peaks)
        {

            SQLiteCommand cmd;

            if (!currentScope.TryGetCommand("INSERT_CHROMATOGRAM_CMD", out cmd))
            {
                cmd = currentScope.PrepareCommand("INSERT_CHROMATOGRAM_CMD", "INSERT INTO Chromatogram VALUES(@runID, @chromatogramID, @description, @peakArray, @peakData);");
            }

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@runID", runID);
            cmd.Parameters.AddWithValue("@chromatogramID", chromatogram.ID);
            cmd.Parameters.AddWithValue("@description", MzLiteJson.ToJson(chromatogram));
            cmd.Parameters.AddWithValue("@peakArray", MzLiteJson.ToJson(peaks));
            cmd.Parameters.AddWithValue("@peakData", encoder.Encode(peaks));

            cmd.ExecuteNonQuery();

        }

        private void SqlSave(MzLiteModel model)
        {
            using (SQLiteCommand cmd = currentScope.CreateCommand("DELETE FROM Model"))
            {
                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmd = currentScope.CreateCommand("INSERT INTO Model VALUES(@lock, @content)"))
            {
                cmd.Parameters.AddWithValue("@lock", 0);
                cmd.Parameters.AddWithValue("@content", MzLiteJson.ToJson(model));
                cmd.ExecuteNonQuery();
            }

        }

        private bool SqlTrySelect(out MzLiteModel model)
        {
            using (SQLiteCommand cmd = currentScope.CreateCommand("SELECT Content FROM Model"))
            {
                string content = cmd.ExecuteScalar() as string;

                if (content != null)
                {
                    model = MzLiteJson.FromJson<MzLiteModel>(content);
                    return true;
                }
                else
                {
                    model = null;
                    return false;
                }
            }

        }

        #endregion

        #region IDisposable Members

        private void RaiseDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            if (currentScope != null)
                currentScope.Dispose();
            if (connection != null)
                connection.Dispose();
            disposed = true;
        }

        #endregion        
    }

    /// <summary>
    /// Provides prepared statements within a SQLite connection.
    /// </summary>
    internal class MzLiteSQLTransactionScope : ITransactionScope
    {

        private readonly SQLiteConnection connection;
        private readonly SQLiteTransaction transaction;
        private readonly MzLiteSQL writer;
        private readonly IDictionary<string, SQLiteCommand> commands = new Dictionary<string, SQLiteCommand>();
        private bool disposed = false;        

        #region ITransactionScope Members

        public void Commit()
        {
            RaiseDisposed();
            transaction.Commit();
        }

        public void Rollback()
        {
            RaiseDisposed();
            transaction.Rollback();
        }

        #endregion

        #region MzLiteSQLTransactionScope Members

        internal MzLiteSQLTransactionScope(MzLiteSQL writer, SQLiteConnection connection)
        {
            this.connection = connection;
            this.transaction = connection.BeginTransaction();
            this.writer = writer;
        }

        internal SQLiteCommand PrepareCommand(string name, string commandText)
        {

            RaiseDisposed();

            SQLiteCommand cmd = CreateCommand(commandText);
            cmd.Prepare();
            commands[name] = cmd;
            return cmd;
        }

        internal SQLiteCommand CreateCommand(string commandText)
        {

            RaiseDisposed();

            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            cmd.Transaction = transaction;
            return cmd;
        }

        internal bool TryGetCommand(string name, out SQLiteCommand cmd)
        {
            RaiseDisposed();
            return commands.TryGetValue(name, out cmd);
        }

        #endregion

        #region IDisposable Members

        private void RaiseDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            foreach (var cmd in commands.Values)
                cmd.Dispose();
            commands.Clear();

            if (transaction != null)
                transaction.Dispose();

            writer.ReleaseTransactionScope();

            disposed = true;
        }

        #endregion
    }
}
