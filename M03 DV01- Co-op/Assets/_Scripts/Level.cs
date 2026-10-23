using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;

public class Level : NetworkBehaviour{
    [SerializeField] private GameObject nextLevelArea;
    [SerializeField] private GameObject gateBoarder;
    [SerializeField] private List<ResourcePallet> pallets;

    private void Update() {
        CheckForLevelCondition();
    }

    private void CheckForLevelCondition() {
        int points  = 0;

        foreach (ResourcePallet pallet in pallets) {
            points += pallet.StackedResoruces;
        }

        if (points >= pallets.Count * 3) {
            OpenNextLevel();
        }
    }

    private void OpenNextLevel() {
        if (nextLevelArea == null) {
            NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        gateBoarder.SetActive(false);
        nextLevelArea.SetActive(true);
    }
}
