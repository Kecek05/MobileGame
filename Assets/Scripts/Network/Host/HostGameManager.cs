using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class HostGameManager : IDisposable //Actual Logic to interact with UGS (Relay, Lobby, etc)
{
    private const int MAX_CONNECTIONS = 2;

    private NetworkServer networkServer;
    private NetworkObject playerPrefab;

    private Allocation allocation;

    private string joinCode;
    public string JoinCode => joinCode;

    private string lobbyId;

    public HostGameManager(NetworkObject _playerPrefab)
    {
        playerPrefab = _playerPrefab;
    }

    public async Task StartHostAsync()
    {
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MAX_CONNECTIONS);
        } catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }

        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);
        } catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = new(allocation, "dtls");

        transport.SetRelayServerData(relayServerData);

        //Create the lobby, before .StartHost an after get joinCode

        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.IsPrivate = false;
            lobbyOptions.Data = new Dictionary<string, DataObject>()
            {
                {
                    "JoinCode", new DataObject(visibility: DataObject.VisibilityOptions.Member, value : joinCode)
                }
            };


            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync($"Player's Lobby", MAX_CONNECTIONS, lobbyOptions);

            lobbyId = lobby.Id;

            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15f));

        } catch (LobbyServiceException lobbyEx)
        {
            Debug.LogException(lobbyEx);
            return;
        }

        networkServer = new NetworkServer(NetworkManager.Singleton, playerPrefab);


        string payload = JsonUtility.ToJson(ClientSingleton.Instance.GameManager.UserData); //serialize the payload to json
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload); // serialize the payload to bytes

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        if(ClientSingleton.Instance != null)
        {
            ClientSingleton.Instance.GameManager.SetIsDedicatedServerGame(false);
        } else
        {
            Debug.LogError("ClientSingleton is null, couldn't set IsDedicatedServerGame to false");
        }

        NetworkManager.Singleton.StartHost();

        //GameStateManager.OnCanCloseServer += GameStateManager_OnCanCloseServer;

        networkServer.OnClientLeft += HandleClientLeft;

        Loader.LoadHostNetwork(Loader.Scene.GameNetCodeTest);

    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId); //Owner of the lobby is allowed to kick players
        }
        catch (LobbyServiceException lobbyEx)
        {
            Debug.LogException(lobbyEx);
        }

        if (GameManager.Instance != null)
        {
            //Client Left, Cancel Game
            ServiceLocator.Get<BaseGameStateManager>().ConnectionLostHostAndClient();
        }
        else
        {
            //Not in game, shutdown
            ShutdownAsync();
        }
    }

    private IEnumerator HeartbeatLobby(float delayHeartbeatSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(delayHeartbeatSeconds); //optimization

        while(true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);

            yield return delay;
        }
    }

    /// <summary>
    /// Call this to shutdown the host. Doesn't go to Main Menu
    /// </summary>
    public async void ShutdownAsync()
    {
        if (string.IsNullOrEmpty(lobbyId)) return;


        HostSingleton.Instance.StopCoroutine(nameof(HeartbeatLobby));

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException lobbyEx)
        {
            Debug.LogException(lobbyEx);
        }
        lobbyId = string.Empty;


        networkServer.OnClientLeft -= HandleClientLeft;

        //GameStateManager.OnCanCloseServer -= GameStateManager_OnCanCloseServer;

        networkServer?.Dispose();
    }

    private void GameStateManager_OnCanCloseServer()
    {
        Debug.Log("OnCanCloseServer on Host");
        ShutdownAsync();
    }
    public NetworkServer GetNetworkServer()
    {
        return networkServer;
    }

    public void Dispose()
    {
        ShutdownAsync();
    }
}
