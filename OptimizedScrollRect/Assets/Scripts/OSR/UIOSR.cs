using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Size Options")]
    [SerializeField] private Vector2 _elementSize = Vector2.one; // NOTE: Value of element size (x or y) must larger or equal to 1
    [SerializeField] private float _containerSpacing = 0; // NOTE: Can be negative
    [SerializeField] private float _elementSpacing = 0; // NOTE: Can be negative
    #endregion

    #region Internal Fields
    private Action<TData> _elementOnClickAction;
    private List<TData> _dataList = new List<TData>();
    private List<TContainer> _containerList = new List<TContainer>();

    // Size cache
    private Vector2 _previousElementSize = Vector2.zero;
    private float _previousContainerSpacing = 0;
    private float _previousElementSpacing = 0;
    private Vector2 _previousViewportSize = Vector2.zero;
    #endregion

    #region Properties
    private Vector2 ElementSize {
        get {
            if (_elementSize.x <= 0 || _elementSize.y <= 0) {
                _elementSize = new Vector2(Mathf.Max(_elementSize.x, 1), Mathf.Max(_elementSize.y, 1));
            }

            return _elementSize;
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

    private float ElementSizeUndraggableDir {
        get {
            return vertical ? ElementSize.x : ElementSize.y;
        }
    }

    private float ElementSizeDraggableDir {
        get {
            return vertical ? ElementSize.y : ElementSize.x;
        }
    }

    public List<TData> DataList {
        get {
            return _dataList;
        }
    }

    public int ElementCountPerLine {
        get;
        private set;
    }

    public int TotalLineCount {
        get;
        private set;
    }

    public bool IsDragging {
        get;
        private set;
    }
    #endregion

    #region Mono behaviour Hooks
    protected override void Awake() {
        base.Awake();

        onValueChanged.AddListener(OnValueChanged);
    }

    private void Update() {
        if (!Application.isPlaying) {
            return;
        }

        if (IsSizeOptionChanged()) {
            Refresh();
        }

        UpdateSizeCache();        
    }

    protected override void OnDisable() {
        base.OnDisable();

        StopAllCoroutines();
    }

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

    #region Override IDragHandler
    public override void OnBeginDrag(PointerEventData eventData) {
        base.OnBeginDrag(eventData);

        IsDragging = true;
        StopAllCoroutines();
    }

    public override void OnEndDrag(PointerEventData eventData) {
        base.OnEndDrag(eventData);

        IsDragging = false;
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

    public void MoveToIndex(int dataIndex, bool skipTween = false) {
        // Check target data exist or not
        if (_dataList == null) {
            return;
        }

        if (dataIndex < 0) {
            return;
        }

        if (dataIndex >= _dataList.Count) {
            return;
        }

        // Try to move target to center
        // Calculate lineindex of target

        int lineIndex = dataIndex / ElementCountPerLine;

        MoveToLine(lineIndex, skipTween);
    }

    public void Refresh() {
        int dataCount = _dataList.Count;

        // Calculate element count per line
        ElementCountPerLine = 1;
        float remainedLength = ViewportLengthUndraggableDir - ElementSizeUndraggableDir;
        if (remainedLength > 0) {
            ElementCountPerLine += (int) (remainedLength / (_elementSpacing + ElementSizeUndraggableDir));
        }

        // Calculate total count of line
        TotalLineCount = dataCount / ElementCountPerLine;
        if (dataCount % ElementCountPerLine != 0) {
            TotalLineCount += 1;
        }

        // Update recttransform of content
        float contentLengthDraggableDir = ElementSizeDraggableDir + (TotalLineCount - 1) * (_containerSpacing + ElementSizeDraggableDir);
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
    private bool IsSizeOptionChanged() {
        if (_previousElementSize != ElementSize) {
            return true;
        }

        if (_previousContainerSpacing != _containerSpacing) {
            return true;
        }

        if (_previousElementSpacing != _elementSpacing) {
            return true;
        }

        if (IsViewportSizeChanged()) {
            return true;
        }

        return false;
    }

    private void UpdateSizeCache() {
        _previousElementSize = ElementSize;
        _previousContainerSpacing = _containerSpacing;
        _previousElementSpacing = _elementSpacing;

        if (IsViewportSizeChanged()) {
            _previousViewportSize = new Vector2(viewport.rect.width, viewport.rect.height);
        }
    }

    private bool IsViewportSizeChanged() {
        return _previousViewportSize.x != viewport.rect.width || _previousViewportSize.x != viewport.rect.height;
    }

    private void RefreshContainer() {
        int maxContainerCount = 1 + 1;
        float remainLength = ViewportLengthDraggableDir - ElementSizeDraggableDir;
        if (remainLength > 0) {
            maxContainerCount += (int) (remainLength / (_containerSpacing + ElementSizeDraggableDir));
            if (remainLength % (_containerSpacing + ElementSizeDraggableDir) != 0) {
                maxContainerCount += 1;
            }
        }

        // Reuse instead re-create all
        for (int containerIndex = 0; containerIndex < maxContainerCount; containerIndex++) {
            // Create new if not enough
            if (containerIndex >= _containerList.Count) {
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
            UIOSRContainer<TData, TElement> container = _containerList[containerIndex];
            container.name = string.Format("Container_{0}", containerIndex);
            container.SetData(_dataList);

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMax = new Vector2(0, 1);
            rect.anchorMin = new Vector2(0, 1);
            rect.pivot = Vector2.up;
            rect.anchoredPosition = GetContainerPosition(containerIndex);
            rect.sizeDelta = vertical ?
                new Vector2(ViewportLengthUndraggableDir, ElementSizeDraggableDir) :
                new Vector2(ElementSizeDraggableDir, ViewportLengthUndraggableDir);

            if (vertical) {
                HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = _elementSpacing;
            }
            else {
                VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = _elementSpacing;
            }
        }

        // Remove unused 
        for (int containerIndex = _containerList.Count - 1; containerIndex >= maxContainerCount; containerIndex--) {
            Destroy(_containerList[containerIndex].gameObject);
            _containerList.RemoveAt(containerIndex);
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
        float elementLength = vertical ? ElementSize.y : ElementSize.x;

        int index = 0;
        if (vertical) {
            if (distance < -_containerSpacing) {
                index = Mathf.CeilToInt(distance / (_containerSpacing + elementLength));
            }
            else {
                if (distance > _containerSpacing + elementLength) {
                    index = Mathf.FloorToInt(distance / (_containerSpacing + elementLength));
                }
            }
        }
        else {
            if (distance > _containerSpacing) {
                index = -Mathf.CeilToInt(distance / (_containerSpacing + elementLength));
            }
            else {
                if (distance < -(_containerSpacing + elementLength)) {
                    index = -Mathf.CeilToInt(distance / (_containerSpacing + elementLength));
                }
            }
        }

        return index;
    }

    private Vector2 GetContainerPosition(int lineIndex) {
        return vertical ?
            new Vector2(0, -lineIndex * (_containerSpacing + ElementSizeDraggableDir)) :
            new Vector2(lineIndex * (_containerSpacing + ElementSizeDraggableDir), 0);
    }

    private void ShowContainer(int topLineIndex, int bottomLineIndex) {
        List<int> unhandledContainerIndex = new List<int>();
        for (int i = 0; i < _containerList.Count; i++) {
            unhandledContainerIndex.Add(i);
        }

        for (int lineIndex = topLineIndex; lineIndex <= bottomLineIndex; lineIndex++) {
            if (lineIndex < 0 || lineIndex >= TotalLineCount) {
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

    private void MoveToLine(int lineIndex, bool skipTween) {
        // NOTE:
        // Modify normalized position of scroll rect
        // If vertical, value of 'verticalNormalizedPosition' is 0 means scroll view at Bottom
        //              value of 'verticalNormalizedPosition' is 1 means scroll view at Top
        // If horizontal, value of 'horizontalNormalizedPosition' 0 means scroll view at Most Left
        //                value of 'horizontalNormalizedPosition' 1 means scroll view at Most Right

        float totalLength = vertical ? content.rect.height : content.rect.width;
        float viewableLength = vertical ? viewport.rect.height : viewport.rect.width;

        float draggableStart = vertical ? (-totalLength + viewableLength / 2) : viewableLength / 2; // Position which normalized position is 0
        float draggableEnd = vertical ? -viewableLength / 2 : (totalLength - viewableLength / 2); // Position which normalized position is 1

        // Center position
        Vector2 containerPosition = GetContainerPosition(lineIndex);
        float targetPosition = vertical ?
            containerPosition.y - ElementSize.y / 2 :
            containerPosition.x + ElementSize.x / 2;

        // Normalized position calculation
        float nPosition = 0;
        if (targetPosition < draggableStart) {
            nPosition = 0;
        }
        else if (targetPosition > draggableEnd) {
            nPosition = 1;
        }
        else {
            nPosition = Mathf.InverseLerp(draggableStart, draggableEnd, targetPosition);
        }

        StartCoroutine(MoveToPosition(nPosition, skipTween));
    }

    private IEnumerator MoveToPosition(float targetNPosition, bool skipTween) {
        float currentNPosition = GetNormalizedPosition();

        if (!skipTween) {
            while (Mathf.Abs(currentNPosition - targetNPosition) > 0.0005f) {
                currentNPosition = Mathf.Lerp(currentNPosition, targetNPosition, 0.05f);
                SetNormalizedPosition(currentNPosition);

                yield return new WaitForEndOfFrame();
            }
        }

        SetNormalizedPosition(targetNPosition);
    }

    private float GetNormalizedPosition() {
        return vertical ? verticalNormalizedPosition : horizontalNormalizedPosition;
    }

    private void SetNormalizedPosition(float nPosition) {
        if (vertical) {
            verticalNormalizedPosition = nPosition;
        }
        else {
            horizontalNormalizedPosition = nPosition;
        }
    }
    #endregion
}
