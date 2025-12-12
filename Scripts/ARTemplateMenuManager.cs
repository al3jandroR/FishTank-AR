using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Templates.AR
{
    public class ARTemplateMenuManager : MonoBehaviour
    {
        [SerializeField] Button m_ConfirmButton;
        public Button confirmButton { get => m_ConfirmButton; set => m_ConfirmButton = value; }

        [SerializeField] Button m_CreateButton;
        public Button createButton { get => m_CreateButton; set => m_CreateButton = value; }

        [SerializeField] Button m_DeleteButton;
        public Button deleteButton { get => m_DeleteButton; set => m_DeleteButton = value; }

        [SerializeField] GameObject m_ObjectMenu;
        public GameObject objectMenu { get => m_ObjectMenu; set => m_ObjectMenu = value; }

        [SerializeField] GameObject m_ModalMenu;
        public GameObject modalMenu { get => m_ModalMenu; set => m_ModalMenu = value; }

        [SerializeField] Animator m_ObjectMenuAnimator;
        public Animator objectMenuAnimator { get => m_ObjectMenuAnimator; set => m_ObjectMenuAnimator = value; }

        [SerializeField] ObjectSpawner m_ObjectSpawner;
        public ObjectSpawner objectSpawner { get => m_ObjectSpawner; set => m_ObjectSpawner = value; }

        [SerializeField] Button m_CancelButton;
        public Button cancelButton { get => m_CancelButton; set => m_CancelButton = value; }

        [SerializeField] XRInteractionGroup m_InteractionGroup;
        public XRInteractionGroup interactionGroup { get => m_InteractionGroup; set => m_InteractionGroup = value; }

        [SerializeField] DebugSlider m_DebugPlaneSlider;
        public DebugSlider debugPlaneSlider { get => m_DebugPlaneSlider; set => m_DebugPlaneSlider = value; }

        [SerializeField] ARPlaneManager m_PlaneManager;
        public ARPlaneManager planeManager { get => m_PlaneManager; set => m_PlaneManager = value; }

        [SerializeField] bool m_UseARPlaneFading = true;
        public bool useARPlaneFading { get => m_UseARPlaneFading; set => m_UseARPlaneFading = value; }

        [SerializeField] ARDebugMenu m_ARDebugMenu;
        public ARDebugMenu arDebugMenu { get => m_ARDebugMenu; set => m_ARDebugMenu = value; }

        [SerializeField] DebugSlider m_DebugMenuSlider;
        public DebugSlider debugMenuSlider { get => m_DebugMenuSlider; set => m_DebugMenuSlider = value; }

        [SerializeField]
        XRInputValueReader<Vector2> m_TapStartPositionInput = new XRInputValueReader<Vector2>("Tap Start Position");
        public XRInputValueReader<Vector2> tapStartPositionInput
        {
            get => m_TapStartPositionInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_TapStartPositionInput, value, this);
        }

        [SerializeField]
        XRInputValueReader<Vector2> m_DragCurrentPositionInput = new XRInputValueReader<Vector2>("Drag Current Position");
        public XRInputValueReader<Vector2> dragCurrentPositionInput
        {
            get => m_DragCurrentPositionInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_DragCurrentPositionInput, value, this);
        }

        bool m_IsPointerOverUI;
        bool m_ShowObjectMenu;
        bool m_ShowOptionsModal;
        bool m_VisualizePlanes = true;
        bool m_ShowDebugMenu;
        bool m_InitializingDebugMenu;
        float m_DebugMenuPlanesButtonValue = 0f;
        Vector2 m_ObjectButtonOffset = Vector2.zero;
        Vector2 m_ObjectMenuOffset = Vector2.zero;
        readonly List<ARPlane> m_ARPlanes = new List<ARPlane>();
        readonly Dictionary<ARPlane, ARPlaneMeshVisualizer> m_ARPlaneMeshVisualizers = new Dictionary<ARPlane, ARPlaneMeshVisualizer>();
        readonly Dictionary<ARPlane, ARPlaneMeshVisualizerFader> m_ARPlaneMeshVisualizerFaders = new Dictionary<ARPlane, ARPlaneMeshVisualizerFader>();

        bool m_IsPlacementLocked = false;

        void OnEnable()
        {
            m_CreateButton.onClick.AddListener(ShowMenu);
            m_CancelButton.onClick.AddListener(HideMenu);
            m_DeleteButton.onClick.AddListener(DeleteFocusedObject);
            m_PlaneManager.trackablesChanged.AddListener(OnPlaneChanged);

            if (m_ConfirmButton != null) m_ConfirmButton.onClick.AddListener(LockPlacement);
            if (m_ObjectSpawner != null) m_ObjectSpawner.objectSpawned += OnObjectSpawned;
        }

        void OnDisable()
        {
            m_ShowObjectMenu = false;
            m_CreateButton.onClick.RemoveListener(ShowMenu);
            m_CancelButton.onClick.RemoveListener(HideMenu);
            m_DeleteButton.onClick.RemoveListener(DeleteFocusedObject);
            m_PlaneManager.trackablesChanged.RemoveListener(OnPlaneChanged);

            // --- NEW CLEANUP ---
            if (m_ConfirmButton != null) m_ConfirmButton.onClick.RemoveListener(LockPlacement);
            if (m_ObjectSpawner != null) m_ObjectSpawner.objectSpawned -= OnObjectSpawned;
        }

        void Start()
        {
            if (m_ARDebugMenu != null)
            {
                m_ARDebugMenu.gameObject.SetActive(true);
                m_InitializingDebugMenu = true;
                InitializeDebugMenuOffsets();
            }

            HideMenu();

            m_DebugMenuSlider.value = m_ShowDebugMenu ? 1 : 0;
            m_DebugPlaneSlider.value = m_VisualizePlanes ? 1 : 0;

            if (m_ConfirmButton != null) m_ConfirmButton.gameObject.SetActive(false);
        }

        void Update()
        {
            if (m_InitializingDebugMenu)
            {
                m_ARDebugMenu.gameObject.SetActive(false);
                m_InitializingDebugMenu = false;
            }

            if (m_IsPlacementLocked) return;

            if (m_ShowObjectMenu || m_ShowOptionsModal)
            {
                if (!m_IsPointerOverUI && (m_TapStartPositionInput.TryReadValue(out _) || m_DragCurrentPositionInput.TryReadValue(out _)))
                {
                    if (m_ShowObjectMenu) HideMenu();
                    if (m_ShowOptionsModal) m_ModalMenu.SetActive(false);
                }

                if (m_ShowObjectMenu)
                    m_DeleteButton.gameObject.SetActive(false);
                else
                    m_DeleteButton.gameObject.SetActive(m_InteractionGroup?.focusInteractable != null);

                m_IsPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
            }
            else
            {
                m_IsPointerOverUI = false;
                m_CreateButton.gameObject.SetActive(true);
                m_DeleteButton.gameObject.SetActive(m_InteractionGroup?.focusInteractable != null);
            }

            if (!m_IsPointerOverUI && m_ShowOptionsModal)
            {
                m_IsPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
            }
        }

        void OnObjectSpawned(GameObject obj)
        {
            if (!m_IsPlacementLocked && m_ConfirmButton != null)
            {
                m_ConfirmButton.gameObject.SetActive(true);
            }
        }

        public void LockPlacement()
        {
            m_IsPlacementLocked = true;

            if (m_ObjectSpawner != null) m_ObjectSpawner.enabled = false;
            if (m_InteractionGroup != null)
            {
                var rayInteractors = m_InteractionGroup.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
                foreach (var ray in rayInteractors) ray.enabled = false;
            }

            if (m_CreateButton != null) m_CreateButton.gameObject.SetActive(false);
            if (m_DeleteButton != null) m_DeleteButton.gameObject.SetActive(false);
            if (m_ObjectMenu != null) m_ObjectMenu.SetActive(false);
            if (m_ConfirmButton != null) m_ConfirmButton.gameObject.SetActive(false);

            var tanks = FindObjectsByType<TankStateController>(FindObjectsSortMode.None);
            foreach (var tank in tanks)
            {
                tank.LockPlacement();
            }

            Debug.Log("AR Placement Locked. Ray Interactors Disabled. Feeding Enabled.");
        }

        public void SetObjectToSpawn(int objectIndex)
        {
            if (m_ObjectSpawner == null) Debug.LogWarning("Object Spawner not configured.");
            else
            {
                if (m_ObjectSpawner.objectPrefabs.Count > objectIndex) m_ObjectSpawner.spawnOptionIndex = objectIndex;
                else Debug.LogWarning("Object Index out of bounds.");
            }
            HideMenu();
        }

        void ShowMenu()
        {
            if (m_IsPlacementLocked) return;
            m_ShowObjectMenu = true;
            m_ObjectMenu.SetActive(true);
            if (!m_ObjectMenuAnimator.GetBool("Show")) m_ObjectMenuAnimator.SetBool("Show", true);
            AdjustARDebugMenuPosition();
        }

        public void ShowHideModal()
        {
            if (m_ModalMenu.activeSelf) { m_ShowOptionsModal = false; m_ModalMenu.SetActive(false); }
            else { m_ShowOptionsModal = true; m_ModalMenu.SetActive(true); }
        }

        public void ShowHideDebugPlane()
        {
            m_VisualizePlanes = !m_VisualizePlanes;
            m_DebugPlaneSlider.value = m_VisualizePlanes ? 1 : 0;
            ChangePlaneVisibility(m_VisualizePlanes);
        }

        public void ShowHideDebugMenu()
        {
            m_ShowDebugMenu = !m_ShowDebugMenu;
            m_DebugMenuSlider.value = m_ShowDebugMenu ? 1 : 0;

            if (m_ShowDebugMenu)
            {
                m_ARDebugMenu.gameObject.SetActive(true);
                AdjustARDebugMenuPosition();
                if (m_ARDebugMenu.showPlanesButton.value != m_DebugMenuPlanesButtonValue)
                    m_ARDebugMenu.showPlanesButton.value = m_DebugMenuPlanesButtonValue;
            }
            else
            {
                m_DebugMenuPlanesButtonValue = m_ARDebugMenu.showPlanesButton.value;
                if (m_DebugMenuPlanesButtonValue == 1f) m_ARDebugMenu.showPlanesButton.value = 0f;
                m_ARDebugMenu.gameObject.SetActive(false);
            }
        }

        public void ClearAllObjects()
        {
            foreach (Transform child in m_ObjectSpawner.transform) Destroy(child.gameObject);
        }

        public void HideMenu()
        {
            m_ObjectMenuAnimator.SetBool("Show", false);
            m_ShowObjectMenu = false;
            AdjustARDebugMenuPosition();
        }

        void ChangePlaneVisibility(bool setVisible)
        {
            foreach (var plane in m_ARPlanes)
            {
                if (m_ARPlaneMeshVisualizers.TryGetValue(plane, out var visualizer)) visualizer.enabled = m_UseARPlaneFading ? true : setVisible;
                if (m_ARPlaneMeshVisualizerFaders.TryGetValue(plane, out var fader))
                {
                    if (m_UseARPlaneFading) fader.visualizeSurfaces = setVisible;
                    else fader.SetVisualsImmediate(1f);
                }
            }
        }

        void DeleteFocusedObject()
        {
            var currentFocusedObject = m_InteractionGroup.focusInteractable;
            if (currentFocusedObject != null)
            {
                Destroy(currentFocusedObject.transform.gameObject);
                if (m_ConfirmButton != null) m_ConfirmButton.gameObject.SetActive(false);
            }
        }

        void InitializeDebugMenuOffsets()
        {
            if (m_CreateButton.TryGetComponent<RectTransform>(out var buttonRect))
                m_ObjectButtonOffset = new Vector2(0f, buttonRect.anchoredPosition.y + buttonRect.rect.height + 10f);
            else
                m_ObjectButtonOffset = new Vector2(0f, 200f);

            if (m_ObjectMenu.TryGetComponent<RectTransform>(out var menuRect))
                m_ObjectMenuOffset = new Vector2(0f, menuRect.anchoredPosition.y + menuRect.rect.height + 10f);
            else
                m_ObjectMenuOffset = new Vector2(0f, 345f);
        }

        void AdjustARDebugMenuPosition()
        {
            if (m_ARDebugMenu == null) return;
            float screenWidthInInches = Screen.width / Screen.dpi;
            if (screenWidthInInches < 5)
            {
                Vector2 menuOffset = m_ShowObjectMenu ? m_ObjectMenuOffset : m_ObjectButtonOffset;
                if (m_ARDebugMenu.toolbar.TryGetComponent<RectTransform>(out var rect))
                {
                    rect.anchorMin = new Vector2(0.5f, 0); rect.anchorMax = new Vector2(0.5f, 0);
                    rect.eulerAngles = new Vector3(rect.eulerAngles.x, rect.eulerAngles.y, 90);
                    rect.anchoredPosition = new Vector2(0, 20) + menuOffset;
                }
            }
        }

        void OnPlaneChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
        {
            if (eventArgs.added.Count > 0)
            {
                foreach (var plane in eventArgs.added)
                {
                    m_ARPlanes.Add(plane);
                    if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var viz))
                    {
                        m_ARPlaneMeshVisualizers.Add(plane, viz);
                        if (!m_UseARPlaneFading) viz.enabled = m_VisualizePlanes;
                    }
                    if (!plane.TryGetComponent<ARPlaneMeshVisualizerFader>(out var vis))
                    {
                        vis = plane.gameObject.AddComponent<ARPlaneMeshVisualizerFader>();
                    }
                    m_ARPlaneMeshVisualizerFaders.Add(plane, vis);
                    vis.visualizeSurfaces = m_VisualizePlanes;
                }
            }
            if (eventArgs.removed.Count > 0)
            {
                foreach (var plane in eventArgs.removed)
                {
                    var planeGameObject = plane.Value;
                    if (planeGameObject == null) continue;
                    if (m_ARPlanes.Contains(planeGameObject)) m_ARPlanes.Remove(planeGameObject);
                    if (m_ARPlaneMeshVisualizers.ContainsKey(planeGameObject)) m_ARPlaneMeshVisualizers.Remove(planeGameObject);
                    if (m_ARPlaneMeshVisualizerFaders.ContainsKey(planeGameObject)) m_ARPlaneMeshVisualizerFaders.Remove(planeGameObject);
                }
            }
            if (m_PlaneManager.trackables.count != m_ARPlanes.Count)
            {
                m_ARPlanes.Clear(); m_ARPlaneMeshVisualizers.Clear(); m_ARPlaneMeshVisualizerFaders.Clear();
                foreach (var plane in m_PlaneManager.trackables)
                {
                    m_ARPlanes.Add(plane);
                    if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var viz))
                    {
                        m_ARPlaneMeshVisualizers.Add(plane, viz);
                        if (!m_UseARPlaneFading) viz.enabled = m_VisualizePlanes;
                    }
                    if (!plane.TryGetComponent<ARPlaneMeshVisualizerFader>(out var fader))
                    {
                        fader = plane.gameObject.AddComponent<ARPlaneMeshVisualizerFader>();
                    }
                    m_ARPlaneMeshVisualizerFaders.Add(plane, fader);
                    fader.visualizeSurfaces = m_VisualizePlanes;
                }
            }
        }
    }
}