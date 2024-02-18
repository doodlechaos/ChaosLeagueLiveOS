using SQLite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
public class SQLiteServiceAsync : MonoBehaviour
{
    private static SQLiteAsyncConnection dbConnection;

    [SerializeField] private string _discordID;
    [SerializeField] private string _displayName;

    [SerializeField] private bool _addEntryButton;
    [SerializeField] private bool _backupDBButton;

    [SerializeField] private string _tableName = "PlayerProfiles";

    private CancellationTokenSource _cts;

    private void Awake()
    {
        // Specify the path to your SQLite database file
        string dbPath = Path.Combine(Application.streamingAssetsPath, $"{_tableName}.db");

        // Create a connection to the database
        dbConnection = new SQLiteAsyncConnection(dbPath);
        dbConnection.CreateTableAsync<PlayerProfile>(); // Create a table for your data
        Debug.Log(dbConnection.GetTableInfoAsync(_tableName));


        _cts = new CancellationTokenSource();

#if !UNITY_EDITOR
        _ = AutoBackupsLoop(_cts);
#endif

    }

    private void OnValidate()
    {
        if (_addEntryButton)
        {
            _addEntryButton = false;
            PlayerProfile newPP = new PlayerProfile();
            newPP.TwitchID = "6940";
            _ = UpdatePlayer(newPP);
        }

        if (_backupDBButton)
        {
            _backupDBButton = false;
            BackupDB();
        }
    }

    public async Task AutoBackupsLoop(CancellationTokenSource cts)
    {
        while (true)
        {
            if (_cts.IsCancellationRequested)
                return;

            try
            {
                await Task.Delay(AppConfig.inst.GetI("MinutesBetweenDBBackups") * 60 * 1000);
                BackupDB();
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred during the database backup process: {e.Message}");
                await Task.Delay(60_000);
            }
        }
    }

    private void BackupDB()
    {
        string backupFolder = Path.Combine(Application.streamingAssetsPath, "DatabaseBackups");
        if (!Directory.Exists(backupFolder))
            Directory.CreateDirectory(backupFolder);

        string backupName = $"{_tableName}_{DateTime.Now:yyyyMMddHHmmss}.db";
        dbConnection.BackupAsync($"{backupFolder}/{backupName}");
        Debug.Log($"Successful backup of db {backupName} in {backupFolder}");
    }

    public void ClearInvitesData()
    {
        _ = ClearPropertyFromAllEntries(_tableName, "InvitedByID");
        _ = ClearPropertyFromAllEntries(_tableName, "InvitesJSON");

    }
    private void OnDestroy()
    {
        CloseConnection(); 
    }

    public static void CloseConnection()
    {
        if (dbConnection != null)
            dbConnection.CloseAsync();
    }



    public static async Task UpdatePlayer(PlayerProfile pp)
    {

        var query = dbConnection.Table<PlayerProfile>().Where(x => x.TwitchID == pp.TwitchID);

        int count = await query.CountAsync();
        if (query != null && count > 0)
        {
            await dbConnection.UpdateAsync(pp);
        }
        else
        {
            await dbConnection.InsertAsync(pp);
        }
    }

    public static async Task ClearPropertyFromAllEntries(string tablename, string propertyName)
    {
        try
        {
            // Generate the raw SQL statement
            string sqlCommand = $"UPDATE {tablename} SET {propertyName} = NULL";

            await dbConnection.ExecuteAsync(sqlCommand);
            Debug.Log($"Cleared all values from {propertyName}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error clearing values from {propertyName}: {e}");
        }
    }

    public static async Task<PlayerProfile> GetPlayer(string twitchID)
    {
        try
        {
            PlayerProfile pp = await dbConnection.Table<PlayerProfile>().Where(x => x.TwitchID==twitchID).FirstOrDefaultAsync();

            if (pp != null)
                return pp;

            pp = new PlayerProfile();
            pp.TwitchID = twitchID;
            Debug.Log("Done getting player from DB");

            return pp;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return null;
    }


}
