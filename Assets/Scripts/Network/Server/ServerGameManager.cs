using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ServerGameManager : IDisposable
{

    private string serverIP;
    private int serverPort;
    private int serverQPort;

    private NetworkServer networkServer;

#if UNITY_SERVER
    private MultiplayAllocationService multiplayAllocationService;
#endif

    public ServerGameManager(string serverIP, int serverPort, int queryPort, NetworkManager networkManager, NetworkObject playerPrefab)
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.serverQPort = queryPort;

        networkServer = new NetworkServer(networkManager, playerPrefab);
#if UNITY_SERVER
        multiplayAllocationService = new MultiplayAllocationService();
#endif

        PearlsManager.OnFinishedCalculationsOnServer += Pearls_OnFinishedCalculationsOnServer;
    }

    public async Task StartGameServerAsync()
    {
#if UNITY_SERVER
        await multiplayAllocationService.BeginServerCheck(); //health check


        try
        {
            MatchmakingResults matchmakerPayload = await GetMatchmakerPayload();

            if(matchmakerPayload != null)
            {
                networkServer.OnUserLeft += UserLeft;
                networkServer.OnUserJoined += UserJoined;
            } else
            {
                Debug.LogError("Failed to get matchmaker payload. Timed out");
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }
#endif
        if (!networkServer.OpenConnection(serverIP, serverPort))
        {
            Debug.LogError("NetworkServer did not start as expected.");
            return;
        }
    }

 #if UNITY_SERVER

    private async Task<MatchmakingResults> GetMatchmakerPayload()
    {
        Task<MatchmakingResults> matchmakerPayloadTask = multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();

        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(20000)) == matchmakerPayloadTask) //pass tasks, when any completes, do the code
        {
            //true if our task finishes before the delay
            return matchmakerPayloadTask.Result;
        }
        return null;
    }


    private void UserJoined(PlayerData playerData)
    {
        multiplayAllocationService.AddPlayer();

        SetReconnectValues(playerData);
    }

    private async void SetReconnectValues(PlayerData playerData)
    {
        await Reconnect.SetIsInMatch(playerData.userData.userAuthId, true);

        await Reconnect.SetPlayerMatchConnection(playerData.userData.userAuthId, serverIP, serverPort);
    }

    private void UserLeft(PlayerData playerData)
    {
        multiplayAllocationService.RemovePlayer();

        Debug.Log($"User Left: {playerData.userData.userName}");

    }

#endif

    private async void Pearls_OnFinishedCalculationsOnServer()
    {
        foreach(PlayerData playerData in NetworkServerProvider.Instance.CurrentNetworkServer.ServerAuthenticationService.PlayerDatas)
        {
            await Reconnect.SetIsInMatch(playerData.userData.userAuthId, false);
        }

        ShutdownServer();
    }

    /// <summary>
    /// Call this to close the server. Match ended or all players quit.
    /// </summary>
    public void ShutdownServer()
    {
        Debug.Log("SHUTING DOWN SERVER");
        Dispose();
        Application.Quit();
    }

    public NetworkServer GetNetworkServer()
    {
        return networkServer;
    }

    public void Dispose()
    {
#if UNITY_SERVER
        multiplayAllocationService?.Dispose();
#endif
        PearlsManager.OnFinishedCalculationsOnServer -= Pearls_OnFinishedCalculationsOnServer;
        networkServer?.Dispose();
    }


}
