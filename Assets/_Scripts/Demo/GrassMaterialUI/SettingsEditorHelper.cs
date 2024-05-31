using UnityEditor;
using UnityEngine;



[ExecuteInEditMode]
public class SettingsEditorHelper : MonoBehaviour
{
    public GameObject settingsWindow;

    public GameObject grassTab;
    public GameObject windTab;
    public GameObject timeTab;
    
    public void OpenSettings()
    {
        gameObject.SetActive(true);
        settingsWindow.transform.localScale = Vector3.one;
    }

    public void OpenTab(GameObject tab)
    {
        OpenSettings();

        CloseTabs();

        tab.SetActive(true);
        tab.transform.localScale = Vector3.one;
    }

    private void CloseTab(GameObject tab)
    {
        tab.transform.localScale = new Vector3 (0, 1, 1);
        tab.SetActive(false);
    }

    private void CloseTabs()
    {
        CloseTab(grassTab);
        CloseTab(windTab);
        CloseTab(timeTab);
    }

   public void CloseAll()
    {
        CloseTabs();

        settingsWindow.transform.localScale = new Vector3(1, 0, 1);
        gameObject.SetActive(false);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SettingsEditorHelper))]
public class SettigEditorHelperEditor: Editor
{
    private SettingsEditorHelper settingsHelper;

    private void OnEnable()
    {
         settingsHelper = (SettingsEditorHelper) target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Close Settings"))
        {
            settingsHelper.CloseAll();
        }

        if (GUILayout.Button("Open Grass"))
        {

            settingsHelper.OpenTab(settingsHelper.grassTab);
        }
        if (GUILayout.Button("Open Wind"))
        {

            settingsHelper.OpenTab(settingsHelper.windTab);
        }
        if (GUILayout.Button("Open Time"))
        {

            settingsHelper.OpenTab(settingsHelper.timeTab);
        }
    }
}
#endif
