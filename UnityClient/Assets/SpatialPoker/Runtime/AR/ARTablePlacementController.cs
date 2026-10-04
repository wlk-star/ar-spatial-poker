using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SpatialPoker.AR
{
    /// <summary>
    /// Detects a horizontal surface and lets the user place one persistent
    /// table root. Poker objects should be children of tableRootPrefab.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public sealed class ARTablePlacementController : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameObject tableRootPrefab;
        [SerializeField] private bool hidePlanesAfterPlacement = true;

        private static readonly List<ARRaycastHit> Hits = new();

        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private GameObject _spawnedTable;

        public bool HasPlacedTable => _spawnedTable != null;
        public Transform TableRoot =>
            _spawnedTable != null ? _spawnedTable.transform : null;

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _planeManager = GetComponent<ARPlaneManager>();
        }

        private void Update()
        {
            if (HasPlacedTable ||
                tableRootPrefab == null ||
                arCamera == null)
            {
                return;
            }

            if (!TryGetScreenPress(out var screenPosition))
                return;

            if (!_raycastManager.Raycast(
                    screenPosition,
                    Hits,
                    TrackableType.PlaneWithinPolygon))
            {
                return;
            }

            var pose = Hits[0].pose;
            _spawnedTable = Instantiate(
                tableRootPrefab,
                pose.position,
                pose.rotation);

            if (hidePlanesAfterPlacement && _planeManager != null)
            {
                _planeManager.enabled = false;
                foreach (var plane in _planeManager.trackables)
                    plane.gameObject.SetActive(false);
            }
        }

        private static bool TryGetScreenPress(out Vector2 position)
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            if (Input.touchCount > 0 &&
                Input.GetTouch(0).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(0).position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
