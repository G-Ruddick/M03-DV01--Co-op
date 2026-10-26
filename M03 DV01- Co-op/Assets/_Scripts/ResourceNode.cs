using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System;
using System.Collections.Generic;

public class ResourceNode : NetworkBehaviour, IInteractable {
    [SerializeField] private SelectionOutline m_selectionOutline;
    [SerializeField] private ComponentController m_componentController;

    [SerializeField] private List<ObjectType> m_toolTypeRequired;
    [SerializeField] private ObjectType m_producedObjectType;
    
    [SerializeField] private int m_amountToSpawn = 3;
    [SerializeField] private InteractAnimation m_interactAnimation;
    [SerializeField] private ItemsAudio m_itemsAudio;

    private NetworkVariable<int> m_health = new(3);

    private ResourceSpawner spawner;

    private void Awake() {
        spawner = FindAnyObjectByType<ResourceSpawner>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            m_health.Value = 3;
        }
    }

    public void Harvest(ObjectType toolType) {
        if (!IsServer) {
            return;
        }

        if (m_toolTypeRequired.Contains(toolType)) {
            Debug.Log("Hit");
            m_health.Value--;
            PlayAudioClientRpc();

            if (m_health.Value > 0) {
                PlayAnimationClientRpc();
            }
            else {
                m_componentController.SetEnabled(false);
                
                for (int i = 0; i < m_amountToSpawn; i++) {
                    Vector3 position = transform.position;
                    position.y = 0;
                    Vector2 offset = UnityEngine.Random.insideUnitCircle;
                    
                    position.x += offset.x;
                    position.z += offset.y;
                    
                    spawner.SpawnResource(m_producedObjectType, position);
                }
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayAnimationClientRpc() {
        if (m_interactAnimation != null) {
            m_interactAnimation.Shake();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayAudioClientRpc() {
        if(m_itemsAudio != null) {
            if (m_health.Value > 0) {
                m_itemsAudio.PlaySound();
            }
            else {
                m_itemsAudio.PlaySoundSeparate();
            }
        }
    }

    public void ToggleSelection(bool isSelected) {
        if (m_selectionOutline != null) {
            m_selectionOutline.ToggleOutline(isSelected);
        }
    }
}