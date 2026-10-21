using UnityEngine;

public class Level : MonoBehaviour
{
    [SerializeField] private GameObject nextLevelArea;
    [SerializeField] private GameObject gateBoarder;

    private void CheckForLevelCondition() {
        
    }

    private void OpenNextLevel() {
        gateBoarder.SetActive(false);
        nextLevelArea.SetActive(true);
    }
}
