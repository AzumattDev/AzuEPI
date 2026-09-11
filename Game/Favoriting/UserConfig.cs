using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace AzuEPI.Game.Favoriting;

public class UserConfig
{
    private static readonly Dictionary<long, UserConfig> PlayerConfigs = new();

    public static UserConfig GetPlayerConfig(long playerID)
    {
        if (PlayerConfigs.TryGetValue(playerID, out UserConfig userConfig)) return userConfig;

        userConfig = new UserConfig(playerID);
        PlayerConfigs[playerID] = userConfig;
        return userConfig;
    }

    private UserConfig(long uid)
    {
        _configPath = Path.Combine(Paths.ConfigPath, $"{ModName}_player_{uid}.dat");

        _mirrorPaths = [Path.Combine(Paths.ConfigPath, $"AzuAutoStore_player_{uid}.dat")];

        Load();
    }

    internal void ResetAllFavoriting()
    {
        AzuExtendedPlayerInventoryLogger.LogWarning("Resetting all favoriting data!");

        _favoritedSlots = [];
        _favoritedItems = [];

        Save();
    }

    private void Save()
    {
        WriteFile(_configPath);

        foreach (string mirrorPath in _mirrorPaths.Where(File.Exists))
        {
            try
            {
                WriteFile(mirrorPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning($"Couldn't sync favorites to {mirrorPath}: {exception.Message}");
            }
        }
    }

    private void WriteFile(string path)
    {
        using Stream stream = File.Open(path, FileMode.Create);
        List<Tuple<int, int>> tupledSlots = [.. _favoritedSlots.Select(item => Tuple.Create(item.x, item.y))];

        Bf.Serialize(stream, tupledSlots);
        Bf.Serialize(stream, _favoritedItems.ToList());
    }

    private static object TryDeserialize(Stream stream)
    {
        object result;

        try
        {
            result = Bf.Deserialize(stream);
        }
        catch (SerializationException)
        {
            result = null!;
        }

        return result;
    }

    private static void LoadProperty<T>(Stream file, out T property) where T : new()
    {
        object obj = TryDeserialize(file);

        if (obj is T property1)
        {
            property = property1;
            return;
        }

        property = Activator.CreateInstance<T>();
    }

    private void Load()
    {
        _favoritedSlots = [];
        _favoritedItems = [];

        // Merge, don't pick one. A lost favorite gets your shit stored away, an extra one doesn't hurt
        foreach (string path in _mirrorPaths.Prepend(_configPath).Where(File.Exists))
        {
            try
            {
                ReadFileInto(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning($"Couldn't read favorites from {path}: {exception.Message}");
            }
        }
    }

    private void ReadFileInto(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        List<Tuple<int, int>>? deserializedFavoritedSlots = [];
        LoadProperty(stream, out deserializedFavoritedSlots);

        if (deserializedFavoritedSlots != null)
            foreach (Tuple<int, int>? item in deserializedFavoritedSlots)
            {
                _favoritedSlots.Add(new Vector2i(item.Item1, item.Item2));
            }

        List<string>? deserializedFavoritedItems = [];
        LoadProperty(stream, out deserializedFavoritedItems);

        if (deserializedFavoritedItems != null)
            foreach (string? item in deserializedFavoritedItems)
            {
                _favoritedItems.Add(item);
            }

    }

    public void ToggleSlotFavoriting(Vector2i position)
    {
        _favoritedSlots.XAdd(position);
        Save();
    }

    public void ToggleItemNameFavoriting(ItemDrop.ItemData.SharedData item)
    {
        _favoritedItems.XAdd(item.m_name);
        Save();
    }

    public bool IsSlotFavorited(Vector2i position)
    {
        return _favoritedSlots.Contains(position);
    }

    public bool IsItemNameFavorited(ItemDrop.ItemData.SharedData item)
    {
        return _favoritedItems.Contains(item.m_name);
    }

    public bool IsItemNameOrSlotFavorited(ItemDrop.ItemData item)
    {
        return IsItemNameFavorited(item.m_shared) || IsSlotFavorited(item.m_gridPos);
    }

    private readonly string _configPath;
    private readonly string[] _mirrorPaths;
    private HashSet<Vector2i> _favoritedSlots = null!;
    private HashSet<string> _favoritedItems = null!;
    private static readonly BinaryFormatter Bf = new BinaryFormatter();
}

public static class CollectionExtension
{
    public static bool XAdd<T>(this HashSet<T> instance, T item)
    {
        return instance.Remove(item) ? false : instance.Add(item);
    }
}
