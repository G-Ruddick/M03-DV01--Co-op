using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour {
    [SerializeField] private MyPlayerInput m_playerInput;
    [SerializeField] private AgentMover m_agentMover;

    [SerializeField] private InteractableDetector m_interactableDetector;
    [SerializeField] private Animator m_animator;
    [SerializeField] private AnimationEvents m_animationEvents;

    private bool m_isInteracting, m_IsChopping;

    private NetworkVariable<ulong> m_heldNetworkObjectId = new(ulong.MaxValue);
    private NetworkVariable<ObjectType> m_heldObjectType = new(ObjectType.None);

    private ResourceSpawner spawner;

    [SerializeField] private GameObject axeModel, pickAxeModel, woodModel, stoneModel;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        m_interactableDetector.Initialize(IsOwner);
        m_heldObjectType.OnValueChanged += HandleHeldItemChanged;
        
        HandleItemOnJoin();

        if (IsOwner) {
            m_animationEvents.OnInteract += HandleInteractAction;
            m_animationEvents.OnAnimationDone += HandleInteractDone;
            m_animationEvents.OnChop += HandleChopAction;
        }
    }
    
    public override void OnNetworkDespawn() {
        m_heldObjectType.OnValueChanged -= HandleHeldItemChanged;

        if (IsOwner) {
            RequestDropServerRpc();

            m_animationEvents.OnInteract -= HandleInteractAction;
            m_animationEvents.OnAnimationDone -= HandleInteractDone;
            m_animationEvents.OnChop -= HandleChopAction;
        }

        base.OnNetworkDespawn();
    }
    
    private void OnEnable() {
        m_playerInput.OnPickUpPressed += PickUpPressed;
        m_playerInput.OnInteractPressed += HandleActionPressed;
    }

    private void OnDisable() {
        m_playerInput.OnPickUpPressed -= PickUpPressed;
        m_playerInput.OnInteractPressed -= HandleActionPressed;
    }

    private void Awake() {
        spawner = FindAnyObjectByType<ResourceSpawner>();
    }

    void Update() {
        if (IsOwner == false) {
            return;
        }

        Vector2 movementInput = m_playerInput.MovementInput;
       
        if (m_IsChopping || m_isInteracting) {
            movementInput = Vector2.zero;
        }

        m_agentMover.Move(movementInput);
    }

    private void HandleActionPressed() {
        if (IsOwner == false) {
            return;
        }
        
        if (m_IsChopping || m_isInteracting) {
            return;
        }

        if (m_heldObjectType.Value is ObjectType.Axe or ObjectType.PickAxe) {
            m_IsChopping = true;
            m_animator.SetTrigger("Chop");
        }
    }

    private void PickUpPressed() {
        if (m_isInteracting || m_IsChopping) {
            return;
        }
        
        if (m_interactableDetector.ClosestInteractable == null) {
            return;
        }

        m_animator.SetBool("Interact", true);
        m_isInteracting = true;
    }

    private void HandleItemOnJoin() {
        if (m_heldObjectType.Value != ObjectType.None) {
            HandleHeldItemChanged(ObjectType.None, m_heldObjectType.Value);
        }
    }

    private void HandleHeldItemChanged(ObjectType previousValue, ObjectType newValue) {
        axeModel.SetActive(newValue == ObjectType.Axe);
        pickAxeModel.SetActive(newValue == ObjectType.PickAxe);
        woodModel.SetActive(newValue == ObjectType.Wood);
        stoneModel.SetActive(newValue == ObjectType.Stone);
    }

    private void HandleInteractAction() {
        if (m_interactableDetector.ClosestInteractable is PickableBase) {
            RequestPickUpServerRpc(
                m_interactableDetector.ClosestInteractable.NetworkObject.NetworkObjectId
            );
        }
        
        if (m_interactableDetector.ClosestInteractable is ResourcePallet) {
            RequestGiveItemServerRpc(
                m_interactableDetector.ClosestInteractable.NetworkObject.NetworkObjectId
            );
        }
    }

    private void HandleChopAction() {
        if (m_heldObjectType.Value is ObjectType.Axe or ObjectType.PickAxe) {
            if (m_interactableDetector.ClosestInteractable is ResourceNode) {
                RequestResourceNodeInteractionServerRpc(
                    m_interactableDetector.ClosestInteractable.NetworkObject.NetworkObjectId
                );
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestResourceNodeInteractionServerRpc(ulong networkObjectId) {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject target)) {
            return;
        }

        if (!target.TryGetComponent(out ResourceNode node)) {
            return;
        }

        node.Harvest(m_heldObjectType.Value);
    }

    private void HandleInteractDone() {
        m_isInteracting = false;
        m_IsChopping = false;
    }

    [Rpc(SendTo.Server)]
    private void RequestPickUpServerRpc(ulong networkObjectId) {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject target)) {
            return;
        }

        if (!target.TryGetComponent(out PickableBase pickableItem)) {
            return;
        }

        if (!pickableItem.CanBePickedUp) {
            return;
        }
        
        if (m_heldObjectType.Value != ObjectType.None) {
            DropCurrentItem();
        }

        if (pickableItem is PickableTool) {
            m_heldNetworkObjectId.Value = networkObjectId;
        }

        m_heldObjectType.Value = pickableItem.ObjectType;
        pickableItem.PickUp();
    }

    [Rpc(SendTo.Server)]
    private void RequestGiveItemServerRpc(ulong networkObjectId) {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject target)) {
            return;
        }

        if (!target.TryGetComponent(out ResourcePallet resourcePallet)) {
            return;
        }

        if (resourcePallet.Interact(m_heldObjectType.Value)) {
            m_heldObjectType.Value = ObjectType.None;
            m_heldNetworkObjectId.Value = ulong.MaxValue;
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestDropServerRpc() {
        DropCurrentItem();
    }

    private void DropCurrentItem() {
        if (IsServer == false) {
            return;
        }

        if (m_heldObjectType.Value == ObjectType.None) {
            m_heldNetworkObjectId.Value = ulong.MaxValue;
            return;
        }

        if (m_heldObjectType.Value is ObjectType.Axe or ObjectType.PickAxe) {
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                m_heldNetworkObjectId.Value, out NetworkObject target
            )) {
                if (target.TryGetComponent(out PickableTool pickableItem)) {
                    pickableItem.Drop(transform.position);
                }
            }
        }
        else {
            spawner.SpawnResource(m_heldObjectType.Value, transform.position);
        }

        m_heldObjectType.Value = ObjectType.None;
        m_heldNetworkObjectId.Value = ulong.MaxValue;
    }
}
