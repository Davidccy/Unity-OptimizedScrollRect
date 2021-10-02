using System;
using UnityEngine;
using UnityEngine.UI;

public class UIOSRElement<TData> : MonoBehaviour where TData : OSRData {
    #region Serialized Fields
    [SerializeField] private GameObject _goContent = null;
    [SerializeField] private Button _btn = null;
    #endregion

    #region Internal Fields
    protected TData _data;
    private Action<TData> _onClickAction = null;
    #endregion

    #region Mono Behaviour Hooks
    private void Awake() {
        _btn.onClick.AddListener(ButtonOnClick);
    }

    private void OnDestroy() {
        _btn.onClick.RemoveAllListeners();
    }
    #endregion

    #region UI Button Handlings
    private void ButtonOnClick() {
        if (_onClickAction != null) {
            _onClickAction(_data);
        }
    }
    #endregion

    #region APIs
    public void SetData(TData data) {
        _data = data;
        Refresh();
    }

    public void SetOnClickAction(Action<TData> action) {
        _onClickAction = action;
    }

    public void Show(bool show) {
        _goContent.SetActive(show);
    }
    #endregion

    #region Virtual Methods
    protected virtual void Refresh() { 
        // NOTE:
        // Content display
    }
    #endregion
}
