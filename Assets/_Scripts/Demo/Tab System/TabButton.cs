using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyTabSystem
{
    [RequireComponent(typeof(Image))]
    public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // Events
        public UnityEvent onTabSelected;
        public UnityEvent onTabDeselected;

        // Memebers and properties
        private TabGroup _tabGroup;

        private Image _image;
        public Image Image { get { return _image; } }

        private Color _originalColor;
        public Color OriginalColor { get { return _originalColor; } }


        private void Awake()
        {
            _image = GetComponent<Image>();
            _originalColor = Image.color;

            _tabGroup = transform.parent.GetComponent<TabGroup>();
            _tabGroup.Subscribe(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _tabGroup.OnTabSelected(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _tabGroup.OnTabEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tabGroup.OnTabExit(this);
        }

        public void Select()
        {
            onTabSelected.Invoke();
        }

        public void Deselect()
        {
            onTabDeselected.Invoke();
        }
    }
}
