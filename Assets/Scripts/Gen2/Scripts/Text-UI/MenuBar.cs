using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuBar : MonoBehaviour
{

    public List<Button> buttons;

    public List<GameObject> pageRoots;


    [SerializeField] private Color buttonPressedColor;
    [SerializeField] private Color buttonDisabledColor;
    [SerializeField] private Color buttonTextPressedColor;
    [SerializeField] private Color buttonTextDisabledColor;

    public void OnButtonPressed(Button _button)
    {
        for(int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];

            if(button == _button)
            {
                pageRoots[i].gameObject.SetActive(true);

                ColorBlock cb = _button.colors;
                cb.normalColor = buttonPressedColor;
                cb.selectedColor = buttonPressedColor;
                cb.disabledColor = buttonPressedColor;
                cb.pressedColor = buttonPressedColor;
                cb.highlightedColor = buttonPressedColor;
                button.colors = cb;

                button.GetComponentInChildren<TextMeshProUGUI>().color = buttonTextPressedColor;
            }
            else
            {
                pageRoots[i].gameObject.SetActive(false);

                ColorBlock cb = _button.colors;
                cb.normalColor = buttonDisabledColor;
                cb.selectedColor = buttonDisabledColor;
                cb.disabledColor = buttonDisabledColor;
                cb.pressedColor = buttonDisabledColor;
                cb.highlightedColor = buttonDisabledColor;
                button.colors = cb;

                button.GetComponentInChildren<TextMeshProUGUI>().color = buttonTextDisabledColor;
            }
        }
    }


}
