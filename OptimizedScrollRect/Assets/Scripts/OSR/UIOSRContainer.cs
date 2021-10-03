using System;
using System.Collections.Generic;
using UnityEngine;

public class UIOSRContainer<TData, TElement> : MonoBehaviour where TData : OSRData where TElement : UIOSRElement<TData> {
    #region Internal Fields
    private List<TData> _data = new List<TData>();
    private List<TElement> _elementList = new List<TElement>();

    private int _lineIndex;
    private int _elementPerLine;
    private TElement _elementGO;
    private Action<TData> _onClickAction;
    #endregion

    #region APIs
    public void SetData(List<TData> data) {
        _data = data;
    }
    
    public void SetLineIndex(int lineIndex) {
        _lineIndex = lineIndex;
    }

    public void SetElementPerLine(int elementPerLine) {
        _elementPerLine = elementPerLine;
    }
    
    public void SetElementGameobject(TElement element) {
        _elementGO = element;
    }

    public void SetElementOnClickAction(Action<TData> action) {
        _onClickAction = action;
    }

    public void ShowContent() {
        for (int i = 0; i < _elementPerLine; i++) {
            if (i >= _elementList.Count) {
                TElement newElement = Instantiate(_elementGO, this.transform);
                _elementList.Add(newElement);
            }

            TElement element = _elementList[i];
            int dataIndex = _lineIndex * _elementPerLine + i;
            if (dataIndex >= _data.Count) {
                element.Show(false);
                continue;
            }
            
            element.Show(true);
            element.SetData(_data[dataIndex]);
            element.SetOnClickAction(_onClickAction);
        }

        // Remove unused elements
        for (int i = _elementList.Count - 1; i >= _elementPerLine; i--) {
            Destroy(_elementList[i].gameObject);
            _elementList.RemoveAt(i);
        }
    }
    #endregion
}
