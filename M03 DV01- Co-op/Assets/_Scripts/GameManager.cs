using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour {
    [SerializeField] private MultiplayerUI m_multiplayerUI;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<ResourcePallet> pallets;
    
    private void Start() {
        if (m_multiplayerUI != null) {
            m_multiplayerUI.OnStartHost += StartHost;
            m_multiplayerUI.OnStartClient += StartClient;
            m_multiplayerUI.OnDiconnectClient += DisconnectClient;
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer == false) {
            return;
        }

        NetworkManager.OnClientConnectedCallback += SpawnPlayer;
        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;

        foreach (ResourcePallet pallet in pallets) {
            pallet.OnPalletFilled += CheckWinCondition;
        }
    }
    
    public override void OnNetworkDespawn() {
        if (IsServer == true) {
            NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;
        }

        foreach (ResourcePallet pallet in pallets) {
            pallet.OnPalletFilled -= CheckWinCondition;
        }
        
        base.OnNetworkDespawn();
    }

    private void CheckWinCondition() {
        int points  = 0;

        foreach (ResourcePallet pallet in pallets) {
            points += pallet.StackedResoruces;
        }

        if (points >= pallets.Count * 3) {
            NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }

    private void StartHost() {
        m_multiplayerUI.DisableButtons();
        NetworkManager.StartHost();
    }

    private void StartClient() {
        m_multiplayerUI.DisableButtons();
        NetworkManager.StartClient();
    }

    private void DisconnectClient() {
        m_multiplayerUI.EnableButtons();
        NetworkManager.Shutdown();
    }

    private void SpawnPlayer(ulong clientID) {
        if (NetworkManager.ConnectedClients[clientID].PlayerObject != null) {
            return;
        }

        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
    }

    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
        foreach (ulong clientID in clientsCompleted) {
            SpawnPlayer(clientID);
        }
    }
}
