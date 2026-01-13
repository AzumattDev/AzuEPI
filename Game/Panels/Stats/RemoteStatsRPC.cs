namespace AzuEPI.Game.Panels.Stats;

public static class RemoteStatsRPC
{
    public const string RequestRPCName = "AzuEPI_RequestStats";
    public const string ResponseRPCName = "AzuEPI_StatsResponse";

    private static ZRoutedRpc? _registeredInstance;

    public static void Register()
    {
        if (ZRoutedRpc.instance == null) return;

        if (_registeredInstance == ZRoutedRpc.instance) return;

        ZRoutedRpc.instance.Register<ZPackage>(RequestRPCName, OnStatsRequested);
        ZRoutedRpc.instance.Register<ZPackage>(ResponseRPCName, OnStatsReceived);

        _registeredInstance = ZRoutedRpc.instance;
        AzuExtendedPlayerInventoryLogger.LogDebug($"Registered remote stats RPCs (local UID: {ZNet.GetUID()}, instance: {ZRoutedRpc.instance.GetHashCode()})");
    }

    public static void Reset()
    {
        _registeredInstance = null;
        AzuExtendedPlayerInventoryLogger.LogDebug("Reset remote stats RPC registration");
    }

    public static void RequestStats(long targetPlayerId)
    {
        if (ZRoutedRpc.instance == null) return;

        long myUid = ZNet.GetUID();
        ZPackage requestPkg = new();
        requestPkg.Write(myUid);

        ZRoutedRpc.instance.InvokeRoutedRPC(targetPlayerId, RequestRPCName, requestPkg);
        AzuExtendedPlayerInventoryLogger.LogDebug($"Requested stats from player {targetPlayerId} (my UID: {myUid})");
    }

    private static void OnStatsRequested(long senderId, ZPackage pkg)
    {
        try
        {
            if (ZRoutedRpc.instance == null || Player.m_localPlayer == null) return;

            long explicitSenderId = pkg.ReadLong();
            AzuExtendedPlayerInventoryLogger.LogDebug($"Stats requested - auto senderId: {senderId}, explicit senderId: {explicitSenderId}");

            RemotePlayerStats stats = RemotePlayerStats.GatherFromLocalPlayer();

            byte[] compressed = stats.ToCompressedBytes();

            ZPackage responsePkg = new();
            responsePkg.Write(compressed);

            long responseTarget = senderId;
            AzuExtendedPlayerInventoryLogger.LogDebug($"Sending compressed stats ({compressed.Length} bytes) to {responseTarget} via {ResponseRPCName}");

            ZRoutedRpc.instance.InvokeRoutedRPC(responseTarget, ResponseRPCName, responsePkg);
            AzuExtendedPlayerInventoryLogger.LogDebug("InvokeRoutedRPC for response completed");
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"Exception in OnStatsRequested: {ex}");
        }
    }

    private static void OnStatsReceived(long senderId, ZPackage pkg)
    {
        try
        {
            AzuExtendedPlayerInventoryLogger.LogDebug($"OnStatsReceived called from senderId: {senderId}");

            byte[] compressed = pkg.ReadByteArray();

            AzuExtendedPlayerInventoryLogger.LogDebug($"Received compressed stats ({compressed.Length} bytes) from player {senderId}");

            RemotePlayerStats? stats = RemotePlayerStats.FromCompressedBytes(compressed);
            if (stats == null)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning("Failed to decompress remote player stats");
                StatsPanelController.OnRemoteStatsReceived(null);
                return;
            }

            AzuExtendedPlayerInventoryLogger.LogDebug($"Decompressed stats for player: {stats.PlayerName}");
            StatsPanelController.OnRemoteStatsReceived(stats);
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"Exception in OnStatsReceived: {ex}");
        }
    }
}

[HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.SetUID))]
internal static class ZRoutedRpc_SetUID_RegisterRPCs
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        RemoteStatsRPC.Register();
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PlayerList))]
internal static class ZNet_RPC_PlayerList_RefreshDropdown
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        StatsPanelController.OnPlayerListChanged();
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
internal static class ZNet_Shutdown_ResetRPCs
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        RemoteStatsRPC.Reset();
    }
}
