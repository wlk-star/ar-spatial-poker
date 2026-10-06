using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SpatialPoker.AR
{
    /// <summary>
    /// Detects a horizontal surface and lets the user place one persistent
    /// table root. Two modes:
    /// <list type="bullet">
    /// <item>Prefab mode: instantiates <see cref="tableRootPrefab"/> at the tap pose.</item>
    /// <item>Scene mode: positions/activates the existing <see cref="sceneTableRoot"/>
    /// (used by the generated AR lab scene, whose table is built by the scene
    /// builder with all presentation references already wired).</item>
    /// </list>
    /// Without AR hardware (Editor, or an unsupported device) the scene table
    /// is placed at <see cref="editorFallbackPosition"/> so the lab stays usable.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public sealed class ARTablePlacementController : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameObject tableRootPrefab;
        [SerializeField] private Transform sceneTableRoot;
        [SerializeField] private bool hidePlanesAfterPlacement = true;
        [SerializeField] private bool faceCameraOnPlace = true;
        [SerializeField] private Vector3 editorFallbackPosition = new Vector3(0f, 0f, -1.2f);

        private static readonly List<ARRaycastHit> Hits = new();

        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private ARAnchorManager _anchorManager;
        private GameObject _spawnedTable;

        public bool HasPlacedTable => _spawnedTable != null;
        public Transform TableRoot =>
            _spawnedTable != null ? _spawnedTable.transform : null;

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _planeManager = GetComponent<ARPlaneManager>();
            _anchorManager = GetComponent<ARAnchorManager>();
        }

        private void Update()
        {
            if (HasPlacedTable || arCamera == null)
                return;

            // No AR hardware: drop the scene table at a fixed pose so the
            // Editor lab (and unsupported devices) stay usable.
            if (sceneTableRoot != null && IsArPermanentlyUnavailable())
            {
                PlaceAt(new Pose(editorFallbackPosition, Quaternion.identity));
                return;
            }

            if (tableRootPrefab == null && sceneTableRoot == null)
                return;

            if (!TryGetScreenPress(out var screenPosition))
                return;

            if (_raycastManager == null ||
                !_raycastManager.Raycast(
                    screenPosition,
                    Hits,
                    TrackableType.PlaneWithinPolygon))
            {
                return;
            }

            PlaceAt(Hits[0].pose);
        }

        private static bool IsArPermanentlyUnavailable()
        {
#if UNITY_EDITOR
            return true;
#else
            return ARSession.state == ARSessionState.Unsupported;
#endif
        }

        private void PlaceAt(Pose pose)
        {
            if (faceCameraOnPlace)
                pose.rotation = YawFacingCamera(pose.position);

            if (sceneTableRoot != null)
                PlaceSceneTable(pose);
            else if (tableRootPrefab != null)
                PlacePrefab(pose);
        }

        private Quaternion YawFacingCamera(Vector3 tablePosition)
        {
            var toCamera = arCamera.transform.position - tablePosition;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            // Table +Z (local seat side) faces the player holding the camera.
            var yaw = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, yaw, 0f);
        }

        private void PlaceSceneTable(Pose pose)
        {
            var table = sceneTableRoot;
            table.SetPositionAndRotation(pose.position, pose.rotation);
            table.gameObject.SetActive(true);
            _spawnedTable = table.gameObject;
            AttachAnchor(pose);
            HidePlanes();
        }

        private void PlacePrefab(Pose pose)
        {
            _spawnedTable = Instantiate(
                tableRootPrefab,
                pose.position,
                pose.rotation);
            AttachAnchor(pose);
            HidePlanes();
        }

        private void AttachAnchor(Pose pose)
        {
            if (_anchorManager == null || _spawnedTable == null)
                return;

            try
            {
                var anchor = _anchorManager.AddAnchor(pose);
                if (anchor != null)
                    _spawnedTable.transform.SetParent(anchor.transform, true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"[SpatialPoker] AddAnchor failed, keeping world placement: {e.Message}");
            }
        }

        private void HidePlanes()
        {
            if (!hidePlanesAfterPlacement || _planeManager == null)
                return;

            _planeManager.enabled = false;
            foreach (var plane in _planeManager.trackables)
                plane.gameObject.SetActive(false);
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
