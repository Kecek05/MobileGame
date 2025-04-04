using System.Collections.Generic;
using UnityEngine;

public class ServerAuthenticationService : IServerAuthenticationService
{

    private Dictionary<string, ulong> authToClientId = new Dictionary<string, ulong>(); 

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>(); 

    private Dictionary<string, PlayerData> authIdToPlayerData = new Dictionary<string, PlayerData>(); 

    private Dictionary<PlayableState, ulong> playableStateToClientId = new Dictionary<PlayableState, ulong>(); 

    private List<PlayerData> playerDatas = new List<PlayerData>();

    private Dictionary<ulong, PlayerData> clientIdToPlayerData = new Dictionary<ulong, PlayerData>();

    public List<PlayerData> PlayerDatas => playerDatas;
    public Dictionary<ulong, PlayerData> ClientIdToPlayerData => clientIdToPlayerData;
    public int RegisteredClientCount => clientIdToAuth.Count;

    public Dictionary<string, ulong>.ValueCollection AuthToClientIdValues => authToClientId.Values;

    public void RegisterClient(PlayerData playerData)
    {
        //if dont exist, add
        clientIdToAuth[playerData.clientId] = playerData.userData.userAuthId;
        authToClientId[playerData.userData.userAuthId] = playerData.clientId;
        authIdToPlayerData[playerData.userData.userAuthId] = playerData;

        playerDatas.Add(playerData);
        clientIdToPlayerData[playerData.clientId] = playerData;
    }

    public void RegisterPlayableClient(PlayerData playerData)
    {
        playableStateToClientId[playerData.playableState] = playerData.clientId;
    }

    public void UnregisterClient(ulong clientId)
    {
        if(clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            authToClientId.Remove(authId);
            clientIdToAuth.Remove(clientId);
            authIdToPlayerData.Remove(authId);
        }
    }

    public PlayerData GetPlayerDataByClientId(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            //Get Auth by client ID
            if (authIdToPlayerData.TryGetValue(authId, out PlayerData playerData))
            {
                return playerData;
            }
        }
        return null;
    }

    public string GetAuthIdByClientId(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            return authId;
        }
        return null;
    }

    public ulong GetClientIdByAuthId(string authId)
    {
        if (authToClientId.TryGetValue(authId, out ulong clientId))
        {
            return clientId;
        }
        return 0;
    }

}
