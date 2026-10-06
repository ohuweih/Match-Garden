using System;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class ProgressFileStore
{
    public string SavePath { get; }
    public string BackupPath => SavePath + ".bak";
    private string TemporaryPath => SavePath + ".tmp";
    public bool CanSave { get; private set; } = true;
    public string Warning { get; private set; }
    private bool recoveredBackup;

    public ProgressFileStore(string directory) : this(directory, "progress.json")
    {
    }

    public ProgressFileStore(string directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory is unavailable.");
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Save file name is unavailable.");
        SavePath = Path.Combine(directory, fileName);
    }

    public ProgressData Load()
    {
        CanSave = true; Warning = null; recoveredBackup = false;
        if (!File.Exists(SavePath) && !File.Exists(BackupPath)) return ProgressData.Empty();
        try { return Read(SavePath); }
        catch (InvalidOperationException error)
        {
            // An older game must never overwrite a newer save format.
            CanSave = false; Warning = error.Message; return ProgressData.Empty();
        }
        catch (Exception error) when (IsFileOrDataError(error)) { Warning = error.Message; }
        try
        {
            var data = Read(BackupPath);
            recoveredBackup = true;
            Warning = "Recovered progress from the backup file.";
            return data;
        }
        catch (Exception error) when (IsFileOrDataError(error) || error is InvalidOperationException)
        {
            CanSave = false;
            Warning = "Neither save file could be read. Existing files were preserved. " + Warning;
            return ProgressData.Empty();
        }
    }

    private static ProgressData Read(string path)
    {
        var data = JsonUtility.FromJson<ProgressData>(File.ReadAllText(path));
        if (data == null) throw new ArgumentException("The save file is empty.");
        // A missing version is corrupt data; a future version is left untouched.
        if (data.version == 0) throw new ArgumentException("Save version is missing.");
        data.Validate();
        return data;
    }

    public bool TrySave(ProgressData data, out string error)
    {
        error = null;
        if (!CanSave) { error = Warning ?? "Saving is disabled until progress is reset."; return false; }
        try
        {
            data.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(data, true));
            using (var stream = new FileStream(TemporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(SavePath))
            {
                // If the primary was corrupt, preserve the good backup during repair.
                File.Replace(TemporaryPath, SavePath, recoveredBackup ? null : BackupPath);
            }
            else
            {
                File.Move(TemporaryPath, SavePath);
                if (!File.Exists(BackupPath))
                {
                    try { File.Copy(SavePath, BackupPath); }
                    catch (Exception backupError) when (IsFileOrDataError(backupError))
                    {
                        // The primary is already committed; do not report that win as unsaved.
                        Debug.LogWarning("[Save] Progress saved, but the backup could not be created: " + backupError.Message);
                    }
                }
            }
            recoveredBackup = false;
            return true;
        }
        catch (Exception failure) when (IsFileOrDataError(failure) || failure is InvalidOperationException || failure is NotSupportedException)
        {
            error = failure.Message;
            return false;
        }
    }

    // Invoked only by the explicit, confirmed editor reset control.
    public bool TryReset(out string error)
    {
        error = null;
        try
        {
            // Remove the backup first so an interruption cannot resurrect old progress.
            foreach (var path in new[] { BackupPath, TemporaryPath, SavePath })
                if (File.Exists(path)) File.Delete(path);
            CanSave = true; recoveredBackup = false; Warning = null;
            return true;
        }
        catch (Exception failure) when (IsFileOrDataError(failure)) { error = failure.Message; return false; }
    }

    private static bool IsFileOrDataError(Exception error) =>
        error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is System.Security.SecurityException;
}
