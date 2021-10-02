using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITest : MonoBehaviour {
    #region Serialized Fields
    [SerializeField] private Button _btn = null;
    [SerializeField] private Button _btnPlus = null;
    [SerializeField] private Button _btnMinus = null;

    [SerializeField] private TMP_InputField _input = null;

    [SerializeField] private UITemplateDataScrollRect _horScrollRect = null;
    [SerializeField] private UITemplateDataScrollRect _verScrollRect = null;

    [SerializeField] private TextMeshProUGUI _textContentHor = null;
    [SerializeField] private TextMeshProUGUI _textContentVer = null;
    #endregion

    #region Internal Fields
    private List<TemplateData> _dataList = new List<TemplateData>();
    #endregion

    #region Mono Behaviour Hooks
    private void Awake() {
        _btn.onClick.AddListener(ButtonOnClick);
        _btnPlus.onClick.AddListener(ButtonPlusOnClick);
        _btnMinus.onClick.AddListener(ButtonMinusOnClick);
    }

    private void OnEnable() {
        _textContentHor.text = string.Empty;
        _textContentVer.text = string.Empty;
    }

    private void OnDestroy() {
        _btn.onClick.RemoveAllListeners();
        _btnPlus.onClick.RemoveAllListeners();
        _btnMinus.onClick.RemoveAllListeners();
    }
    #endregion

    #region Button Handlings
    private void ButtonOnClick() {
        string inputStr = _input.text;
        if (string.IsNullOrEmpty(inputStr) || !int.TryParse(inputStr, out int dataCount)) {
            return;
        }

        _dataList = new List<TemplateData>();
        for (int i = 0; i < dataCount; i++) {
            TemplateData nd = new TemplateData() { TestInput = i };

            _dataList.Add(nd);
        }

        _horScrollRect.AssignData(_dataList);
        _horScrollRect.SetElementOnClickAction(ElementHorOnClickAction);
        _horScrollRect.Refresh();

        _verScrollRect.AssignData(_dataList);
        _verScrollRect.SetElementOnClickAction(ElementVerOnClickAction);
        _verScrollRect.Refresh();
    }

    private void ButtonPlusOnClick() {
        _dataList.Add(new TemplateData() { TestInput = _dataList.Count });

        _horScrollRect.AssignData(_dataList);
        _horScrollRect.Refresh();

        _verScrollRect.AssignData(_dataList);
        _verScrollRect.Refresh();
    }

    private void ButtonMinusOnClick() {
        if (_dataList.Count == 0) {
            return;
        }

        _dataList.RemoveAt(_dataList.Count - 1);

        _horScrollRect.AssignData(_dataList);
        _horScrollRect.Refresh();

        _verScrollRect.AssignData(_dataList);
        _verScrollRect.Refresh();
    }
    #endregion

    #region Internal Methods
    private void ElementHorOnClickAction(TemplateData data) {
        _textContentHor.text = string.Format("Element {0} on clicked", data.TestInput);
    }

    private void ElementVerOnClickAction(TemplateData data) {
        _textContentVer.text = string.Format("Element {0} on clicked", data.TestInput);
    }
    #endregion
}
