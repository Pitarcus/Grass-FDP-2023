using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyTabSystem
{
    public class TabGroup : MonoBehaviour
    {
        // References
        [SerializeField] private List<TabButton> _tabButtons;
        [SerializeField] private List<GameObject> _objectsToSwap;

        [SerializeField] private Color idle;
        [SerializeField] private Color hover;
        [SerializeField] private Color active;

        // Parameters
        [SerializeField] int _defaultTab = 0;

        // Members
        private TabButton _selectedTab;

        private void Start()
        {
            OnTabSelected(_tabButtons[_defaultTab]);
        }

        public void Subscribe(TabButton button)
        {
            if (_tabButtons == null)
            {
                _tabButtons = new List<TabButton>();
            }

            if(_tabButtons.Contains(button)) { return; }

            _tabButtons.Add(button);
            ResetTabs();
        }

        public void OnTabEnter(TabButton button)
        {
            ResetTabs();
            if (_selectedTab == null || _selectedTab != button)
            {
                button.Image.color = button.OriginalColor * hover;
            }
        }
        public void OnTabExit(TabButton button)
        {
            ResetTabs();
        }
        public void OnTabSelected(TabButton button)
        {
            if(_selectedTab == button) { return; }
            if(_selectedTab != null)
            {
                _selectedTab.Deselect();
            }
            _selectedTab = button;
            _selectedTab.Select();
            ResetTabs();

            button.Image.color = button.OriginalColor * active;

            int index = button.transform.GetSiblingIndex();
            for(int i = 0; i < _objectsToSwap.Count; i++)
            {
                if (i == index)
                {
                    _objectsToSwap[i].SetActive(true);
                }
                else
                {
                    _objectsToSwap[i].SetActive(false);
                }
            }
        }

        public void ResetTabs()
        {
            foreach(TabButton button in _tabButtons)
            {
                if(_selectedTab != null && _selectedTab == button)   { continue; }

                button.Image.color = button.OriginalColor * idle; 
            }
        }
    }
}
