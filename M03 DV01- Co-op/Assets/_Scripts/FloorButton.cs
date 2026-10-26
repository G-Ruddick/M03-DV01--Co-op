using UnityEngine;

public class FloorButton : MonoBehaviour {
    [SerializeField] private GameObject moveableObject;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private bool changeVisibility;
    [SerializeField] private bool defaultVisiblility;

    private void OnTriggerEnter(Collider other) {
        Debug.Log(other.gameObject.layer);
        if (!other.gameObject.CompareTag("Player")) {
            return;
        }

        moveableObject.transform.localPosition = endPosition;
        if (changeVisibility) {
            moveableObject.SetActive(!defaultVisiblility);
        }
    }

    private void OnTriggerExit(Collider other) {
        Debug.Log(other.gameObject.layer);
        if (!other.gameObject.CompareTag("Player")) {
            return;
        }

        moveableObject.transform.localPosition = startPosition;
        if (changeVisibility) {
            moveableObject.SetActive(defaultVisiblility);
        }
    }
}
