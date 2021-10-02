using TMPro;
using UnityEngine;

public class UITemplateDataScrollRectElement : UIOSRElement<TemplateData> {
    #region Serialized Fields
    [SerializeField] private TextMeshProUGUI _text = null;
    #endregion

    #region Override Methods
    protected override void Refresh() {
        _text.text = string.Format("{0}", _data.TestInput);
    }
    #endregion
}
