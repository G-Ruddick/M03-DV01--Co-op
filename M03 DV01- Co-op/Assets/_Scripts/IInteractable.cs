using Unity.Netcode;

public interface IInteractable {
    void ToggleSelection(bool isSelectable);
    NetworkObject NetworkObject { get; }
}
