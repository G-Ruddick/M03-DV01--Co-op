using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Collections;

public class PlayerNameUI : NetworkBehaviour {
    [SerializeField] private UIDocument uiPlayerNameDocument;

    private Label playerNameLabel;

    private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake() {
        playerNameLabel = uiPlayerNameDocument.rootVisualElement.Q<Label>("NameLabel");
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        playerName.OnValueChanged += HandlePlayerNameChanged;

        if (IsServer) {
            playerName.Value = $"Player {OwnerClientId}";
        }

        playerNameLabel.text = playerName.Value.ToString();
    }

    private void HandlePlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue) {
        playerNameLabel.text = newValue.ToString();
    }
}
