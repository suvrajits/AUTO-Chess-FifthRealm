using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragPlacementManager : MonoBehaviour
{
    public static DragPlacementManager Instance { get; private set; }

    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private Camera dragCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TryPla
