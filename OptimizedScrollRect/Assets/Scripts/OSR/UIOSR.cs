using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

public class OSRData {
    protected Object Obj;
    protected int Index;
}

public class UIOSR<TData, TContainer, TElement> : ScrollRect 
    where TData : OSRData 
    where TContainer : UIOSRContainer<TData, TElement>
    where TElement : UIOSRElement<TData> {

    // OSR = Optimized Scroll Rect
    // Default draggable direction of scroll rect in Unity
    //      Horizantol : Right
    //      Vertical : Down

    #region Serialized Fields
    [Header("New Feature")]
    [SerializeField] private TElement _elementGO = null;
    [SerializeField] private Vector2 _cellSize = Vector2.one; // NOTE: Value of cell size (x or y) must larger or equal to 1
    [SerializeField] private float _containerSpacing = 0; // NOTE: Can be negative
    [SerializeField] private float _elementSpacing = 0; // NOTE: Can be negative
    #endregion

    #region Internal Fields
    private int _elementCountPerLine = 0;
    private int _totalLineCount = 0;
    private Action<TData> _elementOnClickAction;
    private List<TData> _dataList = new List<TData>();
    private List<TContainer> _containerList = new List<TContainer>();
    #endregion

    #region Properties
    private Vector2 CellSize {
        get {
            if (_cellSize.x <= 0 || _cellSize.y <= 0) {
                _cellSize = new Vector2(Mathf.Max(_cellSize.x, 1), Mathf.Max(_cellSize.y, 1));
            }

            return _cellSize;
        }
    }

    private int ContainerCount {
        get {
            return _containerList.Count;
        }
    }

    private float ViewportLengthUndraggableDir {
        get {
            return vertical ? viewport.rect.width : viewport.rect.height;
        }
    }

    private float ViewportLengthDraggableDir {
        get {
            return vertical ? viewport.rect.height : viewport.rect.width;
        }
    }

    private float CellSizeUndraggableDir {
        get {
            return vertical ? CellSize.x : CellSize.y;
        }
    }

    private float CellSizeDraggableDir {
        get {
            return vertical ? CellSize.y : CellSize.x;
        }
    }

    public List<TData> DataList {
        get {
            return _dataList;
        }
    }

    public int ElementCountPerLine {
        get {
            return _elementCountPerLine;
        }
    }

    public int TotleLineCount {
        get {
            return _totalLineCount;
        }
    }
    #endregion

    #region Mono behaviour Hooks
    protected override void Awake() {
        base.Awake();

        onValueChanged.AddListener(OnValueChanged);
    }

    // TODO
    // Refresh if bound of recttranform changed ?
    //private void Update() {
    //}

    protected override void OnDestroy() {
        base.OnDestroy();

        onValueChanged.RemoveAllListeners();
    }
    #endregion

    #region Scroll Rect Value Handlings
    private void OnValueChanged(Vector2 v) {
        ShowAndHideContainer();
    }
    #endregion

    #region APIs
    public void SetElementOnClickAction(Action<TData> action) {
        _elementOnClickAction = action;
    }

    public void AssignData(List<TData> dataList) {
        _dataList = dataList;
    }

    public void AddData(TData data) {
        _dataList.Add(data);
    }

    public void RemoveData(TData data) {
        _dataList.Remove(data);
    }

    // TODO
    //public void MoveToData(TData data) { 

    //}

    //public void MoveToIndex() { 

    //}

    public void Refresh() {
        int dataCount = _dataList.Count;

        // Count per line
        _elementCountPerLine = 1;
        float remainedLength = ViewportLengthUndraggableDir - CellSizeUndraggableDir;
        if (remainedLength > 0) {
            _elementCountPerLine += (int) (remainedLength / (_elementSpacing + CellSizeUndraggableDir));
        }

        // Total count of line
        _totalLineCount = dataCount / _elementCountPerLine;
        if (dataCount % _elementCountPerLine != 0) {
            _totalLineCount += 1;
        }

        float contentLengthDraggableDir = CellSizeDraggableDir + (_totalLineCount - 1) * (_containerSpacing + CellSizeDraggableDir);
        content.anchorMax = new Vector2(0, 1);
        content.anchorMin = new Vector2(0, 1);
        content.sizeDelta = vertical ? 
            new Vector2(ViewportLengthUndraggableDir, contentLengthDraggableDir) :
            new Vector2(contentLengthDraggableDir, ViewportLengthUndraggableDir);

        // TODO: Keep current normalized position ??
        //content.anchoredPosition = Vector2.zero; 

        RefreshContainer();
        ShowAndHideContainer();
    }
    #endregion

    #region Internal Methods
    private void RefreshContainer() {
        int maxContainerCount = 1 + 1;
        float remainLength = ViewportLengthDraggableDir - CellSizeDraggableDir;
        if (remainLength > 0) {
            maxContainerCount += (int) (remainLength / (_containerSpacing + CellSizeDraggableDir));
            if (remainLength % (_containerSpacing + CellSizeDraggableDir) != 0) {
                maxContainerCount += 1;
            }
        }

        // Reuse instead re-create all
        for (int i = 0; i < maxContainerCount; i++) {
            // Create new if not enough
            if (i >= _containerList.Count) {
                GameObject newContainerGo = new GameObject();
                newContainerGo.transform.SetParent(content);
                newContainerGo.AddComponent<RectTransform>();
                newContainerGo.AddComponent<Image>();
                if (vertical) {
                    newContainerGo.AddComponent<HorizontalLayoutGroup>();
                }
                else {
                    newContainerGo.AddComponent<VerticalLayoutGroup>();
                }

                TContainer newContainer = newContainerGo.AddComponent<TContainer>();
                _containerList.Add(newContainer);
            }

            // Container settings
            UIOSRContainer<TData, TElement> container = _containerList[i];
            container.name = string.Format("Container_{0}", i);
            container.SetData(_dataList);

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMax = new Vector2(0, 1);
            rect.anchorMin = new Vector2(0, 1);
            rect.pivot = Vector2.up;
            rect.anchoredPosition = GetContainerPosition(i);
            rect.sizeDelta = vertical ?
                new Vector2(ViewportLengthUndraggableDir, CellSizeDraggableDir) :
                new Vector2(CellSizeDraggableDir, ViewportLengthUndraggableDir);
        }

        // Remove unused 
        for (int i = maxContainerCount; i < _containerList.Count; i++) {
            Destroy(_containerList[i]);
        }
    }

    private void ShowAndHideContainer() {
        float topDistance = vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
        float bottomDistance = vertical ?
            content.anchoredPosition.y + viewport.rect.height :
            content.anchoredPosition.x - viewport.rect.width;

        int topLineIndex = GetLineIndex(topDistance);
        int bottomLineIndex = GetLineIndex(bottomDistance);

        ShowContainer(topLineIndex, bottomLineIndex);
    }

    private int GetLineIndex(float distance) {
        // Distance is length from pivot point with draggable direction
        float cellLength = vertical ? CellSize.y : CellSize.x;

        int index = 0;
        if (vertical) {
            if (distance < -_containerSpacing) {
                index = Mathf.CeilToInt(distance / (_containerSpacing + cellLength));
            }
            else {
                if (distance > _containerSpacing + cellLength) {
                    index = Mathf.FloorToInt(distance / (_containerSpacing + cellLength));
                }
            }
        }
        else {
            if (distance > _containerSpacing) {
                index = -Mathf.CeilToInt(distance / (_containerSpacing + cellLength));
            }
            else {
                if (distance < -(_containerSpacing + cellLength)) {
                    index = -Mathf.CeilToInt(distance / (_containerSpacing + cellLength));
                }
            }
        }

        return index;
    }

    private Vector2 GetContainerPosition(int lineIndex) {
        return vertical ?
            new Vector2(0, -lineIndex * (_containerSpacing + CellSizeDraggableDir)) :
            new Vector2(lineIndex * (_containerSpacing + CellSizeDraggableDir), 0);
    }

    private void ShowContainer(int topLineIndex, int bottomLineIndex) {
        List<int> unhandledContainerIndex = new List<int>();
        for (int i = 0; i < _containerList.Count; i++) {
            unhandledContainerIndex.Add(i);
        }

        for (int lineIndex = topLineIndex; lineIndex <= bottomLineIndex; lineIndex++) {
            if (lineIndex < 0 || lineIndex >= _totalLineCount) {
                continue;
            }

            int containerIndex = lineIndex % ContainerCount;

            _containerList[containerIndex].gameObject.SetActive(true);
            _containerList[containerIndex].SetLineIndex(lineIndex);
            _containerList[containerIndex].SetElementPerLine(ElementCountPerLine);
            _containerList[containerIndex].SetElementGameobject(_elementGO);
            _containerList[containerIndex].SetElementOnClickAction(_elementOnClickAction);
            _containerList[containerIndex].ShowContent();

            RectTransform rect = _containerList[containerIndex].transform as RectTransform;
            rect.anchoredPosition = GetContainerPosition(lineIndex);
            unhandledContainerIndex.Remove(containerIndex);
        }

        for (int i = 0; i < unhandledContainerIndex.Count; i++) {
            _containerList[unhandledContainerIndex[i]].gameObject.SetActive(false);
        }
    }
    #endregion
}
