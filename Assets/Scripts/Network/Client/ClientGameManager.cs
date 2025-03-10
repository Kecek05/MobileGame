using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable //Actual Logic to interact with UGS (Relay, Lobby, etc)
{
    private NetworkClient networkClient;

    private JoinAllocation joinAllocation;

    private UserData userData;
    public UserData UserData => userData;

    private string joinCode;
    public string JoinCode => joinCode;

    public async Task<bool> InitAsync(bool isAnonymously = false)
    {
        //Authenticate Player

        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);

        AuthState authState = isAnonymously ? await AuthenticationWrapper.DoAuthAnonymously() : await AuthenticationWrapper.DoAuth();

        if(authState == AuthState.Authenticated)
        {
            userData = new UserData
            {
                userName = AuthenticationWrapper.PlayerName,
                userAuthId = AuthenticationService.Instance.PlayerId,
            };

            return true;
        }

        return false;
    }

    public async Task StartRelayClientAsync(string joinCode)
    {
        try
        {
            joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        } catch (Exception ex)
        {
            Debug.LogException(ex);
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

        transport.SetRelayServerData(relayServerData);

        this.joinCode = joinCode;
        Debug.Log("Code Relay:" + this.joinCode);

        ConnectClient();
    }

    private void ConnectClient()
    {
        string payload = JsonUtility.ToJson(userData); //serialize the payload to json
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload); //serialize the payload to bytes

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartClient();

        Debug.Log("Started Client!");
    }


    public void Dispose()
    {
        networkClient?.Dispose();
    }


}
